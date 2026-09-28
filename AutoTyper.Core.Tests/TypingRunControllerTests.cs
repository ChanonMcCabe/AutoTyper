using AutoTyper.Core;

namespace AutoTyper.Core.Tests;

public class TypingRunControllerTests
{
    [Fact]
    public void Pause_SetsIsPausedAndRaisesPausedState()
    {
        var controller = new TypingRunController();
        RunState? lastState = null;
        controller.StateChanged += (_, s) => lastState = s;

        controller.Pause();

        Assert.True(controller.IsPaused);
        Assert.Equal(RunState.Paused, controller.State);
        Assert.Equal(RunState.Paused, lastState);
    }

    [Fact]
    public void Resume_ClearsIsPausedAndRaisesTypingState()
    {
        var controller = new TypingRunController();
        controller.Pause();
        RunState? lastState = null;
        controller.StateChanged += (_, s) => lastState = s;

        controller.Resume();

        Assert.False(controller.IsPaused);
        Assert.Equal(RunState.Typing, controller.State);
        Assert.Equal(RunState.Typing, lastState);
    }

    [Fact]
    public async Task WaitIfNeededAsync_WhilePaused_BlocksUntilResume()
    {
        var controller = new TypingRunController();
        var sender = new FakeKeySender();
        controller.Pause();

        Task waitTask = controller.WaitIfNeededAsync(sender, checkFocus: false, CancellationToken.None);
        await Task.Delay(50);
        Assert.False(waitTask.IsCompleted);

        controller.Resume();
        await waitTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(waitTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task WaitIfNeededAsync_WithFocusCheckAlreadySatisfied_ReturnsImmediately()
    {
        var controller = new TypingRunController();
        var sender = new FakeKeySender { TargetFocused = true };

        await controller.WaitIfNeededAsync(sender, checkFocus: true, CancellationToken.None);

        Assert.Equal(RunState.Typing, controller.State);
    }

    [Fact]
    public async Task WaitIfNeededAsync_WithFocusCheck_BlocksUntilTargetRegainsFocus()
    {
        var controller = new TypingRunController();
        var sender = new FakeKeySender { TargetFocused = false };

        Task waitTask = controller.WaitIfNeededAsync(sender, checkFocus: true, CancellationToken.None);
        await Task.Delay(100);

        Assert.False(waitTask.IsCompleted);
        Assert.Equal(RunState.WaitingForFocus, controller.State);

        sender.TargetFocused = true;
        await waitTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(RunState.Typing, controller.State);
    }

    [Fact]
    public async Task WaitIfNeededAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        var controller = new TypingRunController();
        var sender = new FakeKeySender();
        controller.Pause();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => controller.WaitIfNeededAsync(sender, checkFocus: false, cts.Token));
    }
}
