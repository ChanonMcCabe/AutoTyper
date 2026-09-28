using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Types a single word, occasionally injecting one of two typo styles from
/// the original AHK MVP: a "full-word" typo — type past the mistake for a
/// character or two before noticing, pause, then backspace several
/// characters back to it and retype — or a "partial" typo — a wrong letter
/// caught immediately with a single backspace at boosted speed. Reports
/// elapsed simulated time back to the caller so <see cref="BurstController"/>'s
/// cooldown can count down without any real-clock dependency.
/// </summary>
public class TypoTyper
{
    private static readonly Dictionary<char, string> QwertyNeighbors = new()
    {
        ['q'] = "wa", ['w'] = "qeas", ['e'] = "wrsd", ['r'] = "etdf", ['t'] = "ryfg",
        ['y'] = "tugh", ['u'] = "yihj", ['i'] = "uojk", ['o'] = "ipkl", ['p'] = "ol",
        ['a'] = "qwsz", ['s'] = "awedzx", ['d'] = "serfxc", ['f'] = "drtgcv", ['g'] = "ftyhvb",
        ['h'] = "gyujbn", ['j'] = "huiknm", ['k'] = "jiolm", ['l'] = "kop",
        ['z'] = "asx", ['x'] = "zsdc", ['c'] = "xdfv", ['v'] = "cfgb", ['b'] = "vghn",
        ['n'] = "bhjm", ['m'] = "njk",
    };

    private readonly IKeySender _keySender;
    private readonly TypoSettings _typos;
    private readonly PauseSettings _pauses;
    private readonly Random _random;
    private readonly TypingRunController? _controller;
    private readonly bool _checkFocus;

    public TypoTyper(
        IKeySender keySender,
        TypoSettings typos,
        PauseSettings pauses,
        Random random,
        TypingRunController? controller = null,
        bool checkFocus = false)
    {
        _keySender = keySender ?? throw new ArgumentNullException(nameof(keySender));
        _typos = typos ?? throw new ArgumentNullException(nameof(typos));
        _pauses = pauses ?? throw new ArgumentNullException(nameof(pauses));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _controller = controller;
        _checkFocus = checkFocus;
    }

    /// <summary>Total typos injected (and corrected) across every call to <see cref="TypeWordAsync"/>.</summary>
    public int TyposMade { get; private set; }

    /// <summary>Types <paramref name="word"/> at <paramref name="wpm"/>, returning the total simulated time spent.</summary>
    public async Task<double> TypeWordAsync(string word, double wpm, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(word);

        double elapsedMs = 0;
        int i = 0;

        while (i < word.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WaitIfNeededAsync(cancellationToken);
            char c = word[i];

            bool eligible = _typos.Enabled && QwertyNeighbors.ContainsKey(char.ToLowerInvariant(c));
            if (eligible && _random.NextDouble() * 100 < _typos.ProbabilityPercent)
            {
                bool fullWord = _random.NextDouble() * 100 < _typos.FullWordTypoRatioPercent;
                if (fullWord)
                {
                    // Backspaces all the way back to this position; the
                    // retype below is unconditional (no re-rolling the typo
                    // chance), so this always advances even at a 100% rate.
                    TyposMade++;
                    elapsedMs += await TypeFullWordTypoAsync(word, i, wpm, cancellationToken);
                }
                else
                {
                    TyposMade++;
                    elapsedMs += await TypePartialTypoAsync(c, wpm, cancellationToken);
                }
            }

            elapsedMs += await SendCharWithDelayAsync(c, wpm, cancellationToken);
            i++;
        }

        return elapsedMs;
    }

    private async Task<double> TypeFullWordTypoAsync(string word, int mistakeIndex, double wpm, CancellationToken cancellationToken)
    {
        double elapsedMs = 0;

        await WaitIfNeededAsync(cancellationToken);
        char wrongChar = GetTypoChar(word[mistakeIndex]);
        await _keySender.SendCharAsync(wrongChar);
        elapsedMs += await DelayAsync(TimingService.GetLetterPause(wpm, _pauses, _random), cancellationToken);

        // Keep typing a character or two past the mistake before "noticing".
        int extraChars = Math.Min(_random.Next(1, 4), word.Length - mistakeIndex - 1);
        for (int j = 0; j < extraChars; j++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WaitIfNeededAsync(cancellationToken);
            await _keySender.SendCharAsync(word[mistakeIndex + 1 + j]);
            elapsedMs += await DelayAsync(TimingService.GetLetterPause(wpm, _pauses, _random), cancellationToken);
        }

        int noticeDelay = _random.Next(_typos.NoticeDelayMinMs, _typos.NoticeDelayMaxMs + 1);
        elapsedMs += await DelayAsync(noticeDelay, cancellationToken);

        int backspaceCount = extraChars + 1;
        double backspaceWpm = wpm * _pauses.BackspaceSpeedMultiplier;
        for (int j = 0; j < backspaceCount; j++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WaitIfNeededAsync(cancellationToken);
            await _keySender.SendBackspaceAsync();
            elapsedMs += await DelayAsync(TimingService.GetLetterPause(backspaceWpm, _pauses, _random), cancellationToken);
        }

        return elapsedMs;
    }

    private async Task<double> TypePartialTypoAsync(char correct, double wpm, CancellationToken cancellationToken)
    {
        double elapsedMs = 0;

        await WaitIfNeededAsync(cancellationToken);
        char wrongChar = GetTypoChar(correct);
        await _keySender.SendCharAsync(wrongChar);
        elapsedMs += await DelayAsync(TimingService.GetLetterPause(wpm, _pauses, _random), cancellationToken);

        // Noticed immediately: a single backspace at boosted speed, no notice delay.
        await WaitIfNeededAsync(cancellationToken);
        await _keySender.SendBackspaceAsync();
        double boostedWpm = wpm * _pauses.BackspaceSpeedMultiplier * 1.5;
        elapsedMs += await DelayAsync(TimingService.GetLetterPause(boostedWpm, _pauses, _random), cancellationToken);

        return elapsedMs;
    }

    private Task WaitIfNeededAsync(CancellationToken cancellationToken) =>
        _controller?.WaitIfNeededAsync(_keySender, _checkFocus, cancellationToken) ?? Task.CompletedTask;

    private async Task<double> SendCharWithDelayAsync(char c, double wpm, CancellationToken cancellationToken)
    {
        await _keySender.SendCharAsync(c);
        return await DelayAsync(TimingService.GetLetterPause(wpm, _pauses, _random), cancellationToken);
    }

    private static async Task<double> DelayAsync(int ms, CancellationToken cancellationToken)
    {
        await Task.Delay(ms, cancellationToken);
        return ms;
    }

    private char GetTypoChar(char correct)
    {
        char lower = char.ToLowerInvariant(correct);
        string neighbors = QwertyNeighbors[lower];
        char wrong = neighbors[_random.Next(neighbors.Length)];
        return char.IsUpper(correct) ? char.ToUpperInvariant(wrong) : wrong;
    }
}
