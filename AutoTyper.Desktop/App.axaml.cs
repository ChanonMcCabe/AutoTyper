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

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Settings = SettingsService.Load();
        ThemeManager.Apply(Settings.Preferences.Theme);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Minimising hides the window to the tray, which is not the same as
            // closing it — so the app stays alive while hidden, and genuinely
            // closing the main window still exits, as it did in the WPF build.
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.ShutdownRequested += OnShutdownRequested;
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainWindow? MainWindow =>
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow;

    private void TrayIcon_Clicked(object? sender, EventArgs e) => MainWindow?.RestoreFromTray();

    private void RestoreMenuItem_Click(object? sender, EventArgs e) => MainWindow?.RestoreFromTray();

    private void ExitMenuItem_Click(object? sender, EventArgs e) =>
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

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
