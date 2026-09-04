using AutoTyper.Core;
using AutoTyper.Core.Settings;

namespace AutoTyper.Core.Tests;

public class TimingServiceTests
{
    [Fact]
    public void GetLetterPause_StaysWithinTheJitterBand()
    {
        var pauses = new PauseSettings { JitterPercent = 30 };
        var random = new Random(1);
        double baseMs = 60000.0 / (200 * 5); // 60ms at 200 WPM

        for (int i = 0; i < 500; i++)
        {
            int pause = TimingService.GetLetterPause(200, pauses, random);
            Assert.InRange(pause, (int)(baseMs * 0.7) - 1, (int)(baseMs * 1.3) + 1);
        }
    }

    [Fact]
    public void GetLetterPause_NeverReturnsLessThanOne()
    {
        var pauses = new PauseSettings { JitterPercent = 90 };
        var random = new Random(2);

        for (int i = 0; i < 200; i++)
        {
            Assert.True(TimingService.GetLetterPause(1000, pauses, random) >= 1);
        }
    }

    [Fact]
    public void GetPreBurstPause_StaysWithinConfiguredRange()
    {
        var bursts = new BurstSettings { PreBurstPauseMinMs = 100, PreBurstPauseMaxMs = 300 };
        var random = new Random(3);

        for (int i = 0; i < 200; i++)
        {
            Assert.InRange(TimingService.GetPreBurstPause(bursts, random), 100, 300);
        }
    }

    [Fact]
    public void GetPostBurstPause_StaysWithinConfiguredRange()
    {
        var bursts = new BurstSettings { PostBurstPauseMinMs = 400, PostBurstPauseMaxMs = 800 };
        var random = new Random(4);

        for (int i = 0; i < 200; i++)
        {
            Assert.InRange(TimingService.GetPostBurstPause(bursts, random), 400, 800);
        }
    }

    [Fact]
    public void GetLongPause_StaysWithinConfiguredRange()
    {
        var pauses = new PauseSettings { LongPauseMinMs = 300, LongPauseMaxMs = 900 };
        var random = new Random(5);

        for (int i = 0; i < 200; i++)
        {
            Assert.InRange(TimingService.GetLongPause(pauses, random), 300, 900);
        }
    }

    [Fact]
    public void GetStepAwayDurationMs_WithZeroVariance_ReturnsExactlyTheBaseDuration()
    {
        var stepAway = new StepAwaySettings { DurationSeconds = 60, DurationVariancePercent = 0 };
        var random = new Random(6);

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(60000, TimingService.GetStepAwayDurationMs(stepAway, random));
        }
    }

    [Fact]
    public void GetStepAwayDurationMs_StaysWithinTheVarianceBand()
    {
        var stepAway = new StepAwaySettings { DurationSeconds = 60, DurationVariancePercent = 10 };
        var random = new Random(7);

        for (int i = 0; i < 500; i++)
        {
            Assert.InRange(TimingService.GetStepAwayDurationMs(stepAway, random), 54000, 66000);
        }
    }

    [Fact]
    public void GetStepAwayDurationMs_ClampsVarianceAboveOneHundredPercent()
    {
        var stepAway = new StepAwaySettings { DurationSeconds = 60, DurationVariancePercent = 500 };
        var random = new Random(8);

        for (int i = 0; i < 500; i++)
        {
            // Clamped to 100% => band is [0, 120000], never beyond.
            Assert.InRange(TimingService.GetStepAwayDurationMs(stepAway, random), 0, 120000);
        }
    }

    [Theory]
    [InlineData(40.0, 60.0, 0.15)]
    [InlineData(60.0, 40.0, 0.15)]
    [InlineData(50.0, 50.0, 0.15)]
    public void AdjustWpm_MovesTowardTargetWithoutOvershooting(double current, double target, double step)
    {
        double result = TimingService.AdjustWpm(current, target, step);

        double lower = Math.Min(current, target);
        double upper = Math.Max(current, target);
        Assert.InRange(result, lower, upper);
    }

    [Fact]
    public void AdjustWpm_RepeatedlyAppliedConvergesOnTarget()
    {
        double current = 30;
        const double target = 70;

        for (int i = 0; i < 100; i++)
        {
            current = TimingService.AdjustWpm(current, target, 0.15);
        }

        Assert.InRange(current, target - 0.01, target + 0.01);
    }
}
