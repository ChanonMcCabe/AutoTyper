using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// Accessibility (<c>AXUIElement</c>) P/Invoke surface used to find which app
/// has keyboard focus and to bring an app to the front — the macOS half of
/// "step away" and pause-on-focus-loss. Plain C functions from HIServices
/// (re-exported by <c>ApplicationServices.framework</c>), so no Objective-C
/// runtime interop is involved, and they're covered by the Accessibility grant
/// <see cref="MacAccessibility"/> already requires for typing.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class AccessibilityNativeMethods
{
    private const string ApplicationServices =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    /// <summary><c>kAXErrorSuccess</c> from <c>&lt;AXError.h&gt;</c>.</summary>
    internal const int KAXErrorSuccess = 0;

    // Attribute names. In the SDK these are CFSTR() macros, not exported
    // symbols, so they're turned into CFStrings at runtime from these values.
    internal const string KAXFocusedApplicationAttribute = "AXFocusedApplication";

    internal const string KAXFrontmostAttribute = "AXFrontmost";

    /// <summary>Create Rule: the caller owns the returned element.</summary>
    [DllImport(ApplicationServices)]
    internal static extern IntPtr AXUIElementCreateSystemWide();

    /// <summary>Create Rule: the caller owns the returned element.</summary>
    [DllImport(ApplicationServices)]
    internal static extern IntPtr AXUIElementCreateApplication(int pid);

    /// <summary>Copy Rule: on success the caller owns <paramref name="value"/>.</summary>
    [DllImport(ApplicationServices)]
    internal static extern int AXUIElementCopyAttributeValue(IntPtr element, IntPtr attribute, out IntPtr value);

    [DllImport(ApplicationServices)]
    internal static extern int AXUIElementSetAttributeValue(IntPtr element, IntPtr attribute, IntPtr value);

    [DllImport(ApplicationServices)]
    internal static extern int AXUIElementGetPid(IntPtr element, out int pid);
}
