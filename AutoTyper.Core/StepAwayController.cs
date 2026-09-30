using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Decides, once per typed word, whether the engine should take a "step away"
/// break: fires every <see cref="StepAwaySettings.EveryWords"/> words and then
/// resets its counter. The main loop calls <see cref="AdvanceWord"/> then
/// <see cref="ShouldStepAway"/> each iteration rather than the controller
/// driving the loop, mirroring <see cref="BurstController"/>. Words typed
/// inside a phrase burst count too; the engine only checks
/// <see cref="ShouldStepAway"/> once a burst has finished, so a break that
/// comes due mid-burst never splits it. Cadence is
/// deterministic (word count), so no <see cref="Random"/> is involved here —
/// the break's length is randomized separately by
/// <see cref="TimingService.GetStepAwayDurationMs"/>.
/// </summary>
public class StepAwayController
{
    private readonly StepAwaySettings _settings;
    private int _wordsSinceStepAway;

    public StepAwayController(StepAwaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    public void AdvanceWord() => _wordsSinceStepAway++;

    public bool ShouldStepAway()
    {
        if (!_settings.Enabled || _settings.EveryWords <= 0 || _wordsSinceStepAway < _settings.EveryWords)
        {
            return false;
        }

        _wordsSinceStepAway = 0;
        return true;
    }
}
