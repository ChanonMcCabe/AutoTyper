using System.Runtime.InteropServices;
using System.Text;
using AutoTyper.Desktop.Mac;

namespace AutoTyper.Desktop.Tests;

/// <summary>
/// Covers exactly the part of the macOS hotkey P/Invoke surface that can be
/// verified without a Mac: struct layout and the four-character-code
/// constants transcribed from Carbon's <c>&lt;HIToolbox/Events.h&gt;</c>. A
/// wrong byte in one of those codes fails silently on a real Mac — the
/// handler installs, <c>RegisterEventHotKey</c> succeeds, and the key press
/// simply never fires — so catching a transcription typo here, where it
/// shows up as a failing assertion instead, is the whole point of this file.
/// </summary>
public class CarbonNativeMethodsTests
{
    [Fact]
    public void EventHotKeyId_IsTwoPackedUInt32Fields()
    {
        Assert.Equal(8, Marshal.SizeOf<CarbonNativeMethods.EventHotKeyId>());
    }

    [Fact]
    public void EventTypeSpec_IsTwoPackedUInt32Fields()
    {
        Assert.Equal(8, Marshal.SizeOf<CarbonNativeMethods.EventTypeSpec>());
    }

    [Theory]
    [InlineData("keyb", CarbonNativeMethods.EventClassKeyboard)]
    [InlineData("hkid", CarbonNativeMethods.TypeEventHotKeyId)]
    [InlineData("----", CarbonNativeMethods.EventParamDirectObject)]
    public void FourCharCodeConstants_MatchTheirAsciiBytesBigEndian(string fourCharCode, uint expected)
    {
        // OSType four-character codes pack ASCII bytes big-endian into a
        // uint32: 'abcd' -> (a<<24)|(b<<16)|(c<<8)|d. Computed independently
        // here rather than copied from the constant under test.
        byte[] bytes = Encoding.ASCII.GetBytes(fourCharCode);
        Assert.Equal(4, bytes.Length);

        uint computed = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);

        Assert.Equal(expected, computed);
    }

    [Fact]
    public void ModifierConstants_MatchClassicMacEventRecordBitPositions()
    {
        // From <Events.h>: cmdKeyBit=8, shiftKeyBit=9, optionKeyBit=11,
        // controlKeyBit=12 — these are EventRecord.modifiers bit positions,
        // not NSEvent's modifier flags, which use different bit numbers.
        Assert.Equal(1u << 8, CarbonNativeMethods.CmdKey);
        Assert.Equal(1u << 9, CarbonNativeMethods.ShiftKey);
        Assert.Equal(1u << 11, CarbonNativeMethods.OptionKey);
        Assert.Equal(1u << 12, CarbonNativeMethods.ControlKey);
    }

    [Fact]
    public void AccessibilityConstants_MatchSdkHeaders()
    {
        // kCFStringEncodingUTF8 from <CFString.h>; attribute names from
        // <AXAttributeConstants.h>. A typo in an attribute name makes the AX
        // call fail with kAXErrorAttributeUnsupported, which the step-away
        // code treats as "do nothing" — silently, with no error.
        Assert.Equal(0x08000100u, CoreFoundationNativeMethods.KCFStringEncodingUTF8);
        Assert.Equal("AXFocusedApplication", AccessibilityNativeMethods.KAXFocusedApplicationAttribute);
        Assert.Equal("AXFrontmost", AccessibilityNativeMethods.KAXFrontmostAttribute);
    }
}
