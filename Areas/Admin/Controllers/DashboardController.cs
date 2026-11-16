using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models.Enums;

namespace StokSafe.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Head,Treasurer")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalMembers = await _db.ClubMembers.CountAsync(m => m.ApprovalStatus == "Approved");
        ViewBag.PendingMembers = await _db.ClubMembers.CountAsync(m => m.ApprovalStatus == "Pending");
        ViewBag.TotalLoansOut = await _db.Loans.Where(l => l.Status == LoanStatus.Active).SumAsync(l => l.OutstandingBalance);
        ViewBag.PendingLoans = await _db.Loans.CountAsync(l => l.Status == LoanStatus.Pending);
        ViewBag.TotalContributions = await _db.Contributions.Where(c => c.Status == ContributionStatus.Paid).SumAsync(c => c.Amount);

        return View();
    }
}