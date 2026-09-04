namespace AutoTyper.Core.Settings;

/// <summary>
/// Typing speed: a fixed WPM (owned directly by the Main window, not the
/// Config tabs), an optional random min/max range picked once per run, an
/// optional "type within N minutes" timeframe mode that derives the pace
/// from the passage length, and optional WPM drift — a wandering pace where
/// <see cref="WpmDriftTracker"/> picks a new target within ± <see cref="DriftAmount"/>
/// of the base WPM every few words and steps the current speed toward it,
/// rather than a one-directional ramp — all cut from the original AHK MVP,
/// restored here.
/// </summary>
public class SpeedSettings : SettingsGroupBase, IValidatableSetting
{
    private int _wpm = 40;
    private bool _rangeModeEnabled;
    private int _minWpm = 30;
    private int _maxWpm = 60;
    private bool _timeframeModeEnabled;
    private int _frameMinutes = 5;
    private bool _driftEnabled;
    private int _driftAmount = 10;

    [Setting(Category = "General", DisplayName = "Words Per Minute", Min = 5, Max = 300)]
    public int Wpm
    {
        get => _wpm;
        set => SetField(ref _wpm, value);
    }

    [Setting(Category = "General", DisplayName = "WPM Range")]
    public bool RangeModeEnabled
    {
        get => _rangeModeEnabled;
        set => SetField(ref _rangeModeEnabled, value);
    }

    [Setting(Category = "General", DisplayName = "Minimum WPM", Min = 5, Max = 300)]
    [DependsOn(nameof(RangeModeEnabled))]
    public int MinWpm
    {
        get => _minWpm;
        set => SetField(ref _minWpm, value);
    }

    [Setting(Category = "General", DisplayName = "Maximum WPM", Min = 5, Max = 300)]
    [DependsOn(nameof(RangeModeEnabled))]
    public int MaxWpm
    {
        get => _maxWpm;
        set => SetField(ref _maxWpm, value);
    }

    [Setting(Category = "General", DisplayName = "Type Frame")]
    public bool TimeframeModeEnabled
    {
        get => _timeframeModeEnabled;
        set => SetField(ref _timeframeModeEnabled, value);
    }

    [Setting(Category = "General", DisplayName = "Frame Minutes", Min = 1, Max = 180)]
    [DependsOn(nameof(TimeframeModeEnabled))]
    public int FrameMinutes
    {
        get => _frameMinutes;
        set => SetField(ref _frameMinutes, value);
    }

    [Setting(Category = "Advanced", Group = "Speed Drift", DisplayName = "Gradual Speed Drift", Advanced = true)]
    public bool DriftEnabled
    {
        get => _driftEnabled;
        set => SetField(ref _driftEnabled, value);
    }

    [Setting(Category = "Advanced", Group = "Speed Drift", DisplayName = "Drift Amount (WPM)", Min = 0, Max = 200, Advanced = true)]
    [DependsOn(nameof(DriftEnabled))]
    public int DriftAmount
    {
        get => _driftAmount;
        set => SetField(ref _driftAmount, value);
    }

    public IEnumerable<string> Validate()
    {
        if (RangeModeEnabled && MinWpm > MaxWpm)
        {
            yield return "Minimum WPM must not exceed Maximum WPM.";
        }

        if (RangeModeEnabled && TimeframeModeEnabled)
        {
            yield return "WPM Range and Type Frame cannot both be enabled.";
        }
    }
}
