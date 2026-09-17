using Inkrepublik.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Thin wrapper around ASP.NET Core's PasswordHasher. Wrapping it:
///   - centralizes password policy in one place
///   - makes the auth service easy to unit test
///   - hides the framework type from the rest of the codebase
/// </summary>
public interface IPasswordHasherService
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}

public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<AdminUser> _hasher = new();

    public string Hash(string password)
    {
        // The generic type is just a marker for the hasher — we don't use
        // an actual AdminUser instance here, so pass a throwaway.
        return _hasher.HashPassword(new AdminUser(), password);
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(
            new AdminUser(),
            hashedPassword,
            providedPassword);

        return result is PasswordVerificationResult.Success
                      or PasswordVerificationResult.SuccessRehashNeeded;
    }
}