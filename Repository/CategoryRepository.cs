using KakeiboApp.Models;
using SQLite;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KakeiboApp.Repository;

public class CategoryRepository : ICategoryRepository
{
    private string _dbPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        try
        {
            if (_conn != null)
                return;

            _conn = new SQLiteAsyncConnection(_dbPath);
            await _conn.CreateTableAsync<CategoryData>();

            // Check if the table is empty and initialize with default data
            var count = await _conn.Table<CategoryData>().CountAsync();

            if (count == 0)
            {
                await InitializeDefaultData();
            }
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }

    private async Task InitializeDefaultData()
    {
        var defaultCategories = new List<CategoryData>
        {
            new CategoryData { Name = "�H��" },
            new CategoryData { Name = "���p�i�E�G��" },
            new CategoryData { Name = "��y" },
            new CategoryData { Name = "��" },
            new CategoryData { Name = "���" },
            new CategoryData { Name = "����" }
        };

        await _conn!.InsertAllAsync(defaultCategories);
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        await Init();
        var categoryDataList = await _conn!.Table<CategoryData>().ToListAsync();
        return categoryDataList.Select(ConvertData).ToList();
    }

    public async Task<Category> GetByIdAsync(string name)
    {
        await Init();
        var categoryData = await _conn!.Table<CategoryData>().Where(c => c.Name.Equals(name)).FirstOrDefaultAsync();
        return ConvertData(categoryData);
    }

    public async Task<int> AddAsync(Category category)
    {
        await Init();
        var categoryData = ConvertToData(category);
        return await _conn!.InsertAsync(categoryData);
    }

    public async Task<int> UpdateAsync(Category category)
    {
        await Init();
        var categoryData = ConvertToData(category);
        return await _conn!.UpdateAsync(categoryData);
    }

    public async Task<int> DeleteAsync(string name)
    {
        await Init();
        return await _conn!.Table<CategoryData>().Where(c => c.Name.Equals(name)).DeleteAsync();
    }

    private CategoryData ConvertToData(Category category)
    {
        return new CategoryData
        {
            Name = category.Name
        };
    }

    private Category ConvertData(CategoryData categoryData)
    {
        return new Category
        {
            Name = categoryData.Name
        };
    }
}
