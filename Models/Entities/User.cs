using Microsoft.AspNetCore.Identity;
using StokSafe.Models.Enums;

namespace StokSafe.Models.Entities
{
    public class User : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public string SUrname { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string IdNumber { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string? Address { get; set; }

        public string? BankName { get; set; }

        public string? BankAccountNumber { get; set; }

        public string? BankAccountHolder { get; set; }

        public string? BankBranchCode { get; set; }

        public UserRole Role { get; set; } = UserRole.Member;

        public bool IsVerified { get; set; } = false;

        public string? ProfilePicture { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public bool IsActive { get; set; } = true;


        // Navigation Properties
        public virtual ICollection<ClubMember> ClubMembers { get; set; } = new List<ClubMember>();
        public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
        public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>():
        private virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public virtual ICollection<MonthlyStatement> MonthlyStatements { get; set; } = new List<MonthlyStatement>();

        public bool IsAdmin() => Role == UserRole.Admin;
        public bool IsHead() => Role == UserRole.Head;
        public bool IsSecretary() => Role == UserRole.Secretary;
        public bool IsTreasurer() => Role == UserRole.Treasurer;
        public bool IsManagement() => Role == UserRole.Admin || Role == UserRole.Head || Role == UserRole.Secretary || Role == UserRole.Treasurer;

        public string FullName => $"{Name} {SUrname}";
    }
}