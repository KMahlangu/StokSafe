using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;

namespace StokSafe.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Head,Treasurer")]
public class ProofOfPaymentController : Controller
{
    private readonly ApplicationDbContext _db;

    public ProofOfPaymentController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Admin/ProofOfPayment
    public async Task<IActionResult> Index()
    {
        var pops = await _db.ProofOfPayments
            .Include(p => p.UploadedBy)
            .Include(p => p.Contribution)
            .Include(p => p.Fine)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        return View(pops);
    }

    // POST: /Admin/ProofOfPayment/Approve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var pop = await _db.ProofOfPayments
            .Include(p => p.Contribution)
            .Include(p => p.Fine)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (pop == null) return NotFound();

        pop.Status = "Approved";
        pop.ReviewedAt = DateTime.UtcNow;

        if (pop.Contribution != null)
        {
            pop.Contribution.Status = ContributionStatus.Paid;
            pop.Contribution.PaidAt = DateTime.UtcNow;
        }
        else if (pop.Fine != null)
        {
            pop.Fine.Status = FineStatus.Paid;
            pop.Fine.PaidDate = DateTime.UtcNow;
            pop.Fine.PaidAmount = pop.Fine.CurrentAmount;
            pop.Fine.Balance = 0;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Payment approved and marked as Paid!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/ProofOfPayment/Reject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var pop = await _db.ProofOfPayments.FindAsync(id);
        if (pop == null) return NotFound();

        pop.Status = "Rejected";
        pop.AdminNotes = reason;
        pop.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Proof of Payment rejected.";
        return RedirectToAction(nameof(Index));
    }
}