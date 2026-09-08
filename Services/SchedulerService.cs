using System.Windows.Threading;

namespace UnsplashWallpaper.Services;

/// <summary>
/// Fires a wallpaper change when the configured interval has elapsed since the last change.
/// The check runs on a coarse timer so the schedule survives restarts (based on LastChangeUtc).
/// </summary>
public class SchedulerService
{
    private readonly WallpaperManager _manager;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _timer;

    public SchedulerService(WallpaperManager manager, SettingsService settingsService)
    {
        _manager = manager;
        _settingsService = settingsService;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += async (_, _) => await TickAsync();
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    /// <summary>When the next automatic change is due (null if paused).</summary>
    public DateTime? NextDueUtc()
    {
        var s = _settingsService.Settings;
        if (s.Paused || s.IntervalMinutes <= 0)
            return null;
        var last = s.LastChangeUtc ?? DateTime.MinValue;
        return last == DateTime.MinValue ? DateTime.UtcNow : last.AddMinutes(s.IntervalMinutes);
    }

    private async Task TickAsync()
    {
        var s = _settingsService.Settings;
        if (s.Paused || s.IntervalMinutes <= 0 || string.IsNullOrWhiteSpace(s.AccessKey))
            return;

        var due = NextDueUtc();
        if (due is not null && DateTime.UtcNow >= due.Value)
        {
            await _manager.ChangeNowAsync();
        }
    }
}
