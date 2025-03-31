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
        if (RemainingBudget <= 0) return 0;

        var budgetDate = MonthlyBudget.Date;
        var today = DateTime.Today;
        var thisMonth = new DateTime(today.Year, today.Month, 1);

        //past
        if (budgetDate < thisMonth)
        {
            return RemainingBudget;
        }

        //future
        if (thisMonth < budgetDate)
        {
            int days = DateTime.DaysInMonth(budgetDate.Year, budgetDate.Month);
            return RemainingBudget / days;
        }

        // The budget is for the current month.
        int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var remainingDays = daysInMonth - today.Day + 1;
        return RemainingBudget / remainingDays;
    }
}
