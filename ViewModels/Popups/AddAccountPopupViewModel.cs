using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using CommunityToolkit.Maui.Views;
using KakeiboApp.Repository;
using Syncfusion.Maui.DataForm;
using CommunityToolkit.Maui.Alerts;
using static SQLite.SQLite3;

namespace KakeiboApp.ViewModels.Popups;

public partial class AddAccountPopupViewModel : BaseViewModel
{
    readonly IMonthlyIncomeDataRepository _incomeDataRepository;
    readonly IMonthlyFixedCostDataRepository _fixedCostDataRepository;
    readonly IMonthlySavingDataRepository _savingDataRepository;

    public AddAccountPopupViewModel(IMonthlyIncomeDataRepository incomeDataRepository, IMonthlyFixedCostDataRepository fixedCostDataRepository, IMonthlySavingDataRepository savingDataRepository, object inputObject, string formTitle)
    {
        _incomeDataRepository = incomeDataRepository;
        _fixedCostDataRepository = fixedCostDataRepository;
        _savingDataRepository = savingDataRepository;
        FormDataObject = inputObject;
        FormTitle = formTitle;
    }

    public SfDataForm DataForm = default!;

    [ObservableProperty]
    object _formDataObject;

    [ObservableProperty]
    string _formTitle = string.Empty;

    private void InitializeFormData()
    {
        var monthlyObject = DataForm.DataObject;

        monthlyObject?.GetType().GetProperty("Name")?.SetValue(monthlyObject, string.Empty);
        DataForm.UpdateEditor("Name");
        decimal defaultAmount = 0;
        monthlyObject?.GetType().GetProperty("Amount")?.SetValue(monthlyObject, defaultAmount);
        DataForm.UpdateEditor("Amount");
        monthlyObject?.GetType().GetProperty("Note")?.SetValue(monthlyObject, string.Empty);
        DataForm.UpdateEditor("Note");
    }

    [RelayCommand]
    async Task RegisterAsync(object dataForm)
    {
        if (IsBusy)
            return;
        try
        {
            var dataFormLayout = dataForm as SfDataForm;
            dataFormLayout?.Commit();
            var isValid = dataFormLayout?.Validate() ?? false;
            if (!isValid) return;

            (int result, DateTime date, string name, decimal amount, string note) = await TrySavingInstance();

            if (result != 0)
            {
                var snackBar = Snackbar.Make($"登録されました。\n\n年月: {date:yyyy/MM}\n内容: {name}\n金額: {amount:C}\nメモ: {note}");
                snackBar?.Show();
                InitializeFormData();
            }
            else
            {
                throw new Exception();
            }

            InitializeFormData();
        }
        catch (InvalidDataException)
        {
            await Shell.Current.DisplayAlert("エラー", $"無効な日にちです。\n日にちを選択し、もう一度試してください。", "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("エラー", "登録に失敗しました。もう一度試してください。", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<(int result, DateTime date, string name, decimal amount, string note)> TrySavingInstance()
    {
        if (FormDataObject is MonthlyIncome income)
        {
            if (income.Date.Year < 2020) throw new InvalidDataException();
            var result = await _incomeDataRepository.AddAsync(income);
            if (result != 0)
            {
                return (result, income.Date, income.Name, income.Amount, income.Note);
            }
        }
        else if (FormDataObject is MonthlyFixedCost fixedCost)
        {
            if (fixedCost.Date.Year < 2020) throw new InvalidDataException();
            var result = await _fixedCostDataRepository.AddAsync(fixedCost);
            if (result != 0)
            {
                return (result, fixedCost.Date, fixedCost.Name, fixedCost.Amount, fixedCost.Note);
            }
        }
        else if (FormDataObject is MonthlySaving saving)
        {
            if (saving.Date.Year < 2020) throw new InvalidDataException();
            var result = await _savingDataRepository.AddAsync(saving);
            if (result != 0)
            {
                return (result, saving.Date, saving.Name, saving.Amount, saving.Note);
            }
        }

        return (0, DateTime.Today, string.Empty, 0, string.Empty);
    }
}
