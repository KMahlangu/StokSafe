using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services.Interfaces;

namespace StokSafe.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Head,Treasurer")]
public class LoanController : Controller
{
    private readonly ILoanService _loanService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<LoanController> _logger;

    public LoanController(
        ILoanService loanService,
        INotificationService notificationService,
        ApplicationDbContext db,
        ILogger<LoanController> logger)
    {
        _loanService = loanService;
        _notificationService = notificationService;
        _db = db;
        _logger = logger;
    }

    // GET: /Admin/Loan
    public async Task<IActionResult> Index()
    {
        var loans = await _db.Loans
            .Include(l => l.ClubMember)
                .ThenInclude(m => m.User)
            .Include(l => l.Club)
            .OrderByDescending(l => l.RequestedAt)
            .ToListAsync();

        return View(loans);
    }

    // GET: /Admin/Loan/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var loan = await _db.Loans
            .Include(l => l.ClubMember)
                .ThenInclude(m => m.User)
            .Include(l => l.Club)
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan is null) return NotFound();

        ViewBag.Eligibility = await _loanService.EvaluateEligibilityAsync(
            loan.ClubMemberId, loan.PrincipalAmount);

        return View(loan);
    }

    // POST: /Admin/Loan/Approve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        // The LoanService automatically applies the 50% interest rule.
        var ok = await _loanService.ApproveAsync(id, 50m); 

        if (ok)
        {
            // NOTIFICATION TRIGGER: Approved
            var loan = await _db.Loans
                .Include(l => l.ClubMember).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan?.ClubMember?.User != null)
            {
                var user = loan.ClubMember.User;
                var subject = $"Loan Application Approved (Ref: LN-{loan.Id:D5})";
                var emailBody = $@"
                    <div style='font-family: Arial; padding: 20px;'>
                        <h2 style='color: #198754;'>Loan Approved!</h2>
                        <p>Hello {user.Name},</p>
                        <p>We are pleased to inform you that your loan application has been approved.</p>
                        <p><strong>Amount:</strong> R {loan.PrincipalAmount:N2}</p>
                        <p><strong>Status:</strong> Awaiting disbursement. The funds will be paid out shortly.</p>
                    </div>";

                await _notificationService.SendEmailAsync(user.Email!, subject, emailBody);
                
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    await _notificationService.SendSmsAsync(user.PhoneNumber, $"StokSafe: Your loan of R{loan.PrincipalAmount:N2} is APPROVED. Funds will be paid out soon.");
                }
            }

            TempData["Success"] = $"Loan LN-{id:D5} approved and member notified.";
        }
        else
        {
            TempData["Error"] = "Loan could not be approved.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Admin/Loan/Disburse
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disburse(int id, string? reference)
    {
        var ok = await _loanService.DisburseAsync(id, reference);

        if (ok)
        {
            // NOTIFICATION TRIGGER: Disbursed / Paid out
            var loan = await _db.Loans
                .Include(l => l.ClubMember).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan?.ClubMember?.User != null)
            {
                var user = loan.ClubMember.User;
                var subject = $"Loan Funds Disbursed (Ref: LN-{loan.Id:D5})";
                var emailBody = $@"
                    <div style='font-family: Arial; padding: 20px;'>
                        <h2 style='color: #0d6efd;'>Funds Paid Out</h2>
                        <p>Hello {user.Name},</p>
                        <p>Your loan has been finalized and the funds have been disbursed to you.</p>
                        <p><strong>Amount Paid:</strong> R {loan.PrincipalAmount:N2}</p>
                        <p><strong>Due Date:</strong> {loan.DueDate:yyyy-MM-dd}</p>
                        <p>Please ensure timely repayments to avoid penalties.</p>
                    </div>";

                await _notificationService.SendEmailAsync(user.Email!, subject, emailBody);
                
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    await _notificationService.SendSmsAsync(user.PhoneNumber, $"StokSafe: Funds for loan LN-{loan.Id:D5} have been PAID OUT. Due Date: {loan.DueDate:yyyy-MM-dd}.");
                }
            }

            TempData["Success"] = $"Loan LN-{id:D5} disbursed and member notified.";
        }
        else
        {
            TempData["Error"] = "Loan could not be disbursed.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Admin/Loan/Reject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Please provide a reason for rejecting the loan.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ok = await _loanService.RejectAsync(id, reason);

        if (ok)
        {
            // NOTIFICATION TRIGGER: Rejected
            var loan = await _db.Loans
                .Include(l => l.ClubMember).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan?.ClubMember?.User != null)
            {
                var user = loan.ClubMember.User;
                var subject = $"Loan Application Update (Ref: LN-{loan.Id:D5})";
                var emailBody = $@"
                    <div style='font-family: Arial; padding: 20px;'>
                        <h2 style='color: #dc3545;'>Loan Application Declined</h2>
                        <p>Hello {user.Name},</p>
                        <p>Unfortunately, your loan application could not be approved at this time.</p>
                        <p><strong>Reason:</strong> {reason}</p>
                    </div>";

                await _notificationService.SendEmailAsync(user.Email!, subject, emailBody);
                
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    await _notificationService.SendSmsAsync(user.PhoneNumber, $"StokSafe: Your loan application was DECLINED. Reason: {reason}");
                }
            }

            TempData["Success"] = $"Loan LN-{id:D5} rejected and member notified.";
        }
        else
        {
            TempData["Error"] = "Loan could not be rejected.";
        }

        return RedirectToAction(nameof(Index));
    }
}