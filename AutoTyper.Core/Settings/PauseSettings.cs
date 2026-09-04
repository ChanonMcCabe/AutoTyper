namespace AutoTyper.Core.Settings;

/// <summary>
/// Pacing details beyond raw WPM: how much random jitter each keystroke's
/// timing gets, a randomized long pause after sentence-ending punctuation
/// (<c>GetLongPause</c> in the original), and how much faster backspacing
/// happens relative to normal typing — all previously hardcoded constants in
/// the engine, now user-configurable.
/// </summary>
public class PauseSettings : SettingsGroupBase
{
    private double _jitterPercent = 30;
    private int _longPauseMinMs = 300;
    private int _longPauseMaxMs = 900;
    private double _backspaceSpeedMultiplier = 1.3;

    [Setting(Category = "Advanced", Group = "Timing & Pauses", DisplayName = "Timing Jitter (%)", Min = 0, Max = 90, Advanced = true)]
    public double JitterPercent
    {
        get => _jitterPercent;
        set => SetField(ref _jitterPercent, value);
    }

    [Setting(Category = "Advanced", Group = "Timing & Pauses", DisplayName = "Long Pause After Sentence Min (ms)", Min = 0, Max = 3000, Advanced = true)]
    public int LongPauseMinMs
    {
        get => _longPauseMinMs;
        set => SetField(ref _longPauseMinMs, value);
    }

    [Setting(Category = "Advanced", Group = "Timing & Pauses", DisplayName = "Long Pause After Sentence Max (ms)", Min = 0, Max = 5000, Advanced = true)]
    public int LongPauseMaxMs
    {
        get => _longPauseMaxMs;
        set => SetField(ref _longPauseMaxMs, value);
    }

    [Setting(Category = "Advanced", Group = "Timing & Pauses", DisplayName = "Backspace Speed Multiplier", Min = 0.2, Max = 3, Advanced = true)]
    public double BackspaceSpeedMultiplier
    {
        get => _backspaceSpeedMultiplier;
        set => SetField(ref _backspaceSpeedMultiplier, value);
    }
}
