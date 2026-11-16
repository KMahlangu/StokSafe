using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StokSafe.Models;
using StokSafe.Services.Interfaces;

namespace StokSafe.Areas.Member.Controllers;

[Area("Member")]
[Authorize]
public class ManageController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly INotificationService _notificationService;

    public ManageController(UserManager<User> userManager, SignInManager<User> signInManager, INotificationService notificationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth", new { area = "" });
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string phoneNumber, string bankName, string bankAccountNumber, string bankAccountHolder, string bankBranchCode)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth", new { area = "" });

        user.PhoneNumber = phoneNumber;
        user.BankName = bankName;
        user.BankAccountNumber = bankAccountNumber;
        user.BankAccountHolder = bankAccountHolder;
        user.BankBranchCode = bankBranchCode;
        
        await _userManager.UpdateAsync(user);
        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
    {
        if (profilePicture == null || profilePicture.Length == 0) return RedirectToAction(nameof(Index));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(profilePicture.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
        {
            TempData["Error"] = "Only JPG and PNG images are allowed.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth", new { area = "" });

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
        Directory.CreateDirectory(uploadsFolder); // Ensure folder exists
        
        var uniqueFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await profilePicture.CopyToAsync(stream);
        }

        // Delete old picture if it exists
        if (!string.IsNullOrEmpty(user.ProfilePicture) && user.ProfilePicture != "/images/default-avatar.png")
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }

        user.ProfilePicture = $"/images/profiles/{uniqueFileName}";
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Profile picture updated successfully!";
        return RedirectToAction(nameof(Index));
    }
}