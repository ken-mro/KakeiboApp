using KakeiboApp.Models;
using SQLite;

namespace KakeiboApp.Repository;

public class MonthlyIncomeDataRepository : IMonthlyIncomeDataRepository
{
    private readonly string _dbPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        if (_conn != null)
            return;
        _conn = new SQLiteAsyncConnection(_dbPath);
        await _conn.CreateTableAsync<MonthlyIncomeData>();
    }

    public async Task<IEnumerable<MonthlyIncome>> GetAllAsync()
    {
        await Init();
        var incomeDataList = await _conn!.Table<MonthlyIncomeData>().ToListAsync();
        return incomeDataList.Select(ConvertData).ToList();
    }

    public async Task<MonthlyIncome> GetByIdAsync(int id)
    {
        await Init();
        var incomeData = await _conn!.Table<MonthlyIncomeData>()
            .Where(i => i.Id == id)
            .FirstOrDefaultAsync();
        return ConvertData(incomeData);
    }

    public async Task<int> AddAsync(MonthlyIncome income)
    {
        await Init();
        var incomeData = ConvertToData(income);
        return await _conn!.InsertAsync(incomeData);
    }

    public async Task<int> UpdateAsync(MonthlyIncome income)
    {
        await Init();
        var incomeData = ConvertToData(income);
        return await _conn!.UpdateAsync(incomeData);
    }

    public async Task<int> DeleteAsync(int id)
    {
        await Init();
        return await _conn!.Table<MonthlyIncomeData>().Where(i => i.Id.Equals(id)).DeleteAsync();
    }

    private MonthlyIncome ConvertData(MonthlyIncomeData weeklyBudget)
    {
        return new MonthlyIncome()
        {
            Id = weeklyBudget.Id,
            Date = weeklyBudget.Date,
            Name = weeklyBudget.Name,
            Amount = weeklyBudget.Amount,
            Note = weeklyBudget.Note
        };
    }

    private MonthlyIncomeData ConvertToData(MonthlyIncome MonthlyBudgetData)
    {
        return new MonthlyIncomeData()
        {
            Id = MonthlyBudgetData.Id,
            Date = MonthlyBudgetData.Date,
            Name = MonthlyBudgetData.Name,
            Amount = MonthlyBudgetData.Amount,
            Note = MonthlyBudgetData.Note
        };
    }
}
