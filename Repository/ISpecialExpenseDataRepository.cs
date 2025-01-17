using KakeiboApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KakeiboApp.Repository;

public interface ISpecialExpenseDataRepository
{
    Task<IEnumerable<SpecialExpense>> GetAllAsync();
    Task<SpecialExpense> GetByIdAsync(int id);
    Task<int> AddAsync(SpecialExpense specialExpense);
    Task<int> UpdateAsync(SpecialExpense specialExpense);
    Task<int> DeleteAsync(int id);
}
