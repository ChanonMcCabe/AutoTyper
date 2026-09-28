namespace AutoTyper.Core;

/// <summary>
/// Summary of a completed <see cref="TypingEngine.RunAsync"/> call, for an
/// end-of-run report. Not produced when the run is cancelled or throws.
/// </summary>
/// <param name="Elapsed">Total real-clock time from start to finish.</param>
/// <param name="PausedTime">
/// Portion of <see cref="Elapsed"/> spent not actively typing — paused,
/// waiting for target focus, or stepped away.
/// </param>
/// <param name="CharsTyped">Total characters sent, including whitespace.</param>
/// <param name="WordsTyped">Total non-whitespace words typed.</param>
/// <param name="TyposMade">Total typos injected (and corrected) during the run.</param>
/// <param name="StepAways">Total step-away breaks taken.</param>
/// <param name="EffectiveWpm">
/// Words per minute over active time only (<see cref="Elapsed"/> minus
/// <see cref="PausedTime"/>), so a paused or waiting run isn't counted against
/// its own pace. Zero when there was no active time to measure.
/// </param>
public record TypingRunResult(
    TimeSpan Elapsed,
    TimeSpan PausedTime,
    int CharsTyped,
    int WordsTyped,
    int TyposMade,
    int StepAways,
    double EffectiveWpm);
