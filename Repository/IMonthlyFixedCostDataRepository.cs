using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface IMonthlyFixedCostDataRepository
{
    Task<IEnumerable<MonthlyFixedCost>> GetAllFixedCostsAsync();
    Task<MonthlyFixedCost> GetFixedCostByIdAsync(int id);
    Task<int> AddFixedCostAsync(MonthlyFixedCost fixedCost);
    Task<int> UpdateFixedCostAsync(MonthlyFixedCost fixedCost);
    Task<int> DeleteFixedCostAsync(int id);
}
