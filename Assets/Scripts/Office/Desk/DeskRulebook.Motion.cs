using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// The rulebook folder's motion (DeskRulebook's motion half): its place on the
/// desk, its lift in the stack, its carry floor over a machine, the refusal
/// bounce and carry, its physics feel each frame, and the drop on the counter.
/// Its pages, print and look stay in DeskRulebook.cs.
/// Carved out of DeskRulebook.cs by P0-A (pure move). Owner: DOC-HANDLE.
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

    /// <summary>The booklet's height in the stack (SetLift), its hop over it when dropped and the turn its drops have left it (degrees about its up).</summary>
    private float _height, _restYaw;

    private Spring _hop;

    /// <summary>The least height the dragged folder keeps over a machine's casing under it (SetCarryFloor), eased on the paper feel.</summary>
    private Spring _floor;

    /// <summary>The folder carried along a way (a machine refused it: back over the casing to where it was picked up).</summary>
    private readonly DeskCarry _carry = new DeskCarry();

    /// <summary>A dragged folder over a machine keeps at least <paramref name="height"/> metres over the desk (the casing's top and a clearance; 0 away from every machine): it never passes through a casing.</summary>
    public void SetCarryFloor(float height)
    {
        _floor.Target = Mathf.Max(0f, height);
        if (UiMotion.Amount.Still)
            _floor.Snap(_floor.Target);
    }

    /// <summary>Where the folder's way starts from: its root, lifted by how far it hangs over its stack height (CarriedFrom, as a paper's).</summary>
    public Vector3 CarriedFrom => transform.position + Vector3.up * Mathf.Max(0f, _floor.Value - _height);

    /// <summary>A machine refused the folder (it scans and reads only the traveller's papers): it shakes its "no" and is carried along <paramref name="way"/> (DockPath: back over the casing) in <paramref name="seconds"/>.</summary>
    public void Refused(IReadOnlyList<Vector3> way, float seconds, float swingSpeed)
    {
        float raised = Mathf.Max(0f, _floor.Value - _height);
        transform.position += Vector3.up * raised;
        _floor.Snap(0f);
        _physics?.Nudge(swingSpeed);
        _carry.Start(transform, way, transform.rotation, seconds, null);
        ShowMotion();
    }

    /// <summary>The folder's physics feel (Saleh 2026-10-17: "KEEP IT TOP DOWN BUT GIVE IT PHYSICS"; the folder "can be nudged and slides with weight"): heavier than a paper (MotionKnobs.folderWeight), it swings less and slides to rest slower; it never bends (Saleh's 1018a playtest: "the folder stays weighty").</summary>
    private PaperPhysics _physics;

    /// <summary>Each frame: the folder's swing, slide, spin and hop on top of where the desk puts it (the drag moves its root).</summary>
    private void Update()
    {
        if (booklet == null)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        _physics ??= new PaperPhysics(knobs.folderWeight);
        Vector3 at = transform.position;
        bool held = drag != null && drag.IsDragging;
        (float spin, float hop) = _physics.Step(Time.unscaledDeltaTime, Vector3.Dot(at, transform.right), Vector3.Dot(at, transform.forward), held, knobs, amount);
        _restYaw = Mathf.Clamp(_restYaw + spin, -3f, 3f);
        SpringTuning tuning = knobs.Get(knobs.paperFeel);
        if (hop > 0f)
            _hop.Kick(tuning.KickFor(hop));
        _hop.Step(Time.unscaledDeltaTime, tuning, 1e-5f, 1e-4f);
        if (_hop.Value < 0f)
        {
            _hop.Value = 0f; // it never sinks into what lies under it
            _hop.Velocity = Mathf.Max(0f, _hop.Velocity);
        }
        bool floating = !_floor.AtRest;
        _floor.Step(Time.unscaledDeltaTime, tuning, 1e-5f, 1e-4f);
        _carry.Step(transform, Time.deltaTime);
        if (_physics.Moving || spin != 0f || !_hop.AtRest || floating)
            ShowMotion();
    }

    /// <summary>Draws the booklet at its height, hop, slide and turn.</summary>
    private void ShowMotion()
    {
        if (booklet == null)
            return;
        float ox = _physics != null ? _physics.OffsetX : 0f, oz = _physics != null ? _physics.OffsetZ : 0f, yaw = _physics != null ? _physics.Yaw : 0f;
        booklet.localPosition = new Vector3(ox, Mathf.Max(_height, _floor.Value) + _hop.Value, oz);
        booklet.localRotation = Quaternion.Euler(0f, _restYaw + yaw, 0f);
    }

    /// <summary>The booklet dropped: on the counter it goes back to where it was picked up (it stays on the clerk's side).</summary>
    private void Dropped(DeskDraggable dragged, Vector3 released)
    {
        if (counter != null && counter.Contains(released))
            transform.position = dragged.PickUpPosition;
    }
}
