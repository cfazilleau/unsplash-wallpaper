using System.Text.Json.Serialization;

namespace UnsplashWallpaper.Models;

public enum SourceMode
{
    Random,
    SearchTerms,
    Topics,
    Collections
}

public enum WallpaperStyle
{
    Fill,
    Fit,
    Stretch,
    Center,
    Tile
}

public enum Orientation
{
    Landscape,
    Portrait,
    Squarish,
    Any
}

/// <summary>
/// User-configurable settings, persisted as JSON in %APPDATA%\UnsplashWallpaper\settings.json.
/// </summary>
public class AppSettings
{
    /// <summary>Unsplash API Access Key (Client-ID). Required; entered in the Settings tab.</summary>
    public string AccessKey { get; set; } = string.Empty;

    public SourceMode SourceMode { get; set; } = SourceMode.Random;

    /// <summary>Comma/space separated search terms, e.g. "mountains, minimal".</summary>
    public string SearchTerms { get; set; } = "nature";

    /// <summary>Comma separated Unsplash topic slugs, e.g. "wallpapers,nature".</summary>
    public string TopicSlugs { get; set; } = "wallpapers";

    /// <summary>Comma separated Unsplash collection IDs.</summary>
    public string CollectionIds { get; set; } = string.Empty;

    public Orientation Orientation { get; set; } = Orientation.Landscape;

    /// <summary>How often to fetch a new wallpaper, in minutes. Default 24h.</summary>
    public int IntervalMinutes { get; set; } = 1440;

    public WallpaperStyle WallpaperStyle { get; set; } = WallpaperStyle.Fill;

    public bool RunAtStartup { get; set; } = false;

    public bool Paused { get; set; } = false;

    /// <summary>UTC timestamp of the last successful wallpaper change (for schedule persistence).</summary>
    public DateTime? LastChangeUtc { get; set; }

    /// <summary>Max number of history entries to keep.</summary>
    [JsonIgnore]
    public int HistoryLimit => 30;
}
