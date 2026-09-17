namespace Inkrepublik.Services;

/// <summary>
/// Studio-wide config — owner contact, public URL used in emails.
/// </summary>
public class StudioOptions
{
    public const string SectionName = "Studio";

    public string OwnerEmail { get; set; } = "info@inkrepublikcpt.com";

    /// <summary>
    /// The base URL of the public site, used to build links in emails
    /// (e.g. magic-link URLs). No trailing slash.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "https://localhost:7112";
}