using AutoTyper.Core.Input;

namespace AutoTyper.Core;

/// <summary>
/// Abstraction over sending keystrokes to whatever window has focus. Keeps
/// <see cref="TypingEngine"/> free of any OS-specific dependency so it can be
/// unit-tested with a fake implementation, and so a second platform is a new
/// implementation rather than a change to the engine.
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

    /// <summary>
    /// Snapshots whatever currently has focus as the destination for the run
    /// about to start. Called once, immediately before typing begins.
    /// </summary>
    /// <remarks>
    /// What gets captured is deliberately opaque and platform-specific — a
    /// window handle on Windows, a frontmost-application reference on macOS —
    /// so no OS handle type crosses this interface. Implementations are
    /// responsible for never capturing AutoTyper's own window as the target.
    /// </remarks>
    void CaptureTarget();

    /// <summary>
    /// Gives the sender a chance to put the captured target into a state where
    /// it will actually receive keystrokes, given which combo triggered the
    /// run. Called after <see cref="CaptureTarget"/> and before the first
    /// character.
    /// </summary>
    /// <remarks>
    /// Exists for platform quirks that depend on the trigger. On Windows an
    /// Alt-containing combo can leave focus on the target's own menu/command
    /// bar, so that implementation taps Escape here; on a platform with no such
    /// quirk this is a no-op. Callers must not register their own global
    /// Escape handler until after this returns, or it will intercept the very
    /// keystroke being sent.
    /// </remarks>
    Task PrepareForTypingAsync(HotkeyCombo trigger);
}
