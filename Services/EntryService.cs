using System.Globalization;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.ViewModels.Entries;

namespace KakeiboApp.Services;

/// <summary>
/// A page of the shared list: the rows, grouped, with the count and total shown above them.
/// </summary>
public record EntryPage(IReadOnlyList<EntryGroup> Groups, int Count, decimal Total);

/// <summary>
/// Loads, adds, updates, deletes and restores every kind of entry through its repository, so the list
/// and the sheet do not need to know which table they edit. Totals come from BudgetCalculator.
/// </summary>
public class EntryService(
    IMonthlyIncomeDataRepository incomes,
    IMonthlySavingDataRepository savings,
    IMonthlyFixedCostDataRepository fixedCosts,
    ISpendingItemRepository spendingItems,
    ISpecialExpenseDataRepository specialExpenses,
    IMonthlyBudgetDataRepository budgets,
    ICategoryRepository categories)
{
    static readonly CultureInfo Japanese = new("ja-JP");

    public static string DayHeader(DateTime date) => date.ToString("M月d日（ddd）", Japanese);

    public Task<IEnumerable<Category>> GetCategoriesAsync() => categories.GetAllAsync();

    /// <summary>Every amount of the home page for <paramref name="month"/>.</summary>
    public async Task<MonthSummary> SummarizeAsync(DateTime month, bool carriesOver) =>
        BudgetCalculator.Summarize(month, carriesOver,
            await incomes.GetAllAsync(),
            await savings.GetAllAsync(),
            await fixedCosts.GetAllAsync(),
            await categories.GetAllAsync(),
            await budgets.GetAllAsync(),
            await spendingItems.GetAllAsync(),
            await specialExpenses.GetAllAsync());

    public async Task<EntryPage> LoadAsync(EntryKind kind, DateTime month)
    {
        switch (kind)
        {
            case EntryKind.Income:
            {
                var items = BudgetCalculator.InMonth(await incomes.GetAllAsync(), x => x.Date, month);
                return Undated(items.Select(Row), BudgetCalculator.Total(items, x => x.Amount));
            }
            case EntryKind.Saving:
            {
                var items = BudgetCalculator.SavingsInMonth(await savings.GetAllAsync(), month);
                return Undated(items.Select(Row), BudgetCalculator.Total(items, x => x.Amount));
            }
            case EntryKind.FixedCost:
            {
                var items = BudgetCalculator.InMonth(await fixedCosts.GetAllAsync(), x => x.Date, month);
                return Undated(items.Select(Row), BudgetCalculator.Total(items, x => x.Amount));
            }
            case EntryKind.Spending:
            {
                var items = BudgetCalculator.InMonth(await spendingItems.GetAllAsync(), x => x.Date, month);
                var groups = BudgetCalculator.GroupByDay(items, x => x.Date, x => x.Id)
                    .Select(g => new EntryGroup(DayHeader(g.Key), g.Select(Row))).ToList();
                return new EntryPage(groups, items.Count, BudgetCalculator.Total(items, x => x.Amount));
            }
            case EntryKind.SpecialExpense:
            {
                var items = (await specialExpenses.GetAllAsync()).Where(x => x.Date.Year == month.Year).ToList();
                var groups = BudgetCalculator.GroupByDay(items, x => x.Date, x => x.Id)
                    .Select(g => new EntryGroup(DayHeader(g.Key), g.Select(Row))).ToList();
                return new EntryPage(groups, items.Count, BudgetCalculator.SpecialExpenseTotalInYear(items, month.Year));
            }
            case EntryKind.SavingBalance:
            {
                var balances = BudgetCalculator.SavingsUntilPreviousMonth(await savings.GetAllAsync(), month);
                return Undated(balances.Select(Row), BudgetCalculator.Total(balances, x => x.Balance));
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Budgets are edited from the home page.");
        }
    }

    /// <summary>Search results span every month, so their day headers carry the year.</summary>
    public static string SearchDayHeader(DateTime date) => date.ToString("yyyy年M月d日（ddd）", Japanese);

    /// <summary>
    /// The 生活費 or 特別出費 that match <paramref name="query"/> (see <see cref="EntrySearch"/>), of every month or
    /// only of <paramref name="range"/>, and of every kind or only of <paramref name="categories"/> (生活費; empty is
    /// every kind). With a range or kinds and no words, every row of them.
    /// </summary>
    public async Task<EntryPage> SearchAsync(
        EntryKind kind, string query, (DateTime From, DateTime To)? range = null, IReadOnlyCollection<string>? categories = null)
    {
        var kinds = categories is { Count: > 0 } ? categories : null;
        var terms = EntrySearch.Terms(query);
        var rows = kind switch
        {
            EntryKind.Spending => (await spendingItems.GetAllAsync()).Select(Row),
            EntryKind.SpecialExpense => (await specialExpenses.GetAllAsync()).Select(Row),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only 生活費 and 特別出費 can be searched."),
        };
        var matches = rows
            .Where(r => EntrySearch.InRange(r.Date, range))
            .Where(r => kinds is null || (r.Category is { } c && kinds.Contains(c)))
            .Where(r => terms.Count == 0 ? range is not null || kinds is not null : EntrySearch.Matches(r, terms))
            .ToList();
        var groups = BudgetCalculator.GroupByDay(matches, x => x.Date, x => x.Id)
            .Select(g => new EntryGroup(SearchDayHeader(g.Key), g)).ToList();
        return new EntryPage(groups, matches.Count, BudgetCalculator.Total(matches, x => x.Amount));
    }

    static EntryPage Undated(IEnumerable<EntryRow> rows, decimal total)
    {
        var list = rows.ToList();
        return new EntryPage(list.Count == 0 ? [] : [new EntryGroup(string.Empty, list)], list.Count, total);
    }

    public static EntryRow Row(MonthlyIncome x) => new()
    {
        Kind = EntryKind.Income, Id = x.Id, Date = x.Date, Name = x.Name, Note = x.Note ?? string.Empty, Amount = x.Amount, Source = x,
    };

    public static EntryRow Row(MonthlySaving x) => new()
    {
        Kind = EntryKind.Saving, Id = x.Id, Date = x.Date, Name = x.Name ?? string.Empty, Note = x.Note ?? string.Empty, Amount = x.Amount, Source = x,
    };

    public static EntryRow Row(MonthlyFixedCost x) => new()
    {
        Kind = EntryKind.FixedCost, Id = x.Id, Date = x.Date, Name = x.Name, Note = x.Note ?? string.Empty, Amount = x.Amount, Source = x,
    };

    public static EntryRow Row(SpendingItem x) => new()
    {
        Kind = EntryKind.Spending, Id = x.Id, Date = x.Date, Name = x.Name, Note = x.Note ?? string.Empty, Amount = x.Amount,
        Category = x.Category?.Name, Source = x,
    };

    public static EntryRow Row(SpecialExpense x) => new()
    {
        Kind = EntryKind.SpecialExpense, Id = x.Id, Date = x.Date, Name = x.Name, Note = x.Note ?? string.Empty, Amount = x.Amount,
        FromWhere = x.FromWhere ?? string.Empty, Source = x,
    };

    public static EntryRow Row(SavingResult x) => new()
    {
        Kind = EntryKind.SavingBalance, Id = x.LivingOffSaving.Id, Date = x.LivingOffSaving.Date, Name = x.Name,
        Amount = x.Amount, Secondary = x.LivingOffSavingAmount, Source = x,
    };

    public static EntryRow Row(BudgetControlResult x) => new()
    {
        Kind = EntryKind.Budget, Id = x.MonthlyBudget.Id, Date = x.MonthlyBudget.Date, Category = x.Category.Name,
        Amount = x.MonthlyBudget.Amount, Secondary = x.MonthlySpending, Source = x,
    };

    public async Task AddAsync(EntryKind kind, EntryDraft d)
    {
        var result = kind switch
        {
            EntryKind.Income => await incomes.AddAsync(new MonthlyIncome { Date = d.Date, Name = d.Name, Amount = d.Amount, Note = d.Note }),
            EntryKind.Saving => await savings.AddAsync(new MonthlySaving { Date = d.Date, Name = d.Name, Amount = d.Amount, Note = d.Note }),
            EntryKind.FixedCost => await fixedCosts.AddAsync(new MonthlyFixedCost { Date = d.Date, Name = d.Name, Amount = d.Amount, Note = d.Note }),
            EntryKind.Spending => await spendingItems.AddAsync(new SpendingItem
            {
                Date = d.Date, Category = new Category { Name = d.Category ?? string.Empty }, Name = d.Name, Amount = d.Amount, Note = d.Note,
            }),
            EntryKind.SpecialExpense => await specialExpenses.AddAsync(new SpecialExpense
            {
                Date = d.Date, Name = d.Name, Amount = d.Amount, FromWhere = d.FromWhere, Note = d.Note,
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Use SaveAsync for withdrawals and budgets."),
        };
        EnsureWritten(result);
    }

    /// <summary>Saves the sheet for an existing row. For withdrawals and budgets this adds the month's record when there is none yet.</summary>
    public async Task SaveAsync(EntryRow row, EntryDraft d)
    {
        int result;
        switch (row.Source)
        {
            case MonthlyIncome x:
                x.Name = d.Name; x.Amount = d.Amount; x.Note = d.Note;
                result = await incomes.UpdateAsync(x);
                break;
            case MonthlySaving x:
                x.Name = d.Name; x.Amount = d.Amount; x.Note = d.Note;
                result = await savings.UpdateAsync(x);
                break;
            case MonthlyFixedCost x:
                x.Name = d.Name; x.Amount = d.Amount; x.Note = d.Note;
                result = await fixedCosts.UpdateAsync(x);
                break;
            case SpendingItem x:
                x.Date = d.Date; x.Category = new Category { Name = d.Category ?? x.Category?.Name ?? string.Empty };
                x.Name = d.Name; x.Amount = d.Amount; x.Note = d.Note;
                result = await spendingItems.UpdateAsync(x);
                break;
            case SpecialExpense x:
                x.Date = d.Date; x.Name = d.Name; x.Amount = d.Amount; x.FromWhere = d.FromWhere; x.Note = d.Note;
                result = await specialExpenses.UpdateAsync(x);
                break;
            case SavingResult x:
                if (d.Amount < 0 || x.Amount < d.Amount) throw new ArgumentOutOfRangeException(nameof(d), "切り崩しは貯金額までです。");
                x.SetLivingOffSavingAmount(d.Amount);
                result = x.LivingOffSaving.Id == 0
                    ? await savings.AddAsync(x.LivingOffSaving)
                    : await savings.UpdateAsync(x.LivingOffSaving);
                break;
            case BudgetControlResult x:
                x.MonthlyBudget.Amount = d.Amount;
                result = x.MonthlyBudget.Id == 0
                    ? await budgets.AddAsync(x.MonthlyBudget)
                    : await budgets.UpdateAsync(x.MonthlyBudget);
                // Older versions could save a kind's budget twice in a month; only the one saved here stays.
                if (x.MonthlyBudget.Id != 0) await DeleteBudgetsAsync(x.MonthlyBudget, keepId: x.MonthlyBudget.Id);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(row));
        }
        EnsureWritten(result);
    }

    public async Task DeleteAsync(EntryRow row)
    {
        var result = row.Source switch
        {
            MonthlyIncome x => await incomes.DeleteAsync(x.Id),
            MonthlySaving x => await savings.DeleteAsync(x.Id),
            MonthlyFixedCost x => await fixedCosts.DeleteAsync(x.Id),
            SpendingItem x => await spendingItems.DeleteAsync(x.Id),
            SpecialExpense x => await specialExpenses.DeleteAsync(x.Id),
            SavingResult x => await savings.DeleteAsync(x.LivingOffSaving.Id),
            BudgetControlResult x => await DeleteBudgetsAsync(x.MonthlyBudget, keepId: null),
            _ => throw new ArgumentOutOfRangeException(nameof(row)),
        };
        EnsureWritten(result);
    }

    /// <summary>
    /// Deletes every budget of <paramref name="budget"/>'s kind in its month but <paramref name="keepId"/>. Older
    /// versions could save a kind's budget twice in a month: deleting only the one shown left the other in its place.
    /// </summary>
    async Task<int> DeleteBudgetsAsync(MonthlyBudget budget, int? keepId)
    {
        var deleted = 0;
        foreach (var same in BudgetCalculator.SameBudgets(await budgets.GetAllAsync(), budget))
        {
            if (same.Id != keepId) deleted += await budgets.DeleteAsync(same.Id);
        }
        return deleted;
    }

    /// <summary>Undoes <see cref="DeleteAsync"/> by adding the row back as a new record with the same values.</summary>
    public async Task RestoreAsync(EntryRow row)
    {
        var result = row.Source switch
        {
            MonthlyIncome x => await incomes.AddAsync(x),
            MonthlySaving x => await savings.AddAsync(x),
            MonthlyFixedCost x => await fixedCosts.AddAsync(x),
            SpendingItem x => await spendingItems.AddAsync(x),
            SpecialExpense x => await specialExpenses.AddAsync(x),
            SavingResult x => await savings.AddAsync(new MonthlySaving
            {
                Date = x.LivingOffSaving.Date, Name = x.Name, Amount = -row.Secondary, Note = x.LivingOffSaving.Note ?? string.Empty,
            }),
            BudgetControlResult x => await budgets.AddAsync(new MonthlyBudget
            {
                Date = x.MonthlyBudget.Date, Category = x.Category, Amount = row.Amount,
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(row)),
        };
        EnsureWritten(result);
    }

    /// <summary>The kinds of 今月使えるお金 that can be copied from the previous month, in the order of the home card.</summary>
    public static readonly IReadOnlyList<EntryKind> CopyableKinds = [EntryKind.Income, EntryKind.Saving, EntryKind.FixedCost];

    /// <summary>
    /// Copies the previous month's 収入, 貯金 (not withdrawals) or 固定費 into <paramref name="month"/> as new rows,
    /// next to any the month already has. Returns how many were copied.
    /// </summary>
    public async Task<int> CopyFromPreviousMonthAsync(EntryKind kind, DateTime month)
    {
        switch (kind)
        {
            case EntryKind.Income:
            {
                var copies = BudgetCalculator.CopyIncomesFromPreviousMonth(await incomes.GetAllAsync(), month);
                foreach (var copy in copies) EnsureWritten(await incomes.AddAsync(copy));
                return copies.Count;
            }
            case EntryKind.Saving:
            {
                var copies = BudgetCalculator.CopySavingsFromPreviousMonth(await savings.GetAllAsync(), month);
                foreach (var copy in copies) EnsureWritten(await savings.AddAsync(copy));
                return copies.Count;
            }
            case EntryKind.FixedCost:
            {
                var copies = BudgetCalculator.CopyFixedCostsFromPreviousMonth(await fixedCosts.GetAllAsync(), month);
                if (copies.Count == 0) return 0;
                EnsureWritten(await fixedCosts.AddAsync(copies));
                return copies.Count;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only 収入, 貯金 and 固定費 are copied.");
        }
    }

    /// <summary>How many rows of 収入, 貯金 (not withdrawals) or 固定費 <paramref name="month"/> has: what a copy from it would add.</summary>
    public async Task<int> CountAsync(EntryKind kind, DateTime month) => kind switch
    {
        EntryKind.Income => BudgetCalculator.InMonth(await incomes.GetAllAsync(), x => x.Date, month).Count,
        EntryKind.Saving => BudgetCalculator.SavingsInMonth(await savings.GetAllAsync(), month).Count,
        EntryKind.FixedCost => BudgetCalculator.InMonth(await fixedCosts.GetAllAsync(), x => x.Date, month).Count,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only 収入, 貯金 and 固定費 are counted."),
    };

    /// <summary>Each kind's budget in <paramref name="month"/>, kinds without one left out.</summary>
    public async Task<Dictionary<string, decimal>> GetBudgetsAsync(DateTime month) =>
        BudgetCalculator.BudgetsInMonth(await categories.GetAllAsync(), await budgets.GetAllAsync(), month);

    /// <summary>
    /// 予算を決める: each kind's budget in <paramref name="month"/> as given, one row per kind. A kind given 0
    /// (left blank) has its budget deleted; kinds not given are left as they are. Returns how many rows were written.
    /// </summary>
    public async Task<int> SaveBudgetsAsync(DateTime month, IReadOnlyDictionary<string, decimal> amounts)
    {
        var all = (await budgets.GetAllAsync()).ToList();
        var written = 0;
        foreach (var (name, amount) in amounts)
        {
            var category = new Category { Name = name };
            var same = BudgetCalculator.SameBudgets(all, new MonthlyBudget { Date = month, Category = category });
            if (amount <= 0)
            {
                foreach (var budget in same) EnsureWritten(await budgets.DeleteAsync(budget.Id));
                written += same.Count;
                continue;
            }
            if (same.Count == 1 && same[0].Amount == amount) continue;
            var keep = same.FirstOrDefault();
            if (keep is null)
            {
                EnsureWritten(await budgets.AddAsync(new MonthlyBudget { Date = month, Category = category, Amount = amount }));
            }
            else
            {
                keep.Amount = amount;
                EnsureWritten(await budgets.UpdateAsync(keep));
                foreach (var extra in same.Skip(1)) EnsureWritten(await budgets.DeleteAsync(extra.Id));
            }
            written++;
        }
        return written;
    }

    /// <summary>
    /// Sets every kind's budget in <paramref name="month"/> to the previous month's, where the previous month has one.
    /// Kinds without a budget then keep theirs. Returns how many budgets were added or changed.
    /// </summary>
    public async Task<int> CopyBudgetsFromPreviousMonthAsync(DateTime month)
    {
        var writes = BudgetCalculator.CopyBudgetsFromPreviousMonth(await categories.GetAllAsync(), await budgets.GetAllAsync(), month);
        foreach (var budget in writes)
        {
            EnsureWritten(budget.Id == 0 ? await budgets.AddAsync(budget) : await budgets.UpdateAsync(budget));
        }
        return writes.Count;
    }

    static void EnsureWritten(int rows)
    {
        if (rows == 0) throw new IOException("保存できませんでした。");
    }
}
