namespace AutoTyper.Core;

/// <summary>
/// Point-in-time snapshot of an in-progress <see cref="TypingEngine.RunAsync"/>
/// call, delivered through the caller's <see cref="IProgress{T}"/> after every
/// token and on every <see cref="TypingRunController.StateChanged"/> transition.
/// </summary>
/// <param name="CharsTyped">Running total of characters sent so far.</param>
/// <param name="TotalChars">Length of the normalized passage being typed.</param>
/// <param name="CurrentWpm">The engine's current effective pace.</param>
/// <param name="EstimatedRemaining">
/// Approximate time left, computed from the remaining character count and the
/// current WPM — not a precise ETA, since pace, pauses, and bursts vary.
/// </param>
/// <param name="State">The run's current phase.</param>
public record TypingProgress(int CharsTyped, int TotalChars, double CurrentWpm, TimeSpan EstimatedRemaining, RunState State);
