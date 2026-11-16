using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services.Implementations;
using StokSafe.Services.Interfaces;
using StokSafe.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// 1. MySQL Database
// ---------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ---------------------------------------------------------------
// 2. ASP.NET Core Identity
// ---------------------------------------------------------------
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// ---------------------------------------------------------------
// 3. Application Services (DI)
// ---------------------------------------------------------------
builder.Services.AddScoped<IFineService, FineService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IReportService, ReportService>();
//builder.Services.AddScoped<IOzowPaymentService, OzowPaymentService>();
builder.Services.AddScoped<DbInitializer>();
builder.Services.AddScoped<IContributionService, ContributionService>();
builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddHostedService<WeeklyContributionJob>();
builder.Services.AddScoped<IPayFastService, PayFastService>();

// ---------------------------------------------------------------
// 4. MVC
// ---------------------------------------------------------------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---------------------------------------------------------------
// 5. Ensure DB migrated + seed roles/admin
// ---------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await dbInitializer.SeedAsync();
}

// ---------------------------------------------------------------
// 6. DEV-ONLY: Ensure the logged-in user has a club, membership,
//    and one pending test fine, so the Member dashboard and Fines
//    page have something to display.
// ---------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    // Find or create a test club
    var club = await db.Clubs.FirstOrDefaultAsync(c => c.Name == "Test Stokvel");
    if (club is null)
    {
        club = new Club
        {
            Name = "Test Stokvel",
            Description = "Club for testing dashboard/fines flows",
            CreatedAt = DateTime.UtcNow
        };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
    }

    // Find or create a ClubMember for the seeded admin user
    var admin = await userManager.FindByEmailAsync("mosaprojects1@gmail.com");
    if (admin is not null)
    {
        var member = await db.ClubMembers
            .FirstOrDefaultAsync(m => m.UserId == admin.Id);

        if (member is null)
        {
            member = new ClubMember
            {
                UserId = admin.Id,
                ClubId = club.Id,
                Role = UserRole.Admin,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.ClubMembers.Add(member);
            await db.SaveChangesAsync();
        }

        // Seed one test loan if none exist for this member
        var hasLoans = await db.Loans.AnyAsync(l => l.ClubMemberId == member.Id);
        if (!hasLoans)
        {
            db.Loans.Add(new Loan
            {
                ClubMemberId = member.Id,
                ClubId = club.Id,
                PrincipalAmount = 500m,
                InterestRate = 10m,
                OutstandingBalance = 550m,
                PenaltyAmount = 0m,
                TotalAmountDue = 550m,
                Status = LoanStatus.Active,
                RequestedAt = DateTime.UtcNow.AddDays(-15),
                ApprovedAt = DateTime.UtcNow.AddDays(-14),
                DueDate = DateTime.UtcNow.AddMonths(3)
            });
            await db.SaveChangesAsync();
        }

        // Create one test fine if none exist for this member
        var hasFines = await db.Fines.AnyAsync(f => f.ClubMemberId == member.Id);
        if (!hasFines)
        {
            db.Fines.Add(new Fine
            {
                ClubMemberId = member.Id,
                ClubId = club.Id,
                ContributionId = null,
                FineType = FineType.LateMeetingAttendance,
                OriginalAmount = 100m,
                CurrentAmount = 100m,
                PaidAmount = 0m,
                Balance = 100m,
                DueDate = DateTime.UtcNow.AddDays(7),
                PaidDate = null,
                Status = FineStatus.Pending,
                EscalationLevel = FineEscalationLevel.Level1,
                WeeksOverdue = 0,
                Description = "Test fine for pay/waive flow",
                Reference = "TEST-FINE-001",
                Notes = null,
                IsCompulsory = false,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    // ---------------------------------------------------------------
    // Apply overdue penalties on every app start (dev-safe)
    // ---------------------------------------------------------------
    using (var penaltyScope = app.Services.CreateScope())
    {
        var loanService = penaltyScope.ServiceProvider.GetRequiredService<ILoanService>();
        await loanService.ApplyOverduePenaltiesAsync();
    }
}

// ---------------------------------------------------------------
// 7. Middleware pipeline
// ---------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();