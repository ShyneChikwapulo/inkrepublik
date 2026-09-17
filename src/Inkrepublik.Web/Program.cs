using Inkrepublik.Data;
using Inkrepublik.Web.Components;
using Microsoft.EntityFrameworkCore;
using Inkrepublik.Services.Artists;
using Inkrepublik.Services.Services;
using Inkrepublik.Services.Reviews;
using Inkrepublik.Services.SiteSettings;
using Inkrepublik.Services.Contact;

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
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, logger);
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
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();