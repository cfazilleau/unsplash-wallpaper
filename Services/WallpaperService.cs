using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using UnsplashWallpaper.Models;

namespace UnsplashWallpaper.Services;

/// <summary>Sets the Windows desktop wallpaper and its display style.</summary>
public partial class WallpaperService
{
    private const int SPI_SETDESKWALLPAPER = 0x0014;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDWININICHANGE = 0x02;

    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CXSCREEN = 0;

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int nIndex);

    /// <summary>A sensible target width (in pixels) for the downloaded image.</summary>
    public static int TargetWidth()
    {
        int virt = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int primary = GetSystemMetrics(SM_CXSCREEN);
        int width = Math.Max(virt, primary);
        // Bump modest values up so single-monitor / non-DPI-aware setups still get a crisp image.
        return Math.Clamp(width <= 0 ? 2560 : width, 1920, 5120);
    }

    /// <summary>Applies the given local image file as the desktop wallpaper.</summary>
    public void SetWallpaper(string imagePath, WallpaperStyle style)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Wallpaper image not found.", imagePath);

        ApplyStyleRegistry(style);

        bool ok = SystemParametersInfo(
            SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

        if (!ok)
            throw new InvalidOperationException("Windows rejected the wallpaper change (SystemParametersInfo failed).");
    }

    private static void ApplyStyleRegistry(WallpaperStyle style)
    {
        // HKCU\Control Panel\Desktop : WallpaperStyle / TileWallpaper
        // Fill=10, Fit=6, Stretch=2, Center=0, Tile=(0 + TileWallpaper=1)
        (string wallpaperStyle, string tile) = style switch
        {
            WallpaperStyle.Fill => ("10", "0"),
            WallpaperStyle.Fit => ("6", "0"),
            WallpaperStyle.Stretch => ("2", "0"),
            WallpaperStyle.Center => ("0", "0"),
            WallpaperStyle.Tile => ("0", "1"),
            _ => ("10", "0")
        };

        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
        if (key is null)
            return;
        key.SetValue("WallpaperStyle", wallpaperStyle, RegistryValueKind.String);
        key.SetValue("TileWallpaper", tile, RegistryValueKind.String);
    }
}
