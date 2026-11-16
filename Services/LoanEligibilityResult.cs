namespace StokSafe.Services;

public record LoanEligibilityResult(
    bool IsEligible,
    string Reason,
    decimal MaxEligibleAmount = 0m,
    decimal ContributionTotal = 0m);