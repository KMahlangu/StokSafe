using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Models.ViewModels;
using StokSafe.Services.Interfaces;

namespace StokSafe.Services.Implementations;

public class StatementService : IStatementService
{
    private readonly ApplicationDbContext _db;

    public StatementService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StatementViewModel?> BuildStatementAsync(int clubMemberId, int year, int month)
    {
        var member = await _db.ClubMembers
            .Include(m => m.User)
            .Include(m => m.Club)
            .FirstOrDefaultAsync(m => m.Id == clubMemberId);

        if (member is null || member.User is null) return null;

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        // Contributions in the period
        var contributions = await _db.Contributions
            .Where(c => c.ClubMemberId == clubMemberId
                     && c.ContributionDate >= start
                     && c.ContributionDate < end)
            .OrderBy(c => c.ContributionDate)
            .ToListAsync();

        // Fines in the period
        var fines = await _db.Fines
            .Where(f => f.ClubMemberId == clubMemberId
                     && f.CreatedAt >= start
                     && f.CreatedAt < end)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();

        // Loans disbursed in the period
        var loans = await _db.Loans
            .Where(l => l.ClubMemberId == clubMemberId
                     && l.RequestedAt >= start
                     && l.RequestedAt < end)
            .OrderBy(l => l.RequestedAt)
            .ToListAsync();

        // Loan payments in the period
        var loanPayments = await _db.LoanPayments
            .Where(p => p.Loan!.ClubMemberId == clubMemberId
                     && p.PaidAt >= start
                     && p.PaidAt < end)
            .OrderBy(p => p.PaidAt)
            .ToListAsync();

        var vm = new StatementViewModel
        {
            User = member.User,
            Club = member.Club,
            Membership = member,
            Year = year,
            Month = month,
            PeriodStart = start,
            PeriodEnd = end.AddDays(-1),

            Contributions = contributions,
            Fines = fines,
            Loans = loans,
            LoanPayments = loanPayments,

            ContributionsPaid = contributions
                .Where(c => c.Status == ContributionStatus.Paid)
                .Sum(c => c.Amount),
            ContributionsPending = contributions
                .Where(c => c.Status == ContributionStatus.Pending)
                .Sum(c => c.Amount),

            FinesPaid = fines.Sum(f => f.PaidAmount),
            FinesOutstanding = fines
                .Where(f => f.Status != FineStatus.Paid && f.Status != FineStatus.Waived)
                .Sum(f => f.Balance),

            LoansTaken = loans.Sum(l => l.PrincipalAmount),
            LoansRepaid = loanPayments.Sum(p => p.Amount),
            LoansOutstanding = loans
                .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
                .Sum(l => l.OutstandingBalance)
        };

        return vm;
    }

    public async Task<IEnumerable<(int Year, int Month)>> GetAvailableMonthsAsync(int clubMemberId)
    {
        // Union of months that have any activity
        var contributionMonths = await _db.Contributions
            .Where(c => c.ClubMemberId == clubMemberId)
            .Select(c => new { c.ContributionDate.Year, c.ContributionDate.Month })
            .Distinct()
            .ToListAsync();

        var fineMonths = await _db.Fines
            .Where(f => f.ClubMemberId == clubMemberId)
            .Select(f => new { f.CreatedAt.Year, f.CreatedAt.Month })
            .Distinct()
            .ToListAsync();

        var loanMonths = await _db.Loans
            .Where(l => l.ClubMemberId == clubMemberId)
            .Select(l => new { l.RequestedAt.Year, l.RequestedAt.Month })
            .Distinct()
            .ToListAsync();

        var all = contributionMonths
            .Concat(fineMonths)
            .Concat(loanMonths)
            .Select(m => (m.Year, m.Month))
            .Distinct()
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToList();

        // Always include the current month
        var now = DateTime.UtcNow;
        if (!all.Contains((now.Year, now.Month)))
            all.Insert(0, (now.Year, now.Month));

        return all;
    }

    public async Task<bool> SaveSnapshotAsync(int clubMemberId, int year, int month)
    {
        var vm = await BuildStatementAsync(clubMemberId, year, month);
        if (vm is null) return false;

        // Remove any existing snapshot for this period
        var existing = await _db.MonthlyStatements
            .Where(s => s.UserId == vm.User.Id && s.Year == year && s.Month == month)
            .ToListAsync();

        if (existing.Any())
            _db.MonthlyStatements.RemoveRange(existing);

        _db.MonthlyStatements.Add(new MonthlyStatement
        {
            UserId = vm.User.Id,
            Year = year,
            Month = month,
            TotalContributions = vm.ContributionsPaid,
            TotalFines = vm.FinesPaid,
            TotalLoans = vm.LoansRepaid,
            GeneratedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }
}