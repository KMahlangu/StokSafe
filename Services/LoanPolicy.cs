namespace StokSafe.Services;

/// <summary>
/// Loan rules for StokSafe stokvel.
/// </summary>
public static class LoanPolicy
{
    public const decimal BorrowRatio = 0.50m;
    public const decimal MinLoanAmount = 50m;
    public const int TermWeeks = 4;
    public const decimal OverduePenaltyMultiplier = 2.0m;
}