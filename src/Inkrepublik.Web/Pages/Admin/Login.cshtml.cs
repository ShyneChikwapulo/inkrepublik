using System.Security.Claims;
using Inkrepublik.Services.Admin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Inkrepublik.Web.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly IAdminAuthService _auth;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IAdminAuthService auth, ILogger<LoginModel> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        // If already authenticated, go straight to admin home.
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(returnUrl ?? "/admin");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var password = Request.Form["Password"].ToString();

        var user = await _auth.ValidateCredentialsAsync(Email, password);

        if (user is null)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, "Admin"),
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            });

        await _auth.RecordLoginAsync(user.Id);

        _logger.LogInformation("Admin {Email} signed in.", user.Email);

        var redirectTo = string.IsNullOrWhiteSpace(returnUrl) ? "/admin" : returnUrl;
        return Redirect(redirectTo);
    }
}