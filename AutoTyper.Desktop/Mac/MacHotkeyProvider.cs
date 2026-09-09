using System.Runtime.Versioning;
using AutoTyper.Core.Input;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// macOS <see cref="IHotkeyProvider"/>: registers system-wide hotkeys with
/// Carbon's <c>RegisterEventHotKey</c> and receives them through an event
/// handler installed on the application's own Carbon event target.
/// </summary>
/// <remarks>
/// Must be constructed on the UI thread. Carbon's event dispatch is
/// documented to route through the process's main <c>CFRunLoop</c> — the same
/// run loop every AppKit process already pumps for its own event queue, which
/// is why pure-Cocoa apps with no other Carbon usage can still use
/// <c>RegisterEventHotKey</c> as their global-hotkey mechanism. Avalonia's
/// macOS backend hosts a genuine <c>NSApplication</c> with a standard run
/// loop, so the general mechanism should apply — but the specific interaction
/// of a Carbon handler installed from managed code, inside a run loop
/// Avalonia (not this app) owns and pumps, has zero first-hand verification.
/// A failure to <em>install</em> the handler throws from the constructor (see
/// below); a handler that installs successfully but is simply never invoked
/// by that run loop is a silent failure mode this class cannot detect, and is
/// the single highest-risk unknown in the macOS port.
/// </remarks>
[SupportedOSPlatform("macos")]
public sealed class MacHotkeyProvider : IHotkeyProvider
{
    /// <summary>'ATYP' — this app's own signature, distinguishing its hotkeys
    /// from any other process's in the shared Carbon hotkey ID namespace.</summary>
    private const uint HotKeySignature = 0x41545950;

    // Carbon has no MOD_NOREPEAT-equivalent flag. Community precedent
    // (Hammerspoon, Rectangle) suggests RegisterEventHotKey does not
    // auto-repeat a held key the way a normal keyDown does, but that is
    // secondhand, not verified against real hardware here — this debounce is
    // a safety net, not proof the assumption holds.
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(50);

    private readonly Dictionary<int, HotkeyCombo> _registrations = [];
    private readonly Dictionary<int, IntPtr> _nativeRefs = [];
    private readonly Dictionary<int, DateTime> _lastFired = [];

    // Rooted for the provider's whole lifetime — same discipline as
    // WinHotkeyProvider._wndProc: the native side holds a raw function
    // pointer to this delegate, and letting the GC collect it crashes the
    // process on the next hotkey event rather than failing cleanly.
    private readonly CarbonNativeMethods.EventHandlerProc _handlerProc;

    private IntPtr _handlerRef;
    private int _nextId = 1;
    private bool _disposed;

    public MacHotkeyProvider()
    {
        _handlerProc = HandleCarbonEvent;

        CarbonNativeMethods.EventTypeSpec[] eventTypes =
        [
            new CarbonNativeMethods.EventTypeSpec
            {
                EventClass = CarbonNativeMethods.EventClassKeyboard,
                EventKind = CarbonNativeMethods.EventHotKeyPressed,
            },
        ];

        int status = CarbonNativeMethods.InstallEventHandler(
            CarbonNativeMethods.GetApplicationEventTarget(), _handlerProc, eventTypes.Length,
            eventTypes, IntPtr.Zero, out _handlerRef);

        if (status != 0)
        {
            throw new HotkeyRegistrationException(
                $"Could not install the macOS hotkey event handler (OSStatus {status}). Global hotkeys will not work.");
        }
    }

    public event EventHandler<HotkeyCombo>? HotkeyPressed;

    public int Register(HotkeyCombo combo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!MacVirtualKeyMap.TryGetVirtualKey(combo.Key, out ushort keycode))
        {
            throw new HotkeyRegistrationException($"{combo.Key} has no macOS keycode equivalent.");
        }

        int id = _nextId++;
        var hotKeyId = new CarbonNativeMethods.EventHotKeyId { Signature = HotKeySignature, Id = (uint)id };
        uint modifiers = ToCarbonModifiers(combo.Modifiers);

        int status = CarbonNativeMethods.RegisterEventHotKey(
            keycode, modifiers, hotKeyId, CarbonNativeMethods.GetApplicationEventTarget(), 0, out IntPtr hotKeyRef);

        if (status != 0)
        {
            throw new HotkeyRegistrationException(combo);
        }

        _registrations[id] = combo;
        _nativeRefs[id] = hotKeyRef;
        return id;
    }

    public void Unregister(int id)
    {
        if (_disposed || !_registrations.Remove(id))
        {
            return;
        }

        if (_nativeRefs.Remove(id, out IntPtr hotKeyRef))
        {
            CarbonNativeMethods.UnregisterEventHotKey(hotKeyRef);
        }

        _lastFired.Remove(id);
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

        if (_handlerRef != IntPtr.Zero)
        {
            CarbonNativeMethods.RemoveEventHandler(_handlerRef);
            _handlerRef = IntPtr.Zero;
        }
    }

    private static uint ToCarbonModifiers(HotkeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            result |= CarbonNativeMethods.ControlKey;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            result |= CarbonNativeMethods.OptionKey;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            result |= CarbonNativeMethods.ShiftKey;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Meta))
        {
            result |= CarbonNativeMethods.CmdKey;
        }

        return result;
    }

    private int HandleCarbonEvent(IntPtr inHandlerCallRef, IntPtr inEvent, IntPtr inUserData)
    {
        int status = CarbonNativeMethods.GetEventParameter(
            inEvent, CarbonNativeMethods.EventParamDirectObject, CarbonNativeMethods.TypeEventHotKeyId,
            IntPtr.Zero, (uint)System.Runtime.InteropServices.Marshal.SizeOf<CarbonNativeMethods.EventHotKeyId>(),
            IntPtr.Zero, out CarbonNativeMethods.EventHotKeyId hotKeyId);

        if (status != 0 || hotKeyId.Signature != HotKeySignature)
        {
            return status; // not ours; let Carbon keep dispatching
        }

        int id = (int)hotKeyId.Id;

        DateTime now = DateTime.UtcNow;
        if (_lastFired.TryGetValue(id, out DateTime last) && now - last < DebounceWindow)
        {
            return 0;
        }

        _lastFired[id] = now;

        if (_registrations.TryGetValue(id, out HotkeyCombo combo))
        {
            try
            {
                HotkeyPressed?.Invoke(this, combo);
            }
            catch
            {
                // Suppress exceptions from event subscribers to prevent them from
                // propagating across the native boundary, which is undefined behavior
                // and will crash the process — same reasoning as WinHotkeyProvider.WndProc.
            }
        }

        return 0; // noErr
    }
}
