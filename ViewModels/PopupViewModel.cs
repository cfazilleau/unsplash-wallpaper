using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using UnsplashWallpaper.Models;
using UnsplashWallpaper.Services;

namespace UnsplashWallpaper.ViewModels;

/// <summary>Backing view model for the popup window (Current / Settings / History).</summary>
public class PopupViewModel : INotifyPropertyChanged
{
    private readonly App _app;
    private readonly DispatcherTimer _saveTimer;
    private bool _suppressSave;

    public PopupViewModel()
    {
        _app = (App)System.Windows.Application.Current;
        History = _app.HistoryService.Records;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) => CommitSave();

        NextWallpaperCommand = new RelayCommand(async () => await NextWallpaperAsync(), () => !IsBusy);
        TogglePauseCommand = new RelayCommand(TogglePause);
        ReapplyCommand = new RelayCommand(p => Reapply(p as WallpaperRecord));
        OpenUrlCommand = new RelayCommand(p => OpenUrl(p as string));
        OpenDevPortalCommand = new RelayCommand(() => OpenUrl("https://unsplash.com/developers"));

        LoadFromSettings();
    }

    /// <summary>Restarts the debounce timer so rapid edits coalesce into a single save.</summary>
    private void ScheduleSave()
    {
        if (_suppressSave)
            return;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public ObservableCollection<WallpaperRecord> History { get; }

    // ---- Enum sources for combo boxes ----
    public Array SourceModes => Enum.GetValues(typeof(SourceMode));
    public Array Orientations => Enum.GetValues(typeof(Orientation));
    public Array WallpaperStyles => Enum.GetValues(typeof(WallpaperStyle));

    // ---- Settings-backed editable properties (each autosaves on change) ----
    private string _accessKey = string.Empty;
    public string AccessKey { get => _accessKey; set { if (Set(ref _accessKey, value)) ScheduleSave(); } }

    private SourceMode _sourceMode;
    public SourceMode SourceMode { get => _sourceMode; set { if (Set(ref _sourceMode, value)) { RaiseSourceVisibility(); ScheduleSave(); } } }

    private string _searchTerms = string.Empty;
    public string SearchTerms { get => _searchTerms; set { if (Set(ref _searchTerms, value)) ScheduleSave(); } }

    private string _topicSlugs = string.Empty;
    public string TopicSlugs { get => _topicSlugs; set { if (Set(ref _topicSlugs, value)) ScheduleSave(); } }

    private string _collectionIds = string.Empty;
    public string CollectionIds { get => _collectionIds; set { if (Set(ref _collectionIds, value)) ScheduleSave(); } }

    private Orientation _orientation;
    public Orientation Orientation { get => _orientation; set { if (Set(ref _orientation, value)) ScheduleSave(); } }

    private int _intervalMinutes = 1440;
    public int IntervalMinutes { get => _intervalMinutes; set { if (Set(ref _intervalMinutes, Math.Max(1, value))) ScheduleSave(); } }

    private WallpaperStyle _wallpaperStyle;
    public WallpaperStyle WallpaperStyle { get => _wallpaperStyle; set { if (Set(ref _wallpaperStyle, value)) ScheduleSave(); } }

    private bool _runAtStartup;
    public bool RunAtStartup { get => _runAtStartup; set { if (Set(ref _runAtStartup, value)) ScheduleSave(); } }

    // Visibility helpers for the source-specific input fields.
    public bool ShowSearch => SourceMode == SourceMode.SearchTerms;
    public bool ShowTopics => SourceMode == SourceMode.Topics;
    public bool ShowCollections => SourceMode == SourceMode.Collections;

    private void RaiseSourceVisibility()
    {
        OnPropertyChanged(nameof(ShowSearch));
        OnPropertyChanged(nameof(ShowTopics));
        OnPropertyChanged(nameof(ShowCollections));
    }

    // ---- Runtime/status state ----
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (Set(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                // Force command IsEnabled to re-evaluate now (not just on next UI interaction).
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    public bool IsNotBusy => !IsBusy;

    private bool _paused;
    public bool Paused
    {
        get => _paused;
        set { if (Set(ref _paused, value)) { OnPropertyChanged(nameof(PauseButtonText)); OnPropertyChanged(nameof(NotPaused)); } }
    }
    public bool NotPaused => !Paused;
    public string PauseButtonText => Paused ? "Resume automatic changes" : "Pause automatic changes";

    private string _statusText = string.Empty;
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

    public WallpaperRecord? Current => _app.HistoryService.Current;

    // ---- Commands ----
    public ICommand NextWallpaperCommand { get; }
    public ICommand TogglePauseCommand { get; }
    public ICommand ReapplyCommand { get; }
    public ICommand OpenUrlCommand { get; }
    public ICommand OpenDevPortalCommand { get; }

    /// <summary>Reloads editable fields from the persisted settings.</summary>
    public void LoadFromSettings()
    {
        _suppressSave = true;
        var s = _app.SettingsService.Settings;
        _accessKey = s.AccessKey;
        _sourceMode = s.SourceMode;
        _searchTerms = s.SearchTerms;
        _topicSlugs = s.TopicSlugs;
        _collectionIds = s.CollectionIds;
        _orientation = s.Orientation;
        _intervalMinutes = s.IntervalMinutes;
        _wallpaperStyle = s.WallpaperStyle;
        _runAtStartup = StartupService.IsEnabled();
        _paused = s.Paused;

        // Notify everything changed.
        foreach (var name in new[]
        {
            nameof(AccessKey), nameof(SourceMode), nameof(SearchTerms), nameof(TopicSlugs),
            nameof(CollectionIds), nameof(Orientation), nameof(IntervalMinutes),
            nameof(WallpaperStyle), nameof(RunAtStartup), nameof(Paused), nameof(PauseButtonText)
        })
            OnPropertyChanged(name);
        OnPropertyChanged(nameof(NotPaused));
        RaiseSourceVisibility();
        RefreshStatus();
        _suppressSave = false;
    }

    /// <summary>Refreshes read-only runtime state (current image, status line).</summary>
    public void RefreshRuntime()
    {
        _paused = _app.SettingsService.Settings.Paused;
        OnPropertyChanged(nameof(Paused));
        OnPropertyChanged(nameof(NotPaused));
        OnPropertyChanged(nameof(PauseButtonText));
        OnPropertyChanged(nameof(Current));
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var s = _app.SettingsService.Settings;
        if (string.IsNullOrWhiteSpace(s.AccessKey))
        {
            StatusText = "No Access Key set — add one below and Save.";
            return;
        }
        if (s.Paused)
        {
            StatusText = "Automatic changes are paused.";
            return;
        }
        var due = _app.Scheduler.NextDueUtc();
        if (due is null)
        {
            StatusText = "Automatic changes disabled.";
            return;
        }
        var local = due.Value.ToLocalTime();
        StatusText = due.Value <= DateTime.UtcNow
            ? "Next change is due now."
            : $"Next change around {local:g}.";
    }

    private async Task NextWallpaperAsync()
    {
        if (string.IsNullOrWhiteSpace(_app.SettingsService.Settings.AccessKey))
        {
            StatusText = "Add and Save your Access Key first.";
            return;
        }
        try
        {
            IsBusy = true;
            StatusText = "Fetching a new wallpaper…";
            await _app.WallpaperManager.ChangeNowAsync();
        }
        finally
        {
            IsBusy = false;
            RefreshRuntime();
        }
    }

    /// <summary>Immediately writes any pending (debounced) changes. Call before hiding the window.</summary>
    public void FlushPendingSave()
    {
        if (_saveTimer.IsEnabled)
            CommitSave();
    }

    /// <summary>Persists the current field values to settings (called by the debounce timer).</summary>
    private void CommitSave()
    {
        _saveTimer.Stop();

        var s = _app.SettingsService.Settings;
        bool styleChanged = s.WallpaperStyle != WallpaperStyle;
        bool startupChanged = StartupService.IsEnabled() != RunAtStartup;

        s.AccessKey = AccessKey.Trim();
        s.SourceMode = SourceMode;
        s.SearchTerms = SearchTerms;
        s.TopicSlugs = TopicSlugs;
        s.CollectionIds = CollectionIds;
        s.Orientation = Orientation;
        s.IntervalMinutes = IntervalMinutes;
        s.WallpaperStyle = WallpaperStyle;
        s.RunAtStartup = RunAtStartup;

        if (startupChanged)
        {
            try
            {
                StartupService.SetEnabled(RunAtStartup);
            }
            catch
            {
                // Non-fatal if we can't write the Run key.
            }
        }

        _app.SettingsService.Save();

        // Apply a fit-style change immediately to the current wallpaper, if any.
        if (styleChanged && Current is not null)
            _app.WallpaperManager.ReapplyFromHistory(Current);

        RefreshStatus();
    }

    private void TogglePause()
    {
        var s = _app.SettingsService.Settings;
        s.Paused = !s.Paused;
        _app.SettingsService.Save();
        Paused = s.Paused;
        RefreshStatus();
    }

    private void Reapply(WallpaperRecord? record)
    {
        if (record is null)
            return;
        _app.WallpaperManager.ReapplyFromHistory(record);
        RefreshRuntime();
    }

    private static void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Ignore browser launch failures.
        }
    }

    // ---- INotifyPropertyChanged ----
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
