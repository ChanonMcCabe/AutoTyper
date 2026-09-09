using System.Runtime.InteropServices;
using AutoTyper.Core;
using AutoTyper.Core.Input;

namespace AutoTyper.Desktop;

/// <summary>
/// The composition root for everything OS-specific. Every platform decision in
/// the app is made here and nowhere else, so the rest of the code sees only
/// <see cref="IKeySender"/> and <see cref="IHotkeyProvider"/>.
/// </summary>
public static class PlatformServices
{
    /// <summary>True when this OS has a full set of adapters implemented.</summary>
    public static bool IsSupported => OperatingSystem.IsWindows();

    /// <summary>
    /// A human-readable reason the current OS is unsupported, or
    /// <see langword="null"/> when it is supported.
    /// </summary>
    public static string? UnsupportedReason => IsSupported
        ? null
        : $"AutoTyper does not yet support {RuntimeInformation.OSDescription}. "
          + "Synthetic keystrokes and global hotkeys need a platform-specific implementation.";

    /// <param name="getOwnWindowHandle">
    /// Supplies AutoTyper's own native window handle so the sender never
    /// captures it as the typing target.
    /// </param>
    public static IKeySender CreateKeySender(Func<IntPtr> getOwnWindowHandle)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WinInputKeySender(getOwnWindowHandle);
        }

        throw new PlatformNotSupportedException(UnsupportedReason);
    }

    /// <remarks>
    /// Must be called on the UI thread: the Windows provider owns a
    /// message-only window and needs that thread's message pump to receive
    /// hotkey notifications.
    /// </remarks>
    public static IHotkeyProvider CreateHotkeyProvider()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WinHotkeyProvider();
        }

        throw new PlatformNotSupportedException(UnsupportedReason);
    }
}
