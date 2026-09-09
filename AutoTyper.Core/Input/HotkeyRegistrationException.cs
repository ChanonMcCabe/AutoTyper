namespace AutoTyper.Core.Input;

/// <summary>
/// Thrown when <see cref="IHotkeyProvider.Register"/> cannot claim a combo,
/// usually because another application already holds it system-wide.
/// </summary>
/// <remarks>
/// A dedicated type so callers can catch exactly this. The WPF build threw a
/// bare <see cref="InvalidOperationException"/>, which meant the UI's catch
/// blocks also swallowed unrelated failures.
/// </remarks>
public class HotkeyRegistrationException : Exception
{
    public HotkeyRegistrationException()
    {
    }

    public HotkeyRegistrationException(string message)
        : base(message)
    {
    }

    public HotkeyRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HotkeyRegistrationException(HotkeyCombo combo)
        : base($"Failed to register hotkey {combo}. It may already be in use by another application.") =>
        Combo = combo;

    /// <summary>The combo that could not be registered, when known.</summary>
    public HotkeyCombo? Combo { get; }
}
