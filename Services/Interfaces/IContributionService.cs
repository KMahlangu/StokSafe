using StokSafe.Models;

namespace StokSafe.Services.Interfaces;

public interface IContributionService
{
    Task<IEnumerable<Contribution>> GetMemberContributionsAsync(int clubMemberId);
    Task<Contribution?> GetContributionAsync(int contributionId);
    Task<bool> RecordPaymentAsync(int contributionId, decimal amount, string? reference);
    Task<IEnumerable<Contribution>> GetOutstandingAsync(int clubMemberId);
}