using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Pure timing math for the typing engine: no Win32 or async dependency, no
/// internal state — every method is parameterized entirely by its settings
/// object (and a <see cref="Random"/> for the randomized ones), so each is
/// trivially unit-testable in isolation.
/// </summary>
public static class TimingService
{
    /// <summary>Per-character pause derived from WPM, jittered by <see cref="PauseSettings.JitterPercent"/>.</summary>
    public static int GetLetterPause(double wpm, PauseSettings pauses, Random random)
    {
        double baseMs = 60000.0 / (Math.Max(wpm, 1) * 5);
        double jitterRange = Math.Clamp(pauses.JitterPercent, 0, 99) / 100.0;
        double jitterFactor = (1 - jitterRange) + (random.NextDouble() * jitterRange * 2);
        return (int)Math.Max(1, baseMs * jitterFactor);
    }

    /// <summary>Pause taken just before a phrase burst begins.</summary>
    public static int GetPreBurstPause(BurstSettings bursts, Random random) =>
        random.Next(bursts.PreBurstPauseMinMs, bursts.PreBurstPauseMaxMs + 1);

    /// <summary>Pause taken just after a phrase burst ends.</summary>
    public static int GetPostBurstPause(BurstSettings bursts, Random random) =>
        random.Next(bursts.PostBurstPauseMinMs, bursts.PostBurstPauseMaxMs + 1);

    /// <summary>The longer pause taken after sentence-ending punctuation.</summary>
    public static int GetLongPause(PauseSettings pauses, Random random) =>
        random.Next(pauses.LongPauseMinMs, pauses.LongPauseMaxMs + 1);

    /// <summary>
    /// How long a "step away" break lasts: <see cref="StepAwaySettings.DurationSeconds"/>
    /// widened into a band by <see cref="StepAwaySettings.DurationVariancePercent"/>
    /// (10% around 60s => 54000-66000ms), using the same jitter shape as
    /// <see cref="GetLetterPause"/>.
    /// </summary>
    public static int GetStepAwayDurationMs(StepAwaySettings stepAway, Random random)
    {
        double baseMs = Math.Max(0, stepAway.DurationSeconds) * 1000.0;
        double range = Math.Clamp(stepAway.DurationVariancePercent, 0, 100) / 100.0;
        double factor = (1 - range) + (random.NextDouble() * range * 2);
        return (int)Math.Max(0, baseMs * factor);
    }

    /// <summary>
    /// Steps <paramref name="currentWpm"/> a fraction of the way toward
    /// <paramref name="targetWpm"/> rather than jumping straight to it,
    /// producing a smooth wander instead of a discontinuous speed change.
    /// </summary>
    public static double AdjustWpm(double currentWpm, double targetWpm, double stepFraction = 0.15) =>
        currentWpm + ((targetWpm - currentWpm) * stepFraction);
}
