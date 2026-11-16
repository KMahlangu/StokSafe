using StokSafe.Models;
using StokSafe.Services;

namespace StokSafe.Services.Interfaces;

public interface ILoanService
{
    Task<IEnumerable<Loan>> GetMemberLoansAsync(int clubMemberId);
    Task<Loan?> GetLoanAsync(int loanId);
    Task<Loan> ApplyAsync(int clubMemberId, decimal principal, int termMonths, string? reason);

    Task<LoanEligibilityResult> EvaluateEligibilityAsync(int clubMemberId, decimal requestedAmount);

    Task<bool> ApproveAsync(int loanId, decimal interestRate);
    Task<bool> RejectAsync(int loanId, string reason);
    Task<bool> DisburseAsync(int loanId, string? reference);

    Task<bool> RecordRepaymentAsync(int loanId, decimal amount, string? reference);
    Task<decimal> CalculateOutstandingAsync(int loanId);
    Task<int> ApplyOverduePenaltiesAsync();
}