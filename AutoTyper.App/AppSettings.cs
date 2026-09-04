using AutoTyper.Core.Settings;

namespace AutoTyper.App;

/// <summary>
/// The full persisted state of the main window: passage text, every
/// settings group the typing engine reads, and the trigger hotkey — all as
/// one object graph that <see cref="SettingsSchemaBuilder"/> can walk to
/// drive the settings panel, and that <see cref="SettingsService"/>
/// serializes as-is. <see cref="SchemaVersion"/> is recorded on save so a
/// future format change has something to branch on; today's loader relies on
/// System.Text.Json's default behavior of deserializing onto a freshly
/// defaulted instance, so fields and whole groups missing from an older
/// file simply keep their defaults rather than requiring every field present.
/// </summary>
public class AppSettings
{
    public int SchemaVersion { get; set; } = 1;

    public string PassageText { get; set; } = string.Empty;

    public AppPreferences Preferences { get; set; } = new();

    public SpeedSettings Speed { get; set; } = new();

    public BurstSettings Bursts { get; set; } = new();

    public TypoSettings Typos { get; set; } = new();

    public PauseSettings Pauses { get; set; } = new();

    public StepAwaySettings StepAway { get; set; } = new();

    public FormattingSettings Formatting { get; set; } = new();

    public HotkeySettings Hotkey { get; set; } = new();
}
