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

    /// <summary>Snapshots <paramref name="settings"/>' typing groups as deep copies.</summary>
    public static TypingPreset FromSettings(string name, AppSettings settings) => new()
    {
        Name = name,
        Speed = SettingsService.DeepClone(settings.Speed),
        Bursts = SettingsService.DeepClone(settings.Bursts),
        Typos = SettingsService.DeepClone(settings.Typos),
        Pauses = SettingsService.DeepClone(settings.Pauses),
        StepAway = SettingsService.DeepClone(settings.StepAway),
        Formatting = SettingsService.DeepClone(settings.Formatting),
        Run = SettingsService.DeepClone(settings.Run),
    };

    /// <summary>
    /// Replaces <paramref name="settings"/>' typing groups with deep copies of
    /// this preset's, so later edits to either side don't leak into the other.
    /// </summary>
    public void ApplyTo(AppSettings settings)
    {
        settings.Speed = SettingsService.DeepClone(Speed);
        settings.Bursts = SettingsService.DeepClone(Bursts);
        settings.Typos = SettingsService.DeepClone(Typos);
        settings.Pauses = SettingsService.DeepClone(Pauses);
        settings.StepAway = SettingsService.DeepClone(StepAway);
        settings.Formatting = SettingsService.DeepClone(Formatting);
        settings.Run = SettingsService.DeepClone(Run);
    }
}
