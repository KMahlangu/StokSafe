namespace StokSafe.Models.ViewModels;

public class MemberDashboardViewModel
{
    public User User { get; set; } = null!;
    public Club? Club { get; set; }
    public ClubMember? Membership { get; set; }

    public int PendingFinesCount { get; set; }
    public int PaidFinesCount { get; set; }
    public decimal TotalOutstandingFines { get; set; }
    public decimal TotalPaidFines { get; set; }

    public int ActiveLoansCount { get; set; }
    public decimal TotalLoanBalance { get; set; }

    public decimal TotalContributionsThisMonth { get; set; }

    public List<Fine> RecentFines { get; set; } = new();
    public List<Contribution> RecentContributions { get; set; } = new();
    public List<Loan> ActiveLoans { get; set; } = new();

    // Current week contribution state
    public Contribution? CurrentWeekContribution { get; set; }
    public bool IsCurrentWeekPaid { get; set; }
    public bool HasUnpaidLateFine { get; set; }
    public decimal LateFineBalance { get; set; }

    /// <summary>Minimum amount a member can contribute.</summary>
    public decimal MinimumContribution { get; set; }
}