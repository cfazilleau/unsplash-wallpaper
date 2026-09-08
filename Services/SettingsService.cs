using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnsplashWallpaper.Models;

namespace UnsplashWallpaper.Services;

/// <summary>Loads and saves <see cref="AppSettings"/> as JSON in %APPDATA%.</summary>
public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler? SettingsSaved;

    public void Load()
    {
        AppPaths.EnsureDirectories();
        try
        {
            if (File.Exists(AppPaths.SettingsPath))
            {
                var json = File.ReadAllText(AppPaths.SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                    Settings = loaded;
            }
        }
        catch
        {
            // Corrupt/unreadable settings fall back to defaults rather than crashing.
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        AppPaths.EnsureDirectories();
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        File.WriteAllText(AppPaths.SettingsPath, json);
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }
}
