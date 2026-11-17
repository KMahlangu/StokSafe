using StokSafe.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StokSafe.Models;

namespace StokSafe.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    [HttpGet]
    public async Task<IActionResult> FixGhostUser([FromServices] ApplicationDbContext db)
    {
        var email = "khuselasmart@gmail.com";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        
        if (user != null)
        {
            var member = await db.ClubMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
            if (member != null)
            {
                var fines = await db.Fines.Where(f => f.ClubMemberId == member.Id).ToListAsync();
                db.Fines.RemoveRange(fines);

                var contributions = await db.Contributions.Where(c => c.ClubMemberId == member.Id).ToListAsync();
                db.Contributions.RemoveRange(contributions);

                var loans = await db.Loans.Where(l => l.ClubMemberId == member.Id).ToListAsync();
                db.Loans.RemoveRange(loans);

                db.ClubMembers.Remove(member);
            }

            var notifications = await db.Notifications.Where(n => n.UserId == user.Id).ToListAsync();
            db.Notifications.RemoveRange(notifications);

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Content($"Successfully deleted user {email} and all their data from the database.");
        }
        return Content($"User {email} not found.");
    }
    
    public IActionResult About()
    {
        return View();
    }

    public IActionResult Platform()
    {
        return View();
    }

    public IActionResult Pricing()
    {
        return View();
    }
}
