using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Everything <see cref="TypingEngine"/> needs for a run: the passage text
/// plus the settings groups that control speed, bursts, typos, pacing,
/// step-away breaks, and whitespace handling. The hotkey that triggers typing
/// is a UI/Win32 concern that lives in the App project and isn't part of what
/// the engine consumes.
/// </summary>
public class TypingOptions
{
    public string PassageText { get; set; } = string.Empty;

    public SpeedSettings Speed { get; init; } = new();

    public BurstSettings Bursts { get; init; } = new();

    public TypoSettings Typos { get; init; } = new();

    public PauseSettings Pauses { get; init; } = new();

    public StepAwaySettings StepAway { get; init; } = new();

    public FormattingSettings Formatting { get; init; } = new();

    public RunSettings Run { get; init; } = new();
}
