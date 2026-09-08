# <img src="Assets/icon.png" alt="" height="28" valign="middle" /> Unsplash Wallpaper

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

### Download & run

Grab the latest `UnsplashWallpaper-vX.Y.Z-win-x64.exe` from the
[**Releases**](../../releases) page and double-click it. It's a single self-contained file —
no installer, no .NET runtime needed. The app lives in your system tray; if you don't see the
icon, click the `^` overflow arrow next to the clock.

> [!IMPORTANT]
> The executable is **unsigned**, so Windows SmartScreen may show a
> *"Windows protected your PC"* warning the first time you run it. This is expected for
> apps without a paid code-signing certificate — click **More info → Run anyway** to
> continue.

### Get a free Unsplash Access Key

The app needs an Unsplash API key to fetch photos. It's free and takes about a minute:

1. Open <https://unsplash.com/developers> and sign in (create an account if needed).
2. Click **New Application**, accept the terms, and give it any name.
3. Copy the **Access Key** — the value labelled *Client-ID* (**not** the Secret Key).

> The free *Demo* tier allows 50 requests/hour — far more than wallpaper changes ever need.

### Paste the key

On first launch the **Settings** popup opens automatically. Paste your Access Key, choose an
image source and how often to change, and you're set — the first wallpaper appears right away.

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

## 📝 Attribution & disclaimer

Photos are provided by [Unsplash](https://unsplash.com) and their respective photographers.
The app follows the [Unsplash API guidelines](https://help.unsplash.com/en/articles/2511245-unsplash-api-guidelines):
it credits each photographer with referral links and triggers the required download endpoint
whenever a photo is applied.

That said, we're aware this use case isn't permitted under the
[Unsplash API Terms](https://unsplash.com/api-terms) — automatically downloading photos to set
them as wallpapers falls outside their allowed usage. This project exists only because the
**official [Unsplash Wallpapers](https://apps.apple.com/app/unsplash-wallpapers/id1284863847)
app is macOS-only**; it's essentially an unofficial Windows port for personal use. It is not
affiliated with, endorsed by, or connected to Unsplash in any way. Use at your own discretion.

## 📄 License

MIT — see [LICENSE](LICENSE).
