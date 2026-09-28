using Avalonia;
using Avalonia.Media;
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
///
/// <see cref="AppTheme.AhkClassic"/> applies a custom set of brush and geometry
/// resources programmatically to achieve a flat, square-cornered classic
/// Windows dialog look. Switching to another theme reverts these overrides.
/// </remarks>
public static class ThemeManager
{
    private static bool _ahkClassicThemeApplied = false;
    private static readonly Dictionary<object, object?> _savedResourceValues = [];

    public static void Apply(AppTheme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        // Remove the AhkClassic theme if it was previously applied and we're switching away.
        if (_ahkClassicThemeApplied && theme != AppTheme.AhkClassic)
        {
            RemoveAhkClassicTheme();
        }

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            AppTheme.AhkClassic => ThemeVariant.Light, // Use light as base; override with custom resources.
            _ => ThemeVariant.Default,
        };

        // Apply the AhkClassic theme programmatically if selected.
        if (theme == AppTheme.AhkClassic && !_ahkClassicThemeApplied)
        {
            ApplyAhkClassicTheme();
        }
    }

    private static void ApplyAhkClassicTheme()
    {
        if (Application.Current is null)
        {
            return;
        }

        var resources = Application.Current.Resources;

        // Save and override corner radius resources to 0 (square corners).
        SaveAndSet(resources, "ControlCornerRadius", 0d);
        SaveAndSet(resources, "OverlayCornerRadius", 0d);
        SaveAndSet(resources, "FlyoutPresenterCornerRadius", 0d);
        SaveAndSet(resources, "ControlCornerRadiusThickness", new CornerRadius(0));
        SaveAndSet(resources, "OverlayCornerRadiusThickness", new CornerRadius(0));

        // Base colors: light gray background (#D4D4D4) and classic dialog style.
        var controlColor = Color.Parse("#C0C0C0");
        var backgroundColor = Color.Parse("#D4D4D4");
        var borderColor = Color.Parse("#808080");
        var textColor = Color.Parse("#000000");
        var textSecondary = Color.Parse("#333333");
        var textTertiary = Color.Parse("#666666");

        // Window/application background.
        SaveAndSet(resources, "ApplicationPageBackgroundThemeBrush", new SolidColorBrush(backgroundColor));

        // Card and panel backgrounds.
        var controlBrush = new SolidColorBrush(controlColor);
        SaveAndSet(resources, "CardBackgroundFillColorDefaultBrush", controlBrush);
        SaveAndSet(resources, "CardBackgroundFillColorSecondaryBrush", controlBrush);
        SaveAndSet(resources, "ControlFillColorDefaultBrush", controlBrush);
        SaveAndSet(resources, "ControlFillColorSecondaryBrush", controlBrush);
        SaveAndSet(resources, "ControlFillColorTertiaryBrush", controlBrush);
        SaveAndSet(resources, "ControlAltFillColorSecondaryBrush", controlBrush);
        SaveAndSet(resources, "SubtleBodyStrokeColorBrush", controlBrush);

        // Borders: thin gray lines.
        var borderBrush = new SolidColorBrush(borderColor);
        SaveAndSet(resources, "ControlStrokeColorDefaultBrush", borderBrush);
        SaveAndSet(resources, "ControlStrokeColorSecondaryBrush", new SolidColorBrush(Color.Parse("#999999")));
        SaveAndSet(resources, "CardStrokeColorDefaultBrush", borderBrush);
        SaveAndSet(resources, "DividerStrokeColorDefaultBrush", new SolidColorBrush(Color.Parse("#999999")));

        // Text/foreground.
        SaveAndSet(resources, "TextFillColorPrimaryBrush", new SolidColorBrush(textColor));
        SaveAndSet(resources, "TextFillColorSecondaryBrush", new SolidColorBrush(textSecondary));
        SaveAndSet(resources, "TextFillColorTertiaryBrush", new SolidColorBrush(textTertiary));

        // Accent color (use gray instead of blue for classic look).
        SaveAndSet(resources, "SystemAccentColor", Color.Parse("#999999"));
        SaveAndSet(resources, "AccentFillColorDefaultBrush", new SolidColorBrush(borderColor));
        SaveAndSet(resources, "AccentButtonBackground", new SolidColorBrush(Color.Parse("#999999")));

        // Button styles: flat, no gradient.
        SaveAndSet(resources, "ButtonBackgroundPointerOver", new SolidColorBrush(Color.Parse("#A9A9A9")));
        SaveAndSet(resources, "ButtonBackgroundPressed", new SolidColorBrush(Color.Parse("#707070")));
        SaveAndSet(resources, "ButtonForeground", new SolidColorBrush(textColor));

        // Checkbox and toggle styles.
        SaveAndSet(resources, "CheckBoxForeground", new SolidColorBrush(textColor));
        SaveAndSet(resources, "CheckBoxCheckGlyph", new SolidColorBrush(textColor));
        SaveAndSet(resources, "CheckBoxBackground", controlBrush);
        SaveAndSet(resources, "CheckBoxBorderBrush", borderBrush);
        SaveAndSet(resources, "ToggleSwitchForeground", new SolidColorBrush(textColor));

        // TextBox and input.
        SaveAndSet(resources, "TextControlBackground", new SolidColorBrush(Colors.White));
        SaveAndSet(resources, "TextControlForeground", new SolidColorBrush(textColor));
        SaveAndSet(resources, "TextControlBorderBrush", borderBrush);

        // ProgressBar.
        SaveAndSet(resources, "ProgressBarForeground", new SolidColorBrush(Color.Parse("#999999")));

        _ahkClassicThemeApplied = true;
    }

    private static void RemoveAhkClassicTheme()
    {
        if (Application.Current is null)
        {
            return;
        }

        var resources = Application.Current.Resources;

        // Restore all saved resource values.
        foreach (var key in _savedResourceValues.Keys.ToList())
        {
            if (_savedResourceValues[key] is not null)
            {
                resources[key] = _savedResourceValues[key];
            }
            else
            {
                resources.Remove(key);
            }
        }

        _savedResourceValues.Clear();
        _ahkClassicThemeApplied = false;
    }

    /// <summary>
    /// Saves the current resource value (if it exists) and sets a new one.
    /// </summary>
    private static void SaveAndSet(dynamic resources, object key, object? value)
    {
        if (!_savedResourceValues.ContainsKey(key))
        {
            _savedResourceValues[key] = resources.ContainsKey(key) ? resources[key] : null;
        }

        if (value is not null)
        {
            resources[key] = value;
        }
    }
}
