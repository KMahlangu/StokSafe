using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services.Interfaces;

namespace StokSafe.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Head")]
public class UserController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly UserManager<User> _userManager;

    public UserController(ApplicationDbContext db, INotificationService notificationService,
        IAuditService auditService, UserManager<User> userManager)
    {
        _db = db;
        _notificationService = notificationService;
        _auditService = auditService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var members = await _db.ClubMembers
            .Include(m => m.User)
            .Include(m => m.Club)
            .OrderBy(m => m.ApprovalStatus)
            .ThenByDescending(m => m.JoinedAt)
            .ToListAsync();

        return View(members);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {
        // FIX: We MUST Include the Club and User so they aren't null!
        var member = await _db.ClubMembers
            .Include(m => m.User)
            .Include(m => m.Club)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member == null) return NotFound();

        member.ApprovalStatus = "Approved";
        await _auditService.LogAsync(int.Parse(_userManager.GetUserId(User)!), "ApproveMember", "User", member.UserId.ToString(), $"Approved member {member.User.Name} for club {member.ClubId}");
        member.IsActive = true;
        await _db.SaveChangesAsync();

        // Safe access with null-coalescing operators
        var userName = member.User?.Name ?? "Member";
        var userEmail = member.User?.Email;
        var userPhone = member.User?.PhoneNumber;
        var clubName = member.Club?.Name ?? "the club";

        if (!string.IsNullOrEmpty(userEmail))
        {
            var msg = $"Hello {userName}, your membership to {clubName} has been APPROVED. Welcome!";
            await _notificationService.SendEmailAsync(userEmail, "StokSafe - Membership Approved", $"<p>{msg}</p>");
            
            if (!string.IsNullOrWhiteSpace(userPhone))
                await _notificationService.SendSmsAsync(userPhone, msg);
        }

        TempData["Success"] = $"{userName} has been approved and notified.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var member = await _db.ClubMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member == null) return NotFound();

        var userName = member.User?.Name ?? "Member";
        var userEmail = member.User?.Email;
        var userPhone = member.User?.PhoneNumber;
        var userId = member.UserId;
        var memberId = member.Id;

        try
        {
            // Cleanup related data
            var fines = await _db.Fines.Where(f => f.ClubMemberId == memberId).ToListAsync();
            _db.Fines.RemoveRange(fines);

            var contributions = await _db.Contributions.Where(c => c.ClubMemberId == memberId).ToListAsync();
            _db.Contributions.RemoveRange(contributions);

            var loans = await _db.Loans.Where(l => l.ClubMemberId == memberId).ToListAsync();
            _db.Loans.RemoveRange(loans);

            _db.ClubMembers.Remove(member);
            await _db.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null) await _userManager.DeleteAsync(user);

            if (!string.IsNullOrEmpty(userEmail))
            {
                var msg = $"Hello {userName}, unfortunately your application to StokSafe was not successful.";
                await _notificationService.SendEmailAsync(userEmail, "StokSafe - Application Update", $"<p>{msg}</p>");
                
                if (!string.IsNullOrWhiteSpace(userPhone))
                    await _notificationService.SendSmsAsync(userPhone, msg);
            }

            TempData["Success"] = $"{userName} has been rejected and removed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error removing user: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}