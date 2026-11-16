using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services;
using StokSafe.Services.Implementations;
using StokSafe.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);


// 1. Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 42))));

// Saves security keys to the database so they survive app restarts on MonsterASP
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("StokSafe");




// 2. ASP.NET Core Identity
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

// 3. Application Services (DI)
builder.Services.AddScoped<IFineService, FineService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IContributionService, ContributionService>();
builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPdfService, PdfService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IOzowPaymentService, OzowPaymentService>();

builder.Services.AddScoped<DbInitializer>();

// 4. Background Job
builder.Services.AddHostedService<WeeklyContributionJob>();
builder.Services.AddHostedService<LoanDueReminderJob>();

// 5. MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 6. Migrations & Seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await dbInitializer.SeedAsync();
}

// 7. Middleware
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