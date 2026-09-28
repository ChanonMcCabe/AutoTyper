using System.Diagnostics;
using AutoTyper.Core;

namespace AutoTyper.Core.Tests;

/// <summary>Simple synchronous <see cref="IProgress{T}"/> for tests: invokes the callback on the reporting thread, with no SynchronizationContext marshaling to reason about.</summary>
internal class RecordingProgress : IProgress<TypingProgress>
{
    private readonly Action<TypingProgress> _onReport;

    public RecordingProgress(Action<TypingProgress> onReport) => _onReport = onReport;

    public void Report(TypingProgress value) => _onReport(value);
}

public class TypingEngineTests
{
    private static TypingOptions CreateOptions(int wpm, bool typosEnabled = false)
    {
        var options = new TypingOptions();
        options.Speed.Wpm = wpm;
        options.Typos.Enabled = typosEnabled;
        return options;
    }

    [Fact]
    public async Task RunAsync_WithTyposDisabled_ProducesExactPassage()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(1));
        var options = CreateOptions(wpm: 4000);

        await engine.RunAsync("hello world", options, sender, CancellationToken.None);

        Assert.Equal("hello world", sender.Result);
        Assert.Equal(0, sender.BackspaceCount);
    }

    [Fact]
    public async Task RunAsync_WithTyposEnabled_InjectsTyposAtRoughlyTheTargetRate()
    {
        // TypoSettings.ProbabilityPercent defaults to ~4% per eligible character.
        // A fixed seed keeps this deterministic; the tolerance band is wide
        // enough to absorb minor future tuning of the rate without being
        // meaningless (it would still fail if typo injection were disabled
        // or firing on most characters).
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(42));
        string word = new('a', 150);
        var options = CreateOptions(wpm: 6000, typosEnabled: true);

        await engine.RunAsync(word, options, sender, CancellationToken.None);

        Assert.Equal(word, sender.Result);
        Assert.InRange(sender.BackspaceCount, 1, 18);
    }

    [Fact]
    public async Task RunAsync_WithTyposDisabled_NeverBackspaces()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(7));
        string word = new('a', 150);
        var options = CreateOptions(wpm: 6000);

        await engine.RunAsync(word, options, sender, CancellationToken.None);

        Assert.Equal(0, sender.BackspaceCount);
    }

    [Fact]
    public async Task RunAsync_WithOnlyPartialTypos_EachTypoProducesExactlyOneBackspace()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(42));
        string word = new('a', 150);
        var options = CreateOptions(wpm: 6000, typosEnabled: true);
        options.Typos.FullWordTypoRatioPercent = 0; // force every typo down the "partial" path

        await engine.RunAsync(word, options, sender, CancellationToken.None);

        Assert.Equal(word, sender.Result);
        // ~4% of 150 chars => ~6 typos, each partial typo = exactly 1 backspace.
        Assert.InRange(sender.BackspaceCount, 1, 18);
    }

    [Fact]
    public async Task RunAsync_WithOnlyFullWordTypos_BackspacesPastTheMistake()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(42));
        string word = new('a', 150);
        var options = CreateOptions(wpm: 6000, typosEnabled: true);
        options.Typos.FullWordTypoRatioPercent = 100; // force every typo down the "full-word" path

        await engine.RunAsync(word, options, sender, CancellationToken.None);

        Assert.Equal(word, sender.Result);
        // Each full-word typo backspaces the mistake plus at least one extra
        // character typed past it, so backspaces should exceed the typo count.
        Assert.True(sender.BackspaceCount > 6, $"Expected full-word typos to produce more than one backspace each, got {sender.BackspaceCount}.");
    }

    [Fact]
    public async Task RunAsync_PacesCharactersWithinTheJitterBand()
    {
        // At 200 WPM, base pause is 60ms/char; the default 30% jitter setting
        // varies each pause by 0.7x-1.3x. 8 chars should land within
        // [336ms, 624ms] before accounting for scheduler slack, so assert a
        // generously widened band.
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(3));
        var options = CreateOptions(wpm: 200);

        var stopwatch = Stopwatch.StartNew();
        await engine.RunAsync("abcdefgh", options, sender, CancellationToken.None);
        stopwatch.Stop();

        Assert.InRange(stopwatch.ElapsedMilliseconds, 250, 1500);
    }

    [Fact]
    public async Task RunAsync_WithBurstsAlwaysTriggering_AppliesPreAndPostBurstPauses()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(5));
        var options = CreateOptions(wpm: 6000);
        options.Bursts.Enabled = true;
        options.Bursts.PhraseBurstChancePercent = 100; // always burst
        options.Bursts.BurstWordCount = 1;
        options.Bursts.PreBurstPauseMinMs = 150;
        options.Bursts.PreBurstPauseMaxMs = 150;
        options.Bursts.PostBurstPauseMinMs = 150;
        options.Bursts.PostBurstPauseMaxMs = 150;
        options.Bursts.CooldownMinMs = 0;
        options.Bursts.CooldownMaxMs = 0;

        var stopwatch = Stopwatch.StartNew();
        await engine.RunAsync("one two", options, sender, CancellationToken.None);
        stopwatch.Stop();

        // Two one-word bursts, each with a 150ms pre-burst + 150ms post-burst
        // pause and no cooldown gating, on top of negligible per-char pacing.
        Assert.InRange(stopwatch.ElapsedMilliseconds, 500, 2000);
    }

    [Fact]
    public async Task RunAsync_WithTimeframeMode_UsesTheResolvedSlowPaceNotTheConfiguredWpm()
    {
        // 5 words in 1 minute (FrameMinutes) means an effective pace of 5
        // WPM, genuinely ~2.4s/char — SpeedResolverTests already covers the
        // calculation itself instantly; this only needs to confirm RunAsync
        // actually uses that resolved pace instead of the fast configured
        // Wpm, so it cancels quickly rather than waiting out the full ~1
        // minute a completed run would legitimately take.
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(11));
        var options = CreateOptions(wpm: 6000); // would finish near-instantly if this were used instead
        options.Speed.TimeframeModeEnabled = true;
        options.Speed.FrameMinutes = 1;
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(300);

        Task runTask = engine.RunAsync("one two three four five", options, sender, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.True(sender.Result.Length < 3, $"Expected ~5 WPM pacing to type well under a full word in 300ms, got {sender.Result.Length} chars: '{sender.Result}'.");
    }

    [Fact]
    public async Task RunAsync_BackspaceSpeedMultiplier_ScalesTheBackspacePause()
    {
        // With typos forced on every eligible character and a very slow
        // backspace multiplier, elapsed time should exceed what the same run
        // takes at a normal (1.0x) backspace speed.
        async Task<long> RunWithMultiplier(double multiplier)
        {
            var sender = new FakeKeySender();
            var engine = new TypingEngine(new Random(21));
            var options = CreateOptions(wpm: 3000, typosEnabled: true);
            options.Typos.ProbabilityPercent = 100;
            options.Pauses.BackspaceSpeedMultiplier = multiplier;

            var stopwatch = Stopwatch.StartNew();
            await engine.RunAsync("aaaaaaaaaa", options, sender, CancellationToken.None);
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        long slowBackspace = await RunWithMultiplier(0.2);
        long fastBackspace = await RunWithMultiplier(3.0);

        Assert.True(slowBackspace > fastBackspace, $"Expected a smaller multiplier (slower backspacing) to take longer: slow={slowBackspace}ms, fast={fastBackspace}ms.");
    }

    [Fact]
    public async Task RunAsync_WithPreserveSpacingOff_CollapsesEveryWhitespaceRunToOneSpace()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(1));
        var options = CreateOptions(wpm: 6000);
        options.Formatting.PreserveSpacing = false;

        await engine.RunAsync("the  quick\n\tbrown\n\nfox", options, sender, CancellationToken.None);

        Assert.Equal("the quick brown fox", sender.Result);
    }

    [Fact]
    public async Task RunAsync_WithPreserveSpacingOn_KeepsLineBreaksAndNormalizesCrlf()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(1));
        var options = CreateOptions(wpm: 6000); // PreserveSpacing defaults to true

        await engine.RunAsync("a\r\nb\nc", options, sender, CancellationToken.None);

        Assert.Equal("a\nb\nc", sender.Result); // CRLF collapsed to LF, both breaks kept, none doubled
    }

    [Fact]
    public async Task RunAsync_WithStepAwayEnabled_BlursAndRefocusesOncePerInterval()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(13));
        var options = CreateOptions(wpm: 6000);
        options.StepAway.Enabled = true;
        options.StepAway.EveryWords = 2;
        options.StepAway.DurationSeconds = 0; // instant breaks; bypasses the UI Min

        await engine.RunAsync("a b c d e", options, sender, CancellationToken.None);

        Assert.Equal("a b c d e", sender.Result);
        // 5 words, break after words 2 and 4.
        Assert.Equal(2, sender.BlurCount);
        Assert.Equal(2, sender.FocusCount);
    }

    [Fact]
    public async Task RunAsync_WhenCancelledDuringAStepAway_StillRefocusesTheTarget()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(14));
        var options = CreateOptions(wpm: 6000);
        options.StepAway.Enabled = true;
        options.StepAway.EveryWords = 1;
        options.StepAway.DurationSeconds = 10; // long enough to be mid-break when cancelled
        using var cts = new CancellationTokenSource();

        Task runTask = engine.RunAsync("one two three", options, sender, cts.Token);
        cts.CancelAfter(200);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Equal(1, sender.BlurCount);
        Assert.Equal(sender.BlurCount, sender.FocusCount); // finally handed focus back
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_StopsMidStreamAndThrows()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(9));
        var options = CreateOptions(wpm: 100);
        using var cts = new CancellationTokenSource();

        Task runTask = engine.RunAsync(
            "this passage is long enough that cancellation should land well before it finishes",
            options,
            sender,
            cts.Token);

        cts.CancelAfter(150);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.True(sender.Result.Length < 20, $"Expected only a few characters before cancellation, got {sender.Result.Length}.");
    }

    [Fact]
    public async Task RunAsync_WithPausedController_SendsNothingUntilResumed()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(1));
        var options = CreateOptions(wpm: 6000);
        var controller = new TypingRunController();
        controller.Pause();

        Task<TypingRunResult> runTask = engine.RunAsync("hello world", options, sender, CancellationToken.None, controller);
        await Task.Delay(75);

        Assert.Equal(string.Empty, sender.Result);

        controller.Resume();
        TypingRunResult result = await runTask;

        Assert.Equal("hello world", sender.Result);
        Assert.True(result.PausedTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task RunAsync_CancelledWhilePaused_ThrowsOperationCanceled()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(1));
        var options = CreateOptions(wpm: 6000);
        var controller = new TypingRunController();
        controller.Pause();
        using var cts = new CancellationTokenSource();

        Task runTask = engine.RunAsync("hello world", options, sender, cts.Token, controller);
        cts.CancelAfter(75);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
    }

    [Fact]
    public async Task RunAsync_FocusLoss_BlocksSendingUntilTargetRegainsFocus()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(1));
        var options = CreateOptions(wpm: 6000);
        bool flipped = false;

        var progress = new RecordingProgress(p =>
        {
            if (!flipped && p.CharsTyped >= 3)
            {
                flipped = true;
                sender.TargetFocused = false;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    sender.TargetFocused = true;
                });
            }
        });

        var stopwatch = Stopwatch.StartNew();
        await engine.RunAsync("hello world", options, sender, CancellationToken.None, progress: progress);
        stopwatch.Stop();

        Assert.Equal("hello world", sender.Result);
        Assert.True(stopwatch.ElapsedMilliseconds >= 250,
            $"Expected the run to block for ~300ms while focus was lost, took {stopwatch.ElapsedMilliseconds}ms.");
    }

    [Fact]
    public async Task RunAsync_PauseOnFocusLossDisabled_IgnoresFocusLoss()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(1));
        var options = CreateOptions(wpm: 6000);
        options.Run.PauseOnFocusLoss = false;
        bool flipped = false;

        var progress = new RecordingProgress(p =>
        {
            if (!flipped && p.CharsTyped >= 3)
            {
                flipped = true;
                sender.TargetFocused = false; // never flips back — proves it's ignored, not just fast
            }
        });

        var stopwatch = Stopwatch.StartNew();
        await engine.RunAsync("hello world", options, sender, CancellationToken.None, progress: progress);
        stopwatch.Stop();

        Assert.Equal("hello world", sender.Result);
        Assert.True(stopwatch.ElapsedMilliseconds < 250,
            $"Expected focus loss to be ignored and the run to finish quickly, took {stopwatch.ElapsedMilliseconds}ms.");
    }

    [Fact]
    public async Task RunAsync_Progress_CharsTypedNeverDecreasesAndEndsAtTotal()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(1));
        var options = CreateOptions(wpm: 6000);
        var reports = new List<TypingProgress>();
        var progress = new RecordingProgress(reports.Add);

        await engine.RunAsync("hello world", options, sender, CancellationToken.None, progress: progress);

        Assert.NotEmpty(reports);
        for (int i = 1; i < reports.Count; i++)
        {
            Assert.True(reports[i].CharsTyped >= reports[i - 1].CharsTyped);
        }

        Assert.Equal("hello world".Length, reports[^1].TotalChars);
        Assert.Equal(reports[^1].TotalChars, reports[^1].CharsTyped);
    }

    [Fact]
    public async Task RunAsync_Result_TyposMadeIsPositiveWhenTyposFireOften()
    {
        // Same setup as RunAsync_WithTyposEnabled_InjectsTyposAtRoughlyTheTargetRate,
        // which already establishes this seed/word/probability combination
        // reliably produces several typos — reused here so the assertion on
        // TyposMade doesn't need a slow 100%-probability run to be sure of a
        // nonzero count.
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(42));
        string word = new('a', 150);
        var options = CreateOptions(wpm: 6000, typosEnabled: true);

        TypingRunResult result = await engine.RunAsync(word, options, sender, CancellationToken.None);

        Assert.Equal(word, sender.Result);
        Assert.True(result.TyposMade > 0);
    }

    [Fact]
    public async Task RunAsync_Result_TyposMadeIsZeroWhenTyposDisabled()
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(42));
        string word = new('a', 150);
        var options = CreateOptions(wpm: 6000);

        TypingRunResult result = await engine.RunAsync(word, options, sender, CancellationToken.None);

        Assert.Equal(word, sender.Result);
        Assert.Equal(0, result.TyposMade);
    }

    [Fact]
    public async Task RunAsync_NullArguments_ThrowArgumentNullException()
    {
        var engine = new TypingEngine();
        var sender = new FakeKeySender();
        var options = new TypingOptions();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => engine.RunAsync(null!, options, sender, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => engine.RunAsync("text", null!, sender, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => engine.RunAsync("text", options, null!, CancellationToken.None));
    }
}
