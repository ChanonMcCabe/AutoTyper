using Avalonia;
using Avalonia.Styling;

namespace AutoTyper.Desktop;

/// <summary>
/// Applies the user's theme preference to the whole application.
/// </summary>
/// <remarks>
/// Setting <see cref="Application.RequestedThemeVariant"/> propagates to every
/// open window on its own, and <see cref="ThemeVariant.Default"/> tracks the
/// OS setting live on both Windows and macOS. That replaces the WPF build's
/// manual per-window reapplication, its merged-dictionary reload hack, and its
/// separate system-theme watcher.
/// </remarks>
public static class ThemeManager
{
    public static void Apply(AppTheme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
