using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using Syncfusion.Maui.DataForm;

namespace KakeiboApp.ViewModels.Popups;

public partial class AddSpecialExpensePopupViewModel : BaseViewModel
{
    readonly ISpecialExpenseDataRepository _specialExpenseDataRepository;
    public AddSpecialExpensePopupViewModel(ISpecialExpenseDataRepository specialExpenseDataRepository, SpecialExpense inputObject)
    {
        _specialExpenseDataRepository = specialExpenseDataRepository;
        FormDataObject = inputObject;
        _defaultDate = inputObject.Date;
    }

    private DateTime _defaultDate = DateTime.Now;    

    public SfDataForm DataForm = default!;

    [ObservableProperty]
    SpecialExpense _formDataObject;

    private void InitializeFormData()
    {
        var monthlyObject = DataForm.DataObject;

        monthlyObject?.GetType().GetProperty("Date")?.SetValue(monthlyObject, _defaultDate);
        DataForm.UpdateEditor("Date");
        monthlyObject?.GetType().GetProperty("Name")?.SetValue(monthlyObject, string.Empty);
        DataForm.UpdateEditor("Name");
        decimal defaultAmount = 0;
        monthlyObject?.GetType().GetProperty("Amount")?.SetValue(monthlyObject, defaultAmount);
        DataForm.UpdateEditor("Amount");
        monthlyObject?.GetType().GetProperty("FromWhere")?.SetValue(monthlyObject, string.Empty);
        DataForm.UpdateEditor("FromWhere");
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
            if (FormDataObject.Date.Year < 2020) throw new InvalidDataException();

            int result = 0;
            if (FormDataObject is SpecialExpense specialExpense)
            {
                result = await _specialExpenseDataRepository.AddAsync(specialExpense);
            }

            if (result != 0)
            {
                var snackBar = Snackbar.Make($"登録されました。\n\n日にち: {FormDataObject.Date:yyyy/MM/dd}\n内容: {FormDataObject.Name}\n金額: {FormDataObject.Amount:C}\nどこから:{FormDataObject.FromWhere}\nメモ: {FormDataObject.Note}");
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
}
