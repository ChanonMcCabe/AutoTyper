using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Resolves the base WPM for a typing pass once at the start: a timeframe
/// calculation from passage length + FrameMinutes, a random pick from
/// Min/MaxWpm, or the flat configured WPM — whichever mode is active.
/// </summary>
public static class SpeedResolver
{
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

    private static int CountWords(string passage) =>
        passage.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
