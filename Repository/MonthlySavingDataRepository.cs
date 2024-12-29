using KakeiboApp.Models;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KakeiboApp.Repository;

public class MonthlySavingDataRepository : IMonthlySavingDataRepository
{
    private string _dpPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        if (_conn != null)
            return;
        _conn = new SQLiteAsyncConnection(_dpPath);
        await _conn.CreateTableAsync<MonthlySavingData>();
    }

    public async Task<IEnumerable<MonthlySaving>> GetAllSavingsAsync()
    {
        await Init();
        var savingData = await _conn!.Table<MonthlySavingData>().ToListAsync();
        return savingData.Select(ConvertData).ToList();
    }

    public async Task<MonthlySaving> GetSavingByIdAsync(int id)
    {
        await Init();
        var savingData = await _conn!.Table<MonthlySavingData>()
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();
        return ConvertData(savingData);
    }

    public async Task<int> AddSavingAsync(MonthlySaving saving)
    {
        await Init();
        var savingData = ConvertToData(saving);
        return await _conn!.InsertAsync(savingData);
    }

    public async Task<int> UpdateSavingAsync(MonthlySaving saving)
    {
        await Init();
        var savingData = ConvertToData(saving);
        return await _conn!.UpdateAsync(savingData);
    }

    public async Task<int> DeleteSavingAsync(int id)
    {
        await Init();
        return await _conn!.Table<MonthlySavingData>().Where(i => i.Id.Equals(id)).DeleteAsync();
    }

    private MonthlySaving ConvertData(MonthlySavingData savingData)
    {
        return new MonthlySaving()
        {
            Id = savingData.Id,
            Date = savingData.Date,
            Name = savingData.Name,
            Amount = savingData.Amount,
            Note = savingData.Note
        };
    }

    private MonthlySavingData ConvertToData(MonthlySaving saving)
    {
        return new MonthlySavingData()
        {
            Id = saving.Id,
            Date = saving.Date,
            Name = saving.Name,
            Amount = saving.Amount,
            Note = saving.Note
        };
    }
}
