using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category> GetByIdAsync(string name);
    Task<int> AddAsync(Category category);
    Task<int> UpdateAsync(Category category);
    Task<int> DeleteAsync(string name);
}
