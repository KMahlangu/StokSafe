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
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
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

    public async Task RunOnceAsync(CancellationToken ct = default) => await RunCycleAsync(ct);

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await GenerateWeeklyContributionsAsync(db, ct);
        await ApplyLoanPenaltiesAsync(db, ct);
        
        // DISABLED FOR DEVELOPMENT
        // await ApplyMissedContributionFinesAsync(db, ct);
        // await EscalateUnpaidFinesAsync(db, ct);
    }

    private async Task GenerateWeeklyContributionsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-((int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1));
        var weekNumber = ISOWeek.GetWeekOfYear(now);
        var dueDate = weekStart.AddDays(6).AddHours(15);

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
    
    private async Task ApplyLoanPenaltiesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        
        // Find Active loans past their due date
        var overdueLoans = await db.Loans
            .Where(l => l.Status == LoanStatus.Active && l.DueDate < now)
            .ToListAsync(ct);

        foreach (var loan in overdueLoans)
        {
            // Calculate how many weeks overdue (at least 1 if it just passed)
            var weeksPastDue = (int)Math.Ceiling((now - loan.DueDate!.Value).TotalDays / 7.0);
            
            // If it's a new week overdue that we haven't penalized yet
            if (weeksPastDue > loan.WeeksOverdue)
            {
                // Double the balance
                loan.OutstandingBalance *= 2;
                loan.TotalAmountDue = loan.OutstandingBalance;
                loan.WeeksOverdue = weeksPastDue;
                loan.Status = LoanStatus.Overdue; // Mark as overdue
                
                // Update the due date to push it out another 7 days for the next check
                loan.DueDate = loan.DueDate.Value.AddDays(7 * (weeksPastDue - loan.WeeksOverdue)); 
            }
        }
        await db.SaveChangesAsync(ct);
    }
}