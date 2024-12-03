using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface IMonthlyBudgetDataRepository
{
    Task<IEnumerable<MonthlyBudget>> GetAllBudgetsAsync();
    Task<MonthlyBudget> GetBudgetByIdAsync(int id);
    Task<int> AddBudgetAsync(MonthlyBudget budget);
    Task<int> UpdateBudgetAsync(MonthlyBudget budget);
    Task<int> DeleteBudgetAsync(int id);
}
