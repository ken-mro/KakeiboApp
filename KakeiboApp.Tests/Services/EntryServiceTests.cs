using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;

namespace KakeiboApp.Tests.Services;

public class EntryServiceTests
{
    static readonly DateTime September = new(2026, 9, 1);
    static readonly DateTime October = new(2026, 10, 1);
    static readonly Category Food = new() { Name = "食費" };
    static readonly Category Fun = new() { Name = "娯楽" };

    /// <summary>An in-memory table that copies on the way in and out, like the SQLite repositories.</summary>
    sealed class Table<T>(Func<T, int> idOf, Action<T, int> setId, Func<T, T> copy)
    {
        readonly List<T> _rows = [];
        int _next = 1;

        public IEnumerable<T> All() => _rows.Select(copy).ToList();
        public T? Get(int id) => _rows.Where(r => idOf(r) == id).Select(copy).FirstOrDefault();

        public int Add(T item)
        {
            var row = copy(item);
            setId(row, _next++); // AutoIncrement ignores the id that was passed in.
            _rows.Add(row);
            return 1;
        }

        public int Update(T item)
        {
            var index = _rows.FindIndex(r => idOf(r) == idOf(item));
            if (index < 0) return 0;
            _rows[index] = copy(item);
            return 1;
        }

        public int Delete(int id) => _rows.RemoveAll(r => idOf(r) == id);
    }

    sealed class Incomes : IMonthlyIncomeDataRepository
    {
        public readonly Table<MonthlyIncome> T = new(x => x.Id, (x, id) => x.Id = id,
            x => new MonthlyIncome { Id = x.Id, Date = x.Date, Name = x.Name, Amount = x.Amount, Note = x.Note });
        public Task<IEnumerable<MonthlyIncome>> GetAllAsync() => Task.FromResult(T.All());
        public Task<MonthlyIncome> GetByIdAsync(int id) => Task.FromResult(T.Get(id)!);
        public Task<int> AddAsync(MonthlyIncome x) => Task.FromResult(T.Add(x));
        public Task<int> UpdateAsync(MonthlyIncome x) => Task.FromResult(T.Update(x));
        public Task<int> DeleteAsync(int id) => Task.FromResult(T.Delete(id));
    }

    sealed class Savings : IMonthlySavingDataRepository
    {
        public readonly Table<MonthlySaving> T = new(x => x.Id, (x, id) => x.Id = id,
            x => new MonthlySaving { Id = x.Id, Date = x.Date, Name = x.Name, Amount = x.Amount, Note = x.Note });
        public Task<IEnumerable<MonthlySaving>> GetAllAsync() => Task.FromResult(T.All());
        public Task<MonthlySaving> GetByIdAsync(int id) => Task.FromResult(T.Get(id)!);
        public Task<int> AddAsync(MonthlySaving x) => Task.FromResult(T.Add(x));
        public Task<int> UpdateAsync(MonthlySaving x) => Task.FromResult(T.Update(x));
        public Task<int> DeleteAsync(int id) => Task.FromResult(T.Delete(id));
    }

    sealed class FixedCosts : IMonthlyFixedCostDataRepository
    {
        public readonly Table<MonthlyFixedCost> T = new(x => x.Id, (x, id) => x.Id = id,
            x => new MonthlyFixedCost { Id = x.Id, Date = x.Date, Name = x.Name, Amount = x.Amount, Note = x.Note });
        public Task<IEnumerable<MonthlyFixedCost>> GetAllAsync() => Task.FromResult(T.All());
        public Task<MonthlyFixedCost> GetByIdAsync(int id) => Task.FromResult(T.Get(id)!);
        public Task<int> AddAsync(MonthlyFixedCost x) => Task.FromResult(T.Add(x));
        public Task<int> AddAsync(IEnumerable<MonthlyFixedCost> xs) => Task.FromResult(xs.Sum(T.Add));
        public Task<int> UpdateAsync(MonthlyFixedCost x) => Task.FromResult(T.Update(x));
        public Task<int> DeleteAsync(int id) => Task.FromResult(T.Delete(id));
    }

    sealed class Spending : ISpendingItemRepository
    {
        public readonly Table<SpendingItem> T = new(x => x.Id, (x, id) => x.Id = id,
            x => new SpendingItem { Id = x.Id, Date = x.Date, Category = x.Category, Name = x.Name, Amount = x.Amount, Note = x.Note });
        public Task<IEnumerable<SpendingItem>> GetAllAsync() => Task.FromResult(T.All());
        public Task<SpendingItem> GetByIdAsync(int id) => Task.FromResult(T.Get(id)!);
        public Task<int> AddAsync(SpendingItem x) => Task.FromResult(T.Add(x));
        public Task<int> UpdateAsync(SpendingItem x) => Task.FromResult(T.Update(x));
        public Task<int> DeleteAsync(int id) => Task.FromResult(T.Delete(id));
    }

    sealed class Specials : ISpecialExpenseDataRepository
    {
        public readonly Table<SpecialExpense> T = new(x => x.Id, (x, id) => x.Id = id,
            x => new SpecialExpense { Id = x.Id, Date = x.Date, Name = x.Name, Amount = x.Amount, FromWhere = x.FromWhere, Note = x.Note });
        public Task<IEnumerable<SpecialExpense>> GetAllAsync() => Task.FromResult(T.All());
        public Task<SpecialExpense> GetByIdAsync(int id) => Task.FromResult(T.Get(id)!);
        public Task<int> AddAsync(SpecialExpense x) => Task.FromResult(T.Add(x));
        public Task<int> UpdateAsync(SpecialExpense x) => Task.FromResult(T.Update(x));
        public Task<int> DeleteAsync(int id) => Task.FromResult(T.Delete(id));
    }

    sealed class Budgets : IMonthlyBudgetDataRepository
    {
        public readonly Table<MonthlyBudget> T = new(x => x.Id, (x, id) => x.Id = id,
            x => new MonthlyBudget { Id = x.Id, Date = x.Date, Category = x.Category, Amount = x.Amount });
        public Task<IEnumerable<MonthlyBudget>> GetAllAsync() => Task.FromResult(T.All());
        public Task<MonthlyBudget> GetByIdAsync(int id) => Task.FromResult(T.Get(id)!);
        public Task<int> AddAsync(MonthlyBudget x) => Task.FromResult(T.Add(x));
        public Task<int> UpdateAsync(MonthlyBudget x) => Task.FromResult(T.Update(x));
        public Task<int> DeleteAsync(int id) => Task.FromResult(T.Delete(id));
    }

    sealed class Categories : ICategoryRepository
    {
        public Task<IEnumerable<Category>> GetAllAsync() => Task.FromResult<IEnumerable<Category>>([Food, Fun]);
        public Task<Category> GetByIdAsync(string name) => Task.FromResult(new Category { Name = name });
        public Task<int> AddAsync(Category category) => Task.FromResult(1);
        public Task<int> UpdateAsync(Category category) => Task.FromResult(1);
        public Task<int> DeleteAsync(string name) => Task.FromResult(1);
    }

    readonly Incomes _incomes = new();
    readonly Savings _savings = new();
    readonly FixedCosts _fixedCosts = new();
    readonly Spending _spending = new();
    readonly Specials _specials = new();
    readonly Budgets _budgets = new();
    readonly EntryService _service;

    public EntryServiceTests()
    {
        _service = new EntryService(_incomes, _savings, _fixedCosts, _spending, _specials, _budgets, new Categories());
    }

    static EntryDraft Draft(decimal amount, string name = "", DateTime? date = null, string? category = null) =>
        new() { Amount = amount, Name = name, Date = date ?? October, Category = category };

    [Fact]
    public async Task Load_TotalsAndCountsTheMonth()
    {
        await _service.AddAsync(EntryKind.Income, Draft(300000, "給料"));
        await _service.AddAsync(EntryKind.Income, Draft(15000, "副業"));
        await _service.AddAsync(EntryKind.Income, Draft(999, "先月", September));

        var page = await _service.LoadAsync(EntryKind.Income, October);

        Assert.Equal(2, page.Count);
        Assert.Equal(315000, page.Total);
        Assert.Equal(["給料", "副業"], page.Groups.Single().Select(r => r.Name));
    }

    [Fact]
    public async Task Load_GroupsSpendingByDay_NewestFirst()
    {
        await _service.AddAsync(EntryKind.Spending, Draft(100, "a", new DateTime(2026, 10, 15), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(200, "b", new DateTime(2026, 10, 16), "娯楽"));

        var page = await _service.LoadAsync(EntryKind.Spending, October);

        Assert.Equal(["10月16日（金）", "10月15日（木）"], page.Groups.Select(g => g.Header));
        Assert.Equal(300, page.Total);
    }

    [Fact]
    public async Task Load_SpecialExpenses_CoverTheWholeYear()
    {
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(30000, "結婚式", new DateTime(2026, 10, 10)));
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(50000, "旅行", new DateTime(2026, 3, 20)));
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(1, "去年", new DateTime(2025, 12, 31)));

        var page = await _service.LoadAsync(EntryKind.SpecialExpense, October);

        Assert.Equal(2, page.Count);
        Assert.Equal(80000, page.Total);
    }

    [Theory]
    [InlineData(EntryKind.Income)]
    [InlineData(EntryKind.Saving)]
    [InlineData(EntryKind.FixedCost)]
    [InlineData(EntryKind.Spending)]
    [InlineData(EntryKind.SpecialExpense)]
    public async Task DeleteThenRestore_BringsTheEntryBackWithTheSameValues(EntryKind kind)
    {
        await _service.AddAsync(kind, new EntryDraft
        {
            Date = new DateTime(2026, 10, 5), Name = "内容", Note = "メモ", Amount = 1280, Category = "食費", FromWhere = "貯金",
        });
        var row = (await _service.LoadAsync(kind, October)).Groups.Single().Single();

        await _service.DeleteAsync(row);
        Assert.Equal(0, (await _service.LoadAsync(kind, October)).Count);

        await _service.RestoreAsync(row);
        var restored = (await _service.LoadAsync(kind, October)).Groups.Single().Single();

        Assert.Equal((row.Name, row.Note, row.Amount, row.Date, row.Category, row.FromWhere),
            (restored.Name, restored.Note, restored.Amount, restored.Date, restored.Category, restored.FromWhere));
    }

    [Fact]
    public async Task Save_UpdatesTheEntry()
    {
        await _service.AddAsync(EntryKind.Spending, Draft(1000, "ランチ", new DateTime(2026, 10, 5), "食費"));
        var row = (await _service.LoadAsync(EntryKind.Spending, October)).Groups.Single().Single();

        await _service.SaveAsync(row, Draft(1500, "映画", new DateTime(2026, 10, 6), "娯楽"));

        var saved = _spending.T.All().Single();
        Assert.Equal((1500m, "映画", new DateTime(2026, 10, 6), "娯楽"), (saved.Amount, saved.Name, saved.Date, saved.Category.Name));
    }

    [Fact]
    public async Task Withdrawal_IsSavedAsANegativeSavingOnTheFirstOfTheMonth_AndCanBeUndone()
    {
        await _savings.AddAsync(new MonthlySaving { Date = September, Name = "旅行", Amount = 20000, Note = "" });
        var balance = (await _service.LoadAsync(EntryKind.SavingBalance, October)).Groups.Single().Single();
        Assert.False(balance.CanDelete);

        await _service.SaveAsync(balance, Draft(5000));

        var withdrawal = _savings.T.All().Single(x => x.Amount < 0);
        Assert.Equal((October, "旅行", -5000m), (withdrawal.Date, withdrawal.Name, withdrawal.Amount));
        var page = await _service.LoadAsync(EntryKind.SavingBalance, October);
        Assert.Equal(15000, page.Total);

        var withWithdrawal = page.Groups.Single().Single();
        Assert.True(withWithdrawal.CanDelete);
        await _service.DeleteAsync(withWithdrawal);
        Assert.Equal(20000, (await _service.LoadAsync(EntryKind.SavingBalance, October)).Total);

        await _service.RestoreAsync(withWithdrawal);
        Assert.Equal(15000, (await _service.LoadAsync(EntryKind.SavingBalance, October)).Total);
    }

    [Fact]
    public async Task Withdrawal_CannotExceedTheSaving()
    {
        await _savings.AddAsync(new MonthlySaving { Date = September, Name = "車", Amount = 10000, Note = "" });
        var balance = (await _service.LoadAsync(EntryKind.SavingBalance, October)).Groups.Single().Single();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.SaveAsync(balance, Draft(20000)));
        Assert.DoesNotContain(_savings.T.All(), x => x.Amount < 0);
    }

    [Fact]
    public async Task Budget_IsAddedThenUpdated_AndCanBeDeletedAndRestored()
    {
        var summary = await _service.SummarizeAsync(October, carriesOver: false);
        var food = EntryService.Row(summary.Budgets.Single(b => b.Category == Food));
        Assert.False(food.CanDelete);

        await _service.SaveAsync(food, Draft(50000));
        Assert.Equal(50000, (await _service.SummarizeAsync(October, false)).BudgetTotal);

        var saved = EntryService.Row((await _service.SummarizeAsync(October, false)).Budgets.Single(b => b.Category == Food));
        await _service.SaveAsync(saved, Draft(40000));
        Assert.Single(_budgets.T.All());
        Assert.Equal(40000, (await _service.SummarizeAsync(October, false)).BudgetTotal);

        var updated = EntryService.Row((await _service.SummarizeAsync(October, false)).Budgets.Single(b => b.Category == Food));
        await _service.DeleteAsync(updated);
        Assert.Equal(0, (await _service.SummarizeAsync(October, false)).BudgetTotal);

        await _service.RestoreAsync(updated);
        Assert.Equal(40000, (await _service.SummarizeAsync(October, false)).BudgetTotal);
    }

    [Fact]
    public async Task CopyFixedCosts_CopiesThePreviousMonth()
    {
        await _service.AddAsync(EntryKind.FixedCost, Draft(80000, "家賃", September));
        await _service.AddAsync(EntryKind.FixedCost, Draft(8000, "電気", September));

        Assert.Equal(2, await _service.CountAsync(EntryKind.FixedCost, September));
        Assert.Equal(2, await _service.CopyFromPreviousMonthAsync(EntryKind.FixedCost, October));
        Assert.Equal(88000, (await _service.LoadAsync(EntryKind.FixedCost, October)).Total);
    }

    [Fact]
    public async Task CopyIncomesAndSavings_CopiesThePreviousMonthNextToWhatTheMonthHas()
    {
        await _service.AddAsync(EntryKind.Income, Draft(300000, "給料", September));
        await _service.AddAsync(EntryKind.Income, Draft(5000, "副業", October));
        await _service.AddAsync(EntryKind.Saving, Draft(20000, "旅行", September));

        Assert.Equal(1, await _service.CopyFromPreviousMonthAsync(EntryKind.Income, October));
        Assert.Equal(1, await _service.CopyFromPreviousMonthAsync(EntryKind.Saving, October));

        var summary = await _service.SummarizeAsync(October, carriesOver: false);
        Assert.Equal(305000, summary.IncomeTotal);
        Assert.Equal(20000, summary.SavingTotal);
        // September is left as it was.
        Assert.Equal(300000, (await _service.SummarizeAsync(September, carriesOver: false)).IncomeTotal);
    }

    [Fact]
    public async Task CountSavings_LeavesOutWithdrawals()
    {
        await _savings.AddAsync(new MonthlySaving { Date = September, Name = "旅行", Amount = 20000 });
        await _savings.AddAsync(new MonthlySaving { Date = September, Name = "旅行", Amount = -5000 });

        Assert.Equal(1, await _service.CountAsync(EntryKind.Saving, September));
        Assert.Equal(1, await _service.CopyFromPreviousMonthAsync(EntryKind.Saving, October));
        Assert.Equal(20000, (await _service.SummarizeAsync(October, carriesOver: false)).SavingTotal);
    }

    [Fact]
    public async Task DeleteBudget_DeletesEveryBudgetOfTheKindInTheMonth()
    {
        // Older versions could save a kind's budget twice in a month.
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Food, Amount = 40000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Food, Amount = 30000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Fun, Amount = 10000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = September, Category = Food, Amount = 20000 });

        var food = EntryService.Row((await _service.SummarizeAsync(October, false)).Budgets.Single(b => b.Category == Food));
        await _service.DeleteAsync(food);

        var october = await _service.GetBudgetsAsync(October);
        Assert.False(october.ContainsKey("食費"));
        Assert.Equal(10000, october["娯楽"]);
        Assert.Equal(20000, (await _service.GetBudgetsAsync(September))["食費"]);

        await _service.RestoreAsync(food);
        Assert.Equal(40000, (await _service.GetBudgetsAsync(October))["食費"]);
    }

    [Fact]
    public async Task SaveBudget_LeavesOneBudgetOfTheKindInTheMonth()
    {
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Food, Amount = 40000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Food, Amount = 30000 });

        var food = EntryService.Row((await _service.SummarizeAsync(October, false)).Budgets.Single(b => b.Category == Food));
        await _service.SaveAsync(food, Draft(25000));

        var budget = Assert.Single(_budgets.T.All());
        Assert.Equal(25000, budget.Amount);
    }

    [Fact]
    public async Task SaveBudgets_AddsChangesAndDeletesOneBudgetPerKind()
    {
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Food, Amount = 40000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Food, Amount = 30000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Fun, Amount = 10000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = September, Category = Fun, Amount = 9000 });

        await _service.SaveBudgetsAsync(October, new Dictionary<string, decimal> { ["食費"] = 45000, ["娯楽"] = 0 });

        var october = await _service.GetBudgetsAsync(October);
        Assert.Equal(new Dictionary<string, decimal> { ["食費"] = 45000 }, october);
        // One 食費 row is left, September is untouched.
        Assert.Equal(2, _budgets.T.All().Count());
        Assert.Equal(9000, (await _service.GetBudgetsAsync(September))["娯楽"]);
    }

    [Fact]
    public async Task SaveBudgets_AddsAKindWithoutABudget()
    {
        Assert.Equal(1, await _service.SaveBudgetsAsync(October, new Dictionary<string, decimal> { ["娯楽"] = 8000, ["食費"] = 0 }));

        Assert.Equal(new Dictionary<string, decimal> { ["娯楽"] = 8000 }, await _service.GetBudgetsAsync(October));
    }

    [Fact]
    public async Task CopyBudgets_AddsMissingAndChangesDifferentBudgets()
    {
        await _budgets.AddAsync(new MonthlyBudget { Date = September, Category = Food, Amount = 40000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = September, Category = Fun, Amount = 10000 });
        await _budgets.AddAsync(new MonthlyBudget { Date = October, Category = Fun, Amount = 8000 });

        Assert.Equal(2, await _service.CopyBudgetsFromPreviousMonthAsync(October));

        var october = await _service.GetBudgetsAsync(October);
        Assert.Equal(40000, october["食費"]);
        Assert.Equal(10000, october["娯楽"]);
        // 食費 was added; the 娯楽 row was changed, not added next to the one there was.
        Assert.Equal(4, _budgets.T.All().Count());
        // Already the same: nothing more to write.
        Assert.Equal(0, await _service.CopyBudgetsFromPreviousMonthAsync(October));
    }

    [Fact]
    public async Task Summarize_ReadsEveryRepository()
    {
        await _service.AddAsync(EntryKind.Income, Draft(300000, "給料"));
        await _service.AddAsync(EntryKind.Saving, Draft(20000, "旅行"));
        await _service.AddAsync(EntryKind.FixedCost, Draft(80000, "家賃"));
        await _service.AddAsync(EntryKind.Spending, Draft(5000, "スーパー", new DateTime(2026, 10, 3), "食費"));
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(30000, "旅行", new DateTime(2026, 10, 3)));

        var summary = await _service.SummarizeAsync(October, carriesOver: false);

        Assert.Equal(195000, summary.Remaining);
        Assert.Equal(30000, summary.SpecialExpenseTotal);
    }

    [Fact]
    public async Task Search_FindsEveryMonthNewestFirstWithTheYearInTheHeader()
    {
        await _service.AddAsync(EntryKind.Spending, Draft(1200, "ランチ", new DateTime(2025, 3, 4), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(900, "ランチ", new DateTime(2026, 10, 3), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(3000, "映画", new DateTime(2026, 10, 3), "娯楽"));

        var page = await _service.SearchAsync(EntryKind.Spending, "らんち");

        Assert.Equal(2, page.Count);
        Assert.Equal(2100, page.Total);
        Assert.Equal(["2026年10月3日（土）", "2025年3月4日（火）"], page.Groups.Select(g => g.Header));
    }

    [Fact]
    public async Task Search_InAPeriod_LooksOnlyAtThatPeriod_AndListsItAllWithoutWords()
    {
        await _service.AddAsync(EntryKind.Spending, Draft(1200, "ランチ", new DateTime(2026, 9, 30), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(900, "ランチ", new DateTime(2026, 10, 3), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(3000, "映画", new DateTime(2026, 10, 31), "娯楽"));
        var october = (new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        Assert.Equal(900, (await _service.SearchAsync(EntryKind.Spending, "ランチ", october)).Total);
        Assert.Equal(3900, (await _service.SearchAsync(EntryKind.Spending, "", october)).Total);
        // No words and no period: nothing yet.
        Assert.Equal(0, (await _service.SearchAsync(EntryKind.Spending, "")).Count);
    }

    [Fact]
    public async Task Search_InAPeriod_IncludesBothEndDaysAndNothingOutside()
    {
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(1, "前日", new DateTime(2026, 9, 30, 23, 59, 59)));
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(10, "初日", new DateTime(2026, 10, 1)));
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(100, "最終日", new DateTime(2026, 10, 31, 23, 59, 59)));
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(1000, "翌日", new DateTime(2026, 11, 1)));

        var page = await _service.SearchAsync(EntryKind.SpecialExpense, "", (new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)));

        Assert.Equal(2, page.Count);
        Assert.Equal(110, page.Total);
    }

    [Fact]
    public async Task Search_InAPeriodOfOneDay_FindsThatDayOnly()
    {
        await _service.AddAsync(EntryKind.Spending, Draft(500, "ランチ", new DateTime(2026, 10, 24), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(700, "ランチ", new DateTime(2026, 10, 25), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(900, "ランチ", new DateTime(2026, 10, 26), "食費"));
        var day = new DateTime(2026, 10, 25);

        Assert.Equal(700, (await _service.SearchAsync(EntryKind.Spending, "ランチ", (day, day))).Total);
    }

    [Fact]
    public async Task SaveBudgets_OneYenIsABudget_ZeroIsNone()
    {
        await _service.SaveBudgetsAsync(October, new Dictionary<string, decimal> { ["食費"] = 1, ["娯楽"] = 0 });

        Assert.Equal(new Dictionary<string, decimal> { ["食費"] = 1 }, await _service.GetBudgetsAsync(October));
    }

    [Fact]
    public async Task Search_OfOneCategory_InTheMonth_ListsThatKindOnly_AsFromTheHome()
    {
        await _service.AddAsync(EntryKind.Spending, Draft(500, "ランチ", new DateTime(2026, 10, 3), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(3000, "映画", new DateTime(2026, 10, 3), "娯楽"));
        await _service.AddAsync(EntryKind.Spending, Draft(700, "食費の本", new DateTime(2026, 10, 4), "娯楽"));
        await _service.AddAsync(EntryKind.Spending, Draft(900, "ランチ", new DateTime(2026, 9, 30), "食費"));
        var october = (new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        var food = await _service.SearchAsync(EntryKind.Spending, "", october, ["食費"]);

        // The kind itself, not every row with 食費 in its name; and the home's amount for 食費.
        Assert.Equal(500, food.Total);
        Assert.Equal((await _service.SummarizeAsync(October, false)).Budgets.Single(b => b.Category == Food).MonthlySpending, food.Total);
        // A kind with no period: every month of it.
        Assert.Equal(1400, (await _service.SearchAsync(EntryKind.Spending, "", null, ["食費"])).Total);
        // Words within the kind.
        Assert.Equal(700, (await _service.SearchAsync(EntryKind.Spending, "本", october, ["娯楽"])).Total);
    }

    [Fact]
    public async Task Search_OfSeveralCategories_ListsAnyOfThem()
    {
        await _service.AddAsync(EntryKind.Spending, Draft(500, "ランチ", new DateTime(2026, 10, 3), "食費"));
        await _service.AddAsync(EntryKind.Spending, Draft(3000, "映画", new DateTime(2026, 10, 3), "娯楽"));
        await _service.AddAsync(EntryKind.Spending, Draft(6000, "ガソリン", new DateTime(2026, 10, 4), "車"));
        var october = (new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        Assert.Equal(3500, (await _service.SearchAsync(EntryKind.Spending, "", october, ["食費", "娯楽"])).Total);
        // Every kind chosen is every row; none chosen is no kind filter.
        Assert.Equal(9500, (await _service.SearchAsync(EntryKind.Spending, "", october, ["食費", "娯楽", "車"])).Total);
        Assert.Equal(9500, (await _service.SearchAsync(EntryKind.Spending, "", october, [])).Total);
        // With no period and no kinds and no words: nothing yet.
        Assert.Equal(0, (await _service.SearchAsync(EntryKind.Spending, "", null, [])).Count);
        // A kind no row has.
        Assert.Equal(0, (await _service.SearchAsync(EntryKind.Spending, "", october, ["医療"])).Count);
    }

    [Fact]
    public async Task Search_OfTheMonth_IsTheHomesSpecialExpenseTotal()
    {
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(30000, "旅行", new DateTime(2026, 10, 16)));
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(50000, "旅行", new DateTime(2026, 3, 20)));

        var page = await _service.SearchAsync(EntryKind.SpecialExpense, "", (new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)));

        Assert.Equal((await _service.SummarizeAsync(October, false)).SpecialExpenseTotal, page.Total);
    }

    [Fact]
    public async Task Search_LooksOnlyAtTheKind()
    {
        await _service.AddAsync(EntryKind.Spending, Draft(5000, "旅行の準備", October, "日用品"));
        await _service.AddAsync(EntryKind.SpecialExpense, Draft(30000, "旅行", new DateTime(2024, 8, 10)));

        var page = await _service.SearchAsync(EntryKind.SpecialExpense, "旅行");

        Assert.Equal(1, page.Count);
        Assert.Equal(30000, page.Total);
    }
}
