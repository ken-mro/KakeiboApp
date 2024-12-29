using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface IMonthlyIncomeDataRepository
{
    Task<IEnumerable<MonthlyIncome>> GetAllAsync();
    Task<MonthlyIncome> GetByIdAsync(int id);
    Task<int> AddAsync(MonthlyIncome income);
    Task<int> UpdateAsync(MonthlyIncome income);
    Task<int> DeleteAsync(int id);
}
