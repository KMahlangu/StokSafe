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
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public ContributionController(
        IContributionService contributionService,
        IOzowPaymentService ozowService,
        ApplicationDbContext db,
        UserManager<User> userManager)
    {
        _contributionService = contributionService;
        _ozowService = ozowService;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return View(Enumerable.Empty<Contribution>());

        var contributions = await _contributionService.GetMemberContributionsAsync(member.Id);
        return View(contributions);
    }

    public async Task<IActionResult> Details(int id)
    {
        var contribution = await _contributionService.GetContributionAsync(id);
        if (contribution is null) return NotFound();
        return View(contribution);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayCurrentWeek(decimal amount, string? reference)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return NotFound("Member not found.");

        var contrib = await _db.Contributions
            .Where(c => c.ClubMemberId == member.Id && c.Status == ContributionStatus.Pending)
            .OrderByDescending(c => c.ContributionDate)
            .FirstOrDefaultAsync();

        if (contrib is null)
        {
            TempData["Error"] = "No pending contribution found.";
            return RedirectToAction(nameof(Index));
        }

        // Ozow temporarily disabled. Redirecting to details page for POP upload.
        TempData["Success"] = "Please upload your Proof of Payment below to complete this transaction.";
        return RedirectToAction(nameof(Details), new { id = contrib.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, decimal amount, string? reference)
    {
        // Ozow temporarily disabled. Redirecting to details page for POP upload.
        TempData["Success"] = "Please upload your Proof of Payment below to complete this transaction.";
        return RedirectToAction(nameof(Details), new { id });
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

        TempData["Success"] = "Proof of Payment uploaded! Awaiting Treasurer approval.";
        return RedirectToAction(nameof(Details), new { id });
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
    
    // GET: /Member/Contribution/PayCurrentWeek
    [HttpGet]
    public IActionResult PayCurrentWeek()
    {
        TempData["Error"] = "Please select a specific pending contribution from the list below to make a payment.";
        return RedirectToAction(nameof(Index));
    }
}