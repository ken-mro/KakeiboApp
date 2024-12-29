using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
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
    readonly AppShell _appShell;
    readonly IMonthlyIncomeDataRepository _monthlyIncomeDataRepository;
    readonly IMonthlyFixedCostDataRepository _monthlyFixedCostDataRepository;
    readonly ISpendingItemRepository _spendingItemRepository;
    readonly IMonthlyBudgetDataRepository _monthlyBudgetDataRepository;
    readonly ICategoryRepository _categoryRepository;

    public MainPageViewModel(IBiometric biometric, AppShell appShell, IMonthlyIncomeDataRepository monthlyIncomeDataRepository, IMonthlyFixedCostDataRepository monthlyFixedCostDataRepository, ISpendingItemRepository spendingItemRepository, IMonthlyBudgetDataRepository weeklyBudgetDataRepository, ICategoryRepository categoryRepository)
    {
        _biometric = biometric;
        _appShell = appShell;
        _categoryRepository = categoryRepository;
        _monthlyIncomeDataRepository = monthlyIncomeDataRepository;
        _monthlyFixedCostDataRepository = monthlyFixedCostDataRepository;
        _spendingItemRepository = spendingItemRepository;
        _monthlyBudgetDataRepository = weeklyBudgetDataRepository;
    }

    public async Task InitData()
    {
        await InitIncomes();
        await InitFixedCosts();
        await InitBudgetControlResults();
        CalculateMonthlyRemainingTotal();
        InitTotalSavingsUntilPreviousMonth();
    }

    private async Task InitIncomes()
    {
        var allMonthIncomes = await _monthlyIncomeDataRepository.GetAllIncomesAsync();
        var selectedMonthsIncomes = allMonthIncomes.Where(x => x.Date.Month.Equals(SelectedDate.Month)
                                                            && x.Date.Year.Equals(SelectedDate.Year)).ToList();
        MonthlyIncomes = new ObservableCollection<MonthlyIncome>(selectedMonthsIncomes);
    }

    private async Task InitFixedCosts()
    {
        var allMonthFixedCosts = await _monthlyFixedCostDataRepository.GetAllFixedCostsAsync();
        var selectedMonthsFixedCosts = allMonthFixedCosts.Where(x => x.Date.Month.Equals(SelectedDate.Month)
                                                            && x.Date.Year.Equals(SelectedDate.Year)).ToList();
        MonthlyFixedCosts = new ObservableCollection<MonthlyFixedCost>(selectedMonthsFixedCosts);
    }

    private async Task InitBudgetControlResults()
    {
        var allMonthBudgets = await _monthlyBudgetDataRepository.GetAllBudgetsAsync();
        var selectedMonthsBudgets = allMonthBudgets.Where(x => x.Date.Month.Equals(SelectedDate.Month)
                                                            && x.Date.Year.Equals(SelectedDate.Year)).ToList();

        var allSpendingItems = await _spendingItemRepository.GetAllAsync();
        var selectedMonthsSpendingItems = allSpendingItems.Where(x => x.Date.Month.Equals(SelectedDate.Month)
                                                            && x.Date.Year.Equals(SelectedDate.Year)).ToList();

        var budgetControlResults = new List<BudgetControlResult>();

        var categories = await _categoryRepository.GetAllAsync();
        foreach (var category in categories)
        {
            var monthlySpending = selectedMonthsSpendingItems.Where(x => x.Category.Name.Equals(category.Name))
                                                            .Sum(x => x.Amount);
            var budget = selectedMonthsBudgets.Where(b => b.Category.Equals(category)).FirstOrDefault(new MonthlyBudget() { Category = category, Date = SelectedDate});
            budgetControlResults.Add(new BudgetControlResult()
            {
                MonthlyBudget = budget ?? new()
                {
                    Category = category
                },
                MonthlySpending = monthlySpending
            });
        }

        BudgetControlResults = new ObservableCollection<BudgetControlResult>(budgetControlResults);
    }

    private async Task InitTotalSavingsUntilPreviousMonth()
    {
        var allMonthIncomes = await _monthlyIncomeDataRepository.GetAllIncomesAsync();
        var selectedMonthsIncomes = allMonthIncomes.Where(x => x.Date < SelectedDate).ToList();
        var totalIncomeAmount = selectedMonthsIncomes.Sum(x => x.Amount);

        var allMonthFixedCosts = await _monthlyFixedCostDataRepository.GetAllFixedCostsAsync();
        var selectedMonthsFixedCosts = allMonthFixedCosts.Where(x => x.Date < SelectedDate).ToList();
        var totalFixedCostAmount = selectedMonthsFixedCosts?.Sum(x => x.Amount) ?? 0;

        var allSpendingItems = await _spendingItemRepository.GetAllAsync();
        var selectedMonthsSpendingItems = allSpendingItems.Where(x => x.Date < SelectedDate).ToList();
        var totalSpendingAmount = selectedMonthsSpendingItems?.Sum(x => x.Amount) ?? 0;

        TotalSavingsUntilPreviousMonth = totalIncomeAmount - totalFixedCostAmount - totalSpendingAmount;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyIncomeTotal))]
    [NotifyPropertyChangedFor(nameof(MonthlyUsableMoney))]
    ObservableCollection<MonthlyIncome> _monthlyIncomes = default!;

    public decimal MonthlyIncomeTotal => MonthlyIncomes?.Sum(x => x.Amount) ?? 0;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyFixedCostTotal))]
    [NotifyPropertyChangedFor(nameof(MonthlyUsableMoney))]
    ObservableCollection<MonthlyFixedCost> _monthlyFixedCosts = default!;

    public decimal MonthlyFixedCostTotal => MonthlyFixedCosts?.Sum(x => x.Amount) ?? 0;

    public decimal MonthlyUsableMoney => MonthlyIncomeTotal - MonthlyFixedCostTotal;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyVariableCostTotal))]
    ObservableCollection<BudgetControlResult> _budgetControlResults = default!;

    public decimal MonthlyVariableCostTotal => BudgetControlResults?.Sum(x => x.MonthlySpending) ?? 0;

    [ObservableProperty]
    decimal _monthlyRemainingTotal = default!;

    [ObservableProperty]
    string _remainingTotalStringColor = "Black";

    [ObservableProperty]
    decimal _totalSavingsUntilPreviousMonth = default!;

    private void CalculateMonthlyRemainingTotal()
    {
        MonthlyRemainingTotal = MonthlyIncomeTotal - MonthlyFixedCostTotal - MonthlyVariableCostTotal;
        RemainingTotalStringColor = GetResultTextColor();
    }

    private string GetResultTextColor()
    {

        if (MonthlyRemainingTotal < 0) return "Red";

        // Assuming you have a method to get the current theme
        var currentTheme = Application.Current.RequestedTheme;
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

    [ObservableProperty]
    DateTime _selectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [RelayCommand]
    async Task ShowAddIncomePopup()
    {
        var inputIncome = new MonthlyIncome()
        {
            Date = SelectedDate
        };

        var formTitle = "収入";
        var viewmodel = new AddAccountPopupViewModel(_monthlyIncomeDataRepository, _monthlyFixedCostDataRepository, inputIncome, formTitle);
        await Shell.Current.CurrentPage.ShowPopupAsync(new AddAccountPopup(viewmodel));

        await RefreshIncomeDataGrid();
    }

    [RelayCommand]
    async Task ShowAddFixedCostPopup()
    {
        var inputIncome = new MonthlyFixedCost()
        {
            Date = SelectedDate
        };

        var formTitle = "変動費";
        var viewmodel = new AddAccountPopupViewModel(_monthlyIncomeDataRepository, _monthlyFixedCostDataRepository, inputIncome, formTitle);
        await Shell.Current.CurrentPage.ShowPopupAsync(new AddAccountPopup(viewmodel));

        await RefreshFixedCostDataGrid();
    }

    [RelayCommand]
    async Task DateSelectionChangedAsync(DatePickerSelectionChangedEventArgs args)
    {
        SelectedDate = args.NewValue ?? DateTime.Today;
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
}