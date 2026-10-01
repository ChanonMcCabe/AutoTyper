using System.Runtime.Versioning;
using AutoTyper.Core;
using AutoTyper.Core.Input;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// macOS <see cref="IKeySender"/> implementation. Sends characters as
/// synthetic Unicode input (<c>CGEventKeyboardSetUnicodeString</c>) so it
/// works regardless of keyboard layout, and backspace/newline/tab as real
/// keycode events, both via <c>CGEventPost</c> — the same shape as
/// <see cref="WinInputKeySender"/>, different native calls.
/// </summary>
/// <remarks>
/// The typing target is an <em>app</em> (by pid), not a window: that's the
/// granularity the Accessibility API activates at. Step away
/// (<see cref="BlurTargetAsync"/>/<see cref="FocusTargetAsync"/>) and
/// pause-on-focus-loss (<see cref="IsTargetFocused"/>) go through
/// <see cref="MacFrontmostApp"/>, which is best-effort: if it fails on a real
/// Mac, the target stays unset (pid 0) and both features quietly do nothing,
/// as they did before they were implemented. Typing itself never depends on
/// them, because <c>CGEventPost</c> delivers to whatever has OS-level
/// keyboard focus, exactly like <c>SendInput</c> on Windows.
/// </remarks>
[SupportedOSPlatform("macos")]
public sealed class MacKeySender : IKeySender
{
    private readonly Func<int> _getFocusedAppPid;
    private readonly Func<int, bool> _makeFrontmost;
    private readonly Func<int> _findFinderPid;
    private readonly int _ownPid;

    private int _targetPid;

    public MacKeySender()
        : this(MacFrontmostApp.GetFocusedAppPid, MacFrontmostApp.TryMakeFrontmost,
            MacFrontmostApp.FindFinderPid, Environment.ProcessId)
    {
    }

    /// <summary>Seam for tests, so target tracking can be checked without native calls.</summary>
    internal MacKeySender(Func<int> getFocusedAppPid, Func<int, bool> makeFrontmost,
        Func<int> findFinderPid, int ownPid)
    {
        _getFocusedAppPid = getFocusedAppPid;
        _makeFrontmost = makeFrontmost;
        _findFinderPid = findFinderPid;
        _ownPid = ownPid;
    }

    public Task SendCharAsync(char c)
    {
        switch (c)
        {
            case '\n' or '\r':
                SendKeycode(RequireKeycode(HotkeyKey.Enter));
                return Task.CompletedTask;
            case '\t':
                SendKeycode(RequireKeycode(HotkeyKey.Tab));
                return Task.CompletedTask;
        }

        PostUnicodeChar(c);
        return Task.CompletedTask;
    }

    public Task SendBackspaceAsync()
    {
        SendKeycode(RequireKeycode(HotkeyKey.Back));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Records the focused app's pid, ignoring AutoTyper itself so it never
    /// steps away from and back into its own window.
    /// </remarks>
    public void CaptureTarget()
    {
        int focused = _getFocusedAppPid();
        _targetPid = focused == _ownPid ? 0 : focused;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A failed focus query (pid 0) counts as focused, so an Accessibility
    /// failure can never strand a run waiting for focus.
    /// </remarks>
    public bool IsTargetFocused()
    {
        if (_targetPid == 0)
        {
            return true;
        }

        int focused = _getFocusedAppPid();
        return focused == 0 || focused == _targetPid;
    }

    /// <summary>
    /// Step away: make Finder (which owns the desktop) the active app, the
    /// macOS counterpart of Windows activating the shell. The target app
    /// doesn't move; its caret just goes idle. No-op if no target was
    /// captured or Finder isn't running.
    /// </summary>
    public Task BlurTargetAsync()
    {
        if (_targetPid != 0 && _findFinderPid() is var finder and not 0)
        {
            _makeFrontmost(finder);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step back: re-activate the target app, then let the activation settle
    /// before the next keystroke. No-op if no target was captured.
    /// </summary>
    public async Task FocusTargetAsync()
    {
        if (_targetPid == 0)
        {
            return;
        }

        _makeFrontmost(_targetPid);

        // Intentionally untokened: focus must be handed back even when the run
        // was cancelled mid-step-away.
        await Task.Delay(120);
    }

    /// <summary>
    /// No-op: this exists on Windows purely to route around an Alt-menu-focus
    /// quirk in that platform's window chrome, with no known macOS equivalent.
    /// </summary>
    public Task PrepareForTypingAsync(HotkeyCombo trigger) => Task.CompletedTask;

    private static ushort RequireKeycode(HotkeyKey key) =>
        MacVirtualKeyMap.TryGetVirtualKey(key, out ushort vk)
            ? vk
            : throw new InvalidOperationException($"{key} has no macOS keycode — this should never happen for a fixed control key.");

    private static void PostUnicodeChar(char c)
    {
        char[] unicodeChar = [c];

        using CFTypeHandle down = CreateEvent(0, keyDown: true);
        CoreGraphicsNativeMethods.CGEventKeyboardSetUnicodeString(down.DangerousGetHandle(), 1, unicodeChar);
        CoreGraphicsNativeMethods.CGEventPost(CoreGraphicsNativeMethods.KCGHIDEventTap, down.DangerousGetHandle());

        using CFTypeHandle up = CreateEvent(0, keyDown: false);
        CoreGraphicsNativeMethods.CGEventKeyboardSetUnicodeString(up.DangerousGetHandle(), 1, unicodeChar);
        CoreGraphicsNativeMethods.CGEventPost(CoreGraphicsNativeMethods.KCGHIDEventTap, up.DangerousGetHandle());
    }

    private static void SendKeycode(ushort virtualKey)
    {
        using CFTypeHandle down = CreateEvent(virtualKey, keyDown: true);
        CoreGraphicsNativeMethods.CGEventPost(CoreGraphicsNativeMethods.KCGHIDEventTap, down.DangerousGetHandle());

        using CFTypeHandle up = CreateEvent(virtualKey, keyDown: false);
        CoreGraphicsNativeMethods.CGEventPost(CoreGraphicsNativeMethods.KCGHIDEventTap, up.DangerousGetHandle());
    }

    private static CFTypeHandle CreateEvent(ushort virtualKey, bool keyDown)
    {
        IntPtr handle = CoreGraphicsNativeMethods.CGEventCreateKeyboardEvent(IntPtr.Zero, virtualKey, keyDown);
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("CGEventCreateKeyboardEvent failed — this typically means the system is out of memory or the Accessibility permission is missing.");
        }

        return new(handle);
    }
}
