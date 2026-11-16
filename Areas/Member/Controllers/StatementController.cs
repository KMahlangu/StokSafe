using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Services.Interfaces;

namespace StokSafe.Areas.Member.Controllers;

[Area("Member")]
[Authorize]
public class StatementController : Controller
{
    private readonly IStatementService _statementService;
    private readonly INotificationService _notificationService;
    private readonly IPdfService _pdfService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<StatementController> _logger;

    public StatementController(
        IStatementService statementService,
        INotificationService notificationService,
        IPdfService pdfService,
        ApplicationDbContext db,
        UserManager<User> userManager,
        ILogger<StatementController> logger)
    {
        _statementService = statementService;
        _notificationService = notificationService;
        _pdfService = pdfService;
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Member/Statement  (defaults to current month)
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (member is null)
            return View("NoMembership");

        var now = DateTime.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;

        // Validate
        if (m < 1 || m > 12 || y < 2000 || y > now.Year + 1)
        {
            TempData["Error"] = "Invalid month or year.";
            return RedirectToAction(nameof(Index));
        }

        var vm = await _statementService.BuildStatementAsync(member.Id, y, m);
        if (vm is null) return NotFound();

        ViewBag.AvailableMonths = await _statementService.GetAvailableMonthsAsync(member.Id);
        return View(vm);
    }

    // POST: /Member/Statement/SaveSnapshot
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSnapshot(int year, int month)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return Forbid();

        var ok = await _statementService.SaveSnapshotAsync(member.Id, year, month);

        TempData[ok ? "Success" : "Error"] = ok
            ? $"Statement for {new DateTime(year, month, 1):MMM yyyy} saved."
            : "Could not save statement snapshot.";

        return RedirectToAction(nameof(Index), new { year, month });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailStatement(int year, int month)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return Forbid();

        // 1. Get the data
        var vm = await _statementService.BuildStatementAsync(member.Id, year, month);
        if (vm is null) return NotFound();

        // 2. Build the HTML Email Body
        var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; padding: 20px;'>
                <h2 style='color: #0d6efd; text-align: center;'>StokSafe Statement</h2>
                <p><strong>Member:</strong> {vm.User.DisplayName}</p>
                <p><strong>Period:</strong> {vm.PeriodStart:dd MMM yyyy} - {vm.PeriodEnd:dd MMM yyyy}</p>
                
                <hr />
                <h3>Summary</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #eee;'>Contributions Paid:</td><td style='text-align: right; color: green;'>R {vm.ContributionsPaid:N2}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #eee;'>Fines Paid:</td><td style='text-align: right;'>R {vm.FinesPaid:N2}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #eee;'>Loans Repaid:</td><td style='text-align: right;'>R {vm.LoansRepaid:N2}</td></tr>
                </table>

                <hr />
                <h3>Recent Activity</h3>
                <p><strong>Contributions:</strong> {vm.Contributions.Count} records</p>
                <p><strong>Fines:</strong> {vm.Fines.Count} records</p>
                <p><strong>Loans:</strong> {vm.Loans.Count} records</p>

                <p style='text-align: center; margin-top: 30px; font-size: 12px; color: #777;'>
                    This is an automated message from StokSafe.
                </p>
            </div>";

        // 3. Send it
        await _notificationService.SendEmailAsync(user.Email!, $"StokSafe Statement - {vm.PeriodStart:MMMM yyyy}", emailBody);

        TempData["Success"] = $"Statement for {vm.PeriodStart:MMM yyyy} has been sent to {user.Email}.";
        return RedirectToAction(nameof(Index), new { year, month });
    }
}