using Microsoft.AspNetCore.SignalR;
using StokSafe.Models.Enums;

namespace StokSafe.Models.Entities
{
    public class Loan
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ClubId { get; set; }
        public decimal Amount { get; set; }
        public decimal  InterestRate  { get; set; }
        public int TErmWeeks { get; set; } = 4;
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.Pending;
        public decimal AmountPaid { get; set; } = 0;
        public decimal PenaltyAmount { get; set; } = 0;
        public decimal TotalAmount  { get; set; }
        public string? Description { get; set; }
        public string? Reference { get; set; }
        public string? Purpose { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Club Club { get; set; } = null!;
        public virtual ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();

        public void CalculatePenalty()
        {
            if (Status == LoanStatus.Paid || Status == LoanStatus.Defaulted)
                return;
            
            var daysOverdue = (DateTime.UtcNow - DueDate).Days;
            if (daysOverdue > 0)
            {
                var overdueAmount = TotalAmountDue - AmountPaid;
                if (overdueAmount > 0)
                {
                    PenaltyAmount = overdueAmount * 2; // 100% penalty
                    Status = LoanStatus.Overdue;
                    UpdatedAt = DateTime.UtcNow;
                }
            }

            if (daysOverdue > 90)
            {
                Status = LoanStatus.Defaulted;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public decimal GetTotalOutstanding()
        {
            if (Status == LoanStatus.Paid)
                return 0;
            return (TotalAmountDue - AmountPaid) + PenaltyAmount;
        }
        
        public bool IsOverdue()
        {
            return DateTime.UtcNow > DueDate && Status != LoanStatus.Paid && Status != LoanStatus.Defaulted;
        }

        public bool IsDefaulted()
        {
            var daysOverdue = (DateTime.UtcNow - DueDate).Days;
            return daysOverdue > 90 && Status != LoanStatus.Paid;
        }

        public decimal GetTotalRepayable()
        {
            return TotalAmountDue + PenaltyAmount;
        }

        public decimal GetInterestAmount()
        {
            return Amount * (InterestRate / 100);
        }
    }

    public class LoanPayment
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual Loan Loan { get; set; } = null!;
    }
}