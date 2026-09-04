namespace AutoTyper.Core;

/// <summary>
/// Abstraction over sending keystrokes to whatever window has focus. Keeps
/// <see cref="TypingEngine"/> free of any Win32/OS-specific dependency so it
/// can be unit-tested with a fake implementation.
/// </summary>
public interface IKeySender
{
    Task SendCharAsync(char c);

    Task SendBackspaceAsync();

    /// <summary>
    /// Releases keyboard focus/activation from the window currently being typed
    /// into — the "step away" — so its caret goes idle without the window
    /// moving. A no-op for senders with no real window target (e.g. tests).
    /// </summary>
    Task BlurTargetAsync();

    /// <summary>
    /// Re-activates that same window and restores keyboard focus so typing can
    /// resume in it. Paired with <see cref="BlurTargetAsync"/>; a no-op when
    /// there is no real window target.
    /// </summary>
    Task FocusTargetAsync();
}
