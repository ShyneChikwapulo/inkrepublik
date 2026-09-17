using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Storage;

/// <summary>
/// Saves uploaded files to the local filesystem under a configured root.
/// In production, the root is set to the app's wwwroot folder so files
/// are served as static content.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp",
        };

    /// <summary>
    /// Basic magic-byte checks for image content. Not a full MIME parser,
    /// but blocks non-image files being renamed as images.
    /// </summary>
    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]], // "RIFF"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

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

        // Normalize: strip any leading slash from the relative path so we
        // control exactly where it lands under the root.
        var cleanRelative = relativePath
            .TrimStart('/', '\\')
            .Replace('\\', '/');

        // Derive extension from the ORIGINAL filename; we never trust the
        // client-supplied path beyond the extension.
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException(
                $"File type '{ext}' is not allowed. Use JPG, PNG, or WEBP.");
        }

        // Read into a buffer so we can inspect magic bytes and enforce size.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        if (buffer.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File exceeds {MaxFileSizeBytes / 1024 / 1024} MB limit.");
        }

        if (buffer.Length == 0)
            throw new InvalidOperationException("File is empty.");

        // Magic-byte check.
        if (!HasValidMagicBytes(buffer, ext))
        {
            throw new InvalidOperationException(
                "File doesn't look like a valid image.");
        }

        // Compute full destination path.
        var fullPath = Path.Combine(_rootPath, cleanRelative);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException("Invalid storage path.");

        Directory.CreateDirectory(directory);

        // Reset and write.
        buffer.Position = 0;
        await using (var fs = File.Create(fullPath))
        {
            await buffer.CopyToAsync(fs, ct);
        }

        var publicUrl = "/" + cleanRelative;
        _logger.LogInformation("Saved file to {Path}", publicUrl);

        return publicUrl;
    }

    public Task DeleteAsync(string publicPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(publicPath))
            return Task.CompletedTask;

        // Only delete files under the uploads folder — never anything else.
        var cleanRelative = publicPath.TrimStart('/', '\\').Replace('\\', '/');

        if (!cleanRelative.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Refusing to delete non-upload path: {Path}", publicPath);
            return Task.CompletedTask;
        }

        var fullPath = Path.Combine(_rootPath, cleanRelative);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file {Path}", publicPath);
        }

        return Task.CompletedTask;
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