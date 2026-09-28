using AutoTyper.Core.Settings;

namespace AutoTyper.Desktop;

/// <summary>
/// A named snapshot of typing settings for quick recall. Excludes hotkeys and
/// app preferences (theme, always-on-top), which are configured separately.
/// </summary>
public class TypingPreset
{
    public string Name { get; set; } = string.Empty;

    public SpeedSettings Speed { get; set; } = new();

    public BurstSettings Bursts { get; set; } = new();

    public TypoSettings Typos { get; set; } = new();

    public PauseSettings Pauses { get; set; } = new();

    public StepAwaySettings StepAway { get; set; } = new();

    public FormattingSettings Formatting { get; set; } = new();

    public RunSettings Run { get; set; } = new();
}
