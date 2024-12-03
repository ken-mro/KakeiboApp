using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface IMonthlyIncomeDataRepository
{
    Task<IEnumerable<MonthlyIncome>> GetAllIncomesAsync();
    Task<MonthlyIncome> GetIncomeByIdAsync(int id);
    Task<int> AddIncomeAsync(MonthlyIncome income);
    Task<int> UpdateIncomeAsync(MonthlyIncome income);
    Task<int> DeleteIncomeAsync(int id);
}
