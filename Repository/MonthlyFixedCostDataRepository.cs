using KakeiboApp.Models;
using SQLite;

namespace KakeiboApp.Repository;

public class MonthlyFixedCostDataRepository : IMonthlyFixedCostDataRepository
{
    private string _dpPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        if (_conn != null)
            return;
        _conn = new SQLiteAsyncConnection(_dpPath);
        await _conn.CreateTableAsync<MonthlyFixedCostData>();
    }

    public async Task<IEnumerable<MonthlyFixedCost>> GetAllFixedCostsAsync()
    {
        await Init();
        var fixedCostData = await _conn!.Table<MonthlyFixedCostData>().ToListAsync();
        return fixedCostData.Select(ConvertData).ToList();
    }

    public async Task<MonthlyFixedCost> GetFixedCostByIdAsync(int id)
    {
        await Init();
        var fixedCostData = await _conn!.Table<MonthlyFixedCostData>()
            .Where(f => f.Id == id)
            .FirstOrDefaultAsync();
        return ConvertData(fixedCostData);
    }

    public async Task<int> AddFixedCostAsync(MonthlyFixedCost fixedCost)
    {
        await Init();
        var fixedCostData = ConvertToData(fixedCost);
        return await _conn!.InsertAsync(fixedCostData);
    }

    public async Task<int> UpdateFixedCostAsync(MonthlyFixedCost fixedCost)
    {
        await Init();
        var fixedCostData = ConvertToData(fixedCost);
        return await _conn!.UpdateAsync(fixedCostData);
    }

    public async Task<int> DeleteFixedCostAsync(int id)
    {
        await Init();
        return await _conn!.Table<MonthlyFixedCostData>().Where(i => i.Id.Equals(id)).DeleteAsync();
    }

    private MonthlyFixedCost ConvertData(MonthlyFixedCostData fixedCostData)
    {
        return new MonthlyFixedCost()
        {
            Id = fixedCostData.Id,
            Date = fixedCostData.Date,
            Name = fixedCostData.Name,
            Amount = fixedCostData.Amount,
            Note = fixedCostData.Note
        };
    }

    private MonthlyFixedCostData ConvertToData(MonthlyFixedCost MonthlyFixedCostData)
    {
        return new MonthlyFixedCostData()
        {
            Id = MonthlyFixedCostData.Id,
            Date = MonthlyFixedCostData.Date,
            Name = MonthlyFixedCostData.Name,
            Amount = MonthlyFixedCostData.Amount,
            Note = MonthlyFixedCostData.Note
        };
    }
}
