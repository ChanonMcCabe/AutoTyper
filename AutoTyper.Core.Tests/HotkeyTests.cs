using System.Text.Encodings.Web;
using System.Text.Json;
using AutoTyper.Core.Input;
using AutoTyper.Core.Settings;

namespace AutoTyper.Core.Tests;

public class HotkeyTests
{
    private static HotkeyKey[] AllKeys => Enum.GetValues<HotkeyKey>().Where(k => k != HotkeyKey.None).ToArray();

    [Fact]
    public void ToCanonicalString_OrdersModifiersAndNamesMetaPlatformNeutrally()
    {
        var combo = new HotkeyCombo(
            HotkeyModifiers.Meta | HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Alt,
            HotkeyKey.F9);

        Assert.Equal("Ctrl+Alt+Shift+Meta+F9", combo.ToCanonicalString());
    }

    [Theory]
    [InlineData("Ctrl+Alt+F9")]
    [InlineData("Control+Alt+F9")]
    [InlineData("ctrl+alt+f9")]
    [InlineData(" Ctrl + Alt + F9 ")]
    public void TryParse_AcceptsSpellingVariants(string text)
    {
        Assert.True(HotkeyCombo.TryParse(text, out HotkeyCombo combo));
        Assert.Equal(new HotkeyCombo(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.F9), combo);
    }

    [Theory]
    [InlineData("Win+A")]
    [InlineData("Cmd+A")]
    [InlineData("Command+A")]
    [InlineData("Meta+A")]
    [InlineData("Super+A")]
    public void TryParse_TreatsEveryMetaSpellingAsTheSameModifier(string text)
    {
        Assert.True(HotkeyCombo.TryParse(text, out HotkeyCombo combo));
        Assert.Equal(new HotkeyCombo(HotkeyModifiers.Meta, HotkeyKey.A), combo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+")]
    [InlineData("NotAModifier+A")]
    [InlineData("Ctrl+NotAKey")]
    [InlineData("Ctrl+5")]
    public void TryParse_RejectsMalformedInput(string text)
    {
        Assert.False(HotkeyCombo.TryParse(text, out _));
    }

    [Theory]
    [InlineData("Return", HotkeyKey.Enter)]
    [InlineData("Prior", HotkeyKey.PageUp)]
    [InlineData("Next", HotkeyKey.PageDown)]
    [InlineData("Capital", HotkeyKey.CapsLock)]
    [InlineData("Snapshot", HotkeyKey.PrintScreen)]
    [InlineData("Oem3", HotkeyKey.OemTilde)]
    public void TryParseKey_AcceptsLegacyWpfSpellings(string legacyName, HotkeyKey expected)
    {
        Assert.True(HotkeyCombo.TryParseKey(legacyName, out HotkeyKey key));
        Assert.Equal(expected, key);
    }

    [Fact]
    public void JsonRoundTrip_PreservesTheCombo()
    {
        var original = new HotkeyCombo(HotkeyModifiers.Control | HotkeyModifiers.Shift, HotkeyKey.OemQuestion);

        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<HotkeyCombo>(json);

        Assert.Equal(original, restored);
        Assert.Equal("Ctrl+Shift+OemQuestion", original.ToCanonicalString());
    }

    [Fact]
    public void Serialize_WithTheAppsEncoder_WritesThePlusSignsUnescaped()
    {
        // System.Text.Json's default encoder escapes '+' to +, which would
        // leave a perfectly valid but unreadable "Ctrl+Alt+F9" in a
        // settings file people are expected to be able to open and edit. The
        // relaxed encoder is what keeps it legible; this pins that down so the
        // option can't be dropped from SettingsService without a test failing.
        var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var combo = new HotkeyCombo(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.F9);

        Assert.Equal("\"Ctrl+Alt+F9\"", JsonSerializer.Serialize(combo, options));
    }

    [Fact]
    public void Deserialize_LegacyWpfObjectForm_StillYieldsTheSameHotkey()
    {
        // What the WPF build wrote: HotkeyCombo serialized as an object, with
        // the member names of WPF's own ModifierKeys/Key enums. Losing this
        // would silently wipe the trigger hotkey of every existing user on
        // first launch after the upgrade.
        const string legacyJson = """
            { "Modifiers": "Control, Alt", "Key": "F9" }
            """;

        var combo = JsonSerializer.Deserialize<HotkeyCombo>(legacyJson);

        Assert.Equal(new HotkeyCombo(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.F9), combo);
    }

    [Fact]
    public void Deserialize_LegacyWindowsModifier_MapsToMeta()
    {
        const string legacyJson = """
            { "Modifiers": "Windows, Shift", "Key": "Prior" }
            """;

        var combo = JsonSerializer.Deserialize<HotkeyCombo>(legacyJson);

        Assert.Equal(new HotkeyCombo(HotkeyModifiers.Meta | HotkeyModifiers.Shift, HotkeyKey.PageUp), combo);
    }

    [Fact]
    public void Deserialize_LegacyHotkeySettingsGroup_KeepsTheStoredCombo()
    {
        // The realistic shape: a whole settings group as it sits in an existing
        // settings.json, not a bare combo.
        const string legacyJson = """
            { "Combo": { "Modifiers": "Control, Alt", "Key": "T" } }
            """;

        var settings = JsonSerializer.Deserialize<HotkeySettings>(legacyJson);

        Assert.NotNull(settings);
        Assert.Equal(new HotkeyCombo(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.T), settings!.Combo);
    }

    [Fact]
    public void Deserialize_MissingHotkey_LeavesItUnset()
    {
        var settings = JsonSerializer.Deserialize<HotkeySettings>("{}");

        Assert.NotNull(settings);
        Assert.Null(settings!.Combo);
    }

    [Fact]
    public void IsComplete_IsFalseForAModifierWithNoRealKey()
    {
        Assert.False(new HotkeyCombo(HotkeyModifiers.Control, HotkeyKey.None).IsComplete);
        Assert.True(new HotkeyCombo(HotkeyModifiers.Control, HotkeyKey.A).IsComplete);
    }

    [Fact]
    public void WindowsVirtualKeyMap_CoversEveryKeyExactlyOnce()
    {
        var mapped = new Dictionary<ushort, HotkeyKey>();

        foreach (HotkeyKey key in AllKeys)
        {
            Assert.True(
                WindowsVirtualKeyMap.TryGetVirtualKey(key, out ushort vk),
                $"{key} has no Windows virtual-key mapping.");

            Assert.False(
                mapped.TryGetValue(vk, out HotkeyKey existing),
                $"{key} and {existing} both map to Windows VK 0x{vk:X2}.");

            mapped[vk] = key;
        }
    }

    [Fact]
    public void MacVirtualKeyMap_CoversEveryKeyExactlyOnce_ExceptTheDocumentedGaps()
    {
        // macOS genuinely has no keycode for these: Apple keyboards never had
        // PrintScreen/ScrollLock/Pause, and HIToolbox stops at kVK_F20.
        HotkeyKey[] expectedUnmapped =
        [
            HotkeyKey.PrintScreen,
            HotkeyKey.Scroll,
            HotkeyKey.Pause,
            HotkeyKey.F21,
            HotkeyKey.F22,
            HotkeyKey.F23,
            HotkeyKey.F24,
        ];

        var mapped = new Dictionary<ushort, HotkeyKey>();
        var unmapped = new List<HotkeyKey>();

        foreach (HotkeyKey key in AllKeys)
        {
            if (!MacVirtualKeyMap.TryGetVirtualKey(key, out ushort vk))
            {
                unmapped.Add(key);
                continue;
            }

            Assert.False(
                mapped.TryGetValue(vk, out HotkeyKey existing),
                $"{key} and {existing} both map to macOS keycode 0x{vk:X2}.");

            mapped[vk] = key;
        }

        Assert.Equal(expectedUnmapped.Order().ToArray(), unmapped.Order().ToArray());
    }

    [Fact]
    public void KeyMaps_RejectNone()
    {
        Assert.False(WindowsVirtualKeyMap.TryGetVirtualKey(HotkeyKey.None, out _));
        Assert.False(MacVirtualKeyMap.TryGetVirtualKey(HotkeyKey.None, out _));
    }

    [Fact]
    public void MacVirtualKeyMap_DistinguishesBackspaceFromForwardDelete()
    {
        // The classic macOS trap: kVK_Delete (0x33) is the backspace key above
        // Return, and the key actually labelled Delete is kVK_ForwardDelete.
        Assert.True(MacVirtualKeyMap.TryGetVirtualKey(HotkeyKey.Back, out ushort back));
        Assert.True(MacVirtualKeyMap.TryGetVirtualKey(HotkeyKey.Delete, out ushort forwardDelete));

        Assert.Equal(0x33, back);
        Assert.Equal(0x75, forwardDelete);
    }

    [Fact]
    public void WindowsVirtualKeyMap_UsesTheStandardCodesForCommonKeys()
    {
        Assert.True(WindowsVirtualKeyMap.TryGetVirtualKey(HotkeyKey.A, out ushort a));
        Assert.True(WindowsVirtualKeyMap.TryGetVirtualKey(HotkeyKey.D0, out ushort zero));
        Assert.True(WindowsVirtualKeyMap.TryGetVirtualKey(HotkeyKey.F9, out ushort f9));
        Assert.True(WindowsVirtualKeyMap.TryGetVirtualKey(HotkeyKey.Escape, out ushort escape));

        Assert.Equal(0x41, a);
        Assert.Equal(0x30, zero);
        Assert.Equal(0x78, f9);
        Assert.Equal(0x1B, escape);
    }
}
