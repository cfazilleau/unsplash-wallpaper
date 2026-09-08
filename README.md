# 🖼️ Unsplash Wallpaper

A lightweight **Windows system-tray** app that automatically refreshes your desktop
wallpaper with beautiful photos from [Unsplash](https://unsplash.com), on a schedule you
choose. Click the tray icon for a compact popup showing the current photo, your settings,
and a history of past wallpapers.

Built with **.NET 10 / WPF**. Small, native, and stays out of your way.

<p align="center">
  <img src="Media/current.png" alt="Current photo tab" width="30%" />
  <img src="Media/settings.png" alt="Settings tab" width="30%" />
  <img src="Media/history.png" alt="History tab" width="30%" />
</p>

<p align="center">
  <em>Current&nbsp;·&nbsp;Settings&nbsp;·&nbsp;History</em>
</p>

> [!NOTE]
> This project was generated with the help of AI (Claude). The code has been reviewed
> and tested, but keep that in mind when reading, reusing, or contributing to it.

## ✨ Features

- **Lives in the system tray** — no taskbar clutter, dismisses itself when it loses focus.
- **Automatic refresh** on a configurable interval (60 = hourly, 1440 = daily, 10080 = weekly),
  plus a manual **skip** button. The schedule survives restarts.
- **Flexible image sources**, switchable at runtime:
  - Fully random / editorial
  - Random by **search terms** (e.g. `mountains, minimal`)
  - Unsplash **topics** (slugs like `wallpapers, nature`)
  - Unsplash **collections** (by ID)
- **Fit styles**: Fill · Fit · Stretch · Center · Tile.
- **Photo info + attribution** — photographer name and direct links to the photo and profile
  on Unsplash (with the referral links their API guidelines require).
- **History** with thumbnails and one-click **Set again**.
- **Pause / resume** automatic changes from the tray menu or the bottom bar.
- **Run at Windows startup** (optional).
- **Dark theme**, masked API-key field, and **autosave** — no Save button, changes persist as
  you make them.

## 🚀 Getting started

### 1. Get a free Unsplash Access Key

The official Unsplash API requires a key (the old keyless endpoint was retired).

1. Go to <https://unsplash.com/developers> and sign in.
2. **New Application** → accept the API terms → name it.
3. Copy the **Access Key** (the *Client-ID*, not the Secret Key).

The free *Demo* tier allows 50 requests/hour — far more than wallpaper changes ever need.

### 2. Install

Download the latest `UnsplashWallpaper-vX.Y.Z-win-x64.exe` from the
[**Releases**](../../releases) page and run it. It's a self-contained build — no .NET
runtime required.

> [!IMPORTANT]
> The executable is **unsigned**, so Windows SmartScreen may show a
> *"Windows protected your PC"* warning the first time you run it. This is expected for
> apps without a paid code-signing certificate — click **More info → Run anyway** to
> continue.

### 3. Configure

On first launch the **Settings** popup opens automatically. Paste your Access Key, pick a
source and interval, and you're done — the first wallpaper is fetched right away.

## 🛠️ Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/cfazilleau/unsplash-wallpaper.git
cd unsplash-wallpaper
dotnet run
```

### Publish a standalone executable

```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The single `UnsplashWallpaper.exe` lands in
`bin/Release/net10.0-windows/win-x64/publish/`.

## 📦 Releases

Pushing a version tag builds and publishes a GitHub Release automatically
(see [`.github/workflows/release.yml`](.github/workflows/release.yml)):

```bash
git tag v1.0.0
git push origin v1.0.0
```

## 📂 Where things are stored

| Data | Location |
|------|----------|
| Settings | `%APPDATA%\UnsplashWallpaper\settings.json` |
| History  | `%LOCALAPPDATA%\UnsplashWallpaper\history.json` |
| Image cache | `%LOCALAPPDATA%\UnsplashWallpaper\cache\` |

## 🧱 Project layout

| Path | Purpose |
|------|---------|
| `App.xaml(.cs)` | Startup, tray icon + menu, single-instance guard, wiring |
| `Models/` | `AppSettings`, `UnsplashPhoto` (API DTOs), `WallpaperRecord` |
| `Services/UnsplashClient.cs` | Unsplash API calls, image/thumbnail download, download-tracking |
| `Services/WallpaperService.cs` | Win32 `SystemParametersInfo` + registry fit style |
| `Services/WallpaperManager.cs` | Fetch → download → apply → record history |
| `Services/SchedulerService.cs` | Interval timer based on last-change timestamp |
| `Services/SettingsService.cs` · `HistoryService.cs` | JSON persistence |
| `Services/StartupService.cs` | HKCU `Run` key for launch-at-login |
| `Views/PopupWindow.xaml(.cs)` | Tabbed popup UI (dark theme) |
| `ViewModels/PopupViewModel.cs` | Bindable state, commands, autosave |

## 📝 Attribution

Photos are provided by [Unsplash](https://unsplash.com) and their respective photographers.
This app follows the [Unsplash API guidelines](https://help.unsplash.com/en/articles/2511245-unsplash-api-guidelines):
it credits photographers with referral links and triggers the required download endpoint
whenever a photo is applied.

## 📄 License

MIT — see [LICENSE](LICENSE).

## ⚠️ Disclaimer

We're aware that this use case isn't permitted under the
[Unsplash API Terms](https://unsplash.com/api-terms) — automatically downloading photos to
set them as wallpapers falls outside their allowed usage. This project exists only because
the **official [Unsplash Wallpapers](https://apps.apple.com/app/unsplash-wallpapers/id1284863847)
app is macOS-only**; it's essentially an unofficial Windows port for personal use.

It is not affiliated with, endorsed by, or connected to Unsplash in any way. Use at your
own discretion.
