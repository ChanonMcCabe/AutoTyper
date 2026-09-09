using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// Quartz Event Services P/Invoke surface used to synthesize keystrokes.
/// Bound against <c>ApplicationServices.framework</c>, the umbrella framework
/// that re-exports CoreGraphics's event APIs and has been at this fixed path
/// since Mac OS X 10.0.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class CoreGraphicsNativeMethods
{
    private const string ApplicationServices =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    /// <summary>
    /// Posts as if from the hardware HID layer. Chosen over
    /// <c>kCGSessionEventTap</c> because it's the tap location used by the
    /// majority of prior-art "synthesize keyboard input" tools and only
    /// requires Accessibility trust, matching this app's existing permission
    /// model — a real, documented alternative to try first if a real-Mac
    /// smoke test shows characters not arriving at a focused field.
    /// </summary>
    internal const int KCGHIDEventTap = 0;

    internal const int KCGSessionEventTap = 1;

    /// <param name="source">
    /// Always pass <see cref="IntPtr.Zero"/>. Apple's API explicitly allows
    /// <c>NULL</c> here, substituting a default event source — which sidesteps
    /// needing a disposal hook for a cached <c>CGEventSourceRef</c> that
    /// <see cref="AutoTyper.Core.IKeySender"/> (not <c>IDisposable</c>) has no
    /// way to provide.
    /// </param>
    [DllImport(ApplicationServices)]
    internal static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey,
        [MarshalAs(UnmanagedType.I1)] bool keyDown);

    [DllImport(ApplicationServices)]
    internal static extern void CGEventKeyboardSetUnicodeString(IntPtr theEvent, nuint stringLength,
        char[] unicodeString);

    [DllImport(ApplicationServices)]
    internal static extern void CGEventPost(int tap, IntPtr theEvent);
}
