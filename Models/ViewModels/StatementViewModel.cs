namespace StokSafe.Models.ViewModels;

public class StatementViewModel
{
    public User User { get; set; } = null!;
    public Club? Club { get; set; }
    public ClubMember? Membership { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Totals
    public decimal ContributionsPaid { get; set; }
    public decimal ContributionsPending { get; set; }
    public decimal FinesPaid { get; set; }
    public decimal FinesOutstanding { get; set; }
    public decimal LoansTaken { get; set; }
    public decimal LoansRepaid { get; set; }
    public decimal LoansOutstanding { get; set; }

    public decimal NetPosition =>
        ContributionsPaid - FinesPaid - LoansRepaid;

    // Line items
    public List<Contribution> Contributions { get; set; } = new();
    public List<Fine> Fines { get; set; } = new();
    public List<Loan> Loans { get; set; } = new();
    public List<LoanPayment> LoanPayments { get; set; } = new();
}