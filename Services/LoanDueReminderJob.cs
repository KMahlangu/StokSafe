using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models.Enums;
using StokSafe.Services.Interfaces;

namespace StokSafe.Services;

public class LoanDueReminderJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LoanDueReminderJob> _logger;

    public LoanDueReminderJob(IServiceProvider services, ILogger<LoanDueReminderJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LoanDueReminderJob starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow.AddHours(2); // South Africa Standard Time (SAST is UTC+2)
                
                // Check if it's 9:00 AM in SA
                if (now.Hour == 9)
                {
                    await CheckAndSendRemindersAsync(stoppingToken);
                    
                    // Wait for an hour so we don't send duplicates in the same 9 AM hour
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                else
                {
                    // Check again in 15 minutes
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // This is normal when the app is shutting down. Ignore it.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoanDueReminderJob");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task CheckAndSendRemindersAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DateTime.UtcNow.Date;
        var sevenDaysFromNow = today.AddDays(7);
        var tomorrow = today.AddDays(1);

        // Find active loans
        var activeLoans = await db.Loans
            .Include(l => l.ClubMember)
                .ThenInclude(m => m.User)
            .Where(l => l.Status == LoanStatus.Active && l.DueDate != null)
            .ToListAsync(ct);

        int emailsSent = 0;

        foreach (var loan in activeLoans)
        {
            var user = loan.ClubMember?.User;
            if (user == null || string.IsNullOrWhiteSpace(user.Email)) continue;

            var dueDate = loan.DueDate!.Value.Date;
            string? subject = null;
            string? message = null;

            // Check if due in 7 days
            if (dueDate == sevenDaysFromNow)
            {
                subject = "StokSafe - Loan Due in 7 Days";
                message = $"Hello {user.Name}, this is a reminder that your loan (LN-{loan.Id:D5}) of R {loan.PrincipalAmount:N2} is due in exactly 1 week on {dueDate:yyyy-MM-dd}. Your outstanding balance is R {loan.OutstandingBalance:N2}.";
            }
            // Check if due today
            else if (dueDate == today || dueDate == tomorrow) // tomorrow catch for timezone differences
            {
                subject = "StokSafe - Loan Due TODAY";
                message = $"Hello {user.Name}, your loan (LN-{loan.Id:D5}) is due TODAY ({dueDate:yyyy-MM-dd}). Please ensure you pay the outstanding balance of R {loan.OutstandingBalance:N2} to avoid the 100% penalty.";
            }

            // If a condition matched, send the email
            if (subject != null && message != null)
            {
                var emailBody = $@"
                    <div style='font-family: Arial; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                        <h3 style='color: #0d6efd;'>Loan Payment Reminder</h3>
                        <p>{message}</p>
                        <p><a href='https://crewless-construct-dormitory.ngrok-free.dev/Member/Loan' style='padding: 10px 20px; background-color: #0d6efd; color: white; text-decoration: none; border-radius: 5px;'>View My Loans</a></p>
                    </div>";

                await notificationService.SendEmailAsync(user.Email, subject, emailBody);
                emailsSent++;
                
                // Small delay to prevent email throttling
                await Task.Delay(1000, ct);
            }
        }

        if (emailsSent > 0)
        {
            _logger.LogInformation($"Sent {emailsSent} loan reminder emails.");
        }
    }
}