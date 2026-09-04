namespace AutoTyper.Core.Settings;

/// <summary>
/// An optional "stepped away from the keyboard" break: after
/// <see cref="EveryWords"/> words the engine releases keyboard focus from the
/// window it's typing into (the caret goes idle; the window doesn't move),
/// waits roughly <see cref="DurationSeconds"/> seconds — varied by
/// <see cref="DurationVariancePercent"/> so it isn't a fixed interval — then
/// re-activates the same window and carries on. The word counter resets after
/// each break. How focus is actually released is a Win32 concern that lives in
/// the App project's <c>IKeySender</c> implementation, not the engine.
/// </summary>
public class StepAwaySettings : SettingsGroupBase
{
    private bool _enabled;
    private int _everyWords = 75;
    private int _durationSeconds = 60;
    private double _durationVariancePercent = 10;

    [Setting(Category = "Advanced", Group = "Step Away", DisplayName = "Step Away From Text Box", Advanced = true)]
    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    [Setting(Category = "Advanced", Group = "Step Away", DisplayName = "Step Away Every (words)", Min = 5, Max = 5000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int EveryWords
    {
        get => _everyWords;
        set => SetField(ref _everyWords, value);
    }

    [Setting(Category = "Advanced", Group = "Step Away", DisplayName = "Step Away Duration (seconds)", Min = 1, Max = 3600, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int DurationSeconds
    {
        get => _durationSeconds;
        set => SetField(ref _durationSeconds, value);
    }

    [Setting(Category = "Advanced", Group = "Step Away", DisplayName = "Duration Variance (%)", Min = 0, Max = 100, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public double DurationVariancePercent
    {
        get => _durationVariancePercent;
        set => SetField(ref _durationVariancePercent, value);
    }
}
