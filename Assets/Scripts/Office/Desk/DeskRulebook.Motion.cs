using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The rulebook folder's motion (DeskRulebook's motion half): its place on the
/// desk, its lift in the stack, the refusal shake and its short carry to its
/// rest, its physics feel each frame, and its "no" on the counter. It obeys
/// the papers' rules (DOC-HANDLE, BUG-6b): one mover writes its transform at
/// a time (a carry stops the moment the folder is grabbed, so the drag alone
/// moves it: no jitter, no jump at the carry's end); a refusal shakes in
/// place and the desk lays it just clear of what refused it (never a
/// teleport back to where it was picked up); held, it is drawn above every
/// prop (HeldOnTop, as a paper) and its swing pivots on the held point.
/// Its pages, print and look stay in DeskRulebook.cs.
/// Carved out of DeskRulebook.cs by P0-A. Owner: DOC-HANDLE.
/// </summary>
public sealed partial class DeskRulebook
{
    /// <summary>Lays the booklet on the desk at <paramref name="at"/> (on the desk plane), turned to <paramref name="rotation"/> (the office binder).</summary>
    public void Place(Vector3 at, Quaternion rotation) => transform.SetPositionAndRotation(at, rotation);

    /// <summary>Lifts the booklet <paramref name="height"/> metres off the desk plane (its place in the papers' stack, or the drag's lift: DeskController).</summary>
    public void SetLift(float height)
    {
        _height = height;
        ShowMotion();
    }

    /// <summary>The booklet's height in the stack (SetLift).</summary>
    private float _height;

    /// <summary>Its hop when dropped.</summary>
    private Spring _hop;

    /// <summary>The folder carried along a way (a refusal: the short way to where it rests, just clear of what refused it).</summary>
    private readonly DeskCarry _carry = new DeskCarry();

    /// <summary>The camera the held folder is drawn toward and the ratio (HeldOnTop; 1 when not held).</summary>
    private Camera _heldEye;
    private float _heldRatio = 1f;

    /// <summary>The held point (root-local x, z from its centre) its swing pivots on: kept after a drop until the swing has settled.</summary>
    private Vector2 _pivot;

    /// <summary>The booklet's own scale (the builder's): a held folder is drawn smaller by the held ratio from it.</summary>
    private Vector3? _bookletScale;

    /// <summary>Where the folder's way starts from: its root (the held lift is gone once it is let go).</summary>
    public Vector3 CarriedFrom => transform.position;

    /// <summary>The held folder is drawn above every prop (HeldOnTop: toward <paramref name="eye"/> along its rays by <paramref name="ratio"/>, its picture unchanged; DeskController measures the ratio as it is dragged), or back on the desk (null, 1).</summary>
    public void ShowHeld(Camera eye, float ratio)
    {
        _heldEye = eye;
        _heldRatio = eye != null ? Mathf.Clamp01(ratio) : 1f;
        ShowMotion();
    }

    /// <summary>A machine refused the folder (it scans and reads only the traveller's papers): it shakes its "no" in place and is carried along <paramref name="way"/> (the short way to where it rests, just clear of the machine; empty: it rests where it is) in <paramref name="seconds"/>.</summary>
    public void Refused(IReadOnlyList<Vector3> way, float seconds)
    {
        ShakeNo();
        RestAt(way, seconds);
    }

    /// <summary>The folder's "no": it shakes in place (PaperPhysics.Shake, the desk's refusal shake: UseConfig).</summary>
    private void ShakeNo()
    {
        if (_shakeConfig == null)
            return;
        _physics ??= new PaperPhysics(UiMotion.Knobs.folderWeight);
        _physics.Shake(_shakeConfig.refuseShakeDistance, _shakeConfig.refuseShakeSeconds, _shakeConfig.refuseShakeCycles);
    }

    /// <summary>The folder goes along <paramref name="way"/> (the short way to where it rests; empty: it stays) in <paramref name="seconds"/>: a drop that left it on the counter or over a prop (DeskController, DropResolver).</summary>
    public void RestAt(IReadOnlyList<Vector3> way, float seconds)
    {
        if (way != null && way.Count > 0)
            _carry.Start(transform, way, transform.rotation, seconds, null);
        ShowMotion();
    }

    /// <summary>The folder's physics feel (Saleh 2026-10-17: "KEEP IT TOP DOWN BUT GIVE IT PHYSICS"; the folder "can be nudged and slides with weight"): heavier than a paper (MotionKnobs.folderWeight), it swings less; let go, it stays where it was let go and settles as much as it was moving; its bend is its look's (DOC-LOOK).</summary>
    private PaperPhysics _physics;

    /// <summary>Each frame: the folder's swing, hop and shake on top of where the desk puts it (the drag moves its root; a carry stops once it is grabbed: one mover).</summary>
    private void Update()
    {
        if (booklet == null)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        _physics ??= new PaperPhysics(knobs.folderWeight);
        Vector3 at = transform.position;
        bool held = drag != null && drag.IsDragging;
        if (held)
        {
            _carry.Stop();
            Vector3 point = transform.InverseTransformDirection(-drag.GrabOffset);
            _pivot = new Vector2(point.x, point.z);
        }
        float hop = _physics.Step(Time.unscaledDeltaTime, Vector3.Dot(at, transform.right), Vector3.Dot(at, transform.forward), held, knobs, amount);
        SpringTuning tuning = knobs.Get(knobs.paperFeel);
        if (hop > 0f)
            _hop.Kick(tuning.KickFor(hop));
        _hop.Step(Time.unscaledDeltaTime, tuning, 1e-5f, 1e-4f);
        if (_hop.Value < 0f)
        {
            _hop.Value = 0f; // it never sinks into what lies under it
            _hop.Velocity = Mathf.Max(0f, _hop.Velocity);
        }
        _carry.Step(transform, Time.deltaTime);
        if (_physics.Moving || !_hop.AtRest || held || _heldRatio < 1f)
            ShowMotion();
    }

    /// <summary>Draws the booklet at its height and hop, its swing pivoted on the held point and its shake, and while held toward the camera (HeldOnTop).</summary>
    private void ShowMotion()
    {
        if (booklet == null)
            return;
        _bookletScale ??= booklet.localScale;
        float yaw = _physics != null ? _physics.Yaw : 0f, shake = _physics != null ? _physics.ShakeX : 0f;
        (float ox, float oz) = PaperPhysics.PivotOffset(_pivot.x, _pivot.y, yaw);
        Vector3 local = new Vector3(ox + shake, _height + _hop.Value, oz);
        float s = _heldEye != null ? _heldRatio : 1f;
        if (s < 1f)
        {
            Vector3 eye = transform.InverseTransformPoint(_heldEye.transform.position);
            local = eye + (local - eye) * s;
        }
        booklet.localPosition = local;
        booklet.localRotation = Quaternion.Euler(0f, yaw, 0f);
        booklet.localScale = _bookletScale.Value * s;
    }

    /// <summary>The booklet dropped on the counter (it stays on the clerk's side): it shakes its "no" in place; the desk lays it just off the counter (DeskController.RulebookDropped), never back where it was picked up.</summary>
    private void Dropped(DeskDraggable dragged, Vector3 released)
    {
        if (counter != null && counter.Contains(released))
            ShakeNo();
    }

    /// <summary>The desk tuning the folder's "no" shakes by (DeskController gives it: UseConfig).</summary>
    private DeskConfigSO _shakeConfig;

    /// <summary>The desk tuning for the folder's refusal shake (DeskController, once).</summary>
    public void UseConfig(DeskConfigSO config) => _shakeConfig = config;
}
