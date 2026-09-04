using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AutoTyper.App;

/// <summary>
/// Wraps RegisterHotKey/UnregisterHotKey via HwndSource.AddHook so global
/// hotkey combos fire <see cref="HotkeyPressed"/> even when the app window
/// isn't focused. Supports multiple simultaneous registrations (e.g. the
/// typing trigger and a global Escape-to-cancel) sharing one message hook.
/// </summary>
public class HotkeyManager : IDisposable
{
    private readonly Dictionary<int, HotkeyCombo> _registrations = new();
    private HwndSource? _hwndSource;
    private int _nextId = 1;

    public event EventHandler<HotkeyCombo>? HotkeyPressed;

    /// <summary>Registers <paramref name="combo"/> and returns an id for later <see cref="Unregister"/>.</summary>
    public int Register(Window window, HotkeyCombo combo)
    {
        ArgumentNullException.ThrowIfNull(window);

        IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
        if (_hwndSource is null)
        {
            _hwndSource = HwndSource.FromHwnd(handle)
                ?? throw new InvalidOperationException("Window does not have a native handle.");
            _hwndSource.AddHook(WndProc);
        }

        int id = _nextId++;
        uint modifiers = ToWin32Modifiers(combo.Modifiers) | NativeMethods.ModNoRepeat;
        uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey(combo.Key);

        if (!NativeMethods.RegisterHotKey(handle, id, modifiers, virtualKey))
        {
            throw new InvalidOperationException(
                $"Failed to register hotkey {combo}. It may already be in use by another application.");
        }

        _registrations[id] = combo;
        return id;
    }

    public void Unregister(int id)
    {
        if (_hwndSource is not null && _registrations.Remove(id))
        {
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, id);
        }
    }

    public void UnregisterAll()
    {
        foreach (int id in _registrations.Keys.ToList())
        {
            Unregister(id);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotkey && _registrations.TryGetValue(wParam.ToInt32(), out HotkeyCombo combo))
        {
            HotkeyPressed?.Invoke(this, combo);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static uint ToWin32Modifiers(ModifierKeys modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            result |= NativeMethods.ModAlt;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            result |= NativeMethods.ModControl;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            result |= NativeMethods.ModShift;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            result |= NativeMethods.ModWin;
        }

        return result;
    }

    public void Dispose()
    {
        UnregisterAll();
        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        GC.SuppressFinalize(this);
    }
}
