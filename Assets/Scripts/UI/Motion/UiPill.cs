using UnityEngine;

/// <summary>
/// A selection pill's slide (Saleh 2026-10-07, the reel's segmented control:
/// the pill stretches across to the new item and settles): Slide sends a
/// piece (a segmented pair's "on" face, which lays out on its own) from where
/// the selection was to its own place on a spring, stretched along its travel
/// by its speed and thinner across it (SquashStretch keeps the area), then
/// settling with an overshoot. Reduced Motion and Motion intensity 0 leave it
/// in place; stepped by UiMotion only while it moves.
/// </summary>
[DisallowMultipleComponent]
public sealed class UiPill : MonoBehaviour, IMotionTick
{
    private Spring _x, _y;
    private Vector3 _restScale = Vector3.one;
    private Vector3 _applied;

    /// <summary>True when the slide runs up or down (its stretch is along y), else across.</summary>
    private bool _alongY;
    private bool _posed;

    /// <summary>Slides <paramref name="pill"/> to its place from <paramref name="fromWorld"/> (where the selection was) with the toggle cue.</summary>
    public static void Slide(RectTransform pill, Vector3 fromWorld)
    {
        if (pill == null)
            return;
        Sounds.Play(SoundCues.UiToggle);
        MotionAmount amount = UiMotion.Amount;
        if (amount.Still)
            return;
        if (!pill.TryGetComponent(out UiPill slide))
            slide = pill.gameObject.AddComponent<UiPill>();
        slide.Begin(fromWorld, amount);
    }

    private void Begin(Vector3 fromWorld, MotionAmount amount)
    {
        if (!_posed)
        {
            _restScale = transform.localScale;
            _applied = Vector3.zero;
            _posed = true;
        }
        Transform parent = transform.parent;
        Vector3 from = (parent != null ? parent.InverseTransformPoint(fromWorld) : fromWorld) - (transform.localPosition - _applied);
        _x = new Spring { Value = from.x * amount.Share, Target = 0f };
        _y = new Spring { Value = from.y * amount.Share, Target = 0f };
        _alongY = Mathf.Abs(from.y) > Mathf.Abs(from.x);
        Apply(0f, 0f);
        UiMotion.Run(this);
    }

    /// <summary>Steps the slide; false once it settled.</summary>
    public bool TickMotion(float dt)
    {
        if (this == null)
            return false;
        MotionKnobs knobs = UiMotion.Knobs;
        SpringTuning tuning = knobs.Get(knobs.pillFeel);
        bool moving = _x.Step(dt, tuning, knobs.settleValue * 100f, knobs.settleSpeed * 100f) | _y.Step(dt, tuning, knobs.settleValue * 100f, knobs.settleSpeed * 100f);
        Spring along = _alongY ? _y : _x;
        Apply(moving ? along.Velocity : 0f, moving ? along.Acceleration(tuning) : 0f);
        return moving;
    }

    /// <summary>Back in place at once when it hides mid-slide.</summary>
    private void OnDisable()
    {
        _x.Snap(0f);
        _y.Snap(0f);
        Apply(0f, 0f);
    }

    /// <summary>Draws the offset (only its change applied) and the shape for <paramref name="speed"/> (px/s) and <paramref name="acceleration"/> (px/s²) along the travel: stretched in flight, squashed at the launch, the arrival and each turn of the wobble (SquashStretch.FromMotion).</summary>
    private void Apply(float speed, float acceleration)
    {
        if (!_posed)
            return;
        var offset = new Vector3(_x.Value, _y.Value, 0f);
        transform.localPosition += offset - _applied;
        _applied = offset;
        MotionKnobs knobs = UiMotion.Knobs;
        Stretch s = SquashStretch.FromMotion(speed, acceleration, knobs.stretchPerSpeed, knobs.squashPerAccel, knobs.maxStretch);
        transform.localScale = new Vector3(_restScale.x * (_alongY ? s.Across : s.Along), _restScale.y * (_alongY ? s.Along : s.Across), _restScale.z);
        if (speed != 0f || acceleration != 0f || !_x.AtRest || !_y.AtRest)
            return;
        transform.localScale = _restScale;
        transform.localPosition -= _applied;
        _applied = Vector3.zero;
        _posed = false;
    }
}
