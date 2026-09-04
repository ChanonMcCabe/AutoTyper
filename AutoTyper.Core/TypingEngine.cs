using System.Text;
using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Reassembles the original AHK MVP's HumanType loop: resolve a base WPM
/// once (<see cref="SpeedResolver"/>), then walk the passage word by word,
/// consulting a <see cref="WpmDriftTracker"/> for the current pace and a
/// <see cref="BurstController"/> for whether to run a phrase burst, typing
/// each word through <see cref="TypoTyper"/>, with a long pause after
/// sentence-ending punctuation. <see cref="IKeySender"/> keeps this free of
/// any Win32 dependency so it runs under a fake sender in tests, and
/// <see cref="CancellationToken"/> checks before every await replace the
/// original's GetKeyState polling for a cancel signal.
/// </summary>
public class TypingEngine
{
    private readonly Random _random;

    public TypingEngine(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public async Task RunAsync(string passage, TypingOptions options, IKeySender keySender, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passage);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySender);

        // Normalize line endings so CRLF doesn't produce a double line break.
        passage = passage.Replace("\r\n", "\n").Replace("\r", "\n");
        if (!options.Formatting.PreserveSpacing)
        {
            // Collapse every run of whitespace (incl. newlines/tabs) to one space.
            passage = string.Join(" ", passage.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        double baseWpm = SpeedResolver.ResolveBaseWpm(passage, options.Speed, _random);
        double driftAmount = options.Speed.DriftEnabled ? options.Speed.DriftAmount : 0;
        var driftTracker = new WpmDriftTracker(baseWpm, driftAmount, _random);
        var burstController = new BurstController(options.Bursts, _random);
        var stepAwayController = new StepAwayController(options.StepAway);
        var typoTyper = new TypoTyper(keySender, options.Typos, options.Pauses, _random);

        List<string> tokens = SplitPreservingWhitespace(passage).ToList();
        int pos = 0;

        while (pos < tokens.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string token = tokens[pos];

            if (IsWhitespaceToken(token))
            {
                await keySender.SendCharAsync(token[0]);
                double spaceMs = await DelayAsync(TimingService.GetLetterPause(driftTracker.CurrentWpm, options.Pauses, _random), cancellationToken);
                burstController.AdvanceCooldown(spaceMs);
                pos++;
                continue;
            }

            driftTracker.AdvanceWord();
            double wpm = driftTracker.CurrentWpm;

            if (burstController.ShouldStartBurst())
            {
                await DelayAsync(TimingService.GetPreBurstPause(options.Bursts, _random), cancellationToken);

                int burstWordsRemaining = burstController.GetBurstWordCount();
                double burstWpm = burstController.ApplyBurstSpeed(wpm);

                while (burstWordsRemaining > 0 && pos < tokens.Count)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string burstToken = tokens[pos];

                    if (IsWhitespaceToken(burstToken))
                    {
                        await keySender.SendCharAsync(burstToken[0]);
                        await DelayAsync(TimingService.GetLetterPause(burstWpm, options.Pauses, _random), cancellationToken);
                        pos++;
                        continue;
                    }

                    await typoTyper.TypeWordAsync(burstToken, burstWpm, cancellationToken);
                    await MaybeLongPauseAsync(burstToken, options.Pauses, cancellationToken);
                    pos++;
                    burstWordsRemaining--;

                    if (burstWordsRemaining > 0)
                    {
                        driftTracker.AdvanceWord();
                    }
                }

                await DelayAsync(TimingService.GetPostBurstPause(options.Bursts, _random), cancellationToken);
                burstController.OnBurstFinished();
            }
            else
            {
                double wordElapsedMs = await typoTyper.TypeWordAsync(token, wpm, cancellationToken);
                burstController.AdvanceCooldown(wordElapsedMs);
                await MaybeLongPauseAsync(token, options.Pauses, cancellationToken);

                stepAwayController.AdvanceWord();
                if (stepAwayController.ShouldStepAway())
                {
                    double awayMs = 0;
                    try
                    {
                        await keySender.BlurTargetAsync();
                        awayMs = await DelayAsync(TimingService.GetStepAwayDurationMs(options.StepAway, _random), cancellationToken);
                    }
                    finally
                    {
                        // Always hand focus back — even if the delay was cancelled,
                        // the user's window must not be left deactivated.
                        await keySender.FocusTargetAsync();
                    }

                    burstController.AdvanceCooldown(awayMs);
                }

                pos++;
            }
        }
    }

    private async Task MaybeLongPauseAsync(string word, PauseSettings pauses, CancellationToken cancellationToken)
    {
        if (word.Length > 0 && IsSentenceEnd(word[^1]))
        {
            await DelayAsync(TimingService.GetLongPause(pauses, _random), cancellationToken);
        }
    }

    private static bool IsSentenceEnd(char c) => c is '.' or '!' or '?';

    private static bool IsWhitespaceToken(string token) => token.Length == 1 && char.IsWhiteSpace(token[0]);

    private static async Task<double> DelayAsync(int ms, CancellationToken cancellationToken)
    {
        await Task.Delay(ms, cancellationToken);
        return ms;
    }

    private static IEnumerable<string> SplitPreservingWhitespace(string text)
    {
        var buffer = new StringBuilder();
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }

                yield return c.ToString();
            }
            else
            {
                buffer.Append(c);
            }
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }
}
