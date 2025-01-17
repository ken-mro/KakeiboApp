using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface IMonthlyFixedCostDataRepository
{
    Task<IEnumerable<MonthlyFixedCost>> GetAllAsync();
    Task<MonthlyFixedCost> GetByIdAsync(int id);
    Task<int> AddAsync(MonthlyFixedCost fixedCost);
    Task<int> AddAsync(IEnumerable<MonthlyFixedCost> fixedCosts);
    Task<int> UpdateAsync(MonthlyFixedCost fixedCost);
    Task<int> DeleteAsync(int id);
}
