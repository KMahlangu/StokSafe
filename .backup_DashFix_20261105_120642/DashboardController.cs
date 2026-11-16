using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Models.ViewModels;
using StokSafe.Services;

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

    // GET: /Member/Dashboard
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Auth", new { area = "" });

        // Find the member row for this user
        var membership = await _db.ClubMembers
            .Include(m => m.Club)
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var vm = new MemberDashboardViewModel
        {
            User = user,
            Membership = membership,
            Club = membership?.Club,
            MinimumContribution = ContributionPolicy.MinimumAmount
        };

        if (membership is not null)
        {
            // ============================================================
            // FINES
            // ============================================================
            var fines = await _db.Fines
                .Where(f => f.ClubMemberId == membership.Id)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            vm.PendingFinesCount = fines.Count(f =>
                f.Status == FineStatus.Pending || f.Status == FineStatus.PartiallyPaid);
            vm.PaidFinesCount = fines.Count(f => f.Status == FineStatus.Paid);
            vm.TotalOutstandingFines = fines
                .Where(f => f.Status != FineStatus.Paid && f.Status != FineStatus.Waived)
                .Sum(f => f.Balance);
            vm.TotalPaidFines = fines.Sum(f => f.PaidAmount);
            vm.RecentFines = fines.Take(5).ToList();

            // ============================================================
            // LOANS
            // ============================================================
            var loans = await _db.Loans
                .Where(l => l.ClubMemberId == membership.Id)
                .ToListAsync();

            vm.ActiveLoansCount = loans.Count(l =>
                l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue);
            vm.TotalLoanBalance = loans
                .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
                .Sum(l => l.OutstandingBalance);
            vm.ActiveLoans = loans
                .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
                .Take(5)
                .ToList();

            // ============================================================
            // CONTRIBUTIONS (last 30 days)
            // ============================================================
            var since = DateTime.UtcNow.AddDays(-30);
            var contributions = await _db.Contributions
                .Where(c => c.ClubMemberId == membership.Id
                         && c.ContributionDate >= since)
                .OrderByDescending(c => c.ContributionDate)
                .ToListAsync();

            vm.TotalContributionsThisMonth = contributions
                .Where(c => c.Status == ContributionStatus.Paid)
                .Sum(c => c.Amount);
            vm.RecentContributions = contributions.Take(5).ToList();

            // ============================================================
            // CURRENT WEEK CONTRIBUTION
            // ============================================================
            // 1. Prefer a Pending or Failed contribution (payable)
            var pendingContribution = await _db.Contributions
                .Where(c => c.ClubMemberId == membership.Id
                         && (c.Status == ContributionStatus.Pending
                             || c.Status == ContributionStatus.Failed))
                .OrderBy(c => c.DueDate)
                .FirstOrDefaultAsync();

            // 2. Otherwise use the most recent contribution (paid or not)
            var latestContribution = await _db.Contributions
                .Where(c => c.ClubMemberId == membership.Id)
                .OrderByDescending(c => c.ContributionDate)
                .FirstOrDefaultAsync();

            vm.CurrentWeekContribution = pendingContribution ?? latestContribution;
            vm.IsCurrentWeekPaid = vm.CurrentWeekContribution?.Status == ContributionStatus.Paid;

            // ============================================================
            // UNPAID LATE-CONTRIBUTION FINE (blocks the pay button)
            // ============================================================
            var unpaidLateFineTotal = await _db.Fines
                .Where(f => f.ClubMemberId == membership.Id
                         && f.FineType == FineType.LateContribution
                         && f.Balance > 0)
                .SumAsync(f => (decimal?)f.Balance) ?? 0m;

            vm.HasUnpaidLateFine = unpaidLateFineTotal > 0;
            vm.LateFineBalance = unpaidLateFineTotal;
        }

        // ============================================================
        // DEBUG — prints to the dotnet run console
        // Remove this block once the button shows correctly.
        // ============================================================
        Console.WriteLine(
            $"[DASHBOARD] User={user.Email} | " +
            $"Membership={(membership != null ? "YES" : "NO")} | " +
            $"MembershipId={membership?.Id.ToString() ?? "n/a"} | " +
            $"CurrentWeekId={vm.CurrentWeekContribution?.Id.ToString() ?? "NULL"} | " +
            $"CurrentWeekStatus={vm.CurrentWeekContribution?.Status.ToString() ?? "n/a"} | " +
            $"IsPaid={vm.IsCurrentWeekPaid} | " +
            $"HasLateFine={vm.HasUnpaidLateFine} (R {vm.LateFineBalance:N2}) | " +
            $"MinContribution={vm.MinimumContribution}");

        return View(vm);
    }
}