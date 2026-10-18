using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A thing on the desk carried along a way (DockPath: over a machine's
/// casing, onto its bed or pad, into its slot, or straight across the desk):
/// its root walks the way by length on the desk's move curve
/// (MotionKnobs.deskMoveFeel; never past the way's end), turning to lie at its facing by the time it
/// is <see cref="TurnBy"/> of the way along (so a card is square to a slot
/// before it slides in), and lands exactly at the way's end. The papers
/// (DeskDocument) and the rulebook folder (DeskRulebook) each run one.
/// </summary>
public sealed class DeskCarry
{
    /// <summary>The share of the way by which the thing has turned to its facing.</summary>
    public const float TurnBy = 0.7f;

    private readonly List<(float x, float y, float z)> _way = new List<(float x, float y, float z)>();
    private Quaternion _turnFrom = Quaternion.identity, _turnTo = Quaternion.identity;
    private float _seconds, _elapsed;
    private Action _done;

    /// <summary>True while the thing is carried.</summary>
    public bool Running { get; private set; }

    /// <summary>Starts carrying <paramref name="thing"/> along <paramref name="way"/> (world points, from where it is) in <paramref name="seconds"/>, turning to <paramref name="facing"/>; <paramref name="done"/> when it lands.</summary>
    public void Start(Transform thing, IReadOnlyList<Vector3> way, Quaternion facing, float seconds, Action done)
    {
        _way.Clear();
        _way.Add((thing.position.x, thing.position.y, thing.position.z));
        foreach (Vector3 p in way)
            _way.Add((p.x, p.y, p.z));
        _turnFrom = thing.rotation;
        _turnTo = facing;
        _seconds = seconds;
        _elapsed = 0f;
        _done = done;
        Running = true;
    }

    /// <summary>Moves <paramref name="thing"/> on by <paramref name="dt"/> seconds; at the end it lies exactly at the way's end and facing, and the carry's callback runs.</summary>
    public void Step(Transform thing, float dt)
    {
        if (!Running)
            return;
        _elapsed += dt;
        float t = _seconds > 0f ? Mathf.Clamp01(_elapsed / _seconds) : 1f;
        float along = UiMotion.Ease(t, UiMotion.Knobs.deskMoveFeel, _seconds);
        // never past the way's end: a way's last leg may come down onto a bed or slide into a slot
        (float x, float y, float z) at = DockPath.At(_way, Mathf.Clamp01(along));
        thing.position = new Vector3(at.x, at.y, at.z);
        thing.rotation = t >= 1f ? _turnTo : Quaternion.SlerpUnclamped(_turnFrom, _turnTo, Mathf.Clamp01(along / TurnBy));
        if (t < 1f)
            return;
        Running = false;
        Action done = _done;
        _done = null;
        done?.Invoke();
    }

    /// <summary>The seconds a way of <paramref name="length"/> metres takes at <paramref name="speed"/> metres a second, never less than <paramref name="least"/>.</summary>
    public static float Seconds(float length, float speed, float least) => Mathf.Max(least, speed > 0f ? length / speed : 0f);
}
