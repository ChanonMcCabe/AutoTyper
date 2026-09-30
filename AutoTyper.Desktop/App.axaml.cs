using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AutoTyper.Desktop;

public partial class App : Application
{
    /// <summary>
    /// The settings loaded once at startup. <see cref="MainWindow"/> edits this
    /// same instance.
    /// </summary>
    public AppSettings Settings { get; private set; } = new();

    private NativeMenuItem? _startTypingMenuItem;
    private NativeMenuItem? _pauseResumeMenuItem;
    private NativeMenuItem? _stopTypingMenuItem;
    private NativeMenuItem? _activateMenuItem;
    private NativeMenuItem? _deactivateMenuItem;
    private TrayIcon? _trayIcon;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Settings = SettingsService.Load();
        ThemeManager.Apply(Settings.Preferences.Theme);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Minimising hides the window to the tray, which is not the same as
            // closing it — so the app stays alive while hidden, and genuinely
            // closing the main window still exits.
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.ShutdownRequested += OnShutdownRequested;
            desktop.MainWindow = new MainWindow();
            BuildTrayMenu();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void BuildTrayMenu()
    {
        // TrayIcon.Icons is an attached property on the Application, not a
        // resource, so it has to be read back through its accessor.
        if (TrayIcon.GetIcons(this) is { Count: > 0 } trayIcons)
        {
            _trayIcon = trayIcons[0];
        }

        if (_trayIcon is null)
        {
            return;
        }

        var menu = new NativeMenu();

        _startTypingMenuItem = new NativeMenuItem { Header = "Start Typing" };
        _startTypingMenuItem.Click += (s, e) => MainWindow?.StartTypingFromTray();
        menu.Add(_startTypingMenuItem);

        _pauseResumeMenuItem = new NativeMenuItem { Header = "Pause" };
        _pauseResumeMenuItem.Click += (s, e) => MainWindow?.PauseResumeTyping();
        menu.Add(_pauseResumeMenuItem);

        _stopTypingMenuItem = new NativeMenuItem { Header = "Stop Typing" };
        _stopTypingMenuItem.Click += (s, e) => MainWindow?.StopTypingFromTray();
        menu.Add(_stopTypingMenuItem);

        menu.Add(new NativeMenuItemSeparator());

        _activateMenuItem = new NativeMenuItem { Header = "Activate" };
        _activateMenuItem.Click += (s, e) => MainWindow?.OnActivateMenuItemClick();
        menu.Add(_activateMenuItem);

        _deactivateMenuItem = new NativeMenuItem { Header = "Deactivate" };
        _deactivateMenuItem.Click += (s, e) => MainWindow?.OnDeactivateMenuItemClick();
        menu.Add(_deactivateMenuItem);

        menu.Add(new NativeMenuItemSeparator());

        var restoreMenuItem = new NativeMenuItem { Header = "Restore" };
        restoreMenuItem.Click += (s, e) => MainWindow?.RestoreFromTray();
        menu.Add(restoreMenuItem);

        var exitMenuItem = new NativeMenuItem { Header = "Exit" };
        exitMenuItem.Click += (s, e) =>
            (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        menu.Add(exitMenuItem);

        _trayIcon.Menu = menu;

        // Subscribe to view model changes to update menu
        if (MainWindow is MainWindow mainWindow)
        {
            var viewModel = mainWindow.GetViewModel();
            viewModel.PropertyChanged += (s, e) => UpdateTrayMenuState();
            UpdateTrayMenuState();
        }
    }

    private void UpdateTrayMenuState()
    {
        if (MainWindow is not MainWindow mainWindow)
        {
            return;
        }

        var viewModel = mainWindow.GetViewModel();

        if (_startTypingMenuItem is not null)
        {
            _startTypingMenuItem.IsEnabled = viewModel.HasPassage && !viewModel.IsTyping && PlatformServices.CanTypeNow;
        }

        if (_pauseResumeMenuItem is not null)
        {
            _pauseResumeMenuItem.IsEnabled = viewModel.IsTyping;
            _pauseResumeMenuItem.Header = viewModel.IsPaused ? "Resume" : "Pause";
        }

        if (_stopTypingMenuItem is not null)
        {
            _stopTypingMenuItem.IsEnabled = viewModel.IsTyping;
        }

        if (_activateMenuItem is not null)
        {
            _activateMenuItem.IsEnabled = !viewModel.IsActive && viewModel.HasHotkey && viewModel.HasPassage;
        }

        if (_deactivateMenuItem is not null)
        {
            _deactivateMenuItem.IsEnabled = viewModel.IsActive;
        }

        if (_trayIcon is not null)
        {
            if (viewModel.IsTyping)
            {
                _trayIcon.ToolTipText = $"AutoTyper — typing {(int)viewModel.ProgressPercent}%";
            }
            else if (viewModel.IsActive)
            {
                _trayIcon.ToolTipText = "AutoTyper — active";
            }
            else
            {
                _trayIcon.ToolTipText = "AutoTyper";
            }
        }
    }

    private MainWindow? MainWindow =>
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow;

    private void TrayIcon_Clicked(object? sender, EventArgs e) => MainWindow?.RestoreFromTray();

    /// <summary>
    /// Persist settings on every shutdown path — closing the window and
    /// exiting from the tray menu alike.
    /// </summary>
    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        try
        {
            SettingsService.Save(Settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nothing to show at exit; a failed write leaves the previous file intact.
        }
    }
}
