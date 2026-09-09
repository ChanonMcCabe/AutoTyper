using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AutoTyper.Core.Input;

/// <summary>
/// A captured modifier+key combination, e.g. Ctrl+Alt+T. Produced by the
/// hotkey capture control and consumed by an <see cref="IHotkeyProvider"/>.
/// </summary>
/// <remarks>
/// There are two string forms deliberately. <see cref="ToString"/> is for
/// display and names <see cref="HotkeyModifiers.Meta"/> per-platform ("Win" on
/// Windows, "Cmd" on macOS). <see cref="ToCanonicalString"/> is for
/// persistence and always writes "Meta", so a settings file stays readable the
/// same way on both platforms. <see cref="TryParse"/> accepts either form,
/// plus the legacy WPF spellings.
/// </remarks>
[JsonConverter(typeof(HotkeyComboJsonConverter))]
public readonly record struct HotkeyCombo(HotkeyModifiers Modifiers, HotkeyKey Key)
{
    /// <summary>Legacy WPF key names that this enum renamed for readability.</summary>
    private static readonly Dictionary<string, HotkeyKey> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Prior"] = HotkeyKey.PageUp,
        ["Next"] = HotkeyKey.PageDown,
        ["Return"] = HotkeyKey.Enter,
        ["Capital"] = HotkeyKey.CapsLock,
        ["Snapshot"] = HotkeyKey.PrintScreen,
        ["Backspace"] = HotkeyKey.Back,
        ["Oem1"] = HotkeyKey.OemSemicolon,
        ["Oem2"] = HotkeyKey.OemQuestion,
        ["Oem3"] = HotkeyKey.OemTilde,
        ["Oem4"] = HotkeyKey.OemOpenBrackets,
        ["Oem5"] = HotkeyKey.OemPipe,
        ["Oem6"] = HotkeyKey.OemCloseBrackets,
        ["Oem7"] = HotkeyKey.OemQuotes,
        ["Oem102"] = HotkeyKey.OemBackslash,
    };

    private static readonly Dictionary<string, HotkeyModifiers> ModifierAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ctrl"] = HotkeyModifiers.Control,
        ["Control"] = HotkeyModifiers.Control,
        ["Alt"] = HotkeyModifiers.Alt,
        ["Option"] = HotkeyModifiers.Alt,
        ["Opt"] = HotkeyModifiers.Alt,
        ["Shift"] = HotkeyModifiers.Shift,
        ["Meta"] = HotkeyModifiers.Meta,
        ["Win"] = HotkeyModifiers.Meta,
        ["Windows"] = HotkeyModifiers.Meta,
        ["Cmd"] = HotkeyModifiers.Meta,
        ["Command"] = HotkeyModifiers.Meta,
        ["Super"] = HotkeyModifiers.Meta,
    };

    /// <summary>
    /// True when this combo could actually be registered — a bare modifier
    /// with no real key is not a usable hotkey.
    /// </summary>
    public bool IsComplete => Key != HotkeyKey.None;

    /// <summary>Display form, naming Meta the way this platform's users expect.</summary>
    public override string ToString() => Format(OperatingSystem.IsMacOS() ? "Cmd" : "Win");

    /// <summary>Persistence form; always writes "Meta" so it reads the same on both platforms.</summary>
    public string ToCanonicalString() => Format("Meta");

    private string Format(string metaName)
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Meta))
        {
            parts.Add(metaName);
        }

        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    /// <summary>
    /// Parses "Ctrl+Alt+F9" and its variants. Accepts every modifier spelling
    /// in <see cref="ModifierAliases"/> and the legacy WPF key names, so a
    /// combo written by any build or either platform round-trips.
    /// </summary>
    public static bool TryParse(string? text, out HotkeyCombo combo)
    {
        combo = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!ModifierAliases.TryGetValue(parts[i], out HotkeyModifiers modifier))
            {
                return false;
            }

            modifiers |= modifier;
        }

        if (!TryParseKey(parts[^1], out HotkeyKey key))
        {
            return false;
        }

        combo = new HotkeyCombo(modifiers, key);
        return true;
    }

    /// <summary>
    /// Parses a single key name, accepting the legacy WPF spelling. Rejects
    /// numeric input so a stray "5" can't silently become an arbitrary enum
    /// value.
    /// </summary>
    public static bool TryParseKey(string? text, out HotkeyKey key)
    {
        key = HotkeyKey.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();
        if (KeyAliases.TryGetValue(text, out key))
        {
            return true;
        }

        return !char.IsAsciiDigit(text[0])
            && Enum.TryParse(text, ignoreCase: true, out key)
            && Enum.IsDefined(key);
    }

    /// <summary>
    /// Parses a modifier list, accepting both the canonical "+"-joined form
    /// ("Ctrl+Alt") and the legacy WPF flags-enum form ("Control, Alt").
    /// </summary>
    public static bool TryParseModifiers(string? text, out HotkeyModifiers modifiers)
    {
        modifiers = HotkeyModifiers.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (string part in text.Split([',', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!ModifierAliases.TryGetValue(part, out HotkeyModifiers modifier))
            {
                return false;
            }

            modifiers |= modifier;
        }

        return true;
    }

    [SuppressMessage("Design", "CA1065", Justification = "Parse-or-throw counterpart to TryParse, matching BCL convention.")]
    public static HotkeyCombo Parse(string text) =>
        TryParse(text, out HotkeyCombo combo)
            ? combo
            : throw new FormatException($"'{text}' is not a valid hotkey combination.");
}
