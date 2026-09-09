using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// CoreFoundation primitives shared by every macOS adapter that receives a
/// <c>CFTypeRef</c> from another framework (<see cref="MacKeySender"/>'s
/// <c>CGEventRef</c>s today) and needs to release it.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class CoreFoundationNativeMethods
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CoreFoundation)]
    internal static extern void CFRelease(IntPtr cf);
}
