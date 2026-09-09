using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// Carbon P/Invoke surface used for global hotkey registration. Bound
/// against <c>Carbon.framework</c>, which Apple has kept at this fixed path
/// despite deprecating most of Carbon, specifically because there is still
/// no modern replacement for <c>RegisterEventHotKey</c> — major third-party
/// Mac utilities (Rectangle, Hammerspoon, and others) still rely on it.
/// </summary>
/// <remarks>
/// The modifier-key constants and the four-character-code constants below
/// are transcribed from Carbon's <c>&lt;HIToolbox/Events.h&gt;</c>. A wrong
/// byte in a four-character code fails silently — the handler installs
/// without error, RegisterEventHotKey succeeds, and the key press simply
/// never fires — so these values were hand-verified against the documented
/// bit layout (modifier flags: classic Mac <c>EventRecord.modifiers</c> bit
/// positions, not the newer <c>NSEvent</c> flags; four-char codes: big-endian
/// ASCII packed into a <c>uint32</c>) rather than guessed. Struct sizes and
/// the four-char-code arithmetic itself are covered by
/// <c>AutoTyper.Core.Tests</c> so a future edit that breaks them fails
/// <c>dotnet test</c> rather than surfacing only on a real Mac.
/// </remarks>
[SupportedOSPlatform("macos")]
internal static class CarbonNativeMethods
{
    private const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";

    // Classic Mac modifier-flag bits (EventRecord.modifiers), not NSEvent's.
    internal const uint CmdKey = 0x0100;
    internal const uint ShiftKey = 0x0200;
    internal const uint OptionKey = 0x0800;
    internal const uint ControlKey = 0x1000;

    /// <summary>'keyb' — kEventClassKeyboard.</summary>
    internal const uint EventClassKeyboard = 0x6B657962;

    /// <summary>kEventHotKeyPressed — a plain integer constant, not a four-char code.</summary>
    internal const uint EventHotKeyPressed = 5;

    /// <summary>'----' — kEventParamDirectObject.</summary>
    internal const uint EventParamDirectObject = 0x2D2D2D2D;

    /// <summary>'hkid' — typeEventHotKeyID.</summary>
    internal const uint TypeEventHotKeyId = 0x686B6964;

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventHotKeyId
    {
        public uint Signature;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventTypeSpec
    {
        public uint EventClass;
        public uint EventKind;
    }

    internal delegate int EventHandlerProc(IntPtr inHandlerCallRef, IntPtr inEvent, IntPtr inUserData);

    [DllImport(Carbon)]
    internal static extern IntPtr GetApplicationEventTarget();

    [DllImport(Carbon)]
    internal static extern int InstallEventHandler(
        IntPtr inTarget, EventHandlerProc inHandler, int inNumTypes,
        EventTypeSpec[] inList, IntPtr inUserData, out IntPtr outHandlerRef);

    [DllImport(Carbon)]
    internal static extern int RemoveEventHandler(IntPtr inHandlerRef);

    [DllImport(Carbon)]
    internal static extern int RegisterEventHotKey(
        uint inHotKeyCode, uint inHotKeyModifiers, EventHotKeyId inHotKeyId,
        IntPtr inTarget, uint inOptions, out IntPtr outRef);

    [DllImport(Carbon)]
    internal static extern int UnregisterEventHotKey(IntPtr inHotKey);

    [DllImport(Carbon)]
    internal static extern int GetEventParameter(
        IntPtr inEvent, uint inName, uint inDesiredType, IntPtr outActualType,
        uint inBufferSize, IntPtr outActualSize, out EventHotKeyId outData);
}
