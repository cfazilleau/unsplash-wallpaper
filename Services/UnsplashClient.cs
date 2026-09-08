using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using UnsplashWallpaper.Models;

namespace UnsplashWallpaper.Services;

/// <summary>Thrown for expected, user-actionable API problems (bad key, rate limit, no results).</summary>
public class UnsplashException : Exception
{
    public UnsplashException(string message) : base(message) { }
}

/// <summary>Talks to the official Unsplash API (https://api.unsplash.com).</summary>
public class UnsplashClient
{
    private const string BaseUrl = "https://api.unsplash.com";
    private readonly HttpClient _http;

    public UnsplashClient()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("UnsplashWallpaper/1.0");
        // API version header recommended by Unsplash.
        _http.DefaultRequestHeaders.Add("Accept-Version", "v1");
    }

    /// <summary>Fetches a random photo matching the current settings.</summary>
    public async Task<UnsplashPhoto> GetRandomPhotoAsync(AppSettings settings, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.AccessKey))
            throw new UnsplashException("No Unsplash Access Key set. Add one in Settings.");

        var query = HttpUtility.ParseQueryString(string.Empty);

        if (settings.Orientation != Orientation.Any)
            query["orientation"] = settings.Orientation.ToString().ToLowerInvariant();

        switch (settings.SourceMode)
        {
            case SourceMode.SearchTerms:
                var terms = NormalizeCsv(settings.SearchTerms);
                if (!string.IsNullOrWhiteSpace(terms))
                    query["query"] = terms;
                break;
            case SourceMode.Topics:
                var topics = NormalizeCsv(settings.TopicSlugs);
                if (!string.IsNullOrWhiteSpace(topics))
                    query["topics"] = topics;
                break;
            case SourceMode.Collections:
                var collections = NormalizeCsv(settings.CollectionIds);
                if (!string.IsNullOrWhiteSpace(collections))
                    query["collections"] = collections;
                break;
            case SourceMode.Random:
            default:
                break;
        }

        var url = $"{BaseUrl}/photos/random?{query}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", settings.AccessKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new UnsplashException($"Network error contacting Unsplash: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            throw new UnsplashException("Request to Unsplash timed out.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnsplashException("Unsplash rejected the Access Key (401). Check it in Settings.");
        if ((int)response.StatusCode == 403)
            throw new UnsplashException("Unsplash rate limit reached (403). Try again later.");
        if (!response.IsSuccessStatusCode)
            throw new UnsplashException($"Unsplash returned {(int)response.StatusCode}. Check your source settings.");

        var body = await response.Content.ReadAsStringAsync(ct);

        // With no `count`, the endpoint returns a single object. Guard against an array just in case.
        UnsplashPhoto? photo;
        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('['))
        {
            var list = JsonSerializer.Deserialize<List<UnsplashPhoto>>(body);
            photo = list is { Count: > 0 } ? list[0] : null;
        }
        else
        {
            photo = JsonSerializer.Deserialize<UnsplashPhoto>(body);
        }

        if (photo is null || string.IsNullOrEmpty(photo.Urls.Raw))
            throw new UnsplashException("No photo returned for the current source. Try different terms/topics.");

        return photo;
    }

    /// <summary>
    /// Downloads the full image to disk. Requests a sized variant off the raw URL so we get a
    /// wallpaper-appropriate resolution, and pings the download endpoint per API guidelines.
    /// </summary>
    public async Task<string> DownloadImageAsync(UnsplashPhoto photo, string accessKey, string destinationPath,
        int targetWidth, CancellationToken ct = default)
    {
        // Compose a download URL off the raw url with Imgix params for a crisp, correctly-sized image.
        var raw = photo.Urls.Raw;
        var sep = raw.Contains('?') ? '&' : '?';
        var downloadUrl = $"{raw}{sep}w={targetWidth}&q=90&fm=jpg&fit=max";

        try
        {
            var bytes = await _http.GetByteArrayAsync(downloadUrl, ct);
            await File.WriteAllBytesAsync(destinationPath, bytes, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new UnsplashException($"Failed to download image: {ex.Message}");
        }

        // Fire-and-forget the mandatory download tracking ping (do not let it fail the flow).
        _ = TriggerDownloadAsync(photo, accessKey);
        return destinationPath;
    }

    private async Task TriggerDownloadAsync(UnsplashPhoto photo, string accessKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(photo.Links.DownloadLocation))
                return;
            using var request = new HttpRequestMessage(HttpMethod.Get, photo.Links.DownloadLocation);
            request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", accessKey);
            await _http.SendAsync(request);
        }
        catch
        {
            // Non-fatal per guidelines; ignore failures.
        }
    }

    /// <summary>Downloads a small thumbnail for the History/Current UI.</summary>
    public async Task<string?> DownloadThumbAsync(UnsplashPhoto photo, string destinationPath,
        CancellationToken ct = default)
    {
        try
        {
            var bytes = await _http.GetByteArrayAsync(photo.Urls.Small, ct);
            await File.WriteAllBytesAsync(destinationPath, bytes, ct);
            return destinationPath;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeCsv(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var parts = input
            .Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0);
        return string.Join(",", parts);
    }
}
