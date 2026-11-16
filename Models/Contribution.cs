using StokSafe.Models.Enums;

namespace StokSafe.Models;

public class Contribution
{
    public int Id { get; set; }

    public int ClubMemberId { get; set; }
    public ClubMember ClubMember { get; set; } = null!;

    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public PaymentType PaymentType { get; set; } = PaymentType.Weekly;
    public ContributionStatus Status { get; set; } = ContributionStatus.Pending;

    public decimal Amount { get; set; }

    // New properties referenced by ApplicationDbContext
    public int WeekNumber { get; set; }
    public DateTime ContributionDate { get; set; }

    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
}