using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models.Enums;

namespace StokSafe.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Head,Treasurer")]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReportController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var allContribs = await _db.Contributions.Where(c => c.Status == ContributionStatus.Paid).ToListAsync();
        ViewBag.WeeklyContrib = allContribs.Where(c => c.PaidAt >= startOfWeek).Sum(c => c.Amount);
        ViewBag.MonthlyContrib = allContribs.Where(c => c.PaidAt >= startOfMonth).Sum(c => c.Amount);
        ViewBag.TotalContrib = allContribs.Sum(c => c.Amount);

        var allFines = await _db.Fines.Where(f => f.PaidAmount > 0).ToListAsync();
        ViewBag.WeeklyFines = allFines.Where(f => f.PaidDate >= startOfWeek).Sum(f => f.PaidAmount);
        ViewBag.MonthlyFines = allFines.Where(f => f.PaidDate >= startOfMonth).Sum(f => f.PaidAmount);
        ViewBag.TotalFines = allFines.Sum(f => f.PaidAmount);

        var allLoans = await _db.Loans.Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Paid).ToListAsync();
        ViewBag.WeeklyLoans = allLoans.Where(l => l.ApprovedAt >= startOfWeek).Sum(l => l.PrincipalAmount);
        ViewBag.MonthlyLoans = allLoans.Where(l => l.ApprovedAt >= startOfMonth).Sum(l => l.PrincipalAmount);
        ViewBag.TotalLoans = allLoans.Sum(l => l.PrincipalAmount);

        return View();
    }
}