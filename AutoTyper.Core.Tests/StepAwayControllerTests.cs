using AutoTyper.Core;
using AutoTyper.Core.Settings;

namespace AutoTyper.Core.Tests;

public class StepAwayControllerTests
{
    [Fact]
    public void ShouldStepAway_FiresEveryNWordsAndResetsTheCounter()
    {
        var controller = new StepAwayController(new StepAwaySettings { Enabled = true, EveryWords = 3 });
        var firedAt = new List<int>();

        for (int word = 1; word <= 10; word++)
        {
            controller.AdvanceWord();
            if (controller.ShouldStepAway())
            {
                firedAt.Add(word);
            }
        }

        Assert.Equal(new[] { 3, 6, 9 }, firedAt);
    }

    [Fact]
    public void ShouldStepAway_WhenDisabled_NeverFires()
    {
        var controller = new StepAwayController(new StepAwaySettings { Enabled = false, EveryWords = 2 });

        for (int word = 1; word <= 20; word++)
        {
            controller.AdvanceWord();
            Assert.False(controller.ShouldStepAway());
        }
    }

    [Fact]
    public void ShouldStepAway_WhenEveryWordsIsNotPositive_NeverFires()
    {
        var controller = new StepAwayController(new StepAwaySettings { Enabled = true, EveryWords = 0 });

        for (int word = 1; word <= 20; word++)
        {
            controller.AdvanceWord();
            Assert.False(controller.ShouldStepAway());
        }
    }

    [Fact]
    public void Constructor_NullSettings_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StepAwayController(null!));
    }
}
