using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AutoTyper.Core.Input;

namespace AutoTyper.Desktop;

/// <summary>
/// Win32 <see cref="IHotkeyProvider"/>: registers system-wide hotkeys with
/// <c>RegisterHotKey</c> and receives <c>WM_HOTKEY</c> on its own hidden
/// message-only window.
/// </summary>
/// <remarks>
/// Owning the window is what makes this class independent of the UI framework.
/// <c>RegisterHotKey</c> needs an HWND to deliver to, and the obvious source is
/// the app's main window — but reaching it means WPF's <c>HwndSource.AddHook</c>
/// or Avalonia's <c>Win32Properties.AddWndProcHookCallback</c>, which are
/// different APIs with different lifetimes. A message-only window (one parented
/// to <c>HWND_MESSAGE</c>, never rendered) sidesteps both: this type works the
/// same under either framework, and needs nothing from the host but a thread
/// with a running message pump.
/// <para>
/// Must be constructed on the UI thread. Message-only windows receive their
/// messages through the owning thread's pump, so a background thread would
/// register hotkeys successfully and then never see them fire.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WinHotkeyProvider : IHotkeyProvider
{
    private const uint WsExNoActivate = 0x08000000;

    private readonly Dictionary<int, HotkeyCombo> _registrations = [];
    private readonly string _className = $"AutoTyperHotkeyWindow_{Guid.NewGuid():N}";

    // Rooted for the window's whole lifetime on purpose: the native side keeps
    // a raw function pointer to this delegate, so letting it be collected would
    // crash the process on the next message rather than fail cleanly.
    private readonly NativeMethods.WndProc _wndProc;

    private IntPtr _hwnd;
    private IntPtr _moduleHandle;
    private int _nextId = 1;
    private bool _disposed;

    public WinHotkeyProvider()
    {
        _wndProc = WndProc;
        _moduleHandle = NativeMethods.GetModuleHandle(null);

        var wndClass = new NativeMethods.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            lpfnWndProc = _wndProc,
            hInstance = _moduleHandle,
            lpszClassName = _className,
        };

        if (NativeMethods.RegisterClassEx(ref wndClass) == 0)
        {
            throw new HotkeyRegistrationException(
                $"Could not register the hotkey window class. Win32 error: {Marshal.GetLastWin32Error()}");
        }

        _hwnd = NativeMethods.CreateWindowEx(
            WsExNoActivate,
            _className,
            null,
            0,
            0,
            0,
            0,
            0,
            new IntPtr(NativeMethods.HwndMessage),
            IntPtr.Zero,
            _moduleHandle,
            IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            NativeMethods.UnregisterClass(_className, _moduleHandle);
            throw new HotkeyRegistrationException($"Could not create the hotkey message window. Win32 error: {error}");
        }
    }

    public event EventHandler<HotkeyCombo>? HotkeyPressed;

    public int Register(HotkeyCombo combo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!WindowsVirtualKeyMap.TryGetVirtualKey(combo.Key, out ushort virtualKey))
        {
            throw new HotkeyRegistrationException($"{combo.Key} has no Windows virtual-key equivalent.");
        }

        int id = _nextId++;
        uint modifiers = ToWin32Modifiers(combo.Modifiers) | NativeMethods.ModNoRepeat;

        if (!NativeMethods.RegisterHotKey(_hwnd, id, modifiers, virtualKey))
        {
            throw new HotkeyRegistrationException(combo);
        }

        _registrations[id] = combo;
        return id;
    }

    public void Unregister(int id)
    {
        if (!_disposed && _registrations.Remove(id))
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
        }
    }

    public void UnregisterAll()
    {
        foreach (int id in _registrations.Keys.ToList())
        {
            Unregister(id);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterAll();
        _disposed = true;

        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }

        if (_moduleHandle != IntPtr.Zero)
        {
            NativeMethods.UnregisterClass(_className, _moduleHandle);
            _moduleHandle = IntPtr.Zero;
        }
    }

    private static uint ToWin32Modifiers(HotkeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            result |= NativeMethods.ModAlt;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            result |= NativeMethods.ModControl;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            result |= NativeMethods.ModShift;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Meta))
        {
            result |= NativeMethods.ModWin;
        }

        return result;
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == NativeMethods.WmHotkey && _registrations.TryGetValue(wParam.ToInt32(), out HotkeyCombo combo))
        {
            try
            {
                HotkeyPressed?.Invoke(this, combo);
            }
            catch
            {
                // Suppress exceptions from event subscribers to prevent them from
                // propagating across the native boundary, which is undefined behavior
                // and will crash the process.
            }
            return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }
}
