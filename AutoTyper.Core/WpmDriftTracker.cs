namespace AutoTyper.Core;

/// <summary>
/// Reproduces the MVP's "fluctuating WPM": every <see cref="WordsPerTargetPick"/>
/// words, pick a new target speed within ± driftAmount of the base WPM, and
/// step the current speed toward it each word via <see cref="TimingService.AdjustWpm"/>
/// — a smooth wander rather than a one-directional ramp. When drift is
/// disabled (<c>driftAmount == 0</c>), <see cref="CurrentWpm"/> just stays at
/// the base WPM.
/// </summary>
public class WpmDriftTracker
{
    private const int WordsPerTargetPick = 10;
    private const double StepFraction = 0.15;

    private readonly double _baseWpm;
    private readonly double _driftAmount;
    private readonly Random _random;
    private double _targetWpm;
    private int _wordCounter;

    public WpmDriftTracker(double baseWpm, double driftAmount, Random random)
    {
        _baseWpm = baseWpm;
        _driftAmount = driftAmount;
        _random = random ?? throw new ArgumentNullException(nameof(random));
        CurrentWpm = baseWpm;
        _targetWpm = baseWpm;
    }

    public double CurrentWpm { get; private set; }

    /// <summary>Advances the tracker by one word: called once per word typed.</summary>
    public void AdvanceWord()
    {
        if (_driftAmount <= 0)
        {
            return;
        }

        if (_wordCounter % WordsPerTargetPick == 0)
        {
            _targetWpm = _baseWpm + ((_random.NextDouble() * 2 - 1) * _driftAmount);
        }

        _wordCounter++;
        CurrentWpm = TimingService.AdjustWpm(CurrentWpm, _targetWpm, StepFraction);
    }
}
