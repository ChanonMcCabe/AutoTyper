using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using AutoTyper.Desktop;
using AutoTyper.Core;
using AutoTyper.Core.Input;
using AutoTyper.Harness;

var options = new TypingOptions { PassageText = "The quick brown fox jumps over the lazy dog." };
options.Speed.Wpm = 200;
options.Typos.Enabled = true;

if (args.Contains("--real"))
{
    await RunAgainstNotepadAsync(options);
}
else if (args.Contains("--step-away"))
{
    await RunStepAwayAgainstNotepadAsync();
}
else if (args.Contains("--spacing"))
{
    await RunSpacingAsync();
}
else if (args.Contains("--hotkey-test"))
{
    RunHotkeyTest();
}
else if (args.Contains("--diag-timing"))
{
    await RunTimingDiagnosticsAsync();
}
else
{
    await RunAgainstFakeSenderAsync(options);
}

static async Task RunTimingDiagnosticsAsync()
{
    async Task TimeScenario(string name, Func<CancellationToken, Task> scenario)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var sw = Stopwatch.StartNew();
        try
        {
            await scenario(cts.Token);
            sw.Stop();
            Console.WriteLine($"{name}: {sw.ElapsedMilliseconds} ms");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            Console.WriteLine($"{name}: DID NOT FINISH within 20000ms cap (stopped at {sw.ElapsedMilliseconds}ms)");
        }
    }

    await TimeScenario("TypoTyper full-word 100%/100%, 30 chars", async ct =>
    {
        var sender = new FakeKeySender();
        var typos = new AutoTyper.Core.Settings.TypoSettings { Enabled = true, ProbabilityPercent = 100, FullWordTypoRatioPercent = 100 };
        var pauses = new AutoTyper.Core.Settings.PauseSettings();
        var typoTyper = new TypoTyper(sender, typos, pauses, new Random(300));
        await typoTyper.TypeWordAsync(new string('a', 30), 6000, ct);
    });

    await TimeScenario("Engine timeframe mode, 5 words / 1 min", async ct =>
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(11));
        var opt = new TypingOptions();
        opt.Speed.TimeframeModeEnabled = true;
        opt.Speed.FrameMinutes = 1;
        await engine.RunAsync("one two three four five", opt, sender, ct);
    });

    await TimeScenario("Engine only-full-word typos, 150 chars", async ct =>
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(42));
        var opt = new TypingOptions();
        opt.Speed.Wpm = 6000;
        opt.Typos.Enabled = true;
        opt.Typos.FullWordTypoRatioPercent = 100;
        await engine.RunAsync(new string('a', 150), opt, sender, ct);
    });

    await TimeScenario("Engine bursts always-trigger, 2 words", async ct =>
    {
        var sender = new FakeKeySender();
        var engine = new TypingEngine(new Random(5));
        var opt = new TypingOptions();
        opt.Speed.Wpm = 6000;
        opt.Bursts.Enabled = true;
        opt.Bursts.PhraseBurstChancePercent = 100;
        opt.Bursts.BurstWordCount = 1;
        opt.Bursts.PreBurstPauseMinMs = 150;
        opt.Bursts.PreBurstPauseMaxMs = 150;
        opt.Bursts.PostBurstPauseMinMs = 150;
        opt.Bursts.PostBurstPauseMaxMs = 150;
        opt.Bursts.CooldownMinMs = 0;
        opt.Bursts.CooldownMaxMs = 0;
        await engine.RunAsync("one two", opt, sender, ct);
    });
}

static async Task RunAgainstFakeSenderAsync(TypingOptions options)
{
    var sender = new FakeKeySender();
    var engine = new TypingEngine(new Random(42));

    Console.WriteLine("Typing (seeded RNG, may include simulated typo-and-correction):");
    Console.WriteLine();

    var stopwatch = Stopwatch.StartNew();
    await engine.RunAsync(options.PassageText, options, sender, CancellationToken.None);
    stopwatch.Stop();

    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
    Console.WriteLine($"Final text matches passage: {sender.Result == options.PassageText}");
}

static async Task RunAgainstNotepadAsync(TypingOptions options)
{
    Console.WriteLine("Launching Notepad and typing into it via real SendInput...");
    using var notepad = Process.Start("notepad.exe");
    await Task.Delay(1000);

    var sender = new WinInputKeySender();
    var engine = new TypingEngine(new Random(42));
    await engine.RunAsync(options.PassageText, options, sender, CancellationToken.None);

    Console.WriteLine("Done. Check the Notepad window (left open for inspection).");
}

static Task RunSpacingAsync()
{
    // Types a multi-line, multi-space, tabbed passage into a real multiline
    // TextBox — once with PreserveSpacing on (expect real newlines + tab) and
    // once off (expect one flowing line). Proves WinInputKeySender turns '\n'
    // into a real Enter and '\t' into a real Tab.
    const string passage = "line one\r\nline  two\tindented\n\nline three";
    var done = new TaskCompletionSource();

    var thread = new Thread(() =>
    {
        void RunOnce(bool preserve, Action next)
        {
            var textBox = new System.Windows.Controls.TextBox { AcceptsReturn = true, AcceptsTab = true };
            var window = new Window { Title = $"spacing preserve={preserve}", Width = 460, Height = 220, Topmost = true, Content = textBox };
            window.Loaded += (_, _) => textBox.Focus();
            window.Show();
            window.Activate();
            IntPtr handle = new WindowInteropHelper(window).EnsureHandle();

            var opts = new TypingOptions { PassageText = passage };
            opts.Speed.Wpm = 900;
            opts.Formatting.PreserveSpacing = preserve;

            var sender = new WinInputKeySender();
            sender.SetTypingTarget(handle);
            var engine = new TypingEngine(new Random(1));

            _ = Task.Run(() => engine.RunAsync(passage, opts, sender, CancellationToken.None)).ContinueWith(_ =>
                window.Dispatcher.Invoke(() =>
                {
                    string text = textBox.Text;
                    Console.WriteLine($"--- PreserveSpacing = {preserve} ---");
                    Console.WriteLine($"result lines: {text.Split('\n').Length}, contains tab: {text.Contains('\t')}");
                    Console.WriteLine("result (| = line boundary): " + text.Replace("\r\n", "|").Replace("\n", "|"));
                    window.Close();
                    next();
                }));
        }

        RunOnce(preserve: true, next: () =>
            RunOnce(preserve: false, next: () => Dispatcher.CurrentDispatcher.InvokeShutdown()));

        Dispatcher.Run();
        done.SetResult();
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    return done.Task;
}

static Task RunStepAwayAgainstNotepadAsync()
{
    // Self-contained, deterministic: this process owns the target window, so
    // there's no dependence on Notepad's launch/focus behaviour or on whatever
    // else happens to hold the foreground. Types "one two ... twelve" (12 words)
    // with a step-away every 3 words for exactly 2s, into a real TextBox, then
    // reports whether the text landed and how many times focus left and returned.
    const string passage = "one two three four five six seven eight nine ten eleven twelve";
    var done = new TaskCompletionSource();

    var thread = new Thread(() =>
    {
        var textBox = new System.Windows.Controls.TextBox { AcceptsReturn = true };
        var window = new Window { Title = "step-away target", Width = 480, Height = 200, Topmost = true, Content = textBox };
        window.Loaded += (_, _) => textBox.Focus();
        window.Show();
        window.Activate();

        IntPtr targetHandle = new WindowInteropHelper(window).EnsureHandle();

        var opts = new TypingOptions { PassageText = passage };
        opts.Speed.Wpm = 600;
        opts.StepAway.Enabled = true;
        opts.StepAway.EveryWords = 3;
        opts.StepAway.DurationSeconds = 2;
        opts.StepAway.DurationVariancePercent = 0;

        var sender = new WinInputKeySender();
        sender.SetTypingTarget(targetHandle);
        var engine = new TypingEngine(new Random(42));

        int leftAndReturned = 0;
        bool wasForeground = true;
        var poll = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        poll.Tick += (_, _) =>
        {
            bool isForeground = GetForegroundWindow() == targetHandle;
            if (wasForeground && !isForeground)
            {
                Console.WriteLine("  -> stepped away (target lost foreground)");
            }
            else if (!wasForeground && isForeground)
            {
                leftAndReturned++;
                Console.WriteLine("  -> stepped back  (target regained foreground)");
            }

            wasForeground = isForeground;
        };
        poll.Start();

        var stopwatch = Stopwatch.StartNew();
        _ = Task.Run(() => engine.RunAsync(passage, opts, sender, CancellationToken.None)).ContinueWith(t =>
        {
            stopwatch.Stop();
            window.Dispatcher.Invoke(() =>
            {
                poll.Stop();
                Console.WriteLine();
                Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms (expect ~6s+ from 3 x 2s step-aways).");
                Console.WriteLine($"Target text: \"{textBox.Text.Trim()}\"");
                Console.WriteLine($"Text matches passage: {textBox.Text.Trim() == passage}");
                Console.WriteLine($"Step-away round trips detected: {leftAndReturned} (expect 3)");
                Console.WriteLine(t.IsFaulted ? $"FAULTED: {t.Exception?.InnerException?.Message}" : "PASS: run completed.");
                window.Close();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            });
        });

        Dispatcher.Run();
        done.SetResult();
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    return done.Task;
}

[System.Runtime.InteropServices.DllImport("user32.dll")]
static extern IntPtr GetForegroundWindow();

static void RunHotkeyTest()
{
    var combo = new HotkeyCombo(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.F9);
    bool fired = false;

    var thread = new Thread(() =>
    {
        // No window of any kind: WinHotkeyProvider creates its own message-only
        // window, so all this thread has to supply is a running message pump
        // (Dispatcher.Run below). That the hotkey still fires proves the
        // registration is genuinely system-wide and independent of the UI.
        using var provider = new WinHotkeyProvider();
        provider.HotkeyPressed += (_, _) =>
        {
            fired = true;
            Console.WriteLine($"Hotkey fired: {combo}");
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        };

        provider.Register(combo);
        Console.WriteLine($"Registered {combo} on a message-only window with no UI.");
        Console.WriteLine("Simulating the combo from a background thread in 500ms...");

        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            HotkeySimulator.PressCombo(combo);
        });

        var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timeout.Tick += (_, _) =>
        {
            timeout.Stop();
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        };
        timeout.Start();

        Dispatcher.Run();
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    Console.WriteLine(fired ? "PASS: hotkey event fired." : "FAIL: hotkey event did not fire within timeout.");
}
