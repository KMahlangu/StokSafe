using Microsoft.AspNetCore.Identity;
using StokSafe.Models.Enums;

namespace StokSafe.Models;

public class User : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? IdNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountHolder { get; set; }
    public string? BankBranchCode { get; set; }
    public UserRole Role { get; set; }
    public bool IsVerified { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // OTP Properties for Registration
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }

    // Navigation Properties
    public ICollection<ClubMember> ClubMembers { get; set; } = new List<ClubMember>();
    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<MonthlyStatement> MonthlyStatements { get; set; } = new List<MonthlyStatement>();

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Name) ? Name
        : !string.IsNullOrWhiteSpace(Email) ? Email!
        : UserName ?? "there";
}