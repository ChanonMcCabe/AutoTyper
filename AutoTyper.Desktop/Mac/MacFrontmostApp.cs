using System.Diagnostics;
using System.Runtime.Versioning;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// Best-effort "which app is focused" and "bring this app to the front" for
/// <see cref="MacKeySender"/>, built on the Accessibility API.
/// </summary>
/// <remarks>
/// Every call here degrades to "nothing happened" (pid 0, or
/// <see langword="false"/>) rather than throwing, because none of it can be
/// exercised before it ships to a real Mac and the features that depend on it
/// (step away, pause on focus loss) are optional. A failure leaves typing
/// itself unaffected — it just goes straight through a break, or doesn't
/// pause when focus moves, exactly as before this existed.
/// <para>
/// Setting <c>AXFrontmost</c> on an application element is the activation
/// route window managers rely on, chosen over
/// <c>NSRunningApplication.activate</c> because that needs
/// <c>objc_msgSend</c> interop (a wrong signature crashes the process rather
/// than throwing) and is subject to macOS 14's cooperative activation, which
/// may ignore requests from a non-active app. Unverified on macOS 14+.
/// </para>
/// </remarks>
[SupportedOSPlatform("macos")]
internal static class MacFrontmostApp
{
    // Constant CFStrings, created once and kept for the life of the process.
    private static readonly Lazy<IntPtr> FocusedApplicationAttribute =
        new(() => CreateCFString(AccessibilityNativeMethods.KAXFocusedApplicationAttribute));

    private static readonly Lazy<IntPtr> FrontmostAttribute =
        new(() => CreateCFString(AccessibilityNativeMethods.KAXFrontmostAttribute));

    /// <summary>
    /// The pid of the app that currently has keyboard focus, or 0 if it can't
    /// be determined.
    /// </summary>
    public static int GetFocusedAppPid()
    {
        try
        {
            using var systemWide = new CFTypeHandle(AccessibilityNativeMethods.AXUIElementCreateSystemWide());
            if (systemWide.IsInvalid)
            {
                return 0;
            }

            // The out value is only defined (and only owned by us) on success.
            if (AccessibilityNativeMethods.AXUIElementCopyAttributeValue(
                    systemWide.DangerousGetHandle(), FocusedApplicationAttribute.Value, out IntPtr appRef)
                != AccessibilityNativeMethods.KAXErrorSuccess)
            {
                return 0;
            }

            using var app = new CFTypeHandle(appRef);
            if (app.IsInvalid)
            {
                return 0;
            }

            return AccessibilityNativeMethods.AXUIElementGetPid(app.DangerousGetHandle(), out int pid)
                == AccessibilityNativeMethods.KAXErrorSuccess
                ? pid
                : 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Makes the app with <paramref name="pid"/> the active app. Returns
    /// whether macOS accepted the request.
    /// </summary>
    public static bool TryMakeFrontmost(int pid)
    {
        if (pid <= 0)
        {
            return false;
        }

        try
        {
            using var app = new CFTypeHandle(AccessibilityNativeMethods.AXUIElementCreateApplication(pid));
            return !app.IsInvalid
                && AccessibilityNativeMethods.AXUIElementSetAttributeValue(
                    app.DangerousGetHandle(), FrontmostAttribute.Value, CoreFoundationNativeMethods.CFBooleanTrue())
                == AccessibilityNativeMethods.KAXErrorSuccess;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Finder's pid, or 0 if it isn't running. Finder owns the desktop, so it's
    /// the macOS counterpart of the shell window Windows' step-away activates.
    /// </summary>
    public static int FindFinderPid()
    {
        Process[] finders = Process.GetProcessesByName("Finder");
        try
        {
            return finders.Length > 0 ? finders[0].Id : 0;
        }
        finally
        {
            foreach (Process process in finders)
            {
                process.Dispose();
            }
        }
    }

    private static IntPtr CreateCFString(string value)
    {
        IntPtr cfString = CoreFoundationNativeMethods.CFStringCreateWithCString(
            IntPtr.Zero, value, CoreFoundationNativeMethods.KCFStringEncodingUTF8);
        return cfString != IntPtr.Zero
            ? cfString
            : throw new InvalidOperationException($"CFStringCreateWithCString failed for \"{value}\".");
    }
}
