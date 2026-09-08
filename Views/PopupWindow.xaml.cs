using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UnsplashWallpaper.ViewModels;

namespace UnsplashWallpaper.Views;

public enum PopupTab
{
    Current = 0,
    Settings = 1,
    History = 2
}

public partial class PopupWindow : Window
{
    private readonly PopupViewModel _vm;

    public PopupWindow()
    {
        InitializeComponent();
        _vm = new PopupViewModel();
        DataContext = _vm;
        Loaded += OnLoaded;
        Deactivated += OnDeactivated;
    }

    /// <summary>Dismiss the popup (like a flyout) when it loses focus to another window.</summary>
    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Don't dismiss while a ComboBox dropdown is open (its popup can steal activation).
        if (IsAnyDropDownOpen())
            return;
        Close(); // OnClosing flushes pending saves and hides the window.
    }

    private bool IsAnyDropDownOpen()
    {
        foreach (var combo in FindVisualChildren<ComboBox>(this))
        {
            if (combo.IsDropDownOpen)
                return true;
        }
        return false;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
                yield return typed;
            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionNearTray();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close(); // OnClosing flushes pending saves and hides the window.
    }

    /// <summary>Places the window at the bottom-right of the work area, above the taskbar.</summary>
    private void PositionNearTray()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 12;
        Top = wa.Bottom - Height - 12;
        if (Left < wa.Left) Left = wa.Left;
        if (Top < wa.Top) Top = wa.Top;
    }

    public void SelectTab(PopupTab tab)
    {
        if (Tabs is not null)
            Tabs.SelectedIndex = (int)tab;
    }

    /// <summary>
    /// Refreshes runtime state (current image, status, pause) without touching the editable
    /// settings fields, so a scheduled change never clobbers in-progress edits.
    /// </summary>
    public void RefreshFromApp()
    {
        Dispatcher.Invoke(() => _vm.RefreshRuntime());
    }

    /// <summary>Reloads all editable fields from persisted settings (used when opening).</summary>
    public void ReloadSettings()
    {
        Dispatcher.Invoke(() =>
        {
            _vm.LoadFromSettings();
            // Sync the masked field from the VM without echoing back a change.
            _syncingPassword = true;
            AccessKeyBox.Password = _vm.AccessKey;
            _syncingPassword = false;
        });
    }

    private bool _syncingPassword;

    private void AccessKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncingPassword)
            return;
        _vm.AccessKey = AccessKeyBox.Password;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Flush any pending debounced save before hiding.
        _vm.FlushPendingSave();
        // Hide instead of destroy so state/position persist while the tray app keeps running.
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}
