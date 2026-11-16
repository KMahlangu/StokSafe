using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;

namespace StokSafe.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Head")] // Only Admin or Head can create clubs
public class ClubController : Controller
{
    private readonly ApplicationDbContext _db;

    public ClubController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var clubs = await _db.Clubs.ToListAsync();
        return View(clubs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Club name is required.";
            return RedirectToAction(nameof(Index));
        }

        _db.Clubs.Add(new Club
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Club '{name}' created successfully!";
        return RedirectToAction(nameof(Index));
    }
}