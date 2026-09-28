using AutoTyper.Core;
using AutoTyper.Core.Settings;

namespace AutoTyper.Core.Tests;

public class SpeedResolverTests
{
    [Fact]
    public void ResolveBaseWpm_TimeframeMode_DerivesFromWordCountAndFrameMinutes()
    {
        var speed = new SpeedSettings { TimeframeModeEnabled = true, FrameMinutes = 2 };

        double wpm = SpeedResolver.ResolveBaseWpm("one two three four five six", speed, new Random(1));

        Assert.Equal(3.0, wpm); // 6 words / 2 minutes
    }

    [Fact]
    public void ResolveBaseWpm_RangeMode_PicksWithinConfiguredBounds()
    {
        var speed = new SpeedSettings { RangeModeEnabled = true, MinWpm = 30, MaxWpm = 60 };
        var random = new Random(2);

        for (int i = 0; i < 200; i++)
        {
            double wpm = SpeedResolver.ResolveBaseWpm("irrelevant passage", speed, random);
            Assert.InRange(wpm, 30, 60);
        }
    }

    [Fact]
    public void ResolveBaseWpm_FlatMode_ReturnsConfiguredWpm()
    {
        var speed = new SpeedSettings { Wpm = 77 };

        double wpm = SpeedResolver.ResolveBaseWpm("irrelevant passage", speed, new Random(3));

        Assert.Equal(77, wpm);
    }

    [Fact]
    public void ResolveBaseWpm_TimeframeModeTakesPriorityOverRangeMode()
    {
        var speed = new SpeedSettings
        {
            TimeframeModeEnabled = true,
            FrameMinutes = 1,
            RangeModeEnabled = true,
            MinWpm = 500,
            MaxWpm = 600,
        };

        double wpm = SpeedResolver.ResolveBaseWpm("one two", speed, new Random(4));

        Assert.Equal(2.0, wpm); // 2 words / 1 minute, not the 500-600 range
    }

    [Fact]
    public void EstimateDuration_FlatMode_UsesWordCountOverConfiguredWpm()
    {
        var options = new TypingOptions();
        options.Speed.Wpm = 60;

        TimeSpan estimate = SpeedResolver.EstimateDuration("one two three four five six", options);

        Assert.Equal(TimeSpan.FromMinutes(6 / 60.0), estimate);
    }

    [Fact]
    public void EstimateDuration_RangeMode_UsesMidpointOfMinAndMax()
    {
        var options = new TypingOptions();
        options.Speed.RangeModeEnabled = true;
        options.Speed.MinWpm = 30;
        options.Speed.MaxWpm = 60;

        TimeSpan estimate = SpeedResolver.EstimateDuration("one two three four five six", options);

        Assert.Equal(TimeSpan.FromMinutes(6 / 45.0), estimate); // midpoint = 45 wpm
    }

    [Fact]
    public void EstimateDuration_TimeframeMode_ReturnsFrameMinutes()
    {
        var options = new TypingOptions();
        options.Speed.TimeframeModeEnabled = true;
        options.Speed.FrameMinutes = 4;

        TimeSpan estimate = SpeedResolver.EstimateDuration("one two three", options);

        Assert.Equal(TimeSpan.FromMinutes(4), estimate);
    }

    [Fact]
    public void EstimateDuration_WithStepAwayEnabled_AddsStepAwayTime()
    {
        var options = new TypingOptions();
        options.Speed.Wpm = 60;
        options.StepAway.Enabled = true;
        options.StepAway.EveryWords = 2;
        options.StepAway.DurationSeconds = 30;

        // 6 words / 60 wpm = 6 seconds typing; floor(6/2) = 3 breaks * 30s = 90s.
        TimeSpan estimate = SpeedResolver.EstimateDuration("one two three four five six", options);

        Assert.Equal(TimeSpan.FromSeconds(6) + TimeSpan.FromSeconds(90), estimate);
    }

    [Fact]
    public void EstimateDuration_EmptyPassage_ReturnsZero()
    {
        var options = new TypingOptions();

        TimeSpan estimate = SpeedResolver.EstimateDuration("   ", options);

        Assert.Equal(TimeSpan.Zero, estimate);
    }
}
