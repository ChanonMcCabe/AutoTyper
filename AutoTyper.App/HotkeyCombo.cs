using System.Windows.Input;

namespace AutoTyper.App;

/// <summary>
/// A captured modifier+key combination, e.g. Ctrl+Alt+T. Produced by
/// <see cref="HotkeyCaptureBox"/> and consumed by <see cref="HotkeyManager"/>.
/// </summary>
public readonly record struct HotkeyCombo(ModifierKeys Modifiers, Key Key)
{
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}
