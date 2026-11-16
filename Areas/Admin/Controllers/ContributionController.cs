using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;

namespace StokSafe.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Head,Treasurer")]
public class ContributionController : Controller
{
    private readonly ApplicationDbContext _db;
    public ContributionController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var contributions = await _db.Contributions
            .Include(c => c.ClubMember).ThenInclude(m => m.User)
            .OrderByDescending(c => c.ContributionDate)
            .ToListAsync();
        return View(contributions);
    }
}