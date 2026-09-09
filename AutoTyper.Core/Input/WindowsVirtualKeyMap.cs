namespace AutoTyper.Core.Input;

/// <summary>
/// Translates a platform-neutral <see cref="HotkeyKey"/> into the Win32
/// virtual-key code (<c>VK_*</c>) that <c>RegisterHotKey</c> and the
/// <c>SendInput</c> family expect.
/// </summary>
/// <remarks>
/// Windows virtual-key codes are <em>logical</em>: <c>VK_A</c> means "the key
/// that produces A on the active layout", so a combo captured on QWERTY moves
/// with the letter — not with the physical key — when the user switches to
/// Dvorak. This is the opposite of <see cref="MacVirtualKeyMap"/>, whose codes
/// are positional.
/// <para>
/// Every <see cref="HotkeyKey"/> member except <see cref="HotkeyKey.None"/> is
/// mapped; <c>None</c> is deliberately absent so an incomplete combo fails
/// lookup rather than registering some arbitrary key.
/// </para>
/// </remarks>
public static class WindowsVirtualKeyMap
{
    private static readonly Dictionary<HotkeyKey, ushort> Map = new()
    {
        // Letters — VK_A (0x41) through VK_Z (0x5A).
        [HotkeyKey.A] = 0x41,
        [HotkeyKey.B] = 0x42,
        [HotkeyKey.C] = 0x43,
        [HotkeyKey.D] = 0x44,
        [HotkeyKey.E] = 0x45,
        [HotkeyKey.F] = 0x46,
        [HotkeyKey.G] = 0x47,
        [HotkeyKey.H] = 0x48,
        [HotkeyKey.I] = 0x49,
        [HotkeyKey.J] = 0x4A,
        [HotkeyKey.K] = 0x4B,
        [HotkeyKey.L] = 0x4C,
        [HotkeyKey.M] = 0x4D,
        [HotkeyKey.N] = 0x4E,
        [HotkeyKey.O] = 0x4F,
        [HotkeyKey.P] = 0x50,
        [HotkeyKey.Q] = 0x51,
        [HotkeyKey.R] = 0x52,
        [HotkeyKey.S] = 0x53,
        [HotkeyKey.T] = 0x54,
        [HotkeyKey.U] = 0x55,
        [HotkeyKey.V] = 0x56,
        [HotkeyKey.W] = 0x57,
        [HotkeyKey.X] = 0x58,
        [HotkeyKey.Y] = 0x59,
        [HotkeyKey.Z] = 0x5A,

        // Top-row digits — VK_0 (0x30) through VK_9 (0x39).
        [HotkeyKey.D0] = 0x30,
        [HotkeyKey.D1] = 0x31,
        [HotkeyKey.D2] = 0x32,
        [HotkeyKey.D3] = 0x33,
        [HotkeyKey.D4] = 0x34,
        [HotkeyKey.D5] = 0x35,
        [HotkeyKey.D6] = 0x36,
        [HotkeyKey.D7] = 0x37,
        [HotkeyKey.D8] = 0x38,
        [HotkeyKey.D9] = 0x39,

        // Function keys — VK_F1 (0x70) through VK_F24 (0x87).
        [HotkeyKey.F1] = 0x70,
        [HotkeyKey.F2] = 0x71,
        [HotkeyKey.F3] = 0x72,
        [HotkeyKey.F4] = 0x73,
        [HotkeyKey.F5] = 0x74,
        [HotkeyKey.F6] = 0x75,
        [HotkeyKey.F7] = 0x76,
        [HotkeyKey.F8] = 0x77,
        [HotkeyKey.F9] = 0x78,
        [HotkeyKey.F10] = 0x79,
        [HotkeyKey.F11] = 0x7A,
        [HotkeyKey.F12] = 0x7B,
        [HotkeyKey.F13] = 0x7C,
        [HotkeyKey.F14] = 0x7D,
        [HotkeyKey.F15] = 0x7E,
        [HotkeyKey.F16] = 0x7F,
        [HotkeyKey.F17] = 0x80,
        [HotkeyKey.F18] = 0x81,
        [HotkeyKey.F19] = 0x82,
        [HotkeyKey.F20] = 0x83,
        [HotkeyKey.F21] = 0x84,
        [HotkeyKey.F22] = 0x85,
        [HotkeyKey.F23] = 0x86,
        [HotkeyKey.F24] = 0x87,

        // Numeric keypad — VK_NUMPAD0 (0x60) through VK_NUMPAD9 (0x69) and its operators.
        [HotkeyKey.NumPad0] = 0x60,
        [HotkeyKey.NumPad1] = 0x61,
        [HotkeyKey.NumPad2] = 0x62,
        [HotkeyKey.NumPad3] = 0x63,
        [HotkeyKey.NumPad4] = 0x64,
        [HotkeyKey.NumPad5] = 0x65,
        [HotkeyKey.NumPad6] = 0x66,
        [HotkeyKey.NumPad7] = 0x67,
        [HotkeyKey.NumPad8] = 0x68,
        [HotkeyKey.NumPad9] = 0x69,
        [HotkeyKey.Multiply] = 0x6A,   // VK_MULTIPLY
        [HotkeyKey.Add] = 0x6B,        // VK_ADD
        [HotkeyKey.Subtract] = 0x6D,   // VK_SUBTRACT
        [HotkeyKey.Decimal] = 0x6E,    // VK_DECIMAL
        [HotkeyKey.Divide] = 0x6F,     // VK_DIVIDE

        // Editing and navigation.
        [HotkeyKey.Back] = 0x08,       // VK_BACK
        [HotkeyKey.Tab] = 0x09,        // VK_TAB
        [HotkeyKey.Enter] = 0x0D,      // VK_RETURN
        [HotkeyKey.Escape] = 0x1B,     // VK_ESCAPE
        [HotkeyKey.Space] = 0x20,      // VK_SPACE
        [HotkeyKey.PageUp] = 0x21,     // VK_PRIOR
        [HotkeyKey.PageDown] = 0x22,   // VK_NEXT
        [HotkeyKey.End] = 0x23,        // VK_END
        [HotkeyKey.Home] = 0x24,       // VK_HOME
        [HotkeyKey.Left] = 0x25,       // VK_LEFT
        [HotkeyKey.Up] = 0x26,         // VK_UP
        [HotkeyKey.Right] = 0x27,      // VK_RIGHT
        [HotkeyKey.Down] = 0x28,       // VK_DOWN
        [HotkeyKey.Insert] = 0x2D,     // VK_INSERT
        [HotkeyKey.Delete] = 0x2E,     // VK_DELETE

        // Lock and system keys.
        [HotkeyKey.CapsLock] = 0x14,    // VK_CAPITAL
        [HotkeyKey.NumLock] = 0x90,     // VK_NUMLOCK
        [HotkeyKey.Scroll] = 0x91,      // VK_SCROLL
        [HotkeyKey.Pause] = 0x13,       // VK_PAUSE
        [HotkeyKey.PrintScreen] = 0x2C, // VK_SNAPSHOT

        // Punctuation. These are the US-layout OEM keys; on other layouts the
        // same code reaches the same physical key but may print something else.
        [HotkeyKey.OemPlus] = 0xBB,           // VK_OEM_PLUS
        [HotkeyKey.OemMinus] = 0xBD,          // VK_OEM_MINUS
        [HotkeyKey.OemComma] = 0xBC,          // VK_OEM_COMMA
        [HotkeyKey.OemPeriod] = 0xBE,         // VK_OEM_PERIOD
        [HotkeyKey.OemSemicolon] = 0xBA,      // VK_OEM_1
        [HotkeyKey.OemQuestion] = 0xBF,       // VK_OEM_2
        [HotkeyKey.OemTilde] = 0xC0,          // VK_OEM_3
        [HotkeyKey.OemOpenBrackets] = 0xDB,   // VK_OEM_4
        [HotkeyKey.OemPipe] = 0xDC,           // VK_OEM_5
        [HotkeyKey.OemCloseBrackets] = 0xDD,  // VK_OEM_6
        [HotkeyKey.OemQuotes] = 0xDE,         // VK_OEM_7
        [HotkeyKey.OemBackslash] = 0xE2,      // VK_OEM_102
    };

    /// <summary>
    /// Looks up the Win32 virtual-key code for <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The platform-neutral key to translate.</param>
    /// <param name="virtualKey">The <c>VK_*</c> code, or 0 when unmapped.</param>
    /// <returns>
    /// <see langword="true"/> when a code exists. Only
    /// <see cref="HotkeyKey.None"/> — and any value outside the enum — returns
    /// <see langword="false"/>.
    /// </returns>
    public static bool TryGetVirtualKey(HotkeyKey key, out ushort virtualKey) =>
        Map.TryGetValue(key, out virtualKey);
}
