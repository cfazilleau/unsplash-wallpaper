using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using UnsplashWallpaper.Models;

namespace UnsplashWallpaper.Services;

/// <summary>Keeps a bounded, persisted list of recently applied wallpapers.</summary>
public class HistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Most-recent-first list of applied wallpapers, bindable by the UI.</summary>
    public ObservableCollection<WallpaperRecord> Records { get; } = new();

    public WallpaperRecord? Current => Records.Count > 0 ? Records[0] : null;

    public void Load()
    {
        AppPaths.EnsureDirectories();
        try
        {
            if (File.Exists(AppPaths.HistoryPath))
            {
                var json = File.ReadAllText(AppPaths.HistoryPath);
                var loaded = JsonSerializer.Deserialize<List<WallpaperRecord>>(json, JsonOptions);
                if (loaded is not null)
                {
                    Records.Clear();
                    foreach (var r in loaded)
                        Records.Add(r);
                }
            }
        }
        catch
        {
            // Ignore corrupt history.
        }
    }

    public void Add(WallpaperRecord record, int limit)
    {
        Records.Insert(0, record);
        TrimAndCleanup(limit);
        Save();
    }

    private void TrimAndCleanup(int limit)
    {
        while (Records.Count > limit)
        {
            var removed = Records[^1];
            Records.RemoveAt(Records.Count - 1);
            TryDelete(removed.LocalPath);
            TryDelete(removed.ThumbPath);
        }
    }

    private static void TryDelete(string? path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cache cleanup.
        }
    }

    public void Save()
    {
        AppPaths.EnsureDirectories();
        var json = JsonSerializer.Serialize(Records.ToList(), JsonOptions);
        File.WriteAllText(AppPaths.HistoryPath, json);
    }
}
