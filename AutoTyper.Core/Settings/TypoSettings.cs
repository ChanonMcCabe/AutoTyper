namespace AutoTyper.Core.Settings;

/// <summary>
/// Typo injection: how often a wrong (QWERTY-adjacent) key gets typed, and
/// the split between the MVP's two correction styles — a "full-word" typo
/// (kept typing past the mistake before noticing, then backspaces several
/// characters back to it) versus a "partial" typo (noticed immediately, a
/// single backspace at boosted speed). <see cref="NoticeDelayMinMs"/>/
/// <see cref="NoticeDelayMaxMs"/> only apply to the full-word case, since a
/// partial typo is corrected right away.
/// </summary>
public class TypoSettings : SettingsGroupBase
{
    private bool _enabled = true;
    private double _probabilityPercent = 4.0;
    private double _fullWordTypoRatioPercent = 35.0;
    private int _noticeDelayMinMs = 150;
    private int _noticeDelayMaxMs = 400;

    [Setting(Category = "Typing", DisplayName = "Enable Typos")]
    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    [Setting(Category = "Typing", DisplayName = "Typo Chance (%)", Min = 0, Max = 50)]
    [DependsOn(nameof(Enabled))]
    public double ProbabilityPercent
    {
        get => _probabilityPercent;
        set => SetField(ref _probabilityPercent, value);
    }

    [Setting(Category = "Advanced", Group = "Full-Word Typos", DisplayName = "Full-Word Typo Ratio (%)", Min = 0, Max = 100, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public double FullWordTypoRatioPercent
    {
        get => _fullWordTypoRatioPercent;
        set => SetField(ref _fullWordTypoRatioPercent, value);
    }

    [Setting(Category = "Advanced", Group = "Full-Word Typos", DisplayName = "Full-Word Notice Delay Min (ms)", Min = 0, Max = 2000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int NoticeDelayMinMs
    {
        get => _noticeDelayMinMs;
        set => SetField(ref _noticeDelayMinMs, value);
    }

    [Setting(Category = "Advanced", Group = "Full-Word Typos", DisplayName = "Full-Word Notice Delay Max (ms)", Min = 0, Max = 3000, Advanced = true)]
    [DependsOn(nameof(Enabled))]
    public int NoticeDelayMaxMs
    {
        get => _noticeDelayMaxMs;
        set => SetField(ref _noticeDelayMaxMs, value);
    }
}
