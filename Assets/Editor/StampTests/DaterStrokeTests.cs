using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// STAMP's dater in the editor (BUG-40a, BUG-55b): a carried dater is the
/// desk's hold until it is let go, and a dater mid-stroke takes no grab, so
/// a re-grab can never cut a stroke between the recorded verdict and its ink
/// (the ink rule itself is StampInk's, StampTargetTests). Builds a bare tray
/// (one dater, no drawer, no desk) and drives its drag by its handlers.
/// Needs the editor's engine (GameObjects, physics).
/// </summary>
public class DaterStrokeTests
{
    private GameObject _host, _rack;

    [TearDown]
    public void TearDown()
    {
        HoldRegistry.Desk.Cancel();
        if (_host != null)
            Object.DestroyImmediate(_host);
        if (_rack != null)
            Object.DestroyImmediate(_rack);
    }

    [Test, Category("NeedsEditor")]
    public void ACarriedDater_IsTheHold_AndMidStrokeTakesNoGrab()
    {
        _rack = new GameObject("StampRack");
        var dater = new GameObject("Approved", typeof(BoxCollider));
        dater.transform.SetParent(_rack.transform, false);
        dater.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        Clickable click = dater.AddComponent<Clickable>();
        DeskDraggable drag = dater.AddComponent<DeskDraggable>();
        var die = new GameObject("Die").transform;
        die.SetParent(dater.transform, false);

        _host = new GameObject("StampBar");
        DeskStampTray tray = _host.AddComponent<DeskStampTray>();
        var so = new SerializedObject(tray);
        so.FindProperty("rack").objectReferenceValue = _rack.transform;
        so.FindProperty("approvedStamp").objectReferenceValue = click;
        so.FindProperty("approvedDie").objectReferenceValue = die;
        so.ApplyModifiedPropertiesWithoutUndo();
        // Edit mode calls no Awake: the tray wires its daters there.
        typeof(DeskStampTray).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tray, null);
        tray.SetLive(true, false);
        tray.BeginCase(0);
        Assert.IsTrue(drag.enabled, "a live dater at rest takes a grab");

        var press = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
        drag.OnBeginDrag(press);
        Assert.IsTrue(tray.IsCarrying);
        Assert.IsTrue(HoldRegistry.Desk.IsHolding, "a carried dater is the desk's hold (BUG-55b)");
        HoldRegistry.Desk.Cancel();
        Assert.IsFalse(tray.IsCarrying, "a cancelled hold drops the dater back into the rack, with no press");
        Assert.IsFalse(tray.IsMoving && tray.LastPress != StampPress.Nothing, "no stroke from a cancel");

        drag.OnBeginDrag(press);
        Assert.IsTrue(HoldRegistry.Desk.IsHolding);
        drag.OnEndDrag(press);
        Assert.IsFalse(HoldRegistry.Desk.IsHolding, "the let-go ends the hold");
        Assert.AreEqual(StampPress.Nothing, tray.LastPress, "nothing under it: the bare desk");
        Assert.IsFalse(tray.HasVerdict);
        Assert.IsTrue(tray.IsMoving, "the die strokes onto the wood");
        Assert.IsFalse(drag.enabled, "mid-stroke the dater takes no grab: a re-grab can never cut the stroke (BUG-40a)");
    }
}
