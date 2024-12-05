using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
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

    public MainPageViewModel(IBiometric biometric, AppShell appShell, IMonthlyIncomeDataRepository monthlyIncomeDataRepository, IMonthlyFixedCostDataRepository monthlyFixedCostDataRepository, ISpendingItemRepository spendingItemRepository, IMonthlyBudgetDataRepository weeklyBudgetDataRepository)
    {
        _biometric = biometric;
        _appShell = appShell;
        _monthlyIncomeDataRepository = monthlyIncomeDataRepository;
        _monthlyFixedCostDataRepository = monthlyFixedCostDataRepository;
        _spendingItemRepository = spendingItemRepository;
        _monthlyBudgetDataRepository = weeklyBudgetDataRepository;
    }

    private async Task InitData()
    {
        await InitIncomes();
        await InitFixedCosts();
        await InitBudgetControlResults();
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
        foreach (var budget in selectedMonthsBudgets)
        {
            if (budget.Category.Equals("自分")) continue;       

            var monthlySpending = selectedMonthsSpendingItems.Where(x => x.Category.Name.Equals(budget.Category.Name))
                                                            .Sum(x => x.Amount);
            budgetControlResults.Add(new BudgetControlResult()
            {
                MonthlyBudget = budget,
                MonthlySpending = monthlySpending
            });
        }

        BudgetControlResults = new ObservableCollection<BudgetControlResult>(budgetControlResults);
    }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyIncomeTotal))]
    ObservableCollection<MonthlyIncome> _monthlyIncomes = default!;

    public decimal MonthlyIncomeTotal => MonthlyIncomes?.Sum(x => x.Amount) ?? 0;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyFixedCostTotal))]
    ObservableCollection<MonthlyFixedCost> _monthlyFixedCosts = default!;

    public decimal MonthlyFixedCostTotal => MonthlyFixedCosts.Sum(x => x.Amount);

    //[ObservableProperty]
    //ObservableCollection<MonthlyBudget> _monthlyBudgets = default!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyVariableCostTotal))]
    ObservableCollection<BudgetControlResult> _budgetControlResults = default!;

    public decimal MonthlyVariableCostTotal => BudgetControlResults.Sum(x => x.MonthlySpending);

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
    async Task RefreshIncomeDataGrid()
    {
        try
        {
            IncomeDataGrid.IsBusy = true;
            await InitIncomes();
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
    async Task RefreshFixedCostDataGrid()
    {
        try
        {
            IncomeDataGrid.IsBusy = true;
            await InitFixedCosts();
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
}