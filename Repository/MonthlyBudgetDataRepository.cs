using KakeiboApp.Models;
using SQLite;

namespace KakeiboApp.Repository;

public class MonthlyBudgetDataRepository : IMonthlyBudgetDataRepository
{
    private string _dpPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        if (_conn != null)
            return;
        _conn = new SQLiteAsyncConnection(_dpPath);
        await _conn.CreateTableAsync<MonthlyBudgetData>();
    }

    public async Task<IEnumerable<MonthlyBudget>> GetAllAsync()
    {
        await Init();
        var monthlyBudgetDataList = await _conn!.Table<MonthlyBudgetData>().ToListAsync();
        return monthlyBudgetDataList.Select(ConvertData).ToList();
    }
    
    public async Task<MonthlyBudget> GetByIdAsync(int id)
    {
        await Init();
        var budget = await _conn!.Table<MonthlyBudgetData>().Where(b => b.Id == id).FirstOrDefaultAsync();
        return ConvertData(budget);
    }

    public async Task<int> AddAsync(MonthlyBudget budget)
    {
        await Init();
        return await _conn!.InsertAsync(ConvertToData(budget));
    }

    public async Task<int> UpdateAsync(MonthlyBudget budget)
    {
        await Init();
        return await _conn!.UpdateAsync(ConvertToData(budget));
    }

    public async Task<int> DeleteAsync(int id)
    {
        await Init();
        return await _conn!.DeleteAsync<MonthlyBudgetData>(id);
    }

    private MonthlyBudget ConvertData(MonthlyBudgetData monthlyBudget)    
    {
        return new MonthlyBudget()
        {
            Id = monthlyBudget.Id,
            Date = monthlyBudget.Date,
            Category = new() { Name = monthlyBudget.Category },
            Amount = monthlyBudget.Amount
        };
    }

    private MonthlyBudgetData ConvertToData(MonthlyBudget monthlyBudgetData)
    {
        return new MonthlyBudgetData()
        {
            Id = monthlyBudgetData.Id,
            Date = monthlyBudgetData.Date,
            Category = monthlyBudgetData.Category.Name,
            Amount = monthlyBudgetData.Amount
        };
    }
}
