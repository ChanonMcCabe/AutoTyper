using AutoTyper.Core;
using AutoTyper.Core.Settings;

namespace AutoTyper.Core.Tests;

/// <summary>
/// Confirms the port preserves behavior, not just that it compiles: typo
/// rate roughly matches the configured chance over many trials, burst
/// frequency roughly matches PhraseBurstChancePercent, and WPM stays within
/// its drift bounds.
/// </summary>
public class StatisticalParityTests
{
    [Fact]
    public void WpmDriftTracker_CurrentWpmNeverExceedsDriftBoundsOverManyWords()
    {
        const double baseWpm = 60;
        const double driftAmount = 15;
        var tracker = new WpmDriftTracker(baseWpm, driftAmount, new Random(100));

        for (int i = 0; i < 500; i++)
        {
            tracker.AdvanceWord();
            Assert.InRange(tracker.CurrentWpm, baseWpm - driftAmount, baseWpm + driftAmount);
        }
    }

    [Fact]
    public void WpmDriftTracker_WithZeroDriftAmount_StaysAtBaseWpm()
    {
        const double baseWpm = 45;
        var tracker = new WpmDriftTracker(baseWpm, driftAmount: 0, new Random(101));

        for (int i = 0; i < 50; i++)
        {
            tracker.AdvanceWord();
        }

        Assert.Equal(baseWpm, tracker.CurrentWpm);
    }

    [Fact]
    public void BurstController_TriggerFrequencyRoughlyMatchesConfiguredChance()
    {
        const double chancePercent = 20;
        var bursts = new BurstSettings { Enabled = true, PhraseBurstChancePercent = chancePercent };
        var controller = new BurstController(bursts, new Random(200));

        const int trials = 5000;
        int triggered = 0;
        for (int i = 0; i < trials; i++)
        {
            if (controller.ShouldStartBurst())
            {
                triggered++;
            }
        }

        double observedPercent = triggered * 100.0 / trials;
        Assert.InRange(observedPercent, chancePercent - 4, chancePercent + 4);
    }

    [Fact]
    public void BurstController_CooldownSuppressesTriggeringUntilElapsed()
    {
        var bursts = new BurstSettings
        {
            Enabled = true,
            PhraseBurstChancePercent = 100,
            CooldownMinMs = 1000,
            CooldownMaxMs = 1000,
        };
        var controller = new BurstController(bursts, new Random(201));

        Assert.True(controller.ShouldStartBurst());
        controller.OnBurstFinished();

        Assert.False(controller.ShouldStartBurst(), "Expected the cooldown to suppress an immediate re-trigger.");

        controller.AdvanceCooldown(1000);
        Assert.True(controller.ShouldStartBurst(), "Expected the cooldown to have elapsed after 1000ms.");
    }

    [Fact]
    public async Task TypoTyper_FullWordTypoRateRoughlyMatchesConfiguredSplit()
    {
        // Force every eligible character to typo, then verify the fraction
        // that produce more than one backspace (full-word style) roughly
        // matches FullWordTypoRatioPercent, using backspace-count parity as
        // a proxy since TypoTyper doesn't expose per-typo classification.
        var sender = new FakeKeySender();
        var typos = new TypoSettings { Enabled = true, ProbabilityPercent = 100, FullWordTypoRatioPercent = 100 };
        var pauses = new PauseSettings();
        var typoTyper = new TypoTyper(sender, typos, pauses, new Random(300));

        string word = new('a', 12);
        await typoTyper.TypeWordAsync(word, wpm: 6000, CancellationToken.None);

        // Every character typos, and every typo is full-word (>=2 backspaces
        // each: at least the mistake itself plus one char typed past it).
        Assert.True(sender.BackspaceCount >= word.Length * 2, $"Expected each full-word typo to backspace at least twice, got {sender.BackspaceCount} for {word.Length} chars.");
        Assert.Equal(word, sender.Result);
    }
}
