using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Models.ViewModels;

namespace StokSafe.Areas.Member.Controllers;

[Area("Member")]
[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public DashboardController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var membership = await _db.ClubMembers
            .Include(m => m.Club)
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var vm = new MemberDashboardViewModel
        {
            User = user,
            Membership = membership,
            Club = membership?.Club
        };

        if (membership is not null)
        {
            // Fines
            var fines = await _db.Fines
                .Where(f => f.ClubMemberId == membership.Id)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            vm.PendingFinesCount = fines.Count(f => f.Status == FineStatus.Pending || f.Status == FineStatus.PartiallyPaid);
            vm.TotalOutstandingFines = fines.Where(f => f.Status != FineStatus.Paid && f.Status != FineStatus.Waived).Sum(f => f.Balance);
            vm.TotalPaidFines = fines.Sum(f => f.PaidAmount);
            vm.RecentFines = fines.Take(5).ToList();

            // Late Fine Logic
            var unpaidLateFine = fines
                .Where(f => f.FineType == FineType.LateContribution && f.Balance > 0)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefault();
            
            vm.HasUnpaidLateFine = unpaidLateFine != null;
            vm.LateFineBalance = unpaidLateFine?.Balance ?? 0m;

            // Loans
            var loans = await _db.Loans.Where(l => l.ClubMemberId == membership.Id).ToListAsync();
            vm.ActiveLoansCount = loans.Count(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue);
            vm.TotalLoanBalance = loans.Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue).Sum(l => l.OutstandingBalance);
            vm.ActiveLoans = loans.Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue).Take(5).ToList();

            // Contributions
            var contributions = await _db.Contributions
                .Where(c => c.ClubMemberId == membership.Id)
                .OrderByDescending(c => c.ContributionDate)
                .ToListAsync();

            vm.TotalContributionsThisMonth = contributions.Where(c => c.Status == ContributionStatus.Paid).Sum(c => c.Amount);
            vm.RecentContributions = contributions.Take(5).ToList();

            // Current Week Logic
            var currentWeekStart = DateTime.UtcNow.Date.AddDays(-((int)DateTime.UtcNow.DayOfWeek == 0 ? 6 : (int)DateTime.UtcNow.DayOfWeek - 1));
            var thisWeekContrib = contributions.FirstOrDefault(c => c.ContributionDate >= currentWeekStart);
            
            vm.CurrentWeekContribution = thisWeekContrib;
            vm.IsCurrentWeekPaid = thisWeekContrib?.Status == ContributionStatus.Paid;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateNextWeek()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Auth", new { area = "" });

        var member = await _db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member is null) return NotFound("Member not found.");

        var latest = await _db.Contributions
            .Where(c => c.ClubMemberId == member.Id)
            .OrderByDescending(c => c.WeekNumber)
            .FirstOrDefaultAsync();

        int nextWeek = (latest?.WeekNumber ?? 0) + 1;

        _db.Contributions.Add(new Contribution
        {
            ClubMemberId = member.Id,
            ClubId = member.ClubId,
            PaymentType = PaymentType.Weekly,
            Status = ContributionStatus.Pending,
            Amount = 100m,
            WeekNumber = nextWeek,
            ContributionDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7)
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Generated contribution for Week {nextWeek}.";
        return RedirectToAction(nameof(Index));
    }
}