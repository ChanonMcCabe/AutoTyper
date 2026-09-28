namespace AutoTyper.Core.Settings;

/// <summary>
/// Controls over the run itself, as opposed to how any single word is typed:
/// an optional countdown before the first character goes out, giving the user
/// time to click into the target window, and whether the engine automatically
/// pauses when that window loses keyboard focus rather than typing into
/// whatever the user clicked into instead.
/// </summary>
public class RunSettings : SettingsGroupBase
{
    private int _startDelaySeconds;
    private bool _pauseOnFocusLoss = true;

    [Setting(Category = "General", DisplayName = "Start Delay (seconds)", Min = 0, Max = 30)]
    public int StartDelaySeconds
    {
        get => _startDelaySeconds;
        set => SetField(ref _startDelaySeconds, value);
    }

    [Setting(Category = "Typing", DisplayName = "Pause When Target Window Loses Focus")]
    public bool PauseOnFocusLoss
    {
        get => _pauseOnFocusLoss;
        set => SetField(ref _pauseOnFocusLoss, value);
    }
}
