using AutoTyper.Desktop.Mac;

namespace AutoTyper.Desktop.Tests;

/// <summary>
/// Covers <see cref="MacKeySender"/>'s target tracking (step away and
/// pause-on-focus-loss) through its test seam, with fake pid lookups in place
/// of the Accessibility calls, so it runs on any OS. Whether those native
/// calls actually work can only be checked on a real Mac.
/// </summary>
public class MacKeySenderTests
{
    private const int OwnPid = 100;
    private const int TargetPid = 200;
    private const int FinderPid = 300;
    private const int OtherPid = 400;

    private int _focusedPid = TargetPid;
    private int _finderPid = FinderPid;
    private readonly List<int> _activated = [];

    private MacKeySender CreateSender() =>
        new(() => _focusedPid, pid => { _activated.Add(pid); return true; }, () => _finderPid, OwnPid);

    [Fact]
    public async Task CaptureTarget_IgnoresOwnProcess_SoStepAwayDoesNothing()
    {
        _focusedPid = OwnPid;
        var sender = CreateSender();

        sender.CaptureTarget();
        await sender.BlurTargetAsync();
        await sender.FocusTargetAsync();

        Assert.Empty(_activated);
    }

    [Fact]
    public void IsTargetFocused_WithNoTarget_IsTrue()
    {
        var sender = CreateSender();
        _focusedPid = OtherPid;

        Assert.True(sender.IsTargetFocused());
    }

    [Theory]
    [InlineData(TargetPid, true)]
    [InlineData(OtherPid, false)]
    [InlineData(0, true)] // A failed focus query must never strand a run waiting for focus.
    public void IsTargetFocused_ComparesFocusedAppToTarget(int focusedPid, bool expected)
    {
        var sender = CreateSender();
        sender.CaptureTarget();
        _focusedPid = focusedPid;

        Assert.Equal(expected, sender.IsTargetFocused());
    }

    [Fact]
    public async Task StepAway_ActivatesFinderThenTarget()
    {
        var sender = CreateSender();
        sender.CaptureTarget();

        await sender.BlurTargetAsync();
        await sender.FocusTargetAsync();

        Assert.Equal([FinderPid, TargetPid], _activated);
    }

    [Fact]
    public async Task BlurTarget_WithoutFinder_DoesNothing()
    {
        _finderPid = 0;
        var sender = CreateSender();
        sender.CaptureTarget();

        await sender.BlurTargetAsync();

        Assert.Empty(_activated);
    }
}
