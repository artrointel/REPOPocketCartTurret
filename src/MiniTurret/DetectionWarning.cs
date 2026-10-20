namespace MiniTurret;

// One warning per continuous encounter; short visibility flicker or switching
// to a nearer enemy must not create another temporary warning item every frame.
internal sealed class DetectionWarning
{
    internal const float Delay = 2f;
    internal const float LossGrace = 0.5f;
    private bool _active;
    private bool _visible;
    private float _fireAt;
    private float _lostAt;
    internal bool FirstShotPending { get; private set; }
    internal bool KeepItem => _active && FirstShotPending;

    internal bool Observe(bool eligible, bool visible, float now)
    {
        if (!eligible)
        {
            Reset();
            return false;
        }
        if (!visible)
        {
            if (_visible)
            {
                _lostAt = now;
            }
            _visible = false;
            // Loss of tracking always invalidates the continuous hold time.
            _fireAt = float.PositiveInfinity;
            if (_active && now - _lostAt >= LossGrace)
            {
                Reset();
            }
            return false;
        }
        if (_active && !_visible && now - _lostAt >= LossGrace)
        {
            Reset();
        }
        if (_active && !_visible)
        {
            _visible = true;
            _fireAt = now + Delay;
            bool needsItem = !FirstShotPending;
            FirstShotPending = true;
            return needsItem;
        }
        _visible = true;
        if (_active)
        {
            return false;
        }
        _active = true;
        FirstShotPending = true;
        _fireAt = now + Delay;
        return true;
    }

    internal bool CanFire(float now) => _active && _visible && now >= _fireAt;

    internal bool CanFire(float now, float cycleReadyAt, bool aligned) =>
        CanFire(now) && now >= cycleReadyAt && aligned;

    internal void MarkFired() => FirstShotPending = false;

    internal void Reset()
    {
        _active = false;
        _visible = false;
        FirstShotPending = false;
        _fireAt = float.PositiveInfinity;
        _lostAt = 0f;
    }
}
