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
/// <see cref="CaptureTarget"/>, <see cref="BlurTargetAsync"/>,
/// <see cref="FocusTargetAsync"/> and <see cref="PrepareForTypingAsync"/> are
/// all no-ops — see their doc comments for why. This means the "Step Away"
/// feature (<c>StepAwaySettings.Enabled</c>) silently has no effect on
/// macOS: the passage types straight through a scheduled break. Likewise
/// <c>IsTargetFocused</c> keeps its always-true default, so
/// <c>RunSettings.PauseOnFocusLoss</c> has no effect here either. Typing
/// itself is unaffected, because <c>CGEventPost</c> delivers to whatever has
/// OS-level keyboard focus regardless of what this process last activated,
/// exactly like <c>SendInput</c> on Windows.
/// </remarks>
[SupportedOSPlatform("macos")]
public sealed class MacKeySender : IKeySender
{
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

    /// <summary>
    /// No-op. Activating another app from a background process needs either
    /// hand-rolled <c>objc_msgSend</c> interop into <c>NSRunningApplication</c>
    /// (a wrong selector/argument shape is undefined behavior — a process
    /// crash, not a catchable exception — and this can never be exercised
    /// before it ships) or shelling out to System Events via
    /// <c>osascript</c> (which needs a second, per-target-app Automation
    /// permission prompt that would interrupt mid-typing-run, on top of the
    /// Accessibility grant this app already needs). Both were rejected as
    /// disproportionate to a feature that is off by default. Revisit once a
    /// real Mac is available to validate an <c>objc_msgSend</c>-based
    /// <c>NSRunningApplication.activate</c> implementation against.
    /// </summary>
    public void CaptureTarget()
    {
    }

    /// <inheritdoc cref="CaptureTarget"/>
    public Task BlurTargetAsync() => Task.CompletedTask;

    /// <inheritdoc cref="CaptureTarget"/>
    public Task FocusTargetAsync() => Task.CompletedTask;

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
