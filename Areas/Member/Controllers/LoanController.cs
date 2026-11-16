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
public class LoanController : Controller
{
    private readonly ILoanService _loanService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<LoanController> _logger;

    public LoanController(
        ILoanService loanService,
        INotificationService notificationService,
        ApplicationDbContext db,
        UserManager<User> userManager,
        ILogger<LoanController> logger)
    {
        _loanService = loanService;
        _notificationService = notificationService;
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Member/Loan
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return View(Enumerable.Empty<Loan>());

        var loans = await _loanService.GetMemberLoansAsync(member.Id);
        return View(loans);
    }

    // GET: /Member/Loan/Apply
    [HttpGet]
    public IActionResult Apply()
    {
        return View();
    }

    // POST: /Member/Loan/Apply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(decimal principal, int termMonths, string? reason)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null)
        {
            TempData["Error"] = "You must be a club member to apply for a loan.";
            return RedirectToAction(nameof(Index));
        }

        if (principal <= 0 || termMonths <= 0)
        {
            TempData["Error"] = "Please enter a valid amount and term.";
            return View();
        }

        try
        {
            var loan = await _loanService.ApplyAsync(member.Id, principal, termMonths, reason);

            // =========================================================
            // SEND NOTIFICATIONS
            // =========================================================
            var emailSubject = $"Loan Application Received (Ref: LN-{loan.Id:D5})";
            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; overflow: hidden;'>
                    <div style='background-color: #0d6efd; color: white; padding: 20px; text-align: center;'>
                        <h2>StokSafe</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <h3>Hello {user.Name},</h3>
                        <p>Your loan application has been successfully submitted and is currently pending review.</p>
                        <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                            <tr><td style='padding: 8px; border-bottom: 1px solid #eee;'><strong>Reference:</strong></td><td style='padding: 8px; border-bottom: 1px solid #eee;'>LN-{loan.Id:D5}</td></tr>
                            <tr><td style='padding: 8px; border-bottom: 1px solid #eee;'><strong>Amount:</strong></td><td style='padding: 8px; border-bottom: 1px solid #eee;'>R {principal:N2}</td></tr>
                            <tr><td style='padding: 8px; border-bottom: 1px solid #eee;'><strong>Term:</strong></td><td style='padding: 8px; border-bottom: 1px solid #eee;'>{termMonths} months</td></tr>
                        </table>
                        <p style='margin-top: 20px;'>The committee will review your application shortly.</p>
                    </div>
                </div>";

            // 1. Send Email
            await _notificationService.SendEmailAsync(user.Email!, emailSubject, emailBody);

            // 2. Send SMS (Ensure phone number is not null)
            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                var smsMessage = $"StokSafe: Your loan application of R{principal:N2} (Ref: LN-{loan.Id:D5}) has been received.";
                await _notificationService.SendSmsAsync(user.PhoneNumber, smsMessage);
            }

            TempData["Success"] = $"Loan application submitted (Ref: LN-{loan.Id:D5}). Notifications sent!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply for loan");
            TempData["Error"] = "Could not submit application: " + ex.Message;
            return View();
        }
    }

    // GET: /Member/Loan/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var loan = await _loanService.GetLoanAsync(id);
        if (loan is null) return NotFound();
        return View(loan);
    }

    // POST: /Member/Loan/Pay
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, decimal amount, string? reference)
    {
        var ok = await _loanService.RecordRepaymentAsync(id, amount, reference);

        TempData[ok ? "Success" : "Error"] = ok
            ? $"Repayment of R {amount:N2} recorded."
            : "Repayment could not be recorded.";

        return RedirectToAction(nameof(Details), new { id });
    }
}