using AutoTyper.Core;
using AutoTyper.Core.Input;

namespace AutoTyper.Core.Tests;

/// <summary>
/// In-memory <see cref="IKeySender"/> for tests: tracks the materialized
/// result (accounting for backspaces) and how many backspaces occurred, with
/// no real keystrokes or Win32 dependency involved.
/// </summary>
internal class FakeKeySender : IKeySender
{
    private readonly List<char> _buffer = [];

    public int BackspaceCount { get; private set; }

    public int BlurCount { get; private set; }

    public int FocusCount { get; private set; }

    public Task SendCharAsync(char c)
    {
        _buffer.Add(c);
        return Task.CompletedTask;
    }

    public Task SendBackspaceAsync()
    {
        BackspaceCount++;
        if (_buffer.Count > 0)
        {
            _buffer.RemoveAt(_buffer.Count - 1);
        }

        return Task.CompletedTask;
    }

    public Task BlurTargetAsync()
    {
        BlurCount++;
        return Task.CompletedTask;
    }

    public Task FocusTargetAsync()
    {
        FocusCount++;
        return Task.CompletedTask;
    }

    public void CaptureTarget() => CaptureTargetCount++;

    public Task PrepareForTypingAsync(HotkeyCombo trigger)
    {
        PreparedTrigger = trigger;
        return Task.CompletedTask;
    }

    public int CaptureTargetCount { get; private set; }

    public HotkeyCombo? PreparedTrigger { get; private set; }

    public string Result => new(_buffer.ToArray());
}
