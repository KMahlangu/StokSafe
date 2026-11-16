using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StokSafe.Models;          // <-- brings in Club, ClubMember, User, etc.
using StokSafe.Models.Enums;

namespace StokSafe.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> o) : base(o) { }

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<Fine> Fines => Set<Fine>();
    public DbSet<FinePayment> FinePayments => Set<FinePayment>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<MonthlyStatement> MonthlyStatements => Set<MonthlyStatement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProofOfPayment> ProofOfPayments { get; set; }

        // ADD THIS EXACT LINE:
    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ClubMember>()
            .HasOne(m => m.User)
            .WithMany(u => u.ClubMembers)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    
        // ---------------- Notification ----------------
        b.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    
        // ---------------- MonthlyStatement ----------------
        b.Entity<MonthlyStatement>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---------------- AuditLog ----------------
        b.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);


        // MySQL utf8mb4 for all tables
        b.UseCollation("utf8mb4_unicode_ci");

        // Identity table names (shorter for MySQL index limits)
        b.Entity<User>(e => { e.ToTable("Users"); e.Property(u => u.Id).HasMaxLength(191); });
        b.Entity<Microsoft.AspNetCore.Identity.IdentityRole<int>>(e => { e.ToTable("Roles"); e.Property(r => r.Id).HasMaxLength(191); });
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<int>>(e => { e.ToTable("UserRoles"); });
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<int>>(e => { e.ToTable("UserClaims"); });
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<int>>(e => { e.ToTable("UserLogins"); });
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<int>>(e => { e.ToTable("UserTokens"); });
        b.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>>(e => { e.ToTable("RoleClaims"); });

        // Decimal precision
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
                prop.SetColumnType("decimal(15,2)");

        // Relationships
        b.Entity<ClubMember>()
            .HasIndex(m => new { m.ClubId, m.UserId }).IsUnique();

        b.Entity<Fine>().HasIndex(f => new { f.ClubId, f.Status });
        b.Entity<Loan>().HasIndex(l => new { l.ClubId, l.Status });

        b.Entity<Contribution>()
            .HasIndex(c => new { c.ClubMemberId, c.WeekNumber, c.ContributionDate });
    }
}