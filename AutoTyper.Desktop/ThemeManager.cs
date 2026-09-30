using Avalonia;
using Avalonia.Controls;
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
/// OS setting live on both Windows and macOS.
///
/// <see cref="AppTheme.AhkClassic"/> applies a custom set of brush and geometry
/// resources programmatically, plus flat styles (from AhkClassicTheme.axaml) to
/// achieve a Windows 10 WinForms dialog look (flat #F0F0F0 surface, white inset
/// TextBoxes, flat gray Buttons with blue hover, square group boxes, Segoe UI 9pt).
/// Switching to another theme reverts these overrides and removes the styles.
/// </remarks>
public static class ThemeManager
{
    private static readonly Dictionary<object, object?> _savedResourceValues = [];
    private static bool _ahkClassicThemeApplied;
    private static StyleInclude? _ahkClassicStyleInclude;

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

        var r = Application.Current.Resources;

        static SolidColorBrush B(string hex) => new(Color.Parse(hex));
        void Set(string key, object value) => SaveAndSet(r, key, value);

        // Square corners (WinForms controls have none).
        Set("ControlCornerRadius", new CornerRadius(0));
        Set("OverlayCornerRadius", new CornerRadius(0));

        // Windows 10 WinForms palette.
        var face = B("#F0F0F0");          // window / group box
        var groupBorder = B("#DCDCDC");
        var buttonFace = B("#E1E1E1");
        var buttonBorder = B("#ADADAD");
        var hoverFace = B("#E5F1FB");
        var pressedFace = B("#CCE4F7");
        var blue = B("#0078D7");
        var darkBlue = B("#005499");
        var white = B("#FFFFFF");
        var inputBorder = B("#7A7A7A");
        var glyphBorder = B("#333333");
        var text = B("#000000");
        var secondaryText = B("#333333");
        var disabledText = B("#6D6D6D");
        var disabledFace = B("#CCCCCC");
        var disabledBorder = B("#BFBFBF");

        Set("ApplicationPageBackgroundThemeBrush", face);
        Set("SolidBackgroundFillColorBaseBrush", face);

        // Cards become flat group boxes.
        Set("CardBackgroundFillColorDefaultBrush", face);
        Set("CardBackgroundFillColorSecondaryBrush", face);
        Set("CardStrokeColorDefaultBrush", groupBorder);
        Set("ControlStrokeColorDefaultBrush", groupBorder);
        Set("ControlStrokeColorSecondaryBrush", groupBorder);
        Set("DividerStrokeColorDefaultBrush", groupBorder);
        Set("ControlFillColorDefaultBrush", buttonFace);
        Set("ControlFillColorSecondaryBrush", buttonFace);
        Set("ControlFillColorTertiaryBrush", pressedFace);
        Set("ControlAltFillColorSecondaryBrush", face);

        Set("TextFillColorPrimaryBrush", text);
        Set("TextFillColorSecondaryBrush", secondaryText);
        Set("TextFillColorTertiaryBrush", disabledText);
        Set("TextFillColorDisabledBrush", disabledText);

        Set("SystemAccentColor", Color.Parse("#0078D7"));
        Set("AccentFillColorDefaultBrush", blue);
        Set("AccentFillColorSecondaryBrush", blue);
        Set("AccentFillColorTertiaryBrush", darkBlue);

        // Font family and size come from the Window style in AhkClassicTheme.axaml.
        Set("ControlContentThemeFontSize", 12.0);

        // TextBox: white, 1px gray border, blue on hover/focus.
        Set("TextControlThemeMinHeight", 23.0);
        Set("TextControlBorderThemeThickness", new Thickness(1));
        Set("TextControlBorderThemeThicknessFocused", new Thickness(1));
        foreach (var state in new[] { "", "PointerOver", "Focused" })
        {
            Set("TextControlBackground" + state, white);
            Set("TextControlForeground" + state, text);
            Set("TextControlPlaceholderForeground" + state, disabledText);
        }
        Set("TextControlBorderBrush", inputBorder);
        Set("TextControlBorderBrushPointerOver", blue);
        Set("TextControlBorderBrushFocused", blue);
        Set("TextControlBackgroundDisabled", face);
        Set("TextControlForegroundDisabled", disabledText);
        Set("TextControlBorderBrushDisabled", disabledBorder);
        Set("TextControlPlaceholderForegroundDisabled", disabledText);

        // Button: flat #E1E1E1, blue hover/press.
        Set("ButtonBackground", buttonFace);
        Set("ButtonForeground", text);
        Set("ButtonBorderBrush", buttonBorder);
        Set("ButtonBackgroundPointerOver", hoverFace);
        Set("ButtonForegroundPointerOver", text);
        Set("ButtonBorderBrushPointerOver", blue);
        Set("ButtonBackgroundPressed", pressedFace);
        Set("ButtonForegroundPressed", text);
        Set("ButtonBorderBrushPressed", darkBlue);
        Set("ButtonBackgroundDisabled", face);
        Set("ButtonForegroundDisabled", disabledText);
        Set("ButtonBorderBrushDisabled", disabledBorder);

        // Accent (default) button: same flat face, blue border like a WinForms AcceptButton.
        Set("AccentButtonBackground", buttonFace);
        Set("AccentButtonForeground", text);
        Set("AccentButtonBorderBrush", blue);
        Set("AccentButtonBackgroundPointerOver", hoverFace);
        Set("AccentButtonForegroundPointerOver", text);
        Set("AccentButtonBorderBrushPointerOver", blue);
        Set("AccentButtonBackgroundPressed", pressedFace);
        Set("AccentButtonForegroundPressed", text);
        Set("AccentButtonBorderBrushPressed", darkBlue);
        Set("AccentButtonBackgroundDisabled", face);
        Set("AccentButtonForegroundDisabled", disabledText);
        Set("AccentButtonBorderBrushDisabled", disabledBorder);

        // CheckBox: white box, dark 1px border, blue on hover.
        foreach (var check in new[] { "Unchecked", "Checked", "Indeterminate" })
        {
            foreach (var state in new[] { "", "PointerOver", "Pressed", "Disabled" })
            {
                var disabled = state == "Disabled";
                Set($"CheckBoxBackground{check}{state}", disabled ? face : state == "Pressed" ? pressedFace : white);
                Set($"CheckBoxBorderBrush{check}{state}", disabled ? disabledBorder : state == "PointerOver" ? blue : glyphBorder);
                Set($"CheckBoxForeground{check}{state}", disabled ? disabledText : text);
                Set($"CheckBoxCheckGlyphForeground{check}{state}", disabled ? disabledText : text);
            }
        }

        // ToggleSwitch: flat track; dark knob when off, blue track when on.
        foreach (var state in new[] { "", "PointerOver", "Pressed", "Disabled" })
        {
            var disabled = state == "Disabled";
            Set("ToggleSwitchContainerBackground" + state, B("#00FFFFFF"));
            Set("ToggleSwitchFillOff" + state, disabled ? face : white);
            Set("ToggleSwitchFillOn" + state, disabled ? disabledFace : blue);
            Set("ToggleSwitchStrokeOff" + state, disabled ? disabledBorder : glyphBorder);
            Set("ToggleSwitchStrokeOn" + state, disabled ? disabledBorder : blue);
            Set("ToggleSwitchKnobFillOff" + state, disabled ? disabledText : glyphBorder);
            Set("ToggleSwitchKnobFillOn" + state, disabled ? disabledText : white);
        }
        Set("ToggleSwitchContentForeground", text);
        Set("ToggleSwitchHeaderForeground", text);

        // ComboBox: white box, 1px border; popup items highlight in blue.
        foreach (var state in new[] { "", "PointerOver", "Pressed", "Focused", "FocusedPointerOver" })
        {
            Set("ComboBoxBackground" + state, white);
            Set("ComboBoxForeground" + state, text);
        }
        Set("ComboBoxBorderBrush", inputBorder);
        Set("ComboBoxBorderBrushPointerOver", blue);
        Set("ComboBoxBorderBrushPressed", blue);
        Set("ComboBoxBorderBrushFocused", blue);
        Set("ComboBoxBackgroundDisabled", face);
        Set("ComboBoxForegroundDisabled", disabledText);
        Set("ComboBoxBorderBrushDisabled", disabledBorder);
        Set("ComboBoxDropDownBackground", white);
        Set("ComboBoxDropDownBorderBrush", inputBorder);
        Set("ComboBoxDropDownGlyphForeground", text);
        Set("ComboBoxItemBackground", white);
        Set("ComboBoxItemForeground", text);
        foreach (var state in new[] { "PointerOver", "Pressed", "Selected", "SelectedPointerOver", "SelectedPressed" })
        {
            Set("ComboBoxItemBackground" + state, blue);
            Set("ComboBoxItemForeground" + state, white);
        }

        // Slider: thin gray groove, blue value fill and thumb.
        Set("SliderTrackFill", B("#D6D6D6"));
        Set("SliderTrackFillPointerOver", B("#D6D6D6"));
        Set("SliderTrackFillPressed", B("#D6D6D6"));
        Set("SliderTrackValueFill", blue);
        Set("SliderTrackValueFillPointerOver", blue);
        Set("SliderTrackValueFillPressed", darkBlue);
        Set("SliderTrackValueFillDisabled", disabledFace);
        Set("SliderThumbBackground", blue);
        Set("SliderThumbBackgroundPointerOver", darkBlue);
        Set("SliderThumbBackgroundPressed", darkBlue);
        Set("SliderThumbBackgroundDisabled", disabledFace);
        Set("SliderThumbBorderBrush", blue);

        // ProgressBar: WinForms green fill on a light trough.
        Set("ProgressBarForeground", B("#06B025"));
        Set("ProgressBarBackground", B("#E6E6E6"));
        Set("ProgressBarBorderBrush", B("#BCBCBC"));

        // Expander (settings groups): flat header/content with group-box border.
        Set("ExpanderHeaderBackground", face);
        Set("ExpanderHeaderBackgroundPointerOver", hoverFace);
        Set("ExpanderHeaderBackgroundPressed", pressedFace);
        Set("ExpanderHeaderForeground", text);
        Set("ExpanderHeaderBorderBrush", groupBorder);
        Set("ExpanderContentBackground", face);
        Set("ExpanderContentBorderBrush", groupBorder);

        // TabControl / TabItem.
        Set("TabItemHeaderBackground", face);
        Set("TabItemHeaderBackgroundPointerOver", hoverFace);
        Set("TabItemHeaderBackgroundSelected", white);
        Set("TabItemHeaderBackgroundPressed", pressedFace);
        Set("TabItemHeaderForeground", text);
        Set("TabItemHeaderForegroundPointerOver", text);
        Set("TabItemHeaderForegroundSelected", text);
        Set("TabItemHeaderForegroundPressed", text);
        Set("TabItemHeaderSelectedPipeFill", blue);

        // Flat style overrides (Window font, CheckBox template, card border) are
        // added after FluentAvalonia's base theme so that they win.
        try
        {
            _ahkClassicStyleInclude = new StyleInclude(new Uri("avares://AutoTyper.Desktop/"))
            {
                Source = new Uri("avares://AutoTyper.Desktop/AhkClassicTheme.axaml")
            };
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
            try
            {
                Application.Current.Styles.Remove(_ahkClassicStyleInclude);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to remove AhkClassic theme: {ex.Message}");
            }

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
    private static void SaveAndSet(IResourceDictionary resources, object key, object value)
    {
        if (!_savedResourceValues.ContainsKey(key))
        {
            _savedResourceValues[key] = resources.TryGetValue(key, out object? existing) ? existing : null;
        }

        resources[key] = value;
    }
}
