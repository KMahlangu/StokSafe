using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.ViewModels;
using StokSafe.Services.Interfaces;

namespace StokSafe.Controllers;

public class AuthController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _db;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        INotificationService notificationService,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _notificationService = notificationService;
        _db = db;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive) { ViewBag.Error = "Invalid login or account inactive."; return View(); }

        if (!user.IsVerified)
        {
            await SendOtpAsync(user);
            return RedirectToAction("VerifyOtp", new { email = user.Email });
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
        if (result.Succeeded)
        {
            if (user.Role == StokSafe.Models.Enums.UserRole.Admin || user.Role == StokSafe.Models.Enums.UserRole.Head)
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return RedirectToAction("Index", "Dashboard", new { area = "Member" });
        }

        ViewBag.Error = "Invalid email or password.";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        var clubs = await _db.Clubs.ToListAsync();
        var model = new RegisterViewModel
        {
            AvailableClubs = clubs.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var clubs = await _db.Clubs.ToListAsync();
            model.AvailableClubs = clubs.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
            return View(model);
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            Name = model.Name,
            Surname = model.Surname,
            PhoneNumber = model.PhoneNumber,
            IsVerified = false,
            IsActive = true,
            EmailConfirmed = true,
            Role = model.Role
        };

        Microsoft.AspNetCore.Identity.IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(user, model.Password);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "This email address is already registered.");
            var clubsList = await _db.Clubs.ToListAsync();
            model.AvailableClubs = clubsList.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            return View(model);
        }

        if (result != null && result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role.ToString());

            if (model.Role == StokSafe.Models.Enums.UserRole.Admin || model.Role == StokSafe.Models.Enums.UserRole.Head)
            {
                var newClub = new Club { Name = $"{user.Name}'s Stokvel", CreatedAt = DateTime.UtcNow };
                _db.Clubs.Add(newClub);
                await _db.SaveChangesAsync();

                _db.ClubMembers.Add(new ClubMember
                {
                    UserId = user.Id,
                    ClubId = newClub.Id,
                    Role = model.Role,
                    ApprovalStatus = "Approved",
                    IsActive = true
                });
            }
            else
            {
                _db.ClubMembers.Add(new ClubMember
                {
                    UserId = user.Id,
                    ClubId = model.ClubId,
                    Role = model.Role,
                    ApprovalStatus = "Pending",
                    IsActive = false
                });
            }
            await _db.SaveChangesAsync();

            try
            {
                await _notificationService.SendEmailAsync("mosaprojects1@gmail.com", "New Registration", $"<p>{user.Name} has registered.</p>");
            }
            catch { }

            // CHECK ROLE AND ROUTE IMMEDIATELY FOR ADMIN
            if (model.Role == StokSafe.Models.Enums.UserRole.Admin || model.Role == StokSafe.Models.Enums.UserRole.Head)
            {
                user.IsVerified = true;
                await _userManager.UpdateAsync(user);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            await SendOtpAsync(user);
            return RedirectToAction("VerifyOtp", new { email = user.Email });
        }

        if (result != null)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }
        var availableClubs = await _db.Clubs.ToListAsync();
        model.AvailableClubs = availableClubs.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
        return View(model);
    }

    [HttpGet]
    public IActionResult VerifyOtp(string email) { ViewBag.Email = email; return View(); }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp(string email, string otpCode)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return NotFound("User not found.");

        if (user.OtpCode == otpCode && user.OtpExpiry > DateTime.UtcNow)
        {
            user.IsVerified = true; user.OtpCode = null; user.OtpExpiry = null;
            await _userManager.UpdateAsync(user);
            await _signInManager.SignInAsync(user, isPersistent: false);

            if (user.Role == StokSafe.Models.Enums.UserRole.Admin || user.Role == StokSafe.Models.Enums.UserRole.Head)
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return RedirectToAction("Index", "Dashboard", new { area = "Member" });
        }

        ViewBag.Error = "Invalid or expired OTP."; ViewBag.Email = email; return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null) await SendOtpAsync(user);
        TempData["Success"] = "A new OTP has been sent.";
        return RedirectToAction(nameof(VerifyOtp), new { email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    private async Task SendOtpAsync(User user)
    {
        var otp = new Random().Next(100000, 999999).ToString();
        user.OtpCode = otp;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(5);
        await _userManager.UpdateAsync(user);

        await _notificationService.SendEmailAsync(user.Email!, "StokSafe - Verify Account", $"<p>Your OTP is: <strong>{otp}</strong></p>");
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            await _notificationService.SendSmsAsync(user.PhoneNumber, $"StokSafe OTP: {otp}");
    }
}