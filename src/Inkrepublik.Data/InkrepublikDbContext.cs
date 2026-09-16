using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Data;

/// <summary>
/// EF Core context for the Inkrepublik studio database.
///
/// Relationships are configured explicitly in OnModelCreating rather than
/// through attributes on the entities — this keeps the Domain project free
/// of any framework dependencies.
/// </summary>
public class InkrepublikDbContext : DbContext
{
    public InkrepublikDbContext(DbContextOptions<InkrepublikDbContext> options)
        : base(options)
    {
    }

    // --- Tables ---
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<ArtistImage> ArtistImages => Set<ArtistImage>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        // Artist
        // ============================================================
        modelBuilder.Entity<Artist>(e =>
        {
            e.Property(a => a.Name).IsRequired().HasMaxLength(120);
            e.Property(a => a.Title).HasMaxLength(160);
            e.Property(a => a.Bio).HasMaxLength(4000);
            e.Property(a => a.Specialties).HasMaxLength(400);
            e.Property(a => a.ProfileImagePath).HasMaxLength(500);
            e.Property(a => a.InstagramHandle).HasMaxLength(80);

            e.HasIndex(a => a.DisplayOrder);
        });

        // ============================================================
        // ArtistImage
        // ============================================================
        modelBuilder.Entity<ArtistImage>(e =>
        {
            e.Property(ai => ai.ImagePath).IsRequired().HasMaxLength(500);
            e.Property(ai => ai.Caption).HasMaxLength(300);

            e.HasOne(ai => ai.Artist)
                .WithMany(a => a.PortfolioImages)
                .HasForeignKey(ai => ai.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(ai => new { ai.ArtistId, ai.DisplayOrder });
        });

        // ============================================================
        // Service
        // ============================================================
        modelBuilder.Entity<Service>(e =>
        {
            e.Property(s => s.Name).IsRequired().HasMaxLength(160);
            e.Property(s => s.Description).HasMaxLength(2000);
            e.Property(s => s.PriceFrom).HasPrecision(10, 2);
            e.Property(s => s.PriceTo).HasPrecision(10, 2);

            e.HasIndex(s => s.DisplayOrder);
            e.HasIndex(s => s.Category);
        });

        // ============================================================
        // Review
        // ============================================================
        modelBuilder.Entity<Review>(e =>
        {
            e.Property(r => r.ClientName).IsRequired().HasMaxLength(120);
            e.Property(r => r.Text).IsRequired().HasMaxLength(2000);
            e.Property(r => r.Source).HasMaxLength(60);

            e.HasIndex(r => r.IsApproved);
            e.HasIndex(r => new { r.IsFeatured, r.DisplayOrder });
        });

        // ============================================================
        // BookingRequest
        // ============================================================
        modelBuilder.Entity<BookingRequest>(e =>
        {
            e.Property(b => b.ClientName).IsRequired().HasMaxLength(120);
            e.Property(b => b.ClientEmail).IsRequired().HasMaxLength(200);
            e.Property(b => b.ClientPhone).HasMaxLength(40);
            e.Property(b => b.Description).HasMaxLength(4000);
            e.Property(b => b.OwnerNotes).HasMaxLength(4000);
            e.Property(b => b.MagicToken).IsRequired().HasMaxLength(128);

            e.HasOne(b => b.Artist)
                .WithMany(a => a.BookingRequests)
                .HasForeignKey(b => b.ArtistId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(b => b.Service)
                .WithMany(s => s.BookingRequests)
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(b => b.Status);
            e.HasIndex(b => b.PreferredDateTime);
            e.HasIndex(b => b.MagicToken).IsUnique();
            e.HasIndex(b => b.ClientEmail);
        });

        // ============================================================
        // ContactMessage
        // ============================================================
        modelBuilder.Entity<ContactMessage>(e =>
        {
            e.Property(c => c.Name).IsRequired().HasMaxLength(120);
            e.Property(c => c.Email).IsRequired().HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(40);
            e.Property(c => c.Subject).IsRequired().HasMaxLength(200);
            e.Property(c => c.Message).IsRequired().HasMaxLength(4000);
            e.Property(c => c.OwnerNotes).HasMaxLength(4000);

            e.HasIndex(c => c.IsHandled);
            e.HasIndex(c => c.CreatedAt);
        });

        // ============================================================
        // SiteSetting
        // ============================================================
        modelBuilder.Entity<SiteSetting>(e =>
        {
            e.Property(s => s.Key).IsRequired().HasMaxLength(120);
            e.Property(s => s.Value).IsRequired().HasMaxLength(4000);
            e.Property(s => s.Description).HasMaxLength(300);
            e.Property(s => s.Group).HasMaxLength(60);

            e.HasIndex(s => s.Key).IsUnique();
            e.HasIndex(s => s.Group);
        });

        // ============================================================
        // AdminUser
        // ============================================================
        modelBuilder.Entity<AdminUser>(e =>
        {
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(120);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);

            e.HasIndex(u => u.Email).IsUnique();
        });
    }
}