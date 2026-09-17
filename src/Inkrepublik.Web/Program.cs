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

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// Database
//
// We use AddDbContextFactory (not AddDbContext) because Blazor Server
// scopes services to the entire circuit, not per-request. Multiple
// components can call the DB concurrently, and DbContext is NOT
// thread-safe. The factory lets each operation create its own short-
// lived context.
// ------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found in configuration.");

builder.Services.AddDbContextFactory<InkrepublikDbContext>(options =>
    options.UseSqlServer(connectionString));
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
// Database migration + seed (development convenience)
//
// In development we auto-apply migrations and seed on startup so the
// developer never has to remember to run `dotnet ef database update`.
// In production this is gated by config (Phase 7).
// ------------------------------------------------------------
if (app.Environment.IsDevelopment())
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
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, logger, adminEmail, adminPassword, hasher.Hash);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration or seeding failed on startup.");
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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();