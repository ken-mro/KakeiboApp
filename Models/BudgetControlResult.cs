namespace KakeiboApp.Models;

public class BudgetControlResult
{
    public MonthlyBudget MonthlyBudget { get; set; } = new();
    public decimal MonthlySpending { get; set; }
    public Category Category => MonthlyBudget.Category;
    public decimal RemainingBudget => MonthlyBudget.Amount - MonthlySpending;
    public decimal DailyRemainingBudget => GetDailySpending();

    public decimal GetDailySpending()
    {
        var budgetDate = MonthlyBudget.Date;
        var today = DateTime.Today;
        if (!budgetDate.Year.Equals(today.Year) || !budgetDate.Month.Equals(today.Month))
        {
            return RemainingBudget;
        }

        // The budget is for the current month.
        int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var remainingDays = daysInMonth - today.Day + 1;
        return RemainingBudget / remainingDays;
    }
}
