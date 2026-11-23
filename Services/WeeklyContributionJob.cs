using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using System.Globalization;

namespace StokSafe.Services;

public class WeeklyContributionJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WeeklyContributionJob> _logger;
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(1);

    public WeeklyContributionJob(IServiceProvider services, ILogger<WeeklyContributionJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WeeklyContributionJob cycle failed.");
            }
            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await GenerateWeeklyContributionsAsync(db, ct);
        await ApplyMissedContributionFinesAsync(db, ct);
        await ApplyLoanPenaltiesAsync(db, ct);
    }

    private async Task GenerateWeeklyContributionsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-((int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1));
        var weekNumber = ISOWeek.GetWeekOfYear(now);
        var dueDate = weekStart.AddDays(6).AddHours(15); // Sunday 15:00

        var alreadyGenerated = await db.Contributions.AnyAsync(c => c.WeekNumber == weekNumber && c.ContributionDate.Year == weekStart.Year, ct);
        if (alreadyGenerated) return;

        var members = await db.ClubMembers.Where(m => m.IsActive).ToListAsync(ct);
        foreach (var member in members)
        {
            if (await db.Contributions.AnyAsync(c => c.ClubMemberId == member.Id && c.WeekNumber == weekNumber, ct)) continue;

            db.Contributions.Add(new Contribution
            {
                ClubMemberId = member.Id,
                ClubId = member.ClubId,
                PaymentType = PaymentType.Weekly,
                Status = ContributionStatus.Pending,
                Amount = 100m,
                WeekNumber = weekNumber,
                ContributionDate = weekStart,
                DueDate = dueDate
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyMissedContributionFinesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        
        // Find Pending contributions whose DueDate has passed
        var overdueContributions = await db.Contributions
            .Where(c => c.Status == ContributionStatus.Pending && c.DueDate < now)
            .ToListAsync(ct);

        foreach (var contribution in overdueContributions)
        {
            var fineExists = await db.Fines.AnyAsync(f => f.ContributionId == contribution.Id, ct);
            if (fineExists) continue;

            var fineAmount = Fine.GetFineAmount(FineType.NoBanking); // Should be R50

            db.Fines.Add(new Fine
            {
                ClubMemberId = contribution.ClubMemberId,
                ClubId = contribution.ClubId,
                ContributionId = contribution.Id,
                FineType = FineType.NoBanking,
                OriginalAmount = fineAmount,
                CurrentAmount = fineAmount,
                PaidAmount = 0m,
                Balance = fineAmount,
                DueDate = now.AddDays(7),
                Status = FineStatus.Pending,
                EscalationLevel = FineEscalationLevel.Level1,
                WeeksOverdue = 1,
                Description = $"Missed weekly contribution for week {contribution.WeekNumber}.",
                Reference = $"LATE-WK{contribution.WeekNumber}-{contribution.ClubMemberId}",
                IsCompulsory = true,
                CreatedAt = now
            });

            contribution.Status = ContributionStatus.Failed;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyLoanPenaltiesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var overdueLoans = await db.Loans
            .Where(l => l.Status == LoanStatus.Active && l.DueDate < now)
            .ToListAsync(ct);

        foreach (var loan in overdueLoans)
        {
            var weeksPastDue = (int)Math.Ceiling((now - loan.DueDate!.Value).TotalDays / 7.0);
            
            if (weeksPastDue > loan.WeeksOverdue)
            {
                loan.OutstandingBalance *= 2;
                loan.TotalAmountDue = loan.OutstandingBalance;
                loan.WeeksOverdue = weeksPastDue;
                loan.Status = LoanStatus.Overdue;
                loan.DueDate = loan.DueDate.Value.AddDays(7 * (weeksPastDue - loan.WeeksOverdue)); 
            }
        }
        await db.SaveChangesAsync(ct);
    }
}