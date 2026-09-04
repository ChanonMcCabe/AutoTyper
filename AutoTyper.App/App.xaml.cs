using System.IO;
using System.Windows;

namespace AutoTyper.App;

public partial class App : Application
{
    /// <summary>
    /// The settings loaded once at startup. <see cref="MainWindow"/> edits this
    /// same instance; loading here (rather than in the window constructor) lets
    /// the WPF-UI theme be applied before the first window is created, so its
    /// control styles resolve against the right theme from the first frame.
    /// </summary>
    public AppSettings Settings { get; } = SettingsService.Load();

    protected override void OnStartup(StartupEventArgs e)
    {
        ThemeManager.Apply(Settings.Preferences.Theme);
        base.OnStartup(e);
    }

    /// <summary>
    /// Persist settings on every shutdown path. <c>OnExit</c> runs whether the
    /// user closes the window or exits from the tray menu
    /// (<see cref="Application.Shutdown()"/>), unlike <c>Window.Closing</c> which
    /// the tray-exit path skips.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            SettingsService.Save(Settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nothing to show at exit; a failed write leaves the previous file intact.
        }

        base.OnExit(e);
    }
}
