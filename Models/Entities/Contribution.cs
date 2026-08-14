using StokSafe.Models.Enums;

namespace StokSafe.Models.Entities
{
    public class Contribution
    {
        public int Id { get; set;}
        public int ClubMemberId { get; set; }
        public int ClubId { get; set; }
        public DateTime ContributionDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentType Type { get; set; } = PaymentType.Weekly;
        public ContributionStatus Status { get; set; } = ContributionStatus.Pending;

        public string? TransactionId { get; set;}
        public string? PaymentMethod { get; set; }
        public string? PaymentData { get; set; }
        public int WeekNumber { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ClubMember ClubMember { get; set; } = null!;
        public virtual Club Club  { get; set; } = null!;
        public virtual Fine? Fine { get; set; }

        public void MarkAsPaid(string transactionId, string paymentMethod)
        {
            Status = ContributionStatus.Paid;
            TransactionId = transactionId;
            PaymentMethod = paymentMethod;
            PaidAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public bool IsLate()
        {
            if (Status == ContributionStatus.Paid)
                return false;
            return DateTime.UtcNow > DueDate;
        }

        public int GetDaysLate()
        {
            if (!IsLate())
                return 0;
            return (DateTime.UtcNow - DueDate).Days;
        }
    }    
}
