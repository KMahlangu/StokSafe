using StokSafe.Services.Interfaces;
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
    
    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(string name, string email, string subject, string message, [FromServices] INotificationService notificationService)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            ViewBag.Error = "Please fill in all required fields.";
            return View();
        }

        // Send the email to the Admin/Support
        var emailBody = $@"
            <h3>New Contact Us Message</h3>
            <p><strong>From:</strong> {name} ({email})</p>
            <p><strong>Subject:</strong> {subject}</p>
            <p><strong>Message:</strong></p>
            <p>{message}</p>
        ";

        await notificationService.SendEmailAsync("mosaprojects1@gmail.com", $"Contact Form: {subject}", emailBody);

        TempData["Success"] = "Thank you! Your message has been sent successfully. We will get back to you shortly.";
        return RedirectToAction(nameof(Contact));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCallback(string name, string phoneNumber, string preferredTime, [FromServices] INotificationService notificationService)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phoneNumber))
        {
            TempData["CallbackError"] = "Please provide your name and phone number.";
            return RedirectToAction(nameof(Contact));
        }

        var emailBody = $@"
            <h3>New Call Back Request</h3>
            <p><strong>Name:</strong> {name}</p>
            <p><strong>Phone Number:</strong> {phoneNumber}</p>
            <p><strong>Preferred Time:</strong> {preferredTime ?? "Any time"}</p>
        ";

        await notificationService.SendEmailAsync("mosaprojects1@gmail.com", "StokSafe: Call Back Requested", emailBody);

        TempData["CallbackSuccess"] = "Thank you! We have received your request and will call you back shortly.";
        return RedirectToAction(nameof(Contact));
    }
}
