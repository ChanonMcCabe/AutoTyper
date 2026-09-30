namespace AutoTyper.Core.Settings;

/// <summary>
/// Phrase bursts: each eligible word rolls <see cref="PhraseBurstChancePercent"/>
/// (gated by a cooldown so bursts don't chain back-to-back) to start typing a
/// run of <see cref="BurstWordCount"/> words at a boosted pace, bracketed by
/// a pre-burst and post-burst pause. Cut from the original AHK MVP, restored
/// here as an opt-in setting.
/// </summary>
public class BurstSettings : SettingsGroupBase, IValidatableSetting
{
    private bool _enabled;
    private double _phraseBurstChancePercent = 8.0;
    private int _burstWordCount = 5;
    private double _burstSpeedMultiplier = 1.4;
    private int _preBurstPauseMinMs = 200;
    private int _preBurstPauseMaxMs = 500;
    private int _postBurstPauseMinMs = 400;
    private int _postBurstPauseMaxMs = 1200;
    private int _cooldownMinMs = 2000;
    private int _cooldownMaxMs = 6000;

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Enable Phrase Bursts", Advanced = true)]
    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Phrase Burst Chance (%)", Min = 0, Max = 100, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public double PhraseBurstChancePercent
    {
        get => _phraseBurstChancePercent;
        set => SetField(ref _phraseBurstChancePercent, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Words Per Burst", Min = 1, Max = 30, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int BurstWordCount
    {
        get => _burstWordCount;
        set => SetField(ref _burstWordCount, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Burst Speed Multiplier", Min = 1, Max = 5, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public double BurstSpeedMultiplier
    {
        get => _burstSpeedMultiplier;
        set => SetField(ref _burstSpeedMultiplier, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Pre-Burst Pause Min (ms)", Min = 0, Max = 3000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int PreBurstPauseMinMs
    {
        get => _preBurstPauseMinMs;
        set => SetField(ref _preBurstPauseMinMs, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Pre-Burst Pause Max (ms)", Min = 0, Max = 4000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int PreBurstPauseMaxMs
    {
        get => _preBurstPauseMaxMs;
        set => SetField(ref _preBurstPauseMaxMs, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Post-Burst Pause Min (ms)", Min = 0, Max = 4000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int PostBurstPauseMinMs
    {
        get => _postBurstPauseMinMs;
        set => SetField(ref _postBurstPauseMinMs, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Post-Burst Pause Max (ms)", Min = 0, Max = 6000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int PostBurstPauseMaxMs
    {
        get => _postBurstPauseMaxMs;
        set => SetField(ref _postBurstPauseMaxMs, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Cooldown Between Bursts Min (ms)", Min = 0, Max = 15000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int CooldownMinMs
    {
        get => _cooldownMinMs;
        set => SetField(ref _cooldownMinMs, value);
    }

    [Setting(Category = "Advanced", Group = "Phrase Bursts", DisplayName = "Cooldown Between Bursts Max (ms)", Min = 0, Max = 20000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int CooldownMaxMs
    {
        get => _cooldownMaxMs;
        set => SetField(ref _cooldownMaxMs, value);
    }

    public IEnumerable<string> Validate()
    {
        if (!Enabled)
        {
            yield break;
        }

        if (PreBurstPauseMinMs > PreBurstPauseMaxMs)
        {
            yield return "Pre-Burst Pause Min must not exceed its Max.";
        }

        if (PostBurstPauseMinMs > PostBurstPauseMaxMs)
        {
            yield return "Post-Burst Pause Min must not exceed its Max.";
        }

        if (CooldownMinMs > CooldownMaxMs)
        {
            yield return "Cooldown Between Bursts Min must not exceed its Max.";
        }
    }
}
