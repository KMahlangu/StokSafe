using StokSafe.Models.Enums;

namespace StokSafe.Models;

public class Loan
{
    public int Id { get; set; }

    public int ClubMemberId { get; set; }
    public ClubMember ClubMember { get; set; } = null!;

    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmountDue { get; set; }

    // Tracks how many weeks the loan is overdue (used for doubling penalties)
    public int WeeksOverdue { get; set; } = 0;

    public LoanStatus Status { get; set; } = LoanStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? DueDate { get; set; }
    
    public int? UserId { get; set; }

    public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
}