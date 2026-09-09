namespace AutoTyper.Core.Input;

/// <summary>
/// Platform-neutral modifier keys for a <see cref="HotkeyCombo"/>.
/// </summary>
/// <remarks>
/// The values intentionally match the old WPF <c>ModifierKeys</c> enum so
/// settings written by the WPF build keep their meaning. Only the name of the
/// last flag changed: WPF called it <c>Windows</c>, which has no meaning on
/// macOS. <see cref="Meta"/> is the Windows key on Windows and the Command key
/// on macOS; <see cref="HotkeyCombo.ToString"/> renders it per-platform.
/// </remarks>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,

    /// <summary>Windows key on Windows, Command key on macOS.</summary>
    Meta = 8,
}
