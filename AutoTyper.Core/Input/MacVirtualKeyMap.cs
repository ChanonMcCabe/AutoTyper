namespace AutoTyper.Core.Input;

/// <summary>
/// Translates a platform-neutral <see cref="HotkeyKey"/> into the Carbon /
/// HIToolbox keycode (<c>kVK_*</c> from <c>&lt;HIToolbox/Events.h&gt;</c>) that
/// <c>RegisterEventHotKey</c> expects.
/// </summary>
/// <remarks>
/// macOS keycodes are <em>positional</em>, not logical. <c>kVK_ANSI_A</c> (0x00)
/// names the physical key that types A on a US ANSI layout, and it stays that
/// same physical key on Dvorak, AZERTY or any other layout — where the user
/// would be pressing a different letter. Windows virtual-key codes (see
/// <see cref="WindowsVirtualKeyMap"/>) are the opposite: logical, following the
/// letter across layouts. A combo saved on one platform therefore cannot be
/// guaranteed to sit under the same fingers on the other. The names in this
/// table describe the US-layout legend of each physical key.
/// <para>
/// Keys with no HIToolbox equivalent are deliberately <em>omitted</em> rather
/// than given an invented code, so <see cref="TryGetVirtualKey"/> reports them
/// as unmappable and the caller can refuse the hotkey instead of registering
/// the wrong key. Those are:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="HotkeyKey.PrintScreen"/>, <see cref="HotkeyKey.Scroll"/> and
/// <see cref="HotkeyKey.Pause"/> — Apple keyboards have never had these keys
/// and the header defines no constant for them.
/// </description></item>
/// <item><description>
/// <see cref="HotkeyKey.F21"/> through <see cref="HotkeyKey.F24"/> — HIToolbox
/// stops at <c>kVK_F20</c> (0x5A).
/// </description></item>
/// <item><description>
/// <see cref="HotkeyKey.None"/> — not a real key, so an incomplete combo fails
/// lookup rather than registering something arbitrary.
/// </description></item>
/// </list>
/// </remarks>
public static class MacVirtualKeyMap
{
    private static readonly Dictionary<HotkeyKey, ushort> Map = new()
    {
        // Letters — kVK_ANSI_*. The ordering looks scrambled because these are
        // physical positions on the original Apple keyboard matrix, not an
        // alphabet.
        [HotkeyKey.A] = 0x00,
        [HotkeyKey.B] = 0x0B,
        [HotkeyKey.C] = 0x08,
        [HotkeyKey.D] = 0x02,
        [HotkeyKey.E] = 0x0E,
        [HotkeyKey.F] = 0x03,
        [HotkeyKey.G] = 0x05,
        [HotkeyKey.H] = 0x04,
        [HotkeyKey.I] = 0x22,
        [HotkeyKey.J] = 0x26,
        [HotkeyKey.K] = 0x28,
        [HotkeyKey.L] = 0x25,
        [HotkeyKey.M] = 0x2E,
        [HotkeyKey.N] = 0x2D,
        [HotkeyKey.O] = 0x1F,
        [HotkeyKey.P] = 0x23,
        [HotkeyKey.Q] = 0x0C,
        [HotkeyKey.R] = 0x0F,
        [HotkeyKey.S] = 0x01,
        [HotkeyKey.T] = 0x11,
        [HotkeyKey.U] = 0x20,
        [HotkeyKey.V] = 0x09,
        [HotkeyKey.W] = 0x0D,
        [HotkeyKey.X] = 0x07,
        [HotkeyKey.Y] = 0x10,
        [HotkeyKey.Z] = 0x06,

        // Top-row digits — kVK_ANSI_0 through kVK_ANSI_9. Note 5/6 and 7/8/9/0
        // are interleaved with the Equal and Minus keys in the matrix.
        [HotkeyKey.D0] = 0x1D,
        [HotkeyKey.D1] = 0x12,
        [HotkeyKey.D2] = 0x13,
        [HotkeyKey.D3] = 0x14,
        [HotkeyKey.D4] = 0x15,
        [HotkeyKey.D5] = 0x17,
        [HotkeyKey.D6] = 0x16,
        [HotkeyKey.D7] = 0x1A,
        [HotkeyKey.D8] = 0x1C,
        [HotkeyKey.D9] = 0x19,

        // Function keys — kVK_F1 through kVK_F20. F21-F24 have no constant and
        // are intentionally absent.
        [HotkeyKey.F1] = 0x7A,
        [HotkeyKey.F2] = 0x78,
        [HotkeyKey.F3] = 0x63,
        [HotkeyKey.F4] = 0x76,
        [HotkeyKey.F5] = 0x60,
        [HotkeyKey.F6] = 0x61,
        [HotkeyKey.F7] = 0x62,
        [HotkeyKey.F8] = 0x64,
        [HotkeyKey.F9] = 0x65,
        [HotkeyKey.F10] = 0x6D,
        [HotkeyKey.F11] = 0x67,
        [HotkeyKey.F12] = 0x6F,
        [HotkeyKey.F13] = 0x69,
        [HotkeyKey.F14] = 0x6B,
        [HotkeyKey.F15] = 0x71,
        [HotkeyKey.F16] = 0x6A,
        [HotkeyKey.F17] = 0x40,
        [HotkeyKey.F18] = 0x4F,
        [HotkeyKey.F19] = 0x50,
        [HotkeyKey.F20] = 0x5A,

        // Numeric keypad — kVK_ANSI_Keypad*.
        [HotkeyKey.NumPad0] = 0x52,
        [HotkeyKey.NumPad1] = 0x53,
        [HotkeyKey.NumPad2] = 0x54,
        [HotkeyKey.NumPad3] = 0x55,
        [HotkeyKey.NumPad4] = 0x56,
        [HotkeyKey.NumPad5] = 0x57,
        [HotkeyKey.NumPad6] = 0x58,
        [HotkeyKey.NumPad7] = 0x59,
        [HotkeyKey.NumPad8] = 0x5B,
        [HotkeyKey.NumPad9] = 0x5C,
        [HotkeyKey.Multiply] = 0x43,   // kVK_ANSI_KeypadMultiply
        [HotkeyKey.Add] = 0x45,        // kVK_ANSI_KeypadPlus
        [HotkeyKey.Subtract] = 0x4E,   // kVK_ANSI_KeypadMinus
        [HotkeyKey.Decimal] = 0x41,    // kVK_ANSI_KeypadDecimal
        [HotkeyKey.Divide] = 0x4B,     // kVK_ANSI_KeypadDivide

        // Editing and navigation. The two delete keys are the classic trap:
        // kVK_Delete is backspace (the key above Return), while the key labelled
        // Delete on a full-size keyboard is kVK_ForwardDelete.
        [HotkeyKey.Back] = 0x33,       // kVK_Delete — backspace
        [HotkeyKey.Tab] = 0x30,        // kVK_Tab
        [HotkeyKey.Enter] = 0x24,      // kVK_Return — the main Return, not KeypadEnter
        [HotkeyKey.Escape] = 0x35,     // kVK_Escape
        [HotkeyKey.Space] = 0x31,      // kVK_Space
        [HotkeyKey.PageUp] = 0x74,     // kVK_PageUp
        [HotkeyKey.PageDown] = 0x79,   // kVK_PageDown
        [HotkeyKey.End] = 0x77,        // kVK_End
        [HotkeyKey.Home] = 0x73,       // kVK_Home
        [HotkeyKey.Left] = 0x7B,       // kVK_LeftArrow
        [HotkeyKey.Up] = 0x7E,         // kVK_UpArrow
        [HotkeyKey.Right] = 0x7C,      // kVK_RightArrow
        [HotkeyKey.Down] = 0x7D,       // kVK_DownArrow
        [HotkeyKey.Insert] = 0x72,     // kVK_Help — occupies the Insert position
        [HotkeyKey.Delete] = 0x75,     // kVK_ForwardDelete — the real Delete

        // Lock keys. macOS has no Num Lock; kVK_ANSI_KeypadClear sits in that
        // physical position on the keypad, so Num Lock maps there.
        [HotkeyKey.CapsLock] = 0x39,   // kVK_CapsLock
        [HotkeyKey.NumLock] = 0x47,    // kVK_ANSI_KeypadClear

        // Punctuation. Named for the US-layout legend of the physical key: the
        // enum uses the WPF "Oem" name of the same position.
        [HotkeyKey.OemPlus] = 0x18,           // kVK_ANSI_Equal — "=" / "+"
        [HotkeyKey.OemMinus] = 0x1B,          // kVK_ANSI_Minus — "-" / "_"
        [HotkeyKey.OemComma] = 0x2B,          // kVK_ANSI_Comma
        [HotkeyKey.OemPeriod] = 0x2F,         // kVK_ANSI_Period
        [HotkeyKey.OemSemicolon] = 0x29,      // kVK_ANSI_Semicolon
        [HotkeyKey.OemQuestion] = 0x2C,       // kVK_ANSI_Slash — "/" / "?"
        [HotkeyKey.OemTilde] = 0x32,          // kVK_ANSI_Grave — "`" / "~"
        [HotkeyKey.OemOpenBrackets] = 0x21,   // kVK_ANSI_LeftBracket
        [HotkeyKey.OemCloseBrackets] = 0x1E,  // kVK_ANSI_RightBracket
        [HotkeyKey.OemPipe] = 0x2A,           // kVK_ANSI_Backslash — "\" / "|"
        [HotkeyKey.OemQuotes] = 0x27,         // kVK_ANSI_Quote
        [HotkeyKey.OemBackslash] = 0x0A,      // kVK_ISO_Section — the extra ISO key by left Shift
    };

    /// <summary>
    /// Looks up the Carbon keycode for <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The platform-neutral key to translate.</param>
    /// <param name="virtualKey">The <c>kVK_*</c> code, or 0 when unmapped.</param>
    /// <returns>
    /// <see langword="true"/> when macOS has a keycode for this key;
    /// <see langword="false"/> for <see cref="HotkeyKey.None"/>, for the keys
    /// listed as unmapped in the type's remarks, and for any value outside the
    /// enum. A <see langword="false"/> result means the hotkey cannot be
    /// registered on this platform.
    /// </returns>
    public static bool TryGetVirtualKey(HotkeyKey key, out ushort virtualKey) =>
        Map.TryGetValue(key, out virtualKey);
}
