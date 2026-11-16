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

    // used by LoanService
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmountDue { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? DueDate { get; set; }

    public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
}
