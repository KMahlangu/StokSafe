using StokSafe.Models.Enums;

namespace StokSafe.Models;

public class ClubMember
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // NEW: Membership Approval Status
    public string ApprovalStatus { get; set; } = "Pending";
    public string? ApprovalToken { get; set; } // "Pending", "Approved", "Rejected"

    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
}