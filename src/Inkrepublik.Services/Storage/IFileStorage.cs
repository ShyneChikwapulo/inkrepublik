namespace Inkrepublik.Services.Storage;

/// <summary>
/// Storage abstraction for uploaded files. The current implementation
/// saves to wwwroot/uploads; a future version could save to S3/R2
/// without changing any calling code.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves a file to the given relative path (under the storage root).
    /// Returns the public URL path (e.g. "/uploads/artists/5/profile.jpg").
    /// Throws on invalid file type, size, or I/O failure.
    /// </summary>
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string relativePath,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a file by its public URL path (as returned by SaveAsync).
    /// No-op if the file doesn't exist.
    /// </summary>
    Task DeleteAsync(string publicPath, CancellationToken ct = default);
}