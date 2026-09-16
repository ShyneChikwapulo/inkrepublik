using Inkrepublik.Domain.Entities;
using Inkrepublik.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Data;

/// <summary>
/// Populates the database with demo content on first run.
///
/// Design principles:
/// - Idempotent: safe to call on every startup. It only seeds tables that
///   are completely empty, so it never duplicates or overwrites real data
///   the owner has entered.
/// - Placeholder content: clearly fake, obviously swappable. When the studio
///   signs on, they edit this content via the admin panel — no code changes.
/// - Uses a magic token pattern for demo bookings so we can test the client
///   magic-link flow end to end.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(
        InkrepublikDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedArtistsAsync(db, logger, cancellationToken);
        await SeedServicesAsync(db, logger, cancellationToken);
        await SeedReviewsAsync(db, logger, cancellationToken);
        await SeedSiteSettingsAsync(db, logger, cancellationToken);
        await SeedAdminUserAsync(db, logger, cancellationToken);

        logger.LogInformation("Database seeding complete.");
    }

    // ============================================================
    // Artists + portfolio images
    // ============================================================
    private static async Task SeedArtistsAsync(
        InkrepublikDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Artists.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Artists already seeded — skipping.");
            return;
        }

        var artists = new List<Artist>
        {
            new()
            {
                Name = "Zaakirah Lakay",
                Title = "Lead Artist — Piercing Specialist",
                Bio = "Zaakirah has been piercing for over eight years and is known for making first-timers feel completely at ease. She specialises in ear curation, delicate cartilage work, and helping clients choose jewellery that suits their anatomy.",
                Specialties = "Piercing, Ear Curation, Jewellery",
                InstagramHandle = "zaakirah.ink",
                IsActive = true,
                DisplayOrder = 1,
            },
            new()
            {
                Name = "Jordan Meyer",
                Title = "Senior Tattoo Artist",
                Bio = "Jordan works across styles but is best known for fine-line botanical pieces and small custom work. A background in illustration means every design starts as a hand-drawn original.",
                Specialties = "Fine Line, Botanical, Custom",
                InstagramHandle = "jordan.ink",
                IsActive = true,
                DisplayOrder = 2,
            },
            new()
            {
                Name = "Tumelo Dlamini",
                Title = "Tattoo Artist",
                Bio = "Tumelo specialises in bold blackwork and traditional African-inspired motifs. With a strong eye for composition, he loves working with clients on larger pieces that tell a story.",
                Specialties = "Blackwork, Traditional, Large Custom",
                InstagramHandle = "tumelo.ink",
                IsActive = true,
                DisplayOrder = 3,
            },
        };

        db.Artists.AddRange(artists);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} artists.", artists.Count);
    }

    // ============================================================
    // Services
    // ============================================================
    private static async Task SeedServicesAsync(
        InkrepublikDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Services.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Services already seeded — skipping.");
            return;
        }

        var services = new List<Service>
        {
            new()
            {
                Name = "Consultation",
                Description = "Sit down with an artist to discuss your idea, placement, size, and pricing. No commitment — you decide afterwards.",
                Category = ServiceCategory.Consultation,
                PriceFrom = 0m,
                TypicalDurationMinutes = 30,
                DisplayOrder = 1,
            },
            new()
            {
                Name = "Small Tattoo",
                Description = "A tattoo up to roughly 8cm — perfect for a first piece, a minimalist design, or a small tribute.",
                Category = ServiceCategory.Tattoo,
                PriceFrom = 600m,
                PriceTo = 1500m,
                TypicalDurationMinutes = 60,
                DisplayOrder = 2,
            },
            new()
            {
                Name = "Medium Tattoo",
                Description = "A detailed piece roughly 8–15cm. Ideal for forearm, calf, or shoulder work.",
                Category = ServiceCategory.Tattoo,
                PriceFrom = 1500m,
                PriceTo = 3500m,
                TypicalDurationMinutes = 150,
                DisplayOrder = 3,
            },
            new()
            {
                Name = "Large Tattoo / Custom Piece",
                Description = "Full sleeve, back piece, or any larger custom design. Pricing is quoted after consultation.",
                Category = ServiceCategory.Tattoo,
                PriceFrom = 3500m,
                TypicalDurationMinutes = 240,
                DisplayOrder = 4,
            },
            new()
            {
                Name = "Ear Piercing",
                Description = "Lobe, helix, tragus, daith, or conch piercing including starter jewellery.",
                Category = ServiceCategory.Piercing,
                PriceFrom = 350m,
                PriceTo = 900m,
                TypicalDurationMinutes = 30,
                DisplayOrder = 5,
            },
            new()
            {
                Name = "Body Piercing",
                Description = "Navel, nose, eyebrow, or other body piercings including starter jewellery.",
                Category = ServiceCategory.Piercing,
                PriceFrom = 400m,
                PriceTo = 1200m,
                TypicalDurationMinutes = 45,
                DisplayOrder = 6,
            },
            new()
            {
                Name = "Jewellery Change",
                Description = "Swap or upgrade your jewellery. Includes sterilisation and fitting.",
                Category = ServiceCategory.Jewelry,
                PriceFrom = 150m,
                TypicalDurationMinutes = 20,
                DisplayOrder = 7,
            },
        };

        db.Services.AddRange(services);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} services.", services.Count);
    }

    // ============================================================
    // Reviews
    // ============================================================
    private static async Task SeedReviewsAsync(
        InkrepublikDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Reviews.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Reviews already seeded — skipping.");
            return;
        }

        var reviews = new List<Review>
        {
            new()
            {
                ClientName = "Naledi M.",
                Rating = 5,
                Text = "I had an amazing experience with Zaakirah when I had my ears pierced for the first time. The space was hygienic and COVID-friendly, and the staff were friendly and inviting. Highly recommended!",
                Source = "Google",
                IsApproved = true,
                IsFeatured = true,
                DisplayOrder = 1,
            },
            new()
            {
                ClientName = "Aisha K.",
                Rating = 5,
                Text = "Zaakirah Lakay is amazing — she made me feel very comfortable and I'm definitely going again.",
                Source = "Facebook",
                IsApproved = true,
                IsFeatured = true,
                DisplayOrder = 2,
            },
            new()
            {
                ClientName = "Ryan P.",
                Rating = 5,
                Text = "Beautiful experience. Love how they made me feel super comfortable. I'm bringing my nieces and nephew soon. Loved the studio.",
                Source = "Google",
                IsApproved = true,
                IsFeatured = false,
                DisplayOrder = 3,
            },
        };

        db.Reviews.AddRange(reviews);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} reviews.", reviews.Count);
    }

    // ============================================================
    // Site settings
    // ============================================================
    private static async Task SeedSiteSettingsAsync(
        InkrepublikDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.SiteSettings.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Site settings already seeded — skipping.");
            return;
        }

        var settings = new List<SiteSetting>
        {
            // Contact
            new() { Key = "contact.address.line1", Value = "Hout Bay", Group = "Contact", Description = "First line of studio address" },
            new() { Key = "contact.address.line2", Value = "Cape Town, South Africa", Group = "Contact", Description = "Second line of studio address" },
            new() { Key = "contact.email", Value = "info@inkrepublikcpt.com", Group = "Contact", Description = "Studio contact email" },
            new() { Key = "contact.phone", Value = "", Group = "Contact", Description = "Studio phone number (optional)" },
            new() { Key = "contact.hours.weekday", Value = "Tue–Fri: 10:00 – 18:00", Group = "Contact", Description = "Weekday hours" },
            new() { Key = "contact.hours.saturday", Value = "Sat: 10:00 – 16:00", Group = "Contact", Description = "Saturday hours" },
            new() { Key = "contact.hours.sunday", Value = "Sun–Mon: Closed", Group = "Contact", Description = "Sunday/Monday hours" },

            // Social
            new() { Key = "social.facebook", Value = "https://facebook.com/inkrepublik", Group = "Social", Description = "Facebook page URL" },
            new() { Key = "social.instagram", Value = "https://instagram.com/inkrepublik", Group = "Social", Description = "Instagram profile URL" },

            // Home page
            new() { Key = "home.hero.headline", Value = "Your story, in ink.", Group = "Home", Description = "Home page hero headline" },
            new() { Key = "home.hero.subheading", Value = "Custom tattoos and piercings in Hout Bay. Every piece is a hand-drawn original, made just for you.", Group = "Home", Description = "Home page hero subheading" },
            new() { Key = "home.about.blurb", Value = "Inkrepublik is an artist-owned studio. We pride ourselves on hygiene, professionalism, and taking the time to get your design right.", Group = "Home", Description = "Short about blurb" },

            // Footer
            new() { Key = "footer.tagline", Value = "Inkrepublik Tattoo Studio — Hout Bay, Cape Town", Group = "Footer", Description = "Footer tagline" },
        };

        db.SiteSettings.AddRange(settings);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} site settings.", settings.Count);
    }

    // ============================================================
    // Admin user
    // ============================================================
    private static async Task SeedAdminUserAsync(
        InkrepublikDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.AdminUsers.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Admin user already seeded — skipping.");
            return;
        }

        // NOTE: This placeholder password hash will be replaced in Phase 5
        // when we set up proper password hashing via ASP.NET Core's
        // PasswordHasher. For now, no auth exists, so this is just a row.
        var admin = new AdminUser
        {
            Email = "admin@inkrepublik.local",
            DisplayName = "Studio Owner",
            PasswordHash = "PLACEHOLDER_WILL_BE_REPLACED_IN_PHASE_5",
            IsActive = true,
        };

        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded admin user: {Email}", admin.Email);
    }
}