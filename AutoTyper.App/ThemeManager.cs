using System.Linq;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;

namespace AutoTyper.App;

/// <summary>
/// Adapts the app's <see cref="AppTheme"/> preference onto WPF-UI's
/// <see cref="ApplicationThemeManager"/>. <see cref="AppTheme.System"/> defers to
/// the current Windows app-mode; <see cref="MainWindow"/> additionally calls
/// <see cref="SystemThemeWatcher"/> so that choice keeps tracking live OS
/// changes.
/// <para>
/// WPF-UI's own <c>Apply</c> only refreshes <see cref="System.Windows.Application.MainWindow"/>;
/// a theme change made at runtime (from the Settings dialog) leaves other open
/// windows — and even the main window's templated controls — painted in the old
/// theme until the next launch. To make the switch take effect immediately we
/// re-merge the controls dictionary (forcing every implicitly-styled control to
/// re-resolve its theme brushes) and refresh each open window's resources and
/// backdrop.
/// </para>
/// </summary>
internal static class ThemeManager
{
    public static void Apply(AppTheme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        if (theme == AppTheme.System)
        {
            ApplicationThemeManager.ApplySystemTheme();
        }
        else
        {
            ApplicationThemeManager.Apply(
                theme == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
                WindowBackdropType.Mica);
        }

        ApplicationTheme resolved = ApplicationThemeManager.GetAppTheme();

        // Re-adding ControlsDictionary makes WPF re-evaluate every implicit control
        // style, so control fills/borders pick up the theme that was just applied
        // rather than staying on the one they resolved against when first realized.
        ICollection<ResourceDictionary> merged = Application.Current.Resources.MergedDictionaries;
        foreach (ControlsDictionary stale in merged.OfType<ControlsDictionary>().ToList())
        {
            merged.Remove(stale);
        }

        merged.Add(new ControlsDictionary());

        foreach (Window window in Application.Current.Windows.OfType<Window>().ToList())
        {
            ApplicationThemeManager.Apply(window);
            WindowBackgroundManager.UpdateBackground(window, resolved, WindowBackdropType.Mica);
        }
    }
}
