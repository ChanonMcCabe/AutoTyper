using System.Text;
using AutoTyper.Core;
using AutoTyper.Core.Input;

namespace AutoTyper.Harness;

/// <summary>
/// Echoes each simulated keystroke to the console and tracks the materialized
/// result (accounting for backspaces) so it can be diffed against the source
/// passage. Used to exercise <see cref="TypingEngine"/> without any real
/// Win32 input.
/// </summary>
public class FakeKeySender : IKeySender
{
    private readonly StringBuilder _buffer = new();

    public Task SendCharAsync(char c)
    {
        _buffer.Append(c);
        Console.Write(c);
        return Task.CompletedTask;
    }

    public Task SendBackspaceAsync()
    {
        if (_buffer.Length > 0)
        {
            _buffer.Length--;
        }

        Console.Write("\b \b");
        return Task.CompletedTask;
    }

    public Task BlurTargetAsync()
    {
        Console.Write("[step away]");
        return Task.CompletedTask;
    }

    public Task FocusTargetAsync()
    {
        Console.Write("[back]");
        return Task.CompletedTask;
    }

    public void CaptureTarget()
    {
        // No real window to capture — this sender writes to the console.
    }

    public Task PrepareForTypingAsync(HotkeyCombo trigger) => Task.CompletedTask;

    public string Result => _buffer.ToString();
}
