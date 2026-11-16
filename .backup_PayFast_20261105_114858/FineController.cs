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
public class FineController : Controller
{
    private readonly IFineService _fineService;
    private readonly IPayFastService _payFastService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public FineController(IFineService fineService,
        IPayFastService payFastService, ApplicationDbContext db, UserManager<User> userManager)
    {
        _fineService = fineService;
        _payFastService = payFastService;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return View(Enumerable.Empty<Fine>());

        var fines = await _db.Fines
            .Where(f => f.ClubMemberId == member.Id)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return View(fines);
    }

        [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, decimal amount, string? reference)
    {
        var fine = await _db.Fines.FindAsync(id);
        if (fine is null) return NotFound();

        var paymentUrl = _payFastService.GeneratePaymentUrl(
            fine.Id, 
            amount, 
            $"StokSafe Fine #{fine.Id} - {fine.FineType}"
        );

        return Redirect(paymentUrl);
    });

        var member = await _db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return Forbid();

        var fine = await _db.Fines.FirstOrDefaultAsync(f => f.Id == id && f.ClubMemberId == member.Id);
        if (fine is null) return NotFound();

        if (amount <= 0 || amount > fine.Balance)
        {
            TempData["Error"] = "Invalid payment amount.";
            return RedirectToAction(nameof(Index));
        }

        var ok = await _fineService.ProcessFinePaymentAsync(fine.Id, amount, reference);

        TempData[ok ? "Success" : "Error"] = ok
            ? $"Payment of R {amount:N2} recorded for fine #{fine.Id}."
            : "Payment could not be processed.";

        return RedirectToAction(nameof(Index));
    }
}