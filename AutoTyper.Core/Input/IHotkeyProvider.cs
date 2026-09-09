namespace AutoTyper.Core.Input;

/// <summary>
/// Abstraction over registering system-wide hotkeys, so the app can listen for
/// a trigger combo while another application has focus. The counterpart to
/// <see cref="IKeySender"/>: that one sends keystrokes out, this one receives
/// them.
/// </summary>
/// <remarks>
/// Deliberately synchronous. Both Win32 <c>RegisterHotKey</c> and macOS
/// <c>RegisterEventHotKey</c> are non-blocking calls that either succeed or
/// fail immediately, so an async surface would add ceremony — and force
/// <c>await</c> into event handlers — for no benefit.
/// <para>
/// <see cref="HotkeyPressed"/> is raised on whatever thread the platform
/// delivers the notification on. On Windows that is the UI thread (the
/// registration rides the window message pump), but callers must not rely on
/// it: marshal to the UI thread before touching view-model state.
/// </para>
/// </remarks>
public interface IHotkeyProvider : IDisposable
{
    /// <summary>
    /// Raised when a registered combo is pressed. The combo itself is the
    /// payload so subscribers can tell which one fired by value rather than by
    /// tracking registration ids.
    /// </summary>
    event EventHandler<HotkeyCombo>? HotkeyPressed;

    /// <summary>
    /// Registers <paramref name="combo"/> system-wide and returns an id for
    /// <see cref="Unregister"/>.
    /// </summary>
    /// <exception cref="HotkeyRegistrationException">
    /// The combo could not be registered — most often because another
    /// application already owns it.
    /// </exception>
    int Register(HotkeyCombo combo);

    /// <summary>Releases one registration. Unknown ids are ignored.</summary>
    void Unregister(int id);

    /// <summary>Releases every registration this provider still holds.</summary>
    void UnregisterAll();
}
