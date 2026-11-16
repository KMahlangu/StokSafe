using Microsoft.AspNetCore.Identity;
using StokSafe.Models;
using StokSafe.Models.Enums;

namespace StokSafe.Data;

public class DbInitializer
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public DbInitializer(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        // Seed roles
        string[] roles = { "Admin", "Head", "Secretary", "Treasurer", "Member" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        // Seed default admin
        const string adminEmail = "admin@stoksafe.com";
        if (await _userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Name = "System",
                Surname = "Admin",
                Role = UserRole.Admin,
                IsActive = true,
                IsVerified = true
            };
            var result = await _userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed default member
        const string memberEmail = "member@stoksafe.com";
        if (await _userManager.FindByEmailAsync(memberEmail) is null)
        {
            var member = new User
            {
                UserName = memberEmail,
                Email = memberEmail,
                EmailConfirmed = true,
                Name = "Default",
                Surname = "Member",
                Role = UserRole.Member,
                IsActive = true,
                IsVerified = true
            };
            var result = await _userManager.CreateAsync(member, "Member@123");
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(member, "Member");
        }
    }
}