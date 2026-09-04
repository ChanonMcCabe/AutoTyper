using AutoTyper.Core.Settings;

namespace AutoTyper.Core;

/// <summary>
/// Decides, once per word, whether to start a phrase burst: gated by a
/// cooldown (so bursts don't chain back-to-back) and then a
/// <see cref="BurstSettings.PhraseBurstChancePercent"/> roll. The main loop
/// consults <see cref="ShouldStartBurst"/> each iteration rather than the
/// controller driving the loop itself.
/// </summary>
public class BurstController
{
    private readonly BurstSettings _bursts;
    private readonly Random _random;
    private double _cooldownRemainingMs;

    public BurstController(BurstSettings bursts, Random random)
    {
        _bursts = bursts ?? throw new ArgumentNullException(nameof(bursts));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// Call once per word with the delay just spent (typing the word, any
    /// pauses) so the cooldown counts down in typing time rather than wall
    /// time — keeping the controller free of any real-clock dependency.
    /// </summary>
    public void AdvanceCooldown(double elapsedMs)
    {
        if (_cooldownRemainingMs > 0)
        {
            _cooldownRemainingMs = Math.Max(0, _cooldownRemainingMs - elapsedMs);
        }
    }

    public bool ShouldStartBurst()
    {
        if (!_bursts.Enabled || _cooldownRemainingMs > 0)
        {
            return false;
        }

        return _random.NextDouble() * 100 < _bursts.PhraseBurstChancePercent;
    }

    public int GetBurstWordCount() => _bursts.BurstWordCount;

    public double ApplyBurstSpeed(double wpm) => wpm * _bursts.BurstSpeedMultiplier;

    /// <summary>Call once a burst finishes to arm the cooldown before another can start.</summary>
    public void OnBurstFinished()
    {
        _cooldownRemainingMs = _random.Next(_bursts.CooldownMinMs, _bursts.CooldownMaxMs + 1);
    }
}
