using StokSafe.Models.Enums;

namespace StokSafe.Models.Entities
{
    public class Club
    {
        public int Id { get; set;}
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Logo { get; set; }
        public string HeadId { get; set; } = string.Empty;
        public string SecretaryId { get; set; } = string.Empty;
        public string TreasurerId { get; set; } = string.Empty;
        public decimal WeeklyContribution { get; set; } = 100.00m;
        public decimal InterestRate { get; set; } = 5.00m;
        public decimal FineAmount { get; set; } = 30.00m;
        public int FineGracePeriodWeeks { get; set; } = 4;
        public TimeOnly ContributionDeadline { get; set; } = new TimeOnly(15, 0, 0);
        public string DayOfWeek { get; set; } = "Sunday";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set;}
        public int MaxMember { get; set; } = 50;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual User Head { get; set; } = null!;
        public virtual User Seceretary { get; set; } = null!;
        public virtual User Treasurer { get; set; } = null!;
        public virtual ICollection<ClubMember> ClubMembers { get; set; } = new List<ClubMembers>();
        public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();
        public virtual ICollection<MonthlyStatement> MonthlyStatements { get; set; } = new List<MonthlyStatement>();
        
        public decimal GetTotalSavings()
        {
            return Contributions
                .Where(c => c.Status == ContributionStatus.Paid && c.Type == PaymentType.Weekly)
                .Sum(c => c.Amount);
        }

        public decimal GetTotalFines()
        {
            return Fines
                .Where(f => f.Status == FineStatus.Paid)
                .Sum(f => f.Amount);
        }

        public int GetActiveMembersCount()
        {
            return ClubMembers.Count(cm => cm.IsActive);
        }

        public decimal GetTotalOutstandingLoans()
        {
            return Loans
                .Where(l => 

        public decimal GetTotalFines()
        {
            return Fines
                .Where(f => f.Status == FineStatus.Paid)
                .Sum(f => f.Amount);
        }

        public int GetActiveMembersCount()
        {
            return ClubMembers.Count(cm => cm.IsActive);
        }

        public decimal GetTotalOutstandingLoans()
        {
            return Loans
                .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
                .Sum(l => l.GetTotalOutstanding());
        }

        public ClubWeeklyStatus GetWeeklyContributionsStatus()
        {
            var currentWeek = DateTime.UtcNow.GetWeekOfYear();
            var totalMembers = GetActiveMembersCount();
            var paid = Contributions
                .Where(c => c.WeekNumber == currentWeek && c.Status == ContributionStatus.Paid)
                .Count();
            var pending = Contributions
                .Where(c => c.WeekNumber == currentWeek && c.Status ==ContributionStatus.Pending)
                .Count();
            return new ClubWeeklyStatus
            {
                TotalMembers = totalMembers,
                Paid = paid,
                Pending = pending
                Percentage = totalMembers > 0 ? Math.Round((decimal)paid / totalMembers * 100, 2) : 0
            };
        }
    }

    public class ClubWeeklyStatus
    {
        public int Total { get; set; }
        public int Paid { get; set; }
        public int Pending { get; set; }
        public decimal Percentage { get; set; }   
    }

    public class ClubMember
    {
        public int Id { get; set; }
        public int ClubId { get; set; }
        public string UserId { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? MembershipNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        // Navigation Properties
        public virtual Club Club { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Contribution> Contributions  { get; set; } = new List<Contribution>();
        public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();
        public virtual ICollection<MonthlyStatement> MonthlyStatements { get; set; } = new List<MonthlyStatement>();
    }
};