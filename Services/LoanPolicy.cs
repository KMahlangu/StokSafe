namespace StokSafe.Services;

public static class LoanPolicy
{
    public const decimal BorrowRatio = 0.50m; // 50% of contributions
    public const decimal MinLoanAmount = 50m; // Minimum loan amount
    public const int TermWeeks = 4;           // 4 weeks exactly
    public const decimal InterestRate = 0.50m; // 50% flat interest
    public const decimal OverduePenaltyMultiplier = 2.0m; // 100% increase (doubles) on balance
}