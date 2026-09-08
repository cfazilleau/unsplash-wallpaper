using System.Text.Json.Serialization;

namespace UnsplashWallpaper.Models;

/// <summary>DTOs mapping the relevant subset of the Unsplash /photos/random response.</summary>
public class UnsplashPhoto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("alt_description")]
    public string? AltDescription { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("urls")]
    public PhotoUrls Urls { get; set; } = new();

    [JsonPropertyName("links")]
    public PhotoLinks Links { get; set; } = new();

    [JsonPropertyName("user")]
    public PhotoUser User { get; set; } = new();
}

public class PhotoUrls
{
    [JsonPropertyName("raw")]
    public string Raw { get; set; } = string.Empty;

    [JsonPropertyName("full")]
    public string Full { get; set; } = string.Empty;

    [JsonPropertyName("regular")]
    public string Regular { get; set; } = string.Empty;

    [JsonPropertyName("small")]
    public string Small { get; set; } = string.Empty;

    [JsonPropertyName("thumb")]
    public string Thumb { get; set; } = string.Empty;
}

public class PhotoLinks
{
    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    /// <summary>Endpoint to ping (per API guidelines) when a photo is actually used.</summary>
    [JsonPropertyName("download_location")]
    public string DownloadLocation { get; set; } = string.Empty;
}

public class PhotoUser
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("links")]
    public UserLinks Links { get; set; } = new();
}

public class UserLinks
{
    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;
}
