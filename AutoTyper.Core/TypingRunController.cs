using System.Diagnostics;

namespace AutoTyper.Core;

/// <summary>
/// Phase of an in-progress run, reported through <see cref="TypingRunController.StateChanged"/>
/// and echoed in each <see cref="TypingProgress"/> snapshot.
/// </summary>
public enum RunState
{
    /// <summary>Actively sending keystrokes.</summary>
    Typing,

    /// <summary>Paused by the caller via <see cref="TypingRunController.Pause"/>.</summary>
    Paused,

    /// <summary>
    /// Blocked because <c>PauseOnFocusLoss</c> is on and the target window no
    /// longer has keyboard focus.
    /// </summary>
    WaitingForFocus,

    /// <summary>Mid step-away break; the target has been deliberately blurred.</summary>
    SteppedAway,
}

/// <summary>
/// Caller-owned handle for pausing/resuming an in-progress <see cref="TypingEngine.RunAsync"/>
/// call and observing its state. <see cref="TypingEngine"/> and <see cref="TypoTyper"/>
/// call <see cref="WaitIfNeededAsync"/> before every token/character so a paused
/// run blocks with nothing sent, and — when the caller asks it to check focus —
/// so a run automatically waits out a focus loss on the target window instead
/// of typing into whatever else the user clicked into. One instance is meant
/// to back a single run; create a fresh one for each run rather than reusing
/// one, since <see cref="PausedTime"/> accumulates for the lifetime of the
/// instance.
/// </summary>
public class TypingRunController
{
    private readonly object _lock = new();
    private readonly Stopwatch _idleStopwatch = new();
    private TaskCompletionSource _gate;
    private bool _isPaused;
    private RunState _state = RunState.Typing;

    public TypingRunController()
    {
        _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _gate.SetResult();
    }

    /// <summary>Raised whenever <see cref="State"/> changes.</summary>
    public event EventHandler<RunState>? StateChanged;

    public bool IsPaused
    {
        get { lock (_lock) { return _isPaused; } }
    }

    public RunState State
    {
        get { lock (_lock) { return _state; } }
    }

    /// <summary>
    /// Cumulative time spent not actively typing — paused, waiting for target
    /// focus, or stepped away — since this controller was created. Used to
    /// compute <see cref="TypingRunResult.EffectiveWpm"/> over active time only.
    /// </summary>
    internal TimeSpan PausedTime
    {
        get { lock (_lock) { return _idleStopwatch.Elapsed; } }
    }

    /// <summary>Pauses the run before its next keystroke, until <see cref="Resume"/> is called.</summary>
    public void Pause()
    {
        lock (_lock)
        {
            if (_isPaused)
            {
                return;
            }

            _isPaused = true;
            _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        SetState(RunState.Paused);
    }

    /// <summary>Releases a pause taken with <see cref="Pause"/>.</summary>
    public void Resume()
    {
        TaskCompletionSource? gateToRelease;
        lock (_lock)
        {
            if (!_isPaused)
            {
                return;
            }

            _isPaused = false;
            gateToRelease = _gate;
        }

        gateToRelease.TrySetResult();
        SetState(RunState.Typing);
    }

    /// <summary>
    /// Awaits any active pause, then — if <paramref name="checkFocus"/> is on —
    /// polls <see cref="IKeySender.IsTargetFocused"/> every 250ms until the
    /// target regains focus, raising <see cref="RunState.WaitingForFocus"/> and
    /// back to <see cref="RunState.Typing"/> as that happens. Looped so that a
    /// pause requested mid focus-wait is honored before the focus check resumes.
    /// </summary>
    internal async Task WaitIfNeededAsync(IKeySender sender, bool checkFocus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sender);

        while (true)
        {
            Task gateTask;
            lock (_lock)
            {
                gateTask = _gate.Task;
            }

            await gateTask.WaitAsync(cancellationToken);

            if (!checkFocus || sender.IsTargetFocused())
            {
                return;
            }

            SetState(RunState.WaitingForFocus);
            while (!sender.IsTargetFocused())
            {
                if (IsPaused)
                {
                    break;
                }

                await Task.Delay(250, cancellationToken);
            }

            if (IsPaused)
            {
                // Paused mid focus-wait: loop back and honor it before
                // re-checking focus.
                continue;
            }

            SetState(RunState.Typing);
            return;
        }
    }

    /// <summary>
    /// Sets <see cref="State"/> directly, for transitions <see cref="TypingEngine"/>
    /// drives itself (the <see cref="RunState.SteppedAway"/> break) rather than
    /// ones this controller decides on its own.
    /// </summary>
    internal void SetState(RunState state)
    {
        lock (_lock)
        {
            // A pause taken mid step-away outlives the break: returning from
            // it must not report Typing while the gate is still closed.
            if (state == RunState.Typing && _isPaused)
            {
                state = RunState.Paused;
            }

            bool wasIdle = _state != RunState.Typing;
            bool willBeIdle = state != RunState.Typing;
            _state = state;

            if (!wasIdle && willBeIdle)
            {
                _idleStopwatch.Start();
            }
            else if (wasIdle && !willBeIdle)
            {
                _idleStopwatch.Stop();
            }
        }

        StateChanged?.Invoke(this, state);
    }
}
