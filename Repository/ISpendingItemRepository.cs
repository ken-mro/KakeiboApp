using KakeiboApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KakeiboApp.Repository;

public interface ISpendingItemRepository
{
    Task<IEnumerable<SpendingItem>> GetAllAsync();
    Task<SpendingItem> GetByIdAsync(int id);
    Task<int> AddAsync(SpendingItem item);
    Task<int> UpdateAsync(SpendingItem item);
    Task<int> DeleteAsync(int id);
}
