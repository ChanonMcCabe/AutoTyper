using Avalonia;
using Avalonia.Markup.Xaml.Styling;
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
/// resources programmatically, plus custom ControlThemes (from AhkClassicTheme.axaml)
/// to achieve a 3D-beveled classic Windows 95/98 dialog look (sunken TextBox inputs,
/// raised Buttons, properly centered CheckBox glyphs). Switching to another theme
/// reverts these overrides and removes the custom ControlThemes.
/// </remarks>
public static class ThemeManager
{
    private static bool _ahkClassicThemeApplied = false;
    private static readonly Dictionary<object, object?> _savedResourceValues = [];
    private static StyleInclude? _ahkClassicStyleInclude = null;

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

        // Square corners (classic Windows dialog look).
        SaveAndSet(resources, "ControlCornerRadius", new CornerRadius(0));
        SaveAndSet(resources, "OverlayCornerRadius", new CornerRadius(0));

        // Palette: classic Windows/AutoHotkey colors.
        var controlFace = new SolidColorBrush(Color.Parse("#C0C0C0"));
        var backgroundColor = new SolidColorBrush(Color.Parse("#D4D4D4"));
        var borderColor = new SolidColorBrush(Color.Parse("#808080"));
        var whiteBrush = new SolidColorBrush(Colors.White);
        var blackText = new SolidColorBrush(Color.Parse("#000000"));
        var secondaryText = new SolidColorBrush(Color.Parse("#333333"));
        var tertiaryText = new SolidColorBrush(Color.Parse("#666666"));
        var hoverColor = new SolidColorBrush(Color.Parse("#A9A9A9"));
        var pressedColor = new SolidColorBrush(Color.Parse("#707070"));
        var disabledLightBg = new SolidColorBrush(Color.Parse("#D0D0D0"));
        var disabledText = new SolidColorBrush(Color.Parse("#888888"));

        // Window/application background.
        SaveAndSet(resources, "ApplicationPageBackgroundThemeBrush", backgroundColor);

        // Card and panel backgrounds (reuse existing generic keys).
        SaveAndSet(resources, "CardBackgroundFillColorDefaultBrush", controlFace);
        SaveAndSet(resources, "CardBackgroundFillColorSecondaryBrush", controlFace);
        SaveAndSet(resources, "ControlFillColorDefaultBrush", controlFace);
        SaveAndSet(resources, "ControlFillColorSecondaryBrush", controlFace);
        SaveAndSet(resources, "ControlFillColorTertiaryBrush", controlFace);
        SaveAndSet(resources, "ControlAltFillColorSecondaryBrush", controlFace);

        // Borders: thin gray lines.
        SaveAndSet(resources, "ControlStrokeColorDefaultBrush", borderColor);
        SaveAndSet(resources, "ControlStrokeColorSecondaryBrush", new SolidColorBrush(Color.Parse("#999999")));
        SaveAndSet(resources, "CardStrokeColorDefaultBrush", borderColor);
        SaveAndSet(resources, "DividerStrokeColorDefaultBrush", new SolidColorBrush(Color.Parse("#999999")));

        // Text/foreground (generic).
        SaveAndSet(resources, "TextFillColorPrimaryBrush", blackText);
        SaveAndSet(resources, "TextFillColorSecondaryBrush", secondaryText);
        SaveAndSet(resources, "TextFillColorTertiaryBrush", tertiaryText);

        // Accent color (gray, not blue).
        SaveAndSet(resources, "SystemAccentColor", Color.Parse("#999999"));
        SaveAndSet(resources, "AccentFillColorDefaultBrush", borderColor);
        SaveAndSet(resources, "AccentButtonBackground", new SolidColorBrush(Color.Parse("#999999")));

        // ========== TextBox ==========
        // Base state.
        SaveAndSet(resources, "TextControlBackground", whiteBrush);
        SaveAndSet(resources, "TextControlForeground", blackText);
        SaveAndSet(resources, "TextControlBorderBrush", borderColor);
        SaveAndSet(resources, "TextControlPlaceholderForeground", tertiaryText);

        // Pointer over.
        SaveAndSet(resources, "TextControlBackgroundPointerOver", whiteBrush);
        SaveAndSet(resources, "TextControlForegroundPointerOver", blackText);
        SaveAndSet(resources, "TextControlBorderBrushPointerOver", borderColor);
        SaveAndSet(resources, "TextControlPlaceholderForegroundPointerOver", tertiaryText);

        // Focused (black border for classic sunken-focus look).
        SaveAndSet(resources, "TextControlBackgroundFocused", whiteBrush);
        SaveAndSet(resources, "TextControlForegroundFocused", blackText);
        SaveAndSet(resources, "TextControlBorderBrushFocused", new SolidColorBrush(Color.Parse("#000000")));
        SaveAndSet(resources, "TextControlPlaceholderForegroundFocused", tertiaryText);

        // Disabled.
        SaveAndSet(resources, "TextControlBackgroundDisabled", disabledLightBg);
        SaveAndSet(resources, "TextControlForegroundDisabled", disabledText);
        SaveAndSet(resources, "TextControlBorderBrushDisabled", new SolidColorBrush(Color.Parse("#999999")));
        SaveAndSet(resources, "TextControlPlaceholderForegroundDisabled", new SolidColorBrush(Color.Parse("#999999")));

        // ========== Button ==========
        // Base state.
        SaveAndSet(resources, "ButtonBackground", controlFace);
        SaveAndSet(resources, "ButtonForeground", blackText);
        SaveAndSet(resources, "ButtonBorderBrush", borderColor);

        // Pointer over.
        SaveAndSet(resources, "ButtonBackgroundPointerOver", hoverColor);
        SaveAndSet(resources, "ButtonForegroundPointerOver", blackText);
        SaveAndSet(resources, "ButtonBorderBrushPointerOver", borderColor);

        // Pressed.
        SaveAndSet(resources, "ButtonBackgroundPressed", pressedColor);
        SaveAndSet(resources, "ButtonForegroundPressed", whiteBrush);
        SaveAndSet(resources, "ButtonBorderBrushPressed", borderColor);

        // Disabled.
        SaveAndSet(resources, "ButtonBackgroundDisabled", disabledLightBg);
        SaveAndSet(resources, "ButtonForegroundDisabled", disabledText);
        SaveAndSet(resources, "ButtonBorderBrushDisabled", new SolidColorBrush(Color.Parse("#999999")));

        // ========== CheckBox ==========
        // Checked states.
        SaveAndSet(resources, "CheckBoxBackgroundChecked", controlFace);
        SaveAndSet(resources, "CheckBoxBackgroundCheckedPointerOver", hoverColor);
        SaveAndSet(resources, "CheckBoxBackgroundCheckedPressed", pressedColor);
        SaveAndSet(resources, "CheckBoxBackgroundCheckedDisabled", disabledLightBg);

        SaveAndSet(resources, "CheckBoxBorderBrushChecked", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushCheckedPointerOver", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushCheckedPressed", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushCheckedDisabled", new SolidColorBrush(Color.Parse("#999999")));

        SaveAndSet(resources, "CheckBoxForegroundChecked", blackText);
        SaveAndSet(resources, "CheckBoxForegroundCheckedPointerOver", blackText);
        SaveAndSet(resources, "CheckBoxForegroundCheckedPressed", whiteBrush);
        SaveAndSet(resources, "CheckBoxForegroundCheckedDisabled", disabledText);

        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundChecked", blackText);
        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundCheckedPointerOver", blackText);
        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundCheckedPressed", whiteBrush);
        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundCheckedDisabled", disabledText);

        // Unchecked states.
        SaveAndSet(resources, "CheckBoxBackgroundUnchecked", whiteBrush);
        SaveAndSet(resources, "CheckBoxBackgroundUncheckedPointerOver", whiteBrush);
        SaveAndSet(resources, "CheckBoxBackgroundUncheckedPressed", whiteBrush);
        SaveAndSet(resources, "CheckBoxBackgroundUncheckedDisabled", disabledLightBg);

        SaveAndSet(resources, "CheckBoxBorderBrushUnchecked", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushUncheckedPointerOver", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushUncheckedPressed", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushUncheckedDisabled", new SolidColorBrush(Color.Parse("#999999")));

        SaveAndSet(resources, "CheckBoxForegroundUnchecked", blackText);
        SaveAndSet(resources, "CheckBoxForegroundUncheckedPointerOver", blackText);
        SaveAndSet(resources, "CheckBoxForegroundUncheckedPressed", blackText);
        SaveAndSet(resources, "CheckBoxForegroundUncheckedDisabled", disabledText);

        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundUnchecked", blackText);

        // Indeterminate states.
        SaveAndSet(resources, "CheckBoxBackgroundIndeterminate", controlFace);
        SaveAndSet(resources, "CheckBoxBackgroundIndeterminatePointerOver", hoverColor);
        SaveAndSet(resources, "CheckBoxBackgroundIndeterminatePressed", pressedColor);
        SaveAndSet(resources, "CheckBoxBackgroundIndeterminateDisabled", disabledLightBg);

        SaveAndSet(resources, "CheckBoxBorderBrushIndeterminate", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushIndeterminatePointerOver", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushIndeterminatePressed", borderColor);
        SaveAndSet(resources, "CheckBoxBorderBrushIndeterminateDisabled", new SolidColorBrush(Color.Parse("#999999")));

        SaveAndSet(resources, "CheckBoxForegroundIndeterminate", blackText);
        SaveAndSet(resources, "CheckBoxForegroundIndeterminatePointerOver", blackText);
        SaveAndSet(resources, "CheckBoxForegroundIndeterminatePressed", whiteBrush);
        SaveAndSet(resources, "CheckBoxForegroundIndeterminateDisabled", disabledText);

        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundIndeterminate", blackText);
        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundIndeterminatePointerOver", blackText);
        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundIndeterminatePressed", whiteBrush);
        SaveAndSet(resources, "CheckBoxCheckGlyphForegroundIndeterminateDisabled", disabledText);

        // ========== ToggleSwitch ==========
        // Container (background).
        SaveAndSet(resources, "ToggleSwitchContainerBackground", controlFace);
        SaveAndSet(resources, "ToggleSwitchContainerBackgroundPointerOver", hoverColor);
        SaveAndSet(resources, "ToggleSwitchContainerBackgroundPressed", pressedColor);
        SaveAndSet(resources, "ToggleSwitchContainerBackgroundDisabled", disabledLightBg);

        // Fill (track color when on).
        SaveAndSet(resources, "ToggleSwitchFillOn", controlFace);
        SaveAndSet(resources, "ToggleSwitchFillOnPointerOver", hoverColor);
        SaveAndSet(resources, "ToggleSwitchFillOnPressed", pressedColor);
        SaveAndSet(resources, "ToggleSwitchFillOnDisabled", disabledLightBg);

        // Fill (track color when off).
        SaveAndSet(resources, "ToggleSwitchFillOff", controlFace);
        SaveAndSet(resources, "ToggleSwitchFillOffPointerOver", hoverColor);
        SaveAndSet(resources, "ToggleSwitchFillOffPressed", pressedColor);
        SaveAndSet(resources, "ToggleSwitchFillOffDisabled", disabledLightBg);

        // Stroke (border when on).
        SaveAndSet(resources, "ToggleSwitchStrokeOn", borderColor);
        SaveAndSet(resources, "ToggleSwitchStrokeOnPointerOver", borderColor);
        SaveAndSet(resources, "ToggleSwitchStrokeOnPressed", borderColor);
        SaveAndSet(resources, "ToggleSwitchStrokeOnDisabled", new SolidColorBrush(Color.Parse("#999999")));

        // Stroke (border when off).
        SaveAndSet(resources, "ToggleSwitchStrokeOff", borderColor);
        SaveAndSet(resources, "ToggleSwitchStrokeOffPointerOver", borderColor);
        SaveAndSet(resources, "ToggleSwitchStrokeOffPressed", borderColor);
        SaveAndSet(resources, "ToggleSwitchStrokeOffDisabled", new SolidColorBrush(Color.Parse("#999999")));

        // Knob (thumb) when on.
        SaveAndSet(resources, "ToggleSwitchKnobFillOn", blackText);
        SaveAndSet(resources, "ToggleSwitchKnobFillOnPointerOver", blackText);
        SaveAndSet(resources, "ToggleSwitchKnobFillOnPressed", blackText);
        SaveAndSet(resources, "ToggleSwitchKnobFillOnDisabled", disabledText);

        // Knob (thumb) when off.
        SaveAndSet(resources, "ToggleSwitchKnobFillOff", tertiaryText);
        SaveAndSet(resources, "ToggleSwitchKnobFillOffPointerOver", tertiaryText);
        SaveAndSet(resources, "ToggleSwitchKnobFillOffPressed", tertiaryText);
        SaveAndSet(resources, "ToggleSwitchKnobFillOffDisabled", disabledText);

        // Content/text.
        SaveAndSet(resources, "ToggleSwitchContentForeground", blackText);
        SaveAndSet(resources, "ToggleSwitchHeaderForeground", blackText);

        // ========== ComboBox ==========
        // Base state.
        SaveAndSet(resources, "ComboBoxBackground", whiteBrush);
        SaveAndSet(resources, "ComboBoxForeground", blackText);
        SaveAndSet(resources, "ComboBoxBorderBrush", borderColor);

        // Pointer over.
        SaveAndSet(resources, "ComboBoxBackgroundPointerOver", whiteBrush);
        SaveAndSet(resources, "ComboBoxForegroundPointerOver", blackText);
        SaveAndSet(resources, "ComboBoxBorderBrushPointerOver", borderColor);

        // Pressed.
        SaveAndSet(resources, "ComboBoxBackgroundPressed", whiteBrush);
        SaveAndSet(resources, "ComboBoxForegroundPressed", blackText);
        SaveAndSet(resources, "ComboBoxBorderBrushPressed", borderColor);

        // Focused.
        SaveAndSet(resources, "ComboBoxBackgroundFocused", whiteBrush);
        SaveAndSet(resources, "ComboBoxForegroundFocused", blackText);

        // Disabled.
        SaveAndSet(resources, "ComboBoxBackgroundDisabled", disabledLightBg);
        SaveAndSet(resources, "ComboBoxForegroundDisabled", disabledText);
        SaveAndSet(resources, "ComboBoxBorderBrushDisabled", new SolidColorBrush(Color.Parse("#999999")));

        // Dropdown menu.
        SaveAndSet(resources, "ComboBoxDropDownBackground", controlFace);
        SaveAndSet(resources, "ComboBoxDropDownBorderBrush", borderColor);

        // Dropdown items.
        SaveAndSet(resources, "ComboBoxItemBackground", whiteBrush);
        SaveAndSet(resources, "ComboBoxItemBackgroundPointerOver", hoverColor);
        SaveAndSet(resources, "ComboBoxItemBackgroundSelected", controlFace);

        SaveAndSet(resources, "ComboBoxItemForeground", blackText);
        SaveAndSet(resources, "ComboBoxItemForegroundPointerOver", blackText);

        // ========== Slider ==========
        // Track fill (progress).
        SaveAndSet(resources, "SliderTrackFill", controlFace);
        SaveAndSet(resources, "SliderTrackFillPointerOver", hoverColor);
        SaveAndSet(resources, "SliderTrackFillPressed", pressedColor);
        SaveAndSet(resources, "SliderTrackFillDisabled", disabledLightBg);

        // Track value fill (background trough).
        SaveAndSet(resources, "SliderTrackValueFill", controlFace);
        SaveAndSet(resources, "SliderTrackValueFillPointerOver", hoverColor);
        SaveAndSet(resources, "SliderTrackValueFillPressed", pressedColor);
        SaveAndSet(resources, "SliderTrackValueFillDisabled", disabledLightBg);

        // Thumb (slider button).
        SaveAndSet(resources, "SliderThumbBackground", controlFace);
        SaveAndSet(resources, "SliderThumbBackgroundPointerOver", hoverColor);
        SaveAndSet(resources, "SliderThumbBackgroundPressed", pressedColor);
        SaveAndSet(resources, "SliderThumbBackgroundDisabled", disabledLightBg);

        SaveAndSet(resources, "SliderThumbBorderBrush", borderColor);

        // ========== ProgressBar ==========
        SaveAndSet(resources, "ProgressBarForeground", new SolidColorBrush(Color.Parse("#999999")));
        SaveAndSet(resources, "ProgressBarBackground", controlFace);
        SaveAndSet(resources, "ProgressBarBorderBrush", borderColor);

        // Load custom ControlThemes for 3D bevel effects (TextBox, Button, CheckBox).
        // In Avalonia, later-added Styles take precedence over earlier ones for equal specificity,
        // so adding the custom theme after FluentAvalonia's base theme ensures our ControlThemes are used.
        try
        {
            _ahkClassicStyleInclude = new StyleInclude(new Uri("avares://AutoTyper.Desktop/AhkClassicTheme.axaml"));
            Application.Current.Styles.Add(_ahkClassicStyleInclude);
        }
        catch (Exception ex)
        {
            // If the style file fails to load, log it but don't crash the theme application.
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to load AhkClassicTheme.axaml: {ex.Message}");
        }

        _ahkClassicThemeApplied = true;
    }

    private static void RemoveAhkClassicTheme()
    {
        if (Application.Current is null)
        {
            return;
        }

        var resources = Application.Current.Resources;

        // Remove the custom ControlThemes by removing the StyleInclude.
        if (_ahkClassicStyleInclude is not null)
        {
            Application.Current.Styles.Remove(_ahkClassicStyleInclude);
            _ahkClassicStyleInclude = null;
        }

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
