namespace StokSafe.Models.ViewModels;

public class ContributionIndexViewModel
{
    public List<Contribution> Contributions { get; set; } = new();

    public Contribution? CurrentWeek { get; set; }          // this week's row (Pending/Paid/Failed)
    public bool IsCurrentWeekPaid { get; set; }
    public bool HasUnpaidLateFine { get; set; }
    public decimal LateFineBalance { get; set; }
    public decimal WeeklyAmount { get; set; }

    // For the summary cards
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
}