using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services;
using StokSafe.Services.Interfaces;

namespace StokSafe.Areas.Member.Controllers;

[Area("Member")]
[Authorize]
public class ContributionController : Controller
{
    private readonly IContributionService _contributionService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public ContributionController(IContributionService contributionService, ApplicationDbContext db, UserManager<User> userManager)
    {
        _contributionService = contributionService;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return View(Enumerable.Empty<Contribution>());

        // Pass the window status to the view
        ViewBag.IsWindowOpen = ContributionPolicy.IsPaymentWindowOpen();

        var contributions = await _contributionService.GetMemberContributionsAsync(member.Id);
        return View(contributions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, decimal amount, string? reference)
    {
        // ?? SERVER-SIDE BLOCK: Reject payment if past the cutoff
        if (!ContributionPolicy.IsPaymentWindowOpen())
        {
            TempData["Error"] = "The payment window has closed for this week (Sundays at 15:00). Please try again on Monday.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var ok = await _contributionService.RecordPaymentAsync(id, amount, reference);
            TempData[ok ? "Success" : "Error"] = ok ? $"Contribution of R {amount:N2} paid successfully." : "Contribution could not be recorded.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}