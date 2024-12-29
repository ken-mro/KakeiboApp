using System.Collections.Generic;
using System.Threading.Tasks;
using KakeiboApp.Models;

namespace KakeiboApp.Repository;

public interface IMonthlySavingDataRepository
{
    Task<IEnumerable<MonthlySaving>> GetAllSavingsAsync();
    Task<MonthlySaving> GetSavingByIdAsync(int id);
    Task<int> AddSavingAsync(MonthlySaving saving);
    Task<int> UpdateSavingAsync(MonthlySaving saving);
    Task<int> DeleteSavingAsync(int id);
}
