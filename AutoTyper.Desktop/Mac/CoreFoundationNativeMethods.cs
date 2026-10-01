using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// CoreFoundation primitives shared by every macOS adapter that receives a
/// <c>CFTypeRef</c> from another framework (<see cref="MacKeySender"/>'s
/// <c>CGEventRef</c>s, <see cref="MacFrontmostApp"/>'s <c>AXUIElementRef</c>s)
/// and needs to release it, or needs a <c>CFStringRef</c>/<c>CFBooleanRef</c>
/// to pass into one.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class CoreFoundationNativeMethods
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    /// <summary><c>kCFStringEncodingUTF8</c> from <c>&lt;CFString.h&gt;</c>.</summary>
    internal const uint KCFStringEncodingUTF8 = 0x08000100;

    [DllImport(CoreFoundation)]
    internal static extern void CFRelease(IntPtr cf);

    /// <summary>
    /// Create Rule: the caller owns the returned <c>CFStringRef</c>. Returns
    /// <see cref="IntPtr.Zero"/> on failure.
    /// </summary>
    [DllImport(CoreFoundation)]
    internal static extern IntPtr CFStringCreateWithCString(IntPtr alloc,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string cStr, uint encoding);

    /// <summary>
    /// The <c>kCFBooleanTrue</c> singleton. It's an exported global
    /// <em>variable</em> (a <c>CFBooleanRef</c>), not a function, so it can't
    /// be bound with <c>DllImport</c>: this reads the pointer stored at the
    /// symbol's address. Never released — it's immortal.
    /// </summary>
    internal static IntPtr CFBooleanTrue()
    {
        IntPtr library = NativeLibrary.Load(CoreFoundation);
        return Marshal.ReadIntPtr(NativeLibrary.GetExport(library, "kCFBooleanTrue"));
    }
}
