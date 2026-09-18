using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Storage;

/// <summary>
/// Saves uploaded files to the local filesystem under a configured root.
///
/// PATH CONVENTION:
///   - Callers pass and receive paths RELATIVE to the storage root.
///     e.g. "artists/5/profile.jpg" — NOT "/uploads/artists/5/profile.jpg".
///   - The storage layer is unaware of the public "/uploads" prefix.
///   - When callers need a URL for a database record or img src, they
///     prepend "/uploads/" themselves.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    public const string PublicPrefix = "/uploads";

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp",
        };

    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]],
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly string _rootPath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(string rootPath, ILogger<LocalFileStorage> logger)
    {
        _rootPath = rootPath;
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string relativePath,
        CancellationToken ct = default)
    {
        if (content is null || !content.CanRead)
            throw new ArgumentException("Content stream is required.", nameof(content));

        var cleanRelative = NormalizeRelative(relativePath);

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"File type '{ext}' is not allowed. Use JPG, PNG, or WEBP.");

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        if (buffer.Length > MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"File exceeds {MaxFileSizeBytes / 1024 / 1024} MB limit.");

        if (buffer.Length == 0)
            throw new InvalidOperationException("File is empty.");

        if (!HasValidMagicBytes(buffer, ext))
            throw new InvalidOperationException(
                "File doesn't look like a valid image.");

        var fullPath = Path.Combine(_rootPath, cleanRelative);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException("Invalid storage path.");

        Directory.CreateDirectory(directory);

        buffer.Position = 0;
        await using (var fs = File.Create(fullPath))
        {
            await buffer.CopyToAsync(fs, ct);
        }

        // Return the PUBLIC path so callers can store it in the DB and use
        // it directly in <img src="...">.
        var publicUrl = PublicPrefix + "/" + cleanRelative;
        _logger.LogInformation("Saved file to {Path}", publicUrl);

        return publicUrl;
    }

    public Task DeleteAsync(string publicPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(publicPath))
            return Task.CompletedTask;

        // Accept both "/uploads/artists/..." and "artists/...".
        var cleanRelative = NormalizeRelative(publicPath);

        var fullPath = Path.Combine(_rootPath, cleanRelative);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file {Path}", publicPath);
        }
        else
        {
            _logger.LogDebug("Delete requested but file not found: {Path}", fullPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Normalizes any path form into a clean relative path under the root.
    /// Handles: leading/trailing slashes, backslashes, and an optional
    /// leading "uploads/" segment.
    /// </summary>
    private static string NormalizeRelative(string path)
    {
        var cleaned = path.Trim().TrimStart('/', '\\').Replace('\\', '/');

        // Strip a leading "uploads/" if the caller passed the public form.
        const string uploadsPrefix = "uploads/";
        if (cleaned.StartsWith(uploadsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[uploadsPrefix.Length..];
        }

        return cleaned;
    }

    private static bool HasValidMagicBytes(Stream stream, string ext)
    {
        if (!MagicBytes.TryGetValue(ext, out var signatures))
            return false;

        var longest = signatures.Max(s => s.Length);
        var header = new byte[longest];
        var read = stream.Read(header, 0, longest);

        foreach (var signature in signatures)
        {
            if (read >= signature.Length &&
                header.AsSpan(0, signature.Length).SequenceEqual(signature))
            {
                return true;
            }
        }

        return false;
    }
}