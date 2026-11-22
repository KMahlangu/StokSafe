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
public class FineController : Controller
{
    private readonly IFineService _fineService;
    private readonly IOzowPaymentService _ozowService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public FineController(
        IFineService fineService,
        IOzowPaymentService ozowService,
        INotificationService notificationService,
        ApplicationDbContext db,
        UserManager<User> userManager)
    {
        _fineService = fineService;
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
        if (member is null) return View(Enumerable.Empty<Fine>());

        var fines = await _fineService.GetMemberFinesAsync(member.Id);
        return View(fines);
    }

    public async Task<IActionResult> Details(int id)
    {
        var fine = await _fineService.GetFineAsync(id);
        if (fine is null) return NotFound();
        return View(fine);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, decimal amount, string? reference)
    {
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
            FineId = id,
            UploadedByUserId = user!.Id,
            FilePath = $"/uploads/{uniqueFileName}",
            FileName = file.FileName,
            Status = "Pending"
        });
        await _db.SaveChangesAsync();

        // Send Transaction Attempt Notifications
        var msg = $"StokSafe: You uploaded a Proof of Payment for Fine #{id}. Awaiting Treasurer approval.";
        await _notificationService.SendEmailAsync(user.Email!, "POP Uploaded", $"<p>{msg}</p>");
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            await _notificationService.SendSmsAsync(user.PhoneNumber, msg);

        TempData["Success"] = "Proof of Payment uploaded! Awaiting Treasurer approval.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Waive(int id, string reason)
    {
        var fine = await _db.Fines.FindAsync(id);
        if (fine != null)
        {
            fine.Status = FineStatus.Waived;
            fine.Balance = 0;
            fine.Notes = string.IsNullOrWhiteSpace(fine.Notes) ? $"Waived: {reason}" : $"{fine.Notes}\nWaived: {reason}";
            await _db.SaveChangesAsync();
            TempData["Success"] = "Fine waived successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevPayBypass(int id)
    {
        var fine = await _db.Fines.FindAsync(id);
        if (fine != null)
        {
            fine.Status = FineStatus.Paid;
            fine.PaidDate = DateTime.UtcNow;
            fine.PaidAmount = fine.CurrentAmount;
            fine.Balance = 0;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Payment simulated successfully (Dev Bypass)!";
        }
        return RedirectToAction(nameof(Index));
    }
}