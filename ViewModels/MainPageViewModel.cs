using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Popups;
using KakeiboApp.Views.Popups;
using Plugin.Maui.Biometric;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Picker;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KakeiboApp.ViewModels;

public partial class MainPageViewModel : BaseViewModel
{
    readonly IBiometric _biometric;
       readonly SettingPreferences _settingPreferences;
    readonly AppShell _appShell;
    readonly IMonthlyIncomeDataRepository _monthlyIncomeDataRepository;
    readonly IMonthlySavingDataRepository _monthlySavingDataRepository;
    readonly IMonthlyFixedCostDataRepository _monthlyFixedCostDataRepository;
    readonly ISpendingItemRepository _spendingItemRepository;
    readonly IMonthlyBudgetDataRepository _monthlyBudgetDataRepository;
    readonly ISpecialExpenseDataRepository _specialExpenseDataRepository;
    readonly ICategoryRepository _categoryRepository;

    public MainPageViewModel(IBiometric biometric, AppShell appShell, IMonthlyIncomeDataRepository monthlyIncomeDataRepository, IMonthlySavingDataRepository monthlySavingDataRepository, IMonthlyFixedCostDataRepository monthlyFixedCostDataRepository, ISpendingItemRepository spendingItemRepository, IMonthlyBudgetDataRepository weeklyBudgetDataRepository, ICategoryRepository categoryRepository, ISpecialExpenseDataRepository specialExpenseDataRepository, SettingPreferences settingPreferences)
    {
        _biometric = biometric;
        _settingPreferences = settingPreferences;
        _appShell = appShell;
        _categoryRepository = categoryRepository;
        _monthlyIncomeDataRepository = monthlyIncomeDataRepository;
        _monthlySavingDataRepository = monthlySavingDataRepository;
        _monthlyFixedCostDataRepository = monthlyFixedCostDataRepository;
        _spendingItemRepository = spendingItemRepository;
        _monthlyBudgetDataRepository = weeklyBudgetDataRepository;
        _specialExpenseDataRepository = specialExpenseDataRepository;
    }

    public async Task InitData()
    {
        RecordsTotalRemainingToThisMonth = _settingPreferences.RecordsTotalRemainingToThisMonth;
        await InitIncomes();
        await InitSaving();
        await InitFixedCosts();
        await InitBudgetControlResults();
        await InitTotalRemainingAndSavingUntilPreviousMonth();
        CalculateMonthlyRemainingTotal();
        await InitSpecialExpense();
    }

    private async Task InitIncomes()
    {
        var selectedDate = SharedProperty.Instance.SelectedDate;
        var allMonthIncomes = await _monthlyIncomeDataRepository.GetAllAsync();
        var selectedMonthsIncomes = BudgetCalculator.InMonth(allMonthIncomes, x => x.Date, selectedDate);
        MonthlyIncomes = new ObservableCollection<MonthlyIncome>(selectedMonthsIncomes);
    }

    private async Task InitSaving()
    {
        var selectedDate = SharedProperty.Instance.SelectedDate;
        var allMonthSavings = await _monthlySavingDataRepository.GetAllAsync();
        var selectedMonthsSavings = BudgetCalculator.SavingsInMonth(allMonthSavings, selectedDate);
        MonthlySavings = new ObservableCollection<MonthlySaving>(selectedMonthsSavings);
    }

    private async Task InitFixedCosts()
    {
        var selectedDate = SharedProperty.Instance.SelectedDate;
        var allMonthFixedCosts = await _monthlyFixedCostDataRepository.GetAllAsync();
        var selectedMonthsFixedCosts = BudgetCalculator.InMonth(allMonthFixedCosts, x => x.Date, selectedDate);
        MonthlyFixedCosts = new ObservableCollection<MonthlyFixedCost>(selectedMonthsFixedCosts);
    }

    private async Task InitBudgetControlResults()
    {
        var selectedDate = SharedProperty.Instance.SelectedDate;
        var allMonthBudgets = await _monthlyBudgetDataRepository.GetAllAsync();
        var allSpendingItems = await _spendingItemRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();

        var budgetControlResults = BudgetCalculator.BuildBudgetControlResults(categories, allMonthBudgets, allSpendingItems, selectedDate);

        BudgetControlResults = new ObservableCollection<BudgetControlResult>(budgetControlResults);
    }

    private async Task InitTotalRemainingAndSavingUntilPreviousMonth()
    {
        var selectedDate = SharedProperty.Instance.SelectedDate;
        var allMonthIncomes = await _monthlyIncomeDataRepository.GetAllAsync();
        var allMonthSavings = await _monthlySavingDataRepository.GetAllAsync();
        var allMonthFixedCosts = await _monthlyFixedCostDataRepository.GetAllAsync();
        var allSpendingItems = await _spendingItemRepository.GetAllAsync();

        SavingsUntilPreviousMonth = new ObservableCollection<SavingResult>(BudgetCalculator.SavingsUntilPreviousMonth(allMonthSavings, selectedDate));
        MonthlyLivingOff = SavingsUntilPreviousMonth.Sum(x => x.LivingOffSavingAmount);

        TotalRemainingUntilPreviousMonth = BudgetCalculator.TotalRemainingUntilPreviousMonth(selectedDate, allMonthIncomes, allMonthSavings, allMonthFixedCosts, allSpendingItems);
    }

    private async Task InitSpecialExpense()
    {
        var selectedDate = SharedProperty.Instance.SelectedDate;
        var allSpecialExpenses = await _specialExpenseDataRepository.GetAllAsync();
        var selectedSpecialExpenses = BudgetCalculator.InMonth(allSpecialExpenses, x => x.Date, selectedDate);
        MonthlySpecialExpenses = new ObservableCollection<SpecialExpense>(selectedSpecialExpenses);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyIncomeTotal))]
    [NotifyPropertyChangedFor(nameof(MonthlyUsableMoney))]
    ObservableCollection<MonthlyIncome> _monthlyIncomes = default!;

    public decimal MonthlyIncomeTotal => MonthlyIncomes?.Sum(x => x.Amount) ?? 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlySavingTotal))]
    [NotifyPropertyChangedFor(nameof(MonthlyUsableMoney))]
    ObservableCollection<MonthlySaving> _monthlySavings = default!;

    public decimal MonthlySavingTotal => MonthlySavings?.Sum(x => x.Amount) ?? 0;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyFixedCostTotal))]
    [NotifyPropertyChangedFor(nameof(MonthlyUsableMoney))]
    ObservableCollection<MonthlyFixedCost> _monthlyFixedCosts = default!;

    /// <summary>
    /// 固定費
    /// </summary>
    public decimal MonthlyFixedCostTotal => MonthlyFixedCosts?.Sum(x => x.Amount) ?? 0;

    /// <summary>
    /// 使えるお金
    /// </summary>
    public decimal MonthlyUsableMoney => GetMonthlyUsableMoney();

    private decimal GetMonthlyUsableMoney() =>
        BudgetCalculator.MonthlyUsableMoney(RecordsTotalRemainingToThisMonth, TotalRemainingUntilPreviousMonth,
                                            MonthlyIncomeTotal, MonthlySavingTotal, MonthlyFixedCostTotal);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyVariableCostTotal))]
    [NotifyPropertyChangedFor(nameof(MonthlyBudgetTotal))]
    ObservableCollection<BudgetControlResult> _budgetControlResults = default!;

    /// <summary>
    /// 変動費
    /// </summary>
    public decimal MonthlyVariableCostTotal => BudgetControlResults?.Sum(x => x.MonthlySpending) ?? 0;

    /// <summary>
    /// 予算
    /// </summary>
    public decimal MonthlyBudgetTotal => BudgetControlResults?.Sum(x => x.MonthlyBudget.Amount) ?? 0;

    /// <summary>
    /// 先月までの残高を当月に計上する
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotRecordsTotalRemainingToThisMonth))]    
    bool _recordsTotalRemainingToThisMonth = false;

    public bool NotRecordsTotalRemainingToThisMonth => !RecordsTotalRemainingToThisMonth;

    /// <summary>
    /// 先月までの貯金
    /// </summary>
    [ObservableProperty]
    ObservableCollection<SavingResult> _savingsUntilPreviousMonth = default!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlySpecialExpenseTotal))]
    ObservableCollection<SpecialExpense> _monthlySpecialExpenses = default!;

    /// <summary>
    /// 特別出費
    /// </summary>
    public decimal MonthlySpecialExpenseTotal => MonthlySpecialExpenses?.Sum(x => x.Amount) ?? 0;

    /// <summary>
    /// 残りのお金
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyUsableMoney))]
    decimal _monthlyRemainingTotal = default!;

    /// <summary>
    /// 切り崩し額
    /// </summary>
    [ObservableProperty]
    decimal _monthlyLivingOff = default!;

    [ObservableProperty]
    string _remainingTotalStringColor = "Black";

    /// <summary>
    /// 先月までの残高
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyUsableMoney))]
    decimal _totalRemainingUntilPreviousMonth = default!;

    private void CalculateMonthlyRemainingTotal()
    {
        MonthlyRemainingTotal = BudgetCalculator.MonthlyRemainingTotal(MonthlyUsableMoney, MonthlyVariableCostTotal, MonthlyLivingOff);
        RemainingTotalStringColor = GetResultTextColor();
    }

    private string GetResultTextColor()
    {

        if (MonthlyRemainingTotal < 0) return "Red";

        // Assuming you have a method to get the current theme
        var currentTheme = Application.Current?.RequestedTheme;
        if (currentTheme.Equals(AppTheme.Dark))
        {
            return "White";
        }

        return "Black";
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuthButtonVisible))]
    bool _isAuthenticated = false;

    public bool IsAuthButtonVisible => !IsAuthenticated;

    public async Task AuthenticationRequest()
    {
        if (!_settingPreferences.IsFingerprintEnabled)
        {
            IsAuthenticated = true;
            _appShell.SetTabVisibility(true);
            await InitData();
            return;
        }

        var result = await _biometric.AuthenticateAsync(new AuthenticationRequest()
        {
            Title = "認証",
            Description = "生体認証を行います",
            NegativeText = "キャンセル",
        }, CancellationToken.None);

        if (result.Status.Equals(BiometricResponseStatus.Success))
        {
            IsAuthenticated = true;
            _appShell.SetTabVisibility(true);
            await InitData();
        }
    }

    // Popup v2 draws a rounded, shadowed border by default; keep the flat sheet look.
    static Task ShowAddPopupAsync(Popup popup) =>
        Shell.Current.CurrentPage.ShowPopupAsync(popup, new PopupOptions
        {
            Shape = new Microsoft.Maui.Controls.Shapes.Rectangle { StrokeThickness = 0 },
            Shadow = null,
        });

    [RelayCommand]
    async Task ShowAddIncomePopup()
    {
        var inputDataObject = new MonthlyIncome()
        {
            Date = SharedProperty.Instance.SelectedDate
        };

        var formTitle = "収入";
        var viewmodel = new AddAccountPopupViewModel(_monthlyIncomeDataRepository, _monthlyFixedCostDataRepository, _monthlySavingDataRepository, inputDataObject, formTitle);
        await ShowAddPopupAsync(new AddAccountPopup(viewmodel));

        await RefreshIncomeDataGrid();
    }

    [RelayCommand]
    async Task ShowAddSavingPopup()
    {
        var inputDataObject = new MonthlySaving()
        {
            Date = SharedProperty.Instance.SelectedDate
        };

        var formTitle = "貯金";
        var viewmodel = new AddAccountPopupViewModel(_monthlyIncomeDataRepository, _monthlyFixedCostDataRepository, _monthlySavingDataRepository, inputDataObject, formTitle);
        await ShowAddPopupAsync(new AddAccountPopup(viewmodel));

        await RefreshSavingDataGrid();
    }

    [RelayCommand]
    async Task GetValuesFromPreviousMonth()
    {
        var selectedMonth = SharedProperty.Instance.SelectedDate;
        var allMonthFixedCosts = await _monthlyFixedCostDataRepository.GetAllAsync();
        var convertToSelectedMonthsFixedCosts = BudgetCalculator.CopyFixedCostsFromPreviousMonth(allMonthFixedCosts, selectedMonth);

        var result = await _monthlyFixedCostDataRepository.AddAsync(convertToSelectedMonthsFixedCosts);

        await RefreshFixedCostDataGrid();
    }

    [RelayCommand]
    async Task ShowAddFixedCostPopup()
    {
        var inputDataObject = new MonthlyFixedCost()
        {
            Date = SharedProperty.Instance.SelectedDate
        };

        var formTitle = "変動費";
        var viewmodel = new AddAccountPopupViewModel(_monthlyIncomeDataRepository, _monthlyFixedCostDataRepository, _monthlySavingDataRepository, inputDataObject, formTitle);
        await ShowAddPopupAsync(new AddAccountPopup(viewmodel));

        await RefreshFixedCostDataGrid();
    }

    [RelayCommand]
    async Task ShowAddSpecialExpensePopup()
    {
        var targetDate = BudgetCalculator.DefaultSpecialExpenseDate(SharedProperty.Instance.SelectedDate, DateTime.Today);

        var inputDataObject = new SpecialExpense()
        {
            Date = targetDate,
        };

        var viewmodel = new AddSpecialExpensePopupViewModel (_specialExpenseDataRepository, inputDataObject);
        await ShowAddPopupAsync(new AddSpecialExpensePopup(viewmodel));

        await RefreshSpecialExpenseDataGrid();
    }

    [RelayCommand]
    async Task DateSelectionChangedAsync(DatePickerSelectionChangedEventArgs args)
    {
        SharedProperty.Instance.SelectedDate = args.NewValue ?? DateTime.Today;
        await InitData();
    }

    [RelayCommand]
    async Task AuthenticateAsync()
    {
        await AuthenticationRequest();
    }

    public SfDataGrid IncomeDataGrid { get; set; } = default!;

    [RelayCommand]
    public async Task RefreshIncomeDataGrid()
    {
        try
        {
            IncomeDataGrid.IsBusy = true;
            await InitIncomes();
            CalculateMonthlyRemainingTotal();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            IncomeDataGrid.IsBusy = false;
        }
    }

    public SfDataGrid SavingDataGrid { get; set; } = default!;

    [RelayCommand]
    public async Task RefreshSavingDataGrid()
    {
        try
        {
            SavingDataGrid.IsBusy = true;
            await InitSaving();
            CalculateMonthlyRemainingTotal();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            SavingDataGrid.IsBusy = false;
        }
    }

    public SfDataGrid FixedCostDataGrid { get; set; } = default!;
    

    [RelayCommand]
    public async Task RefreshFixedCostDataGrid()
    {
        try
        {
            FixedCostDataGrid.IsBusy = true;
            await InitFixedCosts();
            CalculateMonthlyRemainingTotal();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            FixedCostDataGrid.IsBusy = false;
        }
    }

    public SfDataGrid BudgetControlResultsDataGrid { get; set; } = default!;

    [RelayCommand]
    public async Task RefreshBudgetControlResultsDataGrid()
    {
        try
        {
            BudgetControlResultsDataGrid.IsBusy = true;
            await InitBudgetControlResults();
            CalculateMonthlyRemainingTotal();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            BudgetControlResultsDataGrid.IsBusy = false;
        }
    }

    public SfDataGrid SavingsUntilPreviousMonthDataGrid { get; set; } = default!;

    [RelayCommand]
    public async Task RefreshSavingsUntilPreviousMonthDataGrid()
    {
        try
        {
            SavingsUntilPreviousMonthDataGrid.IsBusy = true;
            await InitTotalRemainingAndSavingUntilPreviousMonth();
            CalculateMonthlyRemainingTotal();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            SavingsUntilPreviousMonthDataGrid.IsBusy = false;
        }
    }

    public SfDataGrid SpecialExpenseDataGrid { get; set; } = default!;

    [RelayCommand]
    public async Task RefreshSpecialExpenseDataGrid()
    {
        try
        {
            SpecialExpenseDataGrid.IsBusy = true;
            await InitSpecialExpense();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            SpecialExpenseDataGrid.IsBusy = false;
        }
    }
}