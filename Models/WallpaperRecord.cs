namespace UnsplashWallpaper.Models;

/// <summary>A history entry describing a wallpaper that was applied.</summary>
public class WallpaperRecord
{
    public string PhotoId { get; set; } = string.Empty;

    /// <summary>Absolute path to the cached full-size image on disk.</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>Absolute path to a cached thumbnail for the History/Current UI.</summary>
    public string? ThumbPath { get; set; }

    public string PhotographerName { get; set; } = string.Empty;

    public string PhotographerProfileUrl { get; set; } = string.Empty;

    public string PhotoHtmlUrl { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime AppliedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Attribution link with the referral UTM parameters required by Unsplash.</summary>
    public string AttributedPhotoUrl =>
        AppendUtm(PhotoHtmlUrl);

    public string AttributedProfileUrl =>
        AppendUtm(PhotographerProfileUrl);

    private static string AppendUtm(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;
        var sep = url.Contains('?') ? '&' : '?';
        return $"{url}{sep}utm_source=UnsplashWallpaper&utm_medium=referral";
    }
}
