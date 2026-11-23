using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services.Interfaces;

namespace StokSafe.Areas.Member.Controllers;

[Area("Member")]
[Authorize]
public class ContributionController : Controller
{
    private readonly IContributionService _contributionService;
    private readonly IOzowPaymentService _ozowService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public ContributionController(
        IContributionService contributionService,
        IOzowPaymentService ozowService,
        INotificationService notificationService,
        ApplicationDbContext db,
        UserManager<User> userManager)
    {
        _contributionService = contributionService;
        _ozowService = ozowService;
        _notificationService = notificationService;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return View(Enumerable.Empty<Contribution>());

        var now = DateTime.UtcNow;
        var currentWeekNumber = System.Globalization.ISOWeek.GetWeekOfYear(now);

        var currentContribution = await _db.Contributions
            .Where(c => c.ClubMemberId == member.Id && c.WeekNumber == currentWeekNumber)
            .ToListAsync();

        return View(currentContribution);
    }

    // THIS is the action that was missing (causing the 404)
    public async Task<IActionResult> Details(int id)
    {
        var contribution = await _contributionService.GetContributionAsync(id);
        if (contribution is null) return NotFound();
        return View(contribution);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPop(int id, IFormFile file)
    {
        if (file == null || file.Length == 0) 
        {
            TempData["Error"] = "Please select a file.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
        {
            TempData["Error"] = "Only JPG, PNG, and PDF files are allowed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsFolder);
        var uniqueFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var user = await _userManager.GetUserAsync(User);
        
        _db.ProofOfPayments.Add(new ProofOfPayment
        {
            ContributionId = id,
            UploadedByUserId = user!.Id,
            FilePath = $"/uploads/{uniqueFileName}",
            FileName = file.FileName,
            Status = "Pending"
        });
        await _db.SaveChangesAsync();

        var msg = $"StokSafe: You uploaded a Proof of Payment for Contribution #{id}. Awaiting Treasurer approval.";
        await _notificationService.SendEmailAsync(user.Email!, "POP Uploaded", $"<p>{msg}</p>");
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            await _notificationService.SendSmsAsync(user.PhoneNumber, msg);

        TempData["Success"] = "Proof of Payment uploaded! Awaiting Treasurer approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevPayBypass(int id)
    {
        var contrib = await _db.Contributions.FindAsync(id);
        if (contrib != null)
        {
            contrib.Status = ContributionStatus.Paid;
            contrib.PaidAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Payment simulated successfully (Dev Bypass)!";
        }
        return RedirectToAction(nameof(Index));
    }
}