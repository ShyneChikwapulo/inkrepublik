using System.Security.Cryptography;

namespace Inkrepublik.Services.Bookings;

/// <summary>
/// Generates cryptographically secure, URL-safe tokens for booking magic links.
///
/// Design notes:
///   - 32 bytes of entropy = 256 bits. Brute force is computationally infeasible.
///   - Base64Url encoding (RFC 4648 §5) means no padding, no +/ characters —
///     safe to embed directly in URLs and emails.
///   - We could use GUIDs, but they're only 122 bits and often predictably
///     formatted. Base64Url of RandomNumberGenerator output is strictly better.
/// </summary>
public static class MagicTokenGenerator
{
    private const int TokenBytes = 32;

    public static string NewToken()
    {
        Span<byte> buffer = stackalloc byte[TokenBytes];
        RandomNumberGenerator.Fill(buffer);

        // Base64Url: standard base64 with + → -, / → _, and padding stripped.
        return Convert.ToBase64String(buffer)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}