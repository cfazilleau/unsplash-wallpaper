using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using UnsplashWallpaper.Models;
using UnsplashWallpaper.Services;
using UnsplashWallpaper.Views;

namespace UnsplashWallpaper;

public partial class App : Application
{
    private const string MutexName = "UnsplashWallpaper.SingleInstance.Mutex";
    private Mutex? _singleInstanceMutex;

    private TaskbarIcon? _tray;
    private PopupWindow? _popup;

    // Services (simple manual composition; exposed for the popup window).
    public SettingsService SettingsService { get; } = new();
    public HistoryService HistoryService { get; } = new();
    public UnsplashClient UnsplashClient { get; } = new();
    public WallpaperService WallpaperService { get; } = new();
    public WallpaperManager WallpaperManager { get; private set; } = null!;
    public SchedulerService Scheduler { get; private set; } = null!;

    private MenuItem? _pauseMenuItem;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance guard.
        _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Unsplash Wallpaper is already running (check the system tray).",
                "Unsplash Wallpaper", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        AppPaths.EnsureDirectories();
        SettingsService.Load();
        HistoryService.Load();

        WallpaperManager = new WallpaperManager(UnsplashClient, WallpaperService, HistoryService, SettingsService);
        Scheduler = new SchedulerService(WallpaperManager, SettingsService);

        WallpaperManager.Changed += OnWallpaperChanged;
        WallpaperManager.Failed += OnWallpaperFailed;

        BuildTrayIcon();
        Scheduler.Start();

        // Kick off an initial change if it's due (or we've never set one) and a key is present.
        _ = InitialChangeAsync();
    }

    private async Task InitialChangeAsync()
    {
        var s = SettingsService.Settings;
        if (string.IsNullOrWhiteSpace(s.AccessKey))
        {
            ShowBalloon("Welcome", "Add your Unsplash Access Key in Settings to get started.");
            OpenPopup(); // Guide the user straight to settings on first run.
            return;
        }

        var due = Scheduler.NextDueUtc();
        bool never = s.LastChangeUtc is null;
        if (never || (due is not null && DateTime.UtcNow >= due.Value))
        {
            await WallpaperManager.ChangeNowAsync();
        }
    }

    private void BuildTrayIcon()
    {
        _tray = new TaskbarIcon
        {
            ToolTipText = "Unsplash Wallpaper"
        };

        try
        {
            // Load the embedded .ico into a System.Drawing.Icon (most reliable icon source).
            var uri = new Uri("pack://application:,,,/Assets/tray.ico");
            var streamInfo = Application.GetResourceStream(uri);
            if (streamInfo is not null)
            {
                using var stream = streamInfo.Stream;
                _tray.Icon = new System.Drawing.Icon(stream);
            }
        }
        catch
        {
            // If the icon fails to load the tray still works with a default icon.
        }

        _tray.TrayLeftMouseUp += (_, _) => OpenPopup();

        var menu = new ContextMenu();

        var next = new MenuItem { Header = "Next wallpaper" };
        next.Click += async (_, _) => await ManualChangeAsync();
        menu.Items.Add(next);

        _pauseMenuItem = new MenuItem { Header = SettingsService.Settings.Paused ? "Resume" : "Pause" };
        _pauseMenuItem.Click += (_, _) => TogglePause();
        menu.Items.Add(_pauseMenuItem);

        menu.Items.Add(new Separator());

        var settings = new MenuItem { Header = "Settings…" };
        settings.Click += (_, _) => OpenPopup(PopupTab.Settings);
        menu.Items.Add(settings);

        var current = new MenuItem { Header = "Current image…" };
        current.Click += (_, _) => OpenPopup(PopupTab.Current);
        menu.Items.Add(current);

        menu.Items.Add(new Separator());

        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) => ExitApp();
        menu.Items.Add(exit);

        _tray.ContextMenu = menu;

        // Created in code (not XAML) means no Loaded event fires, so create the native icon now.
        _tray.ForceCreate();
    }

    private async Task ManualChangeAsync()
    {
        if (string.IsNullOrWhiteSpace(SettingsService.Settings.AccessKey))
        {
            ShowBalloon("No Access Key", "Add your Unsplash Access Key in Settings first.");
            OpenPopup(PopupTab.Settings);
            return;
        }
        await WallpaperManager.ChangeNowAsync();
    }

    private void TogglePause()
    {
        var s = SettingsService.Settings;
        s.Paused = !s.Paused;
        SettingsService.Save();
        if (_pauseMenuItem is not null)
            _pauseMenuItem.Header = s.Paused ? "Resume" : "Pause";
        ShowBalloon("Unsplash Wallpaper", s.Paused ? "Automatic changes paused." : "Automatic changes resumed.");
        _popup?.RefreshFromApp();
    }

    private void OnWallpaperChanged(WallpaperRecord record)
    {
        Dispatcher.Invoke(() =>
        {
            ShowBalloon("Wallpaper updated",
                string.IsNullOrEmpty(record.PhotographerName)
                    ? "New wallpaper applied."
                    : $"Photo by {record.PhotographerName} on Unsplash");
            _popup?.RefreshFromApp();
        });
    }

    private void OnWallpaperFailed(string message)
    {
        Dispatcher.Invoke(() =>
        {
            ShowBalloon("Wallpaper change failed", message);
            _popup?.RefreshFromApp();
        });
    }

    public void ShowBalloon(string title, string message)
    {
        try
        {
            _tray?.ShowNotification(title, message);
        }
        catch
        {
            // Notifications are best-effort.
        }
    }

    public void OpenPopup(PopupTab tab = PopupTab.Current)
    {
        if (_popup is null)
        {
            _popup = new PopupWindow();
            _popup.Closed += (_, _) => _popup = null;
        }

        _popup.SelectTab(tab);
        _popup.ReloadSettings();
        _popup.RefreshFromApp();

        if (!_popup.IsVisible)
            _popup.Show();

        _popup.Activate();
        _popup.Topmost = true;
        _popup.Topmost = false;
        _popup.Focus();
    }

    private void ExitApp()
    {
        Scheduler.Stop();
        _tray?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
