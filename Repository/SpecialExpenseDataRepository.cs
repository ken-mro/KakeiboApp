using KakeiboApp.Models;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KakeiboApp.Repository;

public class SpecialExpenseDataRepository : ISpecialExpenseDataRepository
{
    private string _dpPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        if (_conn != null)
            return;
        _conn = new SQLiteAsyncConnection(_dpPath);
        await _conn.CreateTableAsync<SpecialExpenseData>();
    }

    public async Task<IEnumerable<SpecialExpense>> GetAllAsync()
    {
        await Init();
        var specialExpenseData = await _conn.Table<SpecialExpenseData>().ToListAsync();
        return specialExpenseData.Select(ConvertData).ToList();
    }

    public async Task<SpecialExpense> GetByIdAsync(int id)
    {
        await Init();
        var specialExpenseData = await _conn.Table<SpecialExpenseData>().FirstOrDefaultAsync(e => e.Id.Equals(id));
        return ConvertData(specialExpenseData);
    }

    public async Task<int> AddAsync(SpecialExpense SpecialExpense)
    {
        await Init();
        var specialExpenseData = ConvertToData(SpecialExpense);
        return await _conn.InsertAsync(specialExpenseData);
    }

    public async Task<int> UpdateAsync(SpecialExpense SpecialExpense)
    {
        await Init();
        var specialExpenseData = ConvertToData(SpecialExpense);
        return await _conn.UpdateAsync(specialExpenseData);
    }

    public async Task<int> DeleteAsync(int id)
    {
        await Init();
        return await _conn!.Table<SpecialExpenseData>().Where(i => i.Id.Equals(id)).DeleteAsync();
    }

    private SpecialExpense ConvertData(SpecialExpenseData specialExpenseData)
    {
        return new SpecialExpense()
        {
            Id = specialExpenseData.Id,
            Date = specialExpenseData.Date,
            Name = specialExpenseData.Name,
            Amount = specialExpenseData.Amount,
            FromWhere = specialExpenseData.FromWhere,
            Note = specialExpenseData.Note
        };
    }

    private SpecialExpenseData ConvertToData(SpecialExpense specialExpense)
    {
        return new SpecialExpenseData()
        {
            Id = specialExpense.Id,
            Date = specialExpense.Date,
            Name = specialExpense.Name,
            Amount = specialExpense.Amount,
            FromWhere = specialExpense.FromWhere,
            Note = specialExpense.Note
        };
    }
}
