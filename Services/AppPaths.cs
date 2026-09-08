using System.IO;

namespace UnsplashWallpaper.Services;

/// <summary>Centralized on-disk locations for settings, history and the image cache.</summary>
public static class AppPaths
{
    public const string AppFolderName = "UnsplashWallpaper";

    public static string ConfigDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);

    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

    public static string CacheDir { get; } = Path.Combine(DataDir, "cache");

    public static string SettingsPath { get; } = Path.Combine(ConfigDir, "settings.json");

    public static string HistoryPath { get; } = Path.Combine(DataDir, "history.json");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(ConfigDir);
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(CacheDir);
    }
}
