using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services;
using StokSafe.Services.Interfaces;

namespace StokSafe.Services.Implementations;

public class ContributionService : IContributionService
{
    private readonly ApplicationDbContext _db;

    public ContributionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Contribution>> GetMemberContributionsAsync(int clubMemberId)
    {
        return await _db.Contributions
            .Where(c => c.ClubMemberId == clubMemberId)
            .OrderByDescending(c => c.ContributionDate)
            .ToListAsync();
    }

    public async Task<Contribution?> GetContributionAsync(int contributionId)
    {
        return await _db.Contributions
            .Include(c => c.ClubMember)
            .FirstOrDefaultAsync(c => c.Id == contributionId);
    }

    public async Task<IEnumerable<Contribution>> GetOutstandingAsync(int clubMemberId)
    {
        return await _db.Contributions
            .Where(c => c.ClubMemberId == clubMemberId
                     && c.Status != ContributionStatus.Paid)
            .OrderBy(c => c.DueDate)
            .ToListAsync();
    }

    public async Task<bool> RecordPaymentAsync(int contributionId, decimal amount, string? reference)
    {
        if (amount < ContributionPolicy.MinimumAmount) return false;

        var contribution = await _db.Contributions
            .FirstOrDefaultAsync(c => c.Id == contributionId);

        if (contribution is null) return false;
        if (contribution.Status == ContributionStatus.Paid) return false;

        // Block if unpaid late-contribution fine exists
        var hasUnpaidLateFine = await _db.Fines.AnyAsync(f =>
            f.ClubMemberId == contribution.ClubMemberId &&
            f.FineType == FineType.LateContribution &&
            f.Balance > 0);

        if (hasUnpaidLateFine)
            throw new InvalidOperationException(
                "You have an unpaid late-contribution fine. " +
                "Settle it before making new contributions.");

        // Store the amount the member actually paid
        contribution.Amount = amount;
        contribution.Status = ContributionStatus.Paid;
        contribution.PaidAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}