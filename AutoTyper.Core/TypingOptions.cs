using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Everything <see cref="TypingEngine"/> needs for a run besides the passage
/// itself (passed to <see cref="TypingEngine.RunAsync"/> directly): the
/// settings groups that control speed, bursts, typos, pacing, step-away
/// breaks, whitespace handling, and run behavior. The trigger hotkey is an app
/// concern (see <see cref="HotkeySettings"/>) and isn't part of what the engine
/// consumes.
/// </summary>
public class TypingOptions
{
    public SpeedSettings Speed { get; init; } = new();

    public BurstSettings Bursts { get; init; } = new();

    public TypoSettings Typos { get; init; } = new();

    public PauseSettings Pauses { get; init; } = new();

    public StepAwaySettings StepAway { get; init; } = new();

    public FormattingSettings Formatting { get; init; } = new();

    public RunSettings Run { get; init; } = new();
}
