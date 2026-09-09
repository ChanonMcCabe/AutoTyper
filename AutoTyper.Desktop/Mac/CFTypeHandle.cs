using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// Owns exactly one reference to a "Create Rule" CoreFoundation object (one
/// where the function that created it, e.g. <c>CGEventCreateKeyboardEvent</c>,
/// hands the caller ownership of a single reference that must be released
/// exactly once) and releases it via <c>CFRelease</c> when disposed.
/// </summary>
/// <remarks>
/// A plain <c>try</c>/<c>finally</c> around <c>CFRelease</c> would work just as
/// well today, but <see cref="SafeHandle"/> gets CLR-guaranteed cleanup
/// (finalization runs even across code paths a future edit might add without
/// preserving the <c>finally</c>) for the cost of one extra type — worth it
/// here because nothing in this class can be exercised before it ships to a
/// real Mac, so minimizing the ways a future edit could silently break the
/// release discipline matters more than usual.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class CFTypeHandle : SafeHandle
{
    public CFTypeHandle(IntPtr handle)
        : base(IntPtr.Zero, ownsHandle: true) =>
        SetHandle(handle);

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            CoreFoundationNativeMethods.CFRelease(handle);
        }

        return true;
    }
}
