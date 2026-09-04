using System.Runtime.InteropServices;
using AutoTyper.Core;

namespace AutoTyper.App;

/// <summary>
/// Win32 <see cref="IKeySender"/> implementation. Sends characters as
/// synthetic Unicode input (KEYEVENTF_UNICODE) so it works regardless of
/// keyboard layout, and backspace as a real VK_BACK key press, both via
/// SendInput targeting whatever window currently has focus.
/// </summary>
public class WinInputKeySender : IKeySender
{
    private IntPtr _typingTarget;

    /// <summary>
    /// The window a run is typing into, captured by the caller when typing
    /// starts. <see cref="BlurTargetAsync"/> / <see cref="FocusTargetAsync"/> act
    /// on it; <see cref="IntPtr.Zero"/> makes both a no-op.
    /// </summary>
    public void SetTypingTarget(IntPtr hwnd) => _typingTarget = hwnd;

    public Task SendCharAsync(char c)
    {
        // Newlines and tabs have to be real key presses — most editors ignore a
        // KEYEVENTF_UNICODE '\n'. Everything else goes as synthetic Unicode.
        switch (c)
        {
            case '\n' or '\r':
                SendVirtualKey(NativeMethods.VkReturn);
                return Task.CompletedTask;
            case '\t':
                SendVirtualKey(NativeMethods.VkTab);
                return Task.CompletedTask;
        }

        var down = new NativeMethods.INPUT
        {
            type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = NativeMethods.KeyEventFUnicode,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };
        var up = down;
        up.U.ki.dwFlags |= NativeMethods.KeyEventFKeyUp;

        SendAndValidate(down, up);
        return Task.CompletedTask;
    }

    public Task SendBackspaceAsync()
    {
        SendVirtualKey(NativeMethods.VkBack);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Taps Escape. A hotkey combo that includes Alt can leave the target
    /// window's keyboard focus on its own menu/toolbar chrome after the Alt
    /// key-up (observed directly: Windows 11 Notepad's command bar gained
    /// focus and subsequent keystrokes navigated it — e.g. Tab moved focus
    /// to its Files button — instead of reaching the document). Escape is
    /// the standard way to dismiss that focus-on-chrome state and return
    /// focus to the actual content. Only call this when the trigger combo
    /// included Alt; an unconditional Escape could cancel unrelated UI
    /// (a dialog, a dropdown) in whatever app the user is typing into.
    /// </summary>
    public Task SendEscapeAsync()
    {
        SendVirtualKey(NativeMethods.VkEscape);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Step away: drop the target window's activation by making the shell (the
    /// desktop) the foreground window. The target doesn't move; its caret just
    /// goes idle. No-op if no target was set.
    /// </summary>
    public Task BlurTargetAsync()
    {
        if (_typingTarget != IntPtr.Zero)
        {
            WithForegroundChange(() => NativeMethods.SetForegroundWindow(NativeMethods.GetShellWindow()));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step back: re-activate the same target window and restore its keyboard
    /// focus, then let the activation settle before the next keystroke. No-op if
    /// no target was set.
    /// </summary>
    public async Task FocusTargetAsync()
    {
        if (_typingTarget == IntPtr.Zero)
        {
            return;
        }

        WithForegroundChange(() =>
        {
            if (NativeMethods.IsIconic(_typingTarget))
            {
                NativeMethods.ShowWindow(_typingTarget, NativeMethods.SwRestore);
            }

            NativeMethods.SetForegroundWindow(_typingTarget);
            NativeMethods.BringWindowToTop(_typingTarget);
            NativeMethods.SetFocus(_typingTarget);
        });

        // Intentionally untokened: focus must be handed back even when the run
        // was cancelled mid-step-away.
        await Task.Delay(120);
    }

    /// <summary>
    /// Runs <paramref name="change"/> with our input queue attached to the
    /// current foreground thread, which is what lets a background process call
    /// <c>SetForegroundWindow</c> without the request being demoted to a taskbar
    /// flash. Every call is best-effort — against an elevated target it fails the
    /// same way <c>SendInput</c> already does and typing just proceeds without a
    /// visible step-away.
    /// </summary>
    private static void WithForegroundChange(Action change)
    {
        uint thisThread = NativeMethods.GetCurrentThreadId();
        uint foregroundThread = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);
        bool attached = foregroundThread != 0
            && foregroundThread != thisThread
            && NativeMethods.AttachThreadInput(thisThread, foregroundThread, true);
        try
        {
            change();
        }
        finally
        {
            if (attached)
            {
                NativeMethods.AttachThreadInput(thisThread, foregroundThread, false);
            }
        }
    }

    private static void SendVirtualKey(ushort virtualKey)
    {
        var down = new NativeMethods.INPUT
        {
            type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };
        var up = down;
        up.U.ki.dwFlags = NativeMethods.KeyEventFKeyUp;

        SendAndValidate(down, up);
    }

    private static void SendAndValidate(NativeMethods.INPUT down, NativeMethods.INPUT up)
    {
        var inputs = new[] { down, up };
        int size = Marshal.SizeOf<NativeMethods.INPUT>();
        uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs, size);
        if (sent != inputs.Length)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SendInput sent {sent}/{inputs.Length} events. Win32 error: {error}");
        }
    }
}
