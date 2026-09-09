namespace AutoTyper.Core.Input;

/// <summary>
/// A platform-neutral keyboard key, as used in a <see cref="HotkeyCombo"/>.
/// Deliberately covers only keys that make sense as a global hotkey trigger —
/// not every key a keyboard can produce.
/// </summary>
/// <remarks>
/// Values are explicit and must never be renumbered: they are persisted (by
/// name) in the user's settings file. Names mostly match
/// <c>System.Windows.Input.Key</c> so hotkeys saved by the old WPF build still
/// parse; where WPF used an unfriendly name (<c>Prior</c>, <c>Next</c>,
/// <c>Capital</c>, <c>Snapshot</c>, the <c>Oem1</c>-<c>Oem7</c> series) this
/// enum uses the readable name and
/// <see cref="HotkeyCombo.TryParse"/> accepts the legacy spelling as an alias.
/// <para>
/// Mapping to an actual OS keycode lives in the platform adapters, not here.
/// Note the two platforms differ in kind, not just in numbering: Windows
/// virtual-key codes are logical (VK_A is "the A key" whatever the layout),
/// while macOS keycodes are positional (kVK_ANSI_A is the physical key that is
/// A on a US layout, and stays that physical key on Dvorak). A combo therefore
/// cannot be guaranteed to sit under the same fingers on both platforms.
/// </para>
/// </remarks>
public enum HotkeyKey
{
    None = 0,

    A = 1,
    B = 2,
    C = 3,
    D = 4,
    E = 5,
    F = 6,
    G = 7,
    H = 8,
    I = 9,
    J = 10,
    K = 11,
    L = 12,
    M = 13,
    N = 14,
    O = 15,
    P = 16,
    Q = 17,
    R = 18,
    S = 19,
    T = 20,
    U = 21,
    V = 22,
    W = 23,
    X = 24,
    Y = 25,
    Z = 26,

    D0 = 30,
    D1 = 31,
    D2 = 32,
    D3 = 33,
    D4 = 34,
    D5 = 35,
    D6 = 36,
    D7 = 37,
    D8 = 38,
    D9 = 39,

    F1 = 50,
    F2 = 51,
    F3 = 52,
    F4 = 53,
    F5 = 54,
    F6 = 55,
    F7 = 56,
    F8 = 57,
    F9 = 58,
    F10 = 59,
    F11 = 60,
    F12 = 61,
    F13 = 62,
    F14 = 63,
    F15 = 64,
    F16 = 65,
    F17 = 66,
    F18 = 67,
    F19 = 68,
    F20 = 69,
    F21 = 70,
    F22 = 71,
    F23 = 72,
    F24 = 73,

    NumPad0 = 80,
    NumPad1 = 81,
    NumPad2 = 82,
    NumPad3 = 83,
    NumPad4 = 84,
    NumPad5 = 85,
    NumPad6 = 86,
    NumPad7 = 87,
    NumPad8 = 88,
    NumPad9 = 89,
    Multiply = 90,
    Add = 91,
    Subtract = 92,
    Decimal = 93,
    Divide = 94,

    Back = 100,
    Tab = 101,
    Enter = 102,
    Escape = 103,
    Space = 104,
    PageUp = 105,
    PageDown = 106,
    End = 107,
    Home = 108,
    Left = 109,
    Up = 110,
    Right = 111,
    Down = 112,
    Insert = 113,
    Delete = 114,

    CapsLock = 120,
    NumLock = 121,
    Scroll = 122,
    Pause = 123,
    PrintScreen = 124,

    OemPlus = 140,
    OemMinus = 141,
    OemComma = 142,
    OemPeriod = 143,
    OemSemicolon = 144,
    OemQuestion = 145,
    OemTilde = 146,
    OemOpenBrackets = 147,
    OemCloseBrackets = 148,
    OemPipe = 149,
    OemQuotes = 150,
    OemBackslash = 151,
}
