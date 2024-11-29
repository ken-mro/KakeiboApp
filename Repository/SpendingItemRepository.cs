using KakeiboApp.Models;
using SQLite;

namespace KakeiboApp.Repository;

public class SpendingItemRepository : ISpendingItemRepository
{
    private string _dbPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        if (_conn != null)
            return;

        _conn = new SQLiteAsyncConnection(_dbPath);
        await _conn.CreateTableAsync<SpendingItemData>();
    }

    public async Task<IEnumerable<SpendingItem>> GetAllAsync()
    {
        await Init();
        var itemDataList = await _conn!.Table<SpendingItemData>().ToListAsync();
        return itemDataList.Select(ConvertData).ToList();
    }

    public async Task<SpendingItem> GetByIdAsync(int id)
    {
        await Init();        
        var itemData = await _conn!.Table<SpendingItemData>().Where(i => i.Id == id).FirstOrDefaultAsync();
        return ConvertData(itemData);
    }

    public async Task<int> AddAsync(SpendingItem item)
    {
        await Init();
        var itemData = ConvertToData(item);
        return await _conn!.InsertAsync(itemData);
    }

    public async Task<int> UpdateAsync(SpendingItem item)
    {
        await Init();
        var itemData = ConvertToData(item);
        return await _conn!.UpdateAsync(itemData);
    }

    public async Task<int> DeleteAsync(int id)
    {
        await Init();
        return await _conn!.Table<SpendingItemData>().Where(i => i.Id == id).DeleteAsync();
    }

    private SpendingItemData ConvertToData(SpendingItem item)
    {
        return new SpendingItemData
        {
            Date = item.Date,
            Category = item.Category.Name,
            Name = item.Name,
            Amount = item.Amount
        };
    }

    private SpendingItem ConvertData(SpendingItemData item)
    {
        return new SpendingItem
        {
            Id = item.Id,
            Date = item.Date,
            Category = new() { Name = item.Category },
            Name = item.Name,
            Amount = item.Amount
        };
    }
}
