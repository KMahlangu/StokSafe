using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services;
using StokSafe.Services.Interfaces;

namespace StokSafe.Services.Implementations;

public class LoanService : ILoanService
{
    private readonly ApplicationDbContext _db;

    public LoanService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ---------------------------------------------------------------
    // Reads
    // ---------------------------------------------------------------

    public async Task<IEnumerable<Loan>> GetMemberLoansAsync(int clubMemberId)
    {
        return await _db.Loans
            .Where(l => l.ClubMemberId == clubMemberId)
            .OrderByDescending(l => l.RequestedAt)
            .ToListAsync();
    }

    public async Task<Loan?> GetLoanAsync(int loanId)
    {
        return await _db.Loans
            .Include(l => l.ClubMember)
            .Include(l => l.Club)
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == loanId);
    }

    public async Task<decimal> CalculateOutstandingAsync(int loanId)
    {
        var loan = await _db.Loans
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan is null) return 0m;

        var paid = loan.Payments.Sum(p => p.Amount);
        var outstanding = loan.TotalAmountDue - paid;
        return outstanding < 0 ? 0m : outstanding;
    }

    // ---------------------------------------------------------------
    // Eligibility — 50% of contributions paid
    // ---------------------------------------------------------------

    public async Task<LoanEligibilityResult> EvaluateEligibilityAsync(
        int clubMemberId,
        decimal requestedAmount)
    {
        var member = await _db.ClubMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == clubMemberId);

        if (member is null)
            return new LoanEligibilityResult(false, "Club member not found.");

        if (!member.IsActive)
            return new LoanEligibilityResult(false, "Membership is inactive.");

        // No double-dipping
        var hasOpenLoan = await _db.Loans.AnyAsync(l =>
            l.ClubMemberId == clubMemberId &&
            (l.Status == LoanStatus.Active ||
             l.Status == LoanStatus.Overdue ||
             l.Status == LoanStatus.Pending ||
             l.Status == LoanStatus.Approved));

        if (hasOpenLoan)
            return new LoanEligibilityResult(
                false,
                "You already have an active, overdue, or pending loan.");

        // Total contributions paid
        var contributionTotal = await _db.Contributions
            .Where(c => c.ClubMemberId == clubMemberId
                     && c.Status == ContributionStatus.Paid)
            .SumAsync(c => (decimal?)c.Amount) ?? 0m;

        if (contributionTotal <= 0)
            return new LoanEligibilityResult(
                false,
                "No contributions on record. You cannot borrow yet.",
                ContributionTotal: 0m);

        var maxEligible = Math.Round(
            contributionTotal * LoanPolicy.BorrowRatio, 2);

        if (requestedAmount < LoanPolicy.MinLoanAmount)
            return new LoanEligibilityResult(
                false,
                $"Minimum loan amount is R {LoanPolicy.MinLoanAmount:N2}.",
                MaxEligibleAmount: maxEligible,
                ContributionTotal: contributionTotal);

        if (requestedAmount > maxEligible)
            return new LoanEligibilityResult(
                false,
                $"You may borrow at most 50% of your contributions. "
                + $"You have contributed R {contributionTotal:N2}, "
                + $"so your limit is R {maxEligible:N2}.",
                MaxEligibleAmount: maxEligible,
                ContributionTotal: contributionTotal);

        return new LoanEligibilityResult(
            IsEligible: true,
            Reason: $"Eligible up to R {maxEligible:N2}.",
            MaxEligibleAmount: maxEligible,
            ContributionTotal: contributionTotal);
    }

    // ---------------------------------------------------------------
    // Apply
    // ---------------------------------------------------------------

    public async Task<Loan> ApplyAsync(
        int clubMemberId,
        decimal principal,
        int termMonths,
        string? reason)
    {
        var eligibility = await EvaluateEligibilityAsync(clubMemberId, principal);

        if (!eligibility.IsEligible)
            throw new InvalidOperationException(eligibility.Reason);

        var member = await _db.ClubMembers
            .FirstOrDefaultAsync(m => m.Id == clubMemberId)
            ?? throw new InvalidOperationException("Club member not found.");

        var requestedAt = DateTime.UtcNow;

        var loan = new Loan
        {
            ClubMemberId = clubMemberId,
            ClubId = member.ClubId,
            PrincipalAmount = principal,
            InterestRate = 0m,
            OutstandingBalance = principal,
            PenaltyAmount = 0m,
            TotalAmountDue = principal,
            Status = LoanStatus.Pending,
            RequestedAt = requestedAt,
            DueDate = requestedAt.AddDays(LoanPolicy.TermWeeks * 7)
        };

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync();
        return loan;
    }

    // ---------------------------------------------------------------
    // Approve — Pending → Approved
    // ---------------------------------------------------------------

    public async Task<bool> ApproveAsync(int loanId, decimal interestRate)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null || loan.Status != LoanStatus.Pending) return false;

        // Re-check eligibility with current data
        var eligibility = await EvaluateEligibilityAsync(
            loan.ClubMemberId, loan.PrincipalAmount);

        if (!eligibility.IsEligible) return false;

        loan.InterestRate = interestRate;
        loan.TotalAmountDue = loan.PrincipalAmount * (1 + interestRate / 100m);
        loan.OutstandingBalance = loan.TotalAmountDue;
        loan.Status = LoanStatus.Approved;
        loan.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    // ---------------------------------------------------------------
    // Reject — Pending → Rejected
    // ---------------------------------------------------------------

    public async Task<bool> RejectAsync(int loanId, string reason)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null || loan.Status != LoanStatus.Pending) return false;

        loan.Status = LoanStatus.Rejected;
        // If Loan has a Notes/RejectionReason field, uncomment:
        // loan.Notes = $"Rejected: {reason}";

        await _db.SaveChangesAsync();
        return true;
    }

    // ---------------------------------------------------------------
    // Disburse — Pending or Approved → Active
    // ---------------------------------------------------------------

    public async Task<bool> DisburseAsync(int loanId, string? reference)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null) return false;

        if (loan.Status != LoanStatus.Pending && loan.Status != LoanStatus.Approved)
            return false;

        if (loan.Status == LoanStatus.Pending)
        {
            loan.InterestRate = 0m;
            loan.TotalAmountDue = loan.PrincipalAmount;
            loan.OutstandingBalance = loan.PrincipalAmount;
            loan.ApprovedAt = DateTime.UtcNow;
        }

        loan.Status = LoanStatus.Active;
        loan.DueDate = DateTime.UtcNow.AddDays(LoanPolicy.TermWeeks * 7);

        await _db.SaveChangesAsync();
        return true;
    }

    // ---------------------------------------------------------------
    // Repay
    // ---------------------------------------------------------------

    public async Task<bool> RecordRepaymentAsync(int loanId, decimal amount, string? reference)
    {
        if (amount <= 0) return false;

        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null) return false;

        if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
            return false;

        if (amount > loan.OutstandingBalance)
            amount = loan.OutstandingBalance;

        _db.LoanPayments.Add(new LoanPayment
        {
            LoanId = loanId,
            Amount = amount,
            Reference = reference ?? string.Empty,
            PaidAt = DateTime.UtcNow
        });

        loan.OutstandingBalance -= amount;

        if (loan.OutstandingBalance <= 0)
        {
            loan.OutstandingBalance = 0;
            loan.Status = LoanStatus.Paid;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    // ---------------------------------------------------------------
    // Overdue penalties — 100% increase once after 4 weeks
    // ---------------------------------------------------------------

    public async Task<int> ApplyOverduePenaltiesAsync()
    {
        var now = DateTime.UtcNow;

        var overdue = await _db.Loans
            .Where(l => l.Status == LoanStatus.Active
                     && l.DueDate != null
                     && l.DueDate < now)
            .ToListAsync();

        if (overdue.Count == 0) return 0;

        foreach (var loan in overdue)
        {
            var before = loan.OutstandingBalance;
            loan.OutstandingBalance = Math.Round(
                loan.OutstandingBalance * LoanPolicy.OverduePenaltyMultiplier, 2);
            loan.PenaltyAmount = loan.OutstandingBalance - before;
            loan.TotalAmountDue = loan.OutstandingBalance;
            loan.Status = LoanStatus.Overdue;
        }

        await _db.SaveChangesAsync();
        return overdue.Count;
    }
}