using Inkrepublik.Data;
using Inkrepublik.Web.Components;
using Microsoft.EntityFrameworkCore;
using Inkrepublik.Services.Artists;
using Inkrepublik.Services.Services;
using Inkrepublik.Services.Reviews;
using Inkrepublik.Services.SiteSettings;
using Inkrepublik.Services.Contact;
using Inkrepublik.Services.Bookings;
using Inkrepublik.Services.Emails;
using Inkrepublik.Services;
using Inkrepublik.Services.Admin;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Inkrepublik.Services.Storage;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// Database
//
// The provider is selected by the DatabaseProvider config value:
//   "SqlServer" (default) or "Sqlite".
//
// Local dev uses SqlServer via docker-compose.
// Render production uses Sqlite (ephemeral file, but simple).
// ------------------------------------------------------------
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    var sqlitePath = builder.Configuration.GetValue<string>("DatabasePath") ?? "inkrepublik.db";
    var fullPath = Path.IsPathRooted(sqlitePath)
        ? sqlitePath
        : Path.Combine(builder.Environment.ContentRootPath, sqlitePath);

    builder.Services.AddDbContextFactory<InkrepublikDbContext>(options =>
        options.UseSqlite($"Data Source={fullPath}"));

    Console.WriteLine($"✓ Database provider: Sqlite ({fullPath})");
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' not found in configuration.");

    builder.Services.AddDbContextFactory<InkrepublikDbContext>(options =>
        options.UseSqlServer(connectionString));

    Console.WriteLine("✓ Database provider: SqlServer");
}
    // ------------------------------------------------------------
    // Application services (read-side queries)
    // ------------------------------------------------------------
builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ISiteSettingService, SiteSettingService>();   
builder.Services.AddScoped<IContactService, ContactService>(); 
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminBookingService, AdminBookingService>();
builder.Services.AddScoped<IAdminCalendarService, AdminCalendarService>();
builder.Services.AddScoped<IAdminServiceCatalogService, AdminServiceCatalogService>();
builder.Services.AddScoped<IAdminArtistService, AdminArtistService>();
builder.Services.AddScoped<IAdminReviewService, AdminReviewService>();
builder.Services.AddScoped<IAdminContactService, AdminContactService>();
builder.Services.AddScoped<IAdminSiteSettingService, AdminSiteSettingService>();

// ------------------------------------------------------------
// File storage
// ------------------------------------------------------------
var uploadsRoot = Path.Combine(builder.Environment.WebRootPath, "uploads");
builder.Services.AddSingleton<IFileStorage>(sp =>
    new LocalFileStorage(uploadsRoot, sp.GetRequiredService<ILogger<LocalFileStorage>>()));


// ------------------------------------------------------------
// Admin authentication
// ------------------------------------------------------------
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.AccessDeniedPath = "/admin/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "inkrepublik.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// ------------------------------------------------------------
// Email
//
// In development we use ConsoleEmailSender (no SMTP needed — emails
// print to the console). In production we swap to SmtpEmailSender.
// The choice is based on the Smtp:Enabled config flag.
// ------------------------------------------------------------
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));

builder.Services.Configure<StudioOptions>(
    builder.Configuration.GetSection(StudioOptions.SectionName));

var smtpEnabled = builder.Configuration.GetValue<bool>("Smtp:Enabled");
var smtpHost = builder.Configuration.GetValue<string>("Smtp:Host") ?? "(unset)";

if (smtpEnabled)
{
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
    Console.WriteLine($"✓ Email: SMTP enabled (host: {smtpHost})");
}
else
{
    builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
    Console.WriteLine("✓ Email: console mode (SMTP disabled)");
}

// ------------------------------------------------------------
// Booking
// ------------------------------------------------------------
builder.Services.AddScoped<IBookingService, BookingService>();

// ------------------------------------------------------------
// Blazor
// ------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ------------------------------------------------------------
// Database initialization on startup.
//
// SqlServer: apply migrations.
// Sqlite:    create schema directly (no migrations — the demo
//            deployment is single-use and doesn't need incremental
//            schema changes). When the studio signs on, we'll
//            switch to a proper Postgres setup with migrations.
//
// The seeder is idempotent, so this is safe to run on every start.
// ------------------------------------------------------------
{
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<InkrepublikDbContext>>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var adminEmail = builder.Configuration.GetValue<string>("Admin:Email");
    var adminPassword = builder.Configuration.GetValue<string>("Admin:Password");

    try
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.MigrateAsync();
        }

        await DbSeeder.SeedAsync(db, logger, adminEmail, adminPassword, hasher.Hash);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed on startup.");
        throw;
    }
}

// ------------------------------------------------------------
// HTTP pipeline
// ------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// ------------------------------------------------------------
// Serve runtime-uploaded files. Must run BEFORE MapStaticAssets
// so we get first crack at /uploads/* paths.
// ------------------------------------------------------------
var uploadsPhysicalPath = Path.Combine(app.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPhysicalPath))
{
    Directory.CreateDirectory(uploadsPhysicalPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPhysicalPath),
    RequestPath = "/uploads",
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings = { [".webp"] = "image/webp" },
    },
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();