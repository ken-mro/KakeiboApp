namespace KakeiboApp.Models;

public class SavingResult
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal LivingOffSavingAmount { get; private set; }
    public MonthlySaving LivingOffSaving { get; init; } = default!;
    public decimal Balance => Amount - LivingOffSavingAmount;

    public SavingResult(MonthlySaving monthlyLivingOff, string name, decimal amount)
    {
        if (monthlyLivingOff is null || monthlyLivingOff.Amount > 0)
        {
            throw new ArgumentException("Living off saving amount must be less than or equal to 0.");
        }

        if (amount < monthlyLivingOff.Amount)
        {
            throw new ArgumentException("Amount must be greater than or equal to living off saving amount.");
        }

        Name = name;
        Amount = amount;
        LivingOffSaving = monthlyLivingOff;
        LivingOffSavingAmount = -1 * monthlyLivingOff.Amount;
    }

    public SavingResult(DateTime date, string name, decimal amount)
    {
        Name = name;
        Amount = amount;
        LivingOffSaving = new MonthlySaving() { Date = date, Name = name };
    }

    public void SetLivingOffSavingAmount(decimal amount)
    {
        if (amount < 0) return;
        LivingOffSavingAmount = amount;
        LivingOffSaving.Amount = -1 * amount;
    }
}
