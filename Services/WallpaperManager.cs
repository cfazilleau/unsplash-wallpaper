using System.IO;
using UnsplashWallpaper.Models;

namespace UnsplashWallpaper.Services;

/// <summary>
/// Orchestrates a full wallpaper change: fetch a photo, download it, apply it,
/// record it in history and update the last-change timestamp.
/// </summary>
public class WallpaperManager
{
    private readonly UnsplashClient _client;
    private readonly WallpaperService _wallpaper;
    private readonly HistoryService _history;
    private readonly SettingsService _settingsService;

    private readonly SemaphoreSlim _gate = new(1, 1);

    public WallpaperManager(UnsplashClient client, WallpaperService wallpaper,
        HistoryService history, SettingsService settingsService)
    {
        _client = client;
        _wallpaper = wallpaper;
        _history = history;
        _settingsService = settingsService;
    }

    /// <summary>Raised (name of photographer, or null) after a successful change.</summary>
    public event Action<WallpaperRecord>? Changed;

    /// <summary>Raised with a user-facing message when a change fails.</summary>
    public event Action<string>? Failed;

    /// <summary>Fetches and applies a brand-new wallpaper. Serialized against itself.</summary>
    public async Task<bool> ChangeNowAsync(CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(0, ct))
            return false; // A change is already in progress.

        try
        {
            var settings = _settingsService.Settings;
            var photo = await _client.GetRandomPhotoAsync(settings, ct);

            AppPaths.EnsureDirectories();
            var imagePath = Path.Combine(AppPaths.CacheDir, $"{photo.Id}.jpg");
            var thumbPath = Path.Combine(AppPaths.CacheDir, $"{photo.Id}_thumb.jpg");

            await _client.DownloadImageAsync(photo, settings.AccessKey, imagePath,
                WallpaperService.TargetWidth(), ct);
            var thumb = await _client.DownloadThumbAsync(photo, thumbPath, ct);

            _wallpaper.SetWallpaper(imagePath, settings.WallpaperStyle);

            var record = new WallpaperRecord
            {
                PhotoId = photo.Id,
                LocalPath = imagePath,
                ThumbPath = thumb,
                PhotographerName = photo.User.Name,
                PhotographerProfileUrl = photo.User.Links.Html,
                PhotoHtmlUrl = photo.Links.Html,
                Description = photo.Description ?? photo.AltDescription,
                AppliedUtc = DateTime.UtcNow
            };

            _history.Add(record, settings.HistoryLimit);

            settings.LastChangeUtc = record.AppliedUtc;
            _settingsService.Save();

            Changed?.Invoke(record);
            return true;
        }
        catch (UnsplashException ex)
        {
            Failed?.Invoke(ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Failed?.Invoke($"Could not change wallpaper: {ex.Message}");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Re-applies an existing history entry without fetching a new photo.</summary>
    public bool ReapplyFromHistory(WallpaperRecord record)
    {
        try
        {
            if (!File.Exists(record.LocalPath))
            {
                Failed?.Invoke("That image is no longer in the cache.");
                return false;
            }
            _wallpaper.SetWallpaper(record.LocalPath, _settingsService.Settings.WallpaperStyle);
            Changed?.Invoke(record);
            return true;
        }
        catch (Exception ex)
        {
            Failed?.Invoke($"Could not re-apply wallpaper: {ex.Message}");
            return false;
        }
    }
}
