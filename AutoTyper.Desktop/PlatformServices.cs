using System.Runtime.InteropServices;
using AutoTyper.Core;
using AutoTyper.Core.Input;
using AutoTyper.Desktop.Mac;

namespace AutoTyper.Desktop;

/// <summary>
/// The composition root for everything OS-specific. Every platform decision in
/// the app is made here and nowhere else, so the rest of the code sees only
/// <see cref="IKeySender"/> and <see cref="IHotkeyProvider"/>.
/// </summary>
public static class PlatformServices
{
    /// <summary>True when this OS has a full set of adapters implemented.</summary>
    public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    /// <summary>
    /// A human-readable reason the current OS is unsupported, or
    /// <see langword="null"/> when it is supported.
    /// </summary>
    public static string? UnsupportedReason => IsSupported
        ? null
        : $"AutoTyper does not yet support {RuntimeInformation.OSDescription}. "
          + "Synthetic keystrokes and global hotkeys need a platform-specific implementation.";

    /// <summary>
    /// True when the current platform's adapters can actually deliver
    /// keystrokes right now. Distinct from <see cref="IsSupported"/>, which
    /// only says an adapter exists for this OS: on Windows there is no OS
    /// permission gate, so this always matches <see cref="IsSupported"/>; on
    /// macOS the adapter exists but <c>CGEventPost</c> silently does nothing
    /// until the user grants Accessibility access, so this is false until
    /// <see cref="MacAccessibility.IsTrusted"/> says otherwise.
    /// </summary>
    public static bool CanTypeNow => !OperatingSystem.IsMacOS() || MacAccessibility.IsTrusted();

    /// <summary>
    /// A human-readable reason typing won't work despite the platform being
    /// supported, or <see langword="null"/> when it will. Distinct from
    /// <see cref="UnsupportedReason"/>: that means "no code exists for this
    /// OS"; this means "code exists and the OS refused it."
    /// </summary>
    public static string? PermissionDeniedReason => CanTypeNow
        ? null
        : "AutoTyper needs Accessibility access to send keystrokes. Open System Settings → "
          + "Privacy & Security → Accessibility, and enable AutoTyper, then try Activate again.";

    /// <param name="getOwnWindowHandle">
    /// Supplies AutoTyper's own native window handle so the sender never
    /// captures it as the typing target. Unused on macOS, where
    /// <see cref="MacKeySender"/> captures an app by pid and excludes its own
    /// process instead.
    /// </param>
    public static IKeySender CreateKeySender(Func<IntPtr> getOwnWindowHandle)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WinInputKeySender(getOwnWindowHandle);
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacKeySender();
        }

        throw new PlatformNotSupportedException(UnsupportedReason);
    }

    /// <remarks>
    /// Must be called on the UI thread: the Windows provider owns a
    /// message-only window and needs that thread's message pump to receive
    /// hotkey notifications, and the macOS provider installs a Carbon event
    /// handler against the application's own event target, which likewise
    /// needs the main run loop to actually pump it.
    /// </remarks>
    public static IHotkeyProvider CreateHotkeyProvider()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WinHotkeyProvider();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacHotkeyProvider();
        }

        throw new PlatformNotSupportedException(UnsupportedReason);
    }
}
