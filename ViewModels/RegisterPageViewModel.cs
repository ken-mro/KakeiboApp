using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using Syncfusion.Maui.DataForm;
using System.Collections.ObjectModel;

namespace KakeiboApp.ViewModels;

public partial class RegisterPageViewModel : BaseViewModel
{
    readonly ICategoryRepository _categoryRepository;
    readonly ISpendingItemRepository _spendingItemRepository;
    public RegisterPageViewModel(ICategoryRepository categoryRepository, ISpendingItemRepository spendingItemRepository)
    {
        _categoryRepository = categoryRepository;
        _spendingItemRepository = spendingItemRepository;
        _ = InitializeAsync();
    }

    [ObservableProperty]
    SpendingItem _formDataObject = new();

    public Button RegisterButton = default!;

    public SfDataForm DataForm = default!;

    public async Task InitializeAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        Categories = new ObservableCollection<Category>(categories);
        InitializeFormData();
    }

    [ObservableProperty]
    ObservableCollection<Category> _categories = default!;

    private void InitializeFormData()
    {
        var spendingItem = DataForm.DataObject as SpendingItem;
        spendingItem?.GetType().GetProperty(nameof(SpendingItem.Date))?.SetValue(spendingItem, DateTime.Today);
        DataForm.UpdateEditor(nameof(SpendingItem.Date));
        spendingItem?.GetType().GetProperty(nameof(SpendingItem.Category))?.SetValue(spendingItem, Categories.First());
        DataForm.UpdateEditor(nameof(SpendingItem.Category));
        spendingItem?.GetType().GetProperty(nameof(SpendingItem.Name))?.SetValue(spendingItem, string.Empty);
        DataForm.UpdateEditor(nameof(SpendingItem.Name));
        decimal defaultAmount = 0;
        spendingItem?.GetType().GetProperty(nameof(SpendingItem.Amount))?.SetValue(spendingItem, defaultAmount);
        DataForm.UpdateEditor(nameof(SpendingItem.Amount));
        spendingItem?.GetType().GetProperty(nameof(SpendingItem.Note))?.SetValue(spendingItem, string.Empty);
        DataForm.UpdateEditor(nameof(SpendingItem.Note));
    }

    [RelayCommand]
    public async Task RegisterItemAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            var isValid = DataForm.Validate();
            if (!isValid) return;
            if (FormDataObject.Date.Year < 2020) throw new InvalidDataException();
            var result = await _spendingItemRepository.AddAsync(FormDataObject);

            if (result != 0)
            {
                var snackBar = Snackbar.Make($"登録されました。\n\n日にち: {FormDataObject.Date:yyyy/MM/dd}\n種類: {FormDataObject.Category}\n内容: {FormDataObject.Name}\n金額: {FormDataObject.Amount:C}\nメモ: {FormDataObject.Note}", anchor: RegisterButton);
                snackBar?.Show();
                InitializeFormData();
            }
            else
            {
                throw new Exception();
            }            
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