using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoTyper.App;

/// <summary>
/// Read-only text box that captures a modifier+key combo from the keyboard
/// instead of accepting typed text — the WPF replacement for the AHK MVP's
/// fixed hotkey dropdown. Press Escape to clear the current combo.
/// </summary>
public class HotkeyCaptureBox : TextBox
{
    private const string Placeholder = "Press a key combo...";

    public static readonly DependencyProperty ComboProperty =
        DependencyProperty.Register(
            nameof(Combo),
            typeof(HotkeyCombo?),
            typeof(HotkeyCaptureBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComboChanged));

    public HotkeyCaptureBox()
    {
        // Implicit styles (e.g. the WPF-UI themed TextBox style) are keyed by the
        // exact type and don't reach subclasses, so opt in explicitly.
        SetResourceReference(StyleProperty, typeof(TextBox));

        IsReadOnly = true;
        IsReadOnlyCaretVisible = false;
        Text = Placeholder;
        Cursor = Cursors.Arrow;
    }

    public HotkeyCombo? Combo
    {
        get => (HotkeyCombo?)GetValue(ComboProperty);
        set => SetValue(ComboProperty, value);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        e.Handled = true;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            Combo = null;
            Text = Placeholder;
            return;
        }

        if (IsModifierKey(key))
        {
            return;
        }

        Combo = new HotkeyCombo(Keyboard.Modifiers, key);
    }

    private static bool IsModifierKey(Key key) => key is Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift
        or Key.LWin or Key.RWin
        or Key.System;

    private static void OnComboChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HotkeyCaptureBox box)
        {
            return;
        }

        box.Text = e.NewValue is HotkeyCombo combo ? combo.ToString() : Placeholder;
    }
}
