using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface IMonthlySavingDataRepository
{
    Task<IEnumerable<MonthlySaving>> GetAllAsync();
    Task<MonthlySaving> GetByIdAsync(int id);
    Task<int> AddAsync(MonthlySaving saving);
    Task<int> UpdateAsync(MonthlySaving saving);
    Task<int> DeleteAsync(int id);
}
