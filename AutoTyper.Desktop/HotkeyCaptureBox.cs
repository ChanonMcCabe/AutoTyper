using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using AutoTyper.Core.Input;

namespace AutoTyper.Desktop;

/// <summary>
/// Read-only text box that captures a modifier+key combo from the keyboard
/// instead of accepting typed text. Press Escape to clear the current combo.
/// </summary>
public class HotkeyCaptureBox : TextBox
{
    private const string Placeholder = "Press a key combo...";

    public static readonly StyledProperty<HotkeyCombo?> ComboProperty =
        AvaloniaProperty.Register<HotkeyCaptureBox, HotkeyCombo?>(
            nameof(Combo),
            defaultBindingMode: BindingMode.TwoWay);

    public HotkeyCaptureBox()
    {
        IsReadOnly = true;
        Text = Placeholder;
        Cursor = new Cursor(StandardCursorType.Arrow);

        // Tunnelling, so the combo is claimed before TextBox's own key handling
        // or focus navigation sees it — otherwise Tab would move focus away
        // instead of being captured as part of a hotkey.
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Styles are matched by type in Avalonia, so without this a subclass gets
    /// none of the theme's TextBox styling.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(TextBox);

    public HotkeyCombo? Combo
    {
        get => GetValue(ComboProperty);
        set => SetValue(ComboProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ComboProperty)
        {
            Text = change.GetNewValue<HotkeyCombo?>() is { } combo ? combo.ToString() : Placeholder;
        }
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            Combo = null;
            Text = Placeholder;
            return;
        }

        if (IsModifierKey(e.Key))
        {
            return;
        }

        // Avalonia's Key names line up with HotkeyKey's, and TryParseKey absorbs
        // the few spellings this enum renamed (Return, Prior, Oem1...). A key
        // with no HotkeyKey equivalent is not a usable hotkey, so ignoring it is
        // the right response.
        if (HotkeyCombo.TryParseKey(e.Key.ToString(), out HotkeyKey key))
        {
            Combo = new HotkeyCombo(ToHotkeyModifiers(e.KeyModifiers), key);
        }
    }

    private static bool IsModifierKey(Key key) => key is Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift
        or Key.LWin or Key.RWin;

    private static HotkeyModifiers ToHotkeyModifiers(KeyModifiers modifiers)
    {
        var result = HotkeyModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= HotkeyModifiers.Alt;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            result |= HotkeyModifiers.Control;
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= HotkeyModifiers.Shift;
        }

        if (modifiers.HasFlag(KeyModifiers.Meta))
        {
            result |= HotkeyModifiers.Meta;
        }

        return result;
    }
}
