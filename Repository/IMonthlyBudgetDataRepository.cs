using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface IMonthlyBudgetDataRepository
{
    Task<IEnumerable<MonthlyBudget>> GetAllAsync();
    Task<MonthlyBudget> GetByIdAsync(int id);
    Task<int> AddAsync(MonthlyBudget budget);
    Task<int> UpdateAsync(MonthlyBudget budget);
    Task<int> DeleteAsync(int id);
}
