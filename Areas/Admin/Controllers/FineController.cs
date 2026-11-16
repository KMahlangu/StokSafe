using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;

namespace StokSafe.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Head,Treasurer")]
public class FineController : Controller
{
    private readonly ApplicationDbContext _db;
    public FineController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var fines = await _db.Fines
            .Include(f => f.ClubMember).ThenInclude(m => m.User)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
        return View(fines);
    }
}