using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Resolves the base WPM for a typing pass once at the start: a timeframe
/// calculation from passage length + FrameMinutes, a random pick from
/// Min/MaxWpm, or the flat configured WPM — whichever mode is active.
/// </summary>
public static class SpeedResolver
{
    /// <summary>
    /// Resolves the base WPM for a run once: timeframe mode divides the
    /// passage's word count by <see cref="SpeedSettings.FrameMinutes"/>,
    /// range mode picks a random value between <see cref="SpeedSettings.MinWpm"/>
    /// and <see cref="SpeedSettings.MaxWpm"/>, otherwise the flat
    /// <see cref="SpeedSettings.Wpm"/> is used.
    /// </summary>
    public static double ResolveBaseWpm(string passage, SpeedSettings speed, Random random)
    {
        ArgumentNullException.ThrowIfNull(passage);
        ArgumentNullException.ThrowIfNull(speed);
        ArgumentNullException.ThrowIfNull(random);

        if (speed.TimeframeModeEnabled && speed.FrameMinutes > 0)
        {
            int wordCount = Math.Max(1, CountWords(passage));
            return wordCount / (double)speed.FrameMinutes;
        }

        if (speed.RangeModeEnabled)
        {
            return random.Next(speed.MinWpm, speed.MaxWpm + 1);
        }

        return speed.Wpm;
    }

    /// <summary>
    /// Estimates how long a run would take without actually running it — used
    /// for a pre-run estimate in the UI. Deterministic even where the run
    /// itself is randomized: range mode uses the midpoint of
    /// <see cref="SpeedSettings.MinWpm"/>/<see cref="SpeedSettings.MaxWpm"/>
    /// rather than rolling a value, so the same passage and settings always
    /// produce the same estimate. Adds the expected step-away time on top of
    /// the typing time when <see cref="StepAwaySettings.Enabled"/> is on.
    /// </summary>
    public static TimeSpan EstimateDuration(string passage, TypingOptions options)
    {
        ArgumentNullException.ThrowIfNull(passage);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(passage))
        {
            return TimeSpan.Zero;
        }

        int wordCount = Math.Max(1, CountWords(passage));
        SpeedSettings speed = options.Speed;

        TimeSpan typingTime;
        if (speed.TimeframeModeEnabled && speed.FrameMinutes > 0)
        {
            typingTime = TimeSpan.FromMinutes(speed.FrameMinutes);
        }
        else
        {
            double wpm = speed.RangeModeEnabled ? (speed.MinWpm + speed.MaxWpm) / 2.0 : speed.Wpm;
            typingTime = wpm > 0 ? TimeSpan.FromMinutes(wordCount / wpm) : TimeSpan.Zero;
        }

        return typingTime + EstimateStepAwayTime(wordCount, options.StepAway);
    }

    private static TimeSpan EstimateStepAwayTime(int wordCount, StepAwaySettings stepAway)
    {
        if (!stepAway.Enabled || stepAway.EveryWords <= 0)
        {
            return TimeSpan.Zero;
        }

        int breaks = wordCount / stepAway.EveryWords;
        return TimeSpan.FromSeconds(breaks * stepAway.DurationSeconds);
    }

    private static int CountWords(string passage) =>
        passage.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
