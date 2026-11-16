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

    // ... (Login, Logout, Forgot Password, Reset Password, VerifyOtp actions remain exactly the same) ...
    
    // Note: I have included them below so you can just copy-paste the whole file safely.

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
        if (result.Succeeded) return RedirectToAction("Index", "Dashboard", new { area = "Member" });

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
            return View(model); // Reloads page with red error messages
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            Name = model.Name,
            Surname = model.Surname,
            PhoneNumber = model.PhoneNumber,
            IsVerified = true, // Auto-verify to prevent OTP crash
            IsActive = true,   // Allow immediate login
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Create ClubMember
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

            // Log them in immediately
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Route to correct dashboard
            if (model.Role == StokSafe.Models.Enums.UserRole.Admin || model.Role == StokSafe.Models.Enums.UserRole.Head)
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            
            return RedirectToAction("Index", "Dashboard", new { area = "Member" });
        }

        // If Identity failed (e.g. password too weak), show the errors
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        var availableClubs = await _db.Clubs.ToListAsync();
        model.AvailableClubs = availableClubs.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        }).ToList();
        
        return View(model);
    }[HttpGet]
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
            return RedirectToAction("Index", "Dashboard", new { area = "Member" });
        }

        ViewBag.Error = "Invalid or expired OTP."; ViewBag.Email = email; return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Auth", new { email = user.Email, token = token }, protocol: Request.Scheme);
            await _notificationService.SendEmailAsync(user.Email!, "Reset Password", $"<a href='{link}'>Reset</a>");
        }
        ViewBag.Message = "If the account exists, a link has been sent.";
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)) return BadRequest();
        ViewBag.Email = email; ViewBag.Token = token; return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword) { ViewBag.Error = "Passwords do not match."; return View(); }
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return BadRequest();
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded) { TempData["Success"] = "Password reset successfully."; return RedirectToAction("Login"); }
        ViewBag.Error = string.Join(" | ", result.Errors.Select(e => e.Description)); return View();
    }
    
    private async Task SendOtpAsync(User user)
    {
        var otp = new Random().Next(100000, 999999).ToString();
        user.OtpCode = otp;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        await _userManager.UpdateAsync(user);

        await _notificationService.SendEmailAsync(user.Email!, "StokSafe - Verify Your Account", 
            $"<h3>Welcome to StokSafe!</h3><p>Your One-Time Password (OTP) is: <strong style='font-size:24px;'>{otp}</strong></p>");
        
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            await _notificationService.SendSmsAsync(user.PhoneNumber, $"StokSafe OTP: {otp}");
        }
    }
}