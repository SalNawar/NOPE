using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// STAMP's dater in the editor (BUG-40a, BUG-55b): a carried dater is the
/// desk's hold until it is let go, and a dater mid-stroke takes no grab, so
/// a re-grab can never cut a stroke between the recorded verdict and its ink;
/// a let-go over the rack or a machine does not stroke down through it; the
/// press looks along the pointer's ray the dater last aimed by; and an owed
/// print lands when the case ends or the papers are handed back mid-stroke
/// (the ink rule itself is StampInk's, StampTargetTests). Builds a bare tray
/// (two daters, no drawer, no desk: the world's up is the desk's) and drives
/// its drags by their handlers. Needs the editor's engine (GameObjects,
/// physics), so it runs in Unity's EditMode suite only.
/// </summary>
public class DaterStrokeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly PointerEventData _press = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
    private GameObject _host, _rack, _props;

    [TearDown]
    public void TearDown()
    {
        HoldRegistry.Desk.Cancel();
        foreach (GameObject go in new[] { _host, _rack, _props })
            if (go != null)
                Object.DestroyImmediate(go);
    }

    [Test, Category("NeedsEditor")]
    public void ACarriedDater_IsTheHold_AndMidStrokeTakesNoGrab()
    {
        DeskStampTray tray = Tray(out Dater approved, out _);
        tray.BeginCase(0);
        Assert.IsTrue(approved.Drag.enabled, "a live dater at rest takes a grab");

        approved.Drag.OnBeginDrag(_press);
        Assert.IsTrue(tray.IsCarrying);
        Assert.IsTrue(HoldRegistry.Desk.IsHolding, "a carried dater is the desk's hold (BUG-55b)");
        HoldRegistry.Desk.Cancel();
        Assert.IsFalse(tray.IsCarrying, "a cancelled hold drops the dater back into the rack, with no press");
        Assert.IsFalse(tray.IsMoving, "no stroke from a cancel (and the dater never left its place: nothing to return)");
        Assert.IsTrue(approved.Drag.enabled, "no stroke from a cancel: the dater takes a grab again at once");
        Assert.AreEqual(approved.Home, approved.Root.localPosition, "the dater sits at home");

        approved.Drag.OnBeginDrag(_press);
        Assert.IsTrue(HoldRegistry.Desk.IsHolding);
        approved.Root.position = new Vector3(-0.5f, 0.06f, 0f); // over the bare desk, clear of the rack
        approved.Drag.OnEndDrag(_press);
        Assert.IsFalse(HoldRegistry.Desk.IsHolding, "the let-go ends the hold");
        Assert.AreEqual(StampPress.Nothing, tray.LastPress, "nothing under it: the bare desk");
        Assert.IsFalse(tray.HasVerdict);
        Assert.IsTrue(tray.IsMoving, "the die strokes onto the wood");
        Assert.IsFalse(approved.Drag.enabled, "mid-stroke the dater takes no grab: a re-grab can never cut the stroke (BUG-40a)");
        Assert.AreEqual(0f, tray.LastPressPoint.y, 1e-5f, "the bare desk's stroke ends on the desk's plane");
    }

    [Test, Category("NeedsEditor")]
    public void ALetGo_OverTheRackOrAMachine_StrokesNothing_AndNeverGoesThroughIt()
    {
        DeskStampTray tray = Tray(out Dater approved, out _);
        tray.BeginCase(0);
        // A machine on the desk (the chip reader's click box): 5 cm tall.
        _props = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _props.name = "ChipReader";
        _props.transform.position = new Vector3(0.5f, 0.025f, 0f);
        _props.transform.localScale = Vector3.one * 0.05f;

        // Over the rack's box (4 cm tall; it has no collider): the old plunge into the brass.
        approved.Drag.OnBeginDrag(_press);
        approved.Root.position = new Vector3(0.05f, 0.06f, 0f);
        approved.Drag.OnEndDrag(_press);
        Assert.AreEqual(StampPress.Nothing, tray.LastPress);
        Assert.IsTrue(approved.Drag.enabled, "over the rack no stroke starts: the dater just goes back");
        Assert.GreaterOrEqual(approved.Die.position.y, 0.04f, "no part of the dater goes below the rack's top");

        // Over the machine.
        approved.Drag.OnBeginDrag(_press);
        approved.Root.position = new Vector3(0.5f, 0.07f, 0f);
        approved.Drag.OnEndDrag(_press);
        Assert.AreEqual(StampPress.Nothing, tray.LastPress);
        Assert.IsTrue(approved.Drag.enabled, "over a machine no stroke starts: the dater just goes back");
        Assert.GreaterOrEqual(approved.Die.position.y, 0.05f, "no part of the dater goes below the machine's top");

        // The bare desk beside them still thumps.
        approved.Drag.OnBeginDrag(_press);
        approved.Root.position = new Vector3(-0.5f, 0.06f, 0f);
        approved.Drag.OnEndDrag(_press);
        Assert.IsFalse(approved.Drag.enabled, "over the bare desk the die strokes onto the wood");
    }

    [Test, Category("NeedsEditor")]
    public void ThePress_LooksAlongThePointersRay_AndAnOwedPrint_LandsAtTheCut()
    {
        DeskStampTray tray = Tray(out Dater approved, out Dater denied);
        _rack.transform.position = new Vector3(0f, 0f, 1f); // the rack well clear of the papers
        _props = new GameObject("Papers");
        // A raised paper straight under the die (x up to 2 mm), and the paper the pointer shows 4 mm beside it, lying on the desk.
        Paper("Raised", new Vector3(-0.049f, 0.0052f, 0f), new Vector3(0.102f, 0.0104f, 0.1f));
        Paper("Aimed", new Vector3(0.0515f, 0.0002f, 0f), new Vector3(0.097f, 0.0004f, 0.1f));
        tray.BeginCase(0); // an unbound DeskDocument is paper 0: both are the passport

        approved.Drag.OnBeginDrag(_press);
        approved.Root.position = new Vector3(0f, 0.03f, 0f);
        var aimed = new Vector3(0.004f, 0.0004f, 0f);
        var eye = new Vector3(1f, 1f, 0f);
        typeof(DeskStampTray).GetMethod("Aim", Private).Invoke(tray, new object[] { new Ray(eye, aimed - eye) });
        approved.Drag.OnEndDrag(_press);
        Assert.AreEqual(StampPress.Stamped, tray.LastPress);
        Assert.AreEqual(aimed.x, tray.LastPressPoint.x, 1e-3f, "the press lands where the pointer's ray met the papers, not straight under the die (the raised paper)");
        Assert.AreEqual(aimed.y, tray.LastPressPoint.y, 1e-4f);
        Assert.IsTrue(Owed(tray, 0), "the print is owed from the press until the stroke bottoms out");
        tray.EndCase();
        Assert.IsFalse(Owed(tray, 0), "the case ending mid-stroke lands the owed print (BeginCase/EndCase: SettleInk)");

        tray.BeginCase(0);
        denied.Drag.OnBeginDrag(_press);
        denied.Root.position = new Vector3(0.05f, 0.03f, 0.02f);
        denied.Drag.OnEndDrag(_press);
        Assert.AreEqual(StampPress.Stamped, tray.LastPress, "straight down onto the aimed paper (no pointer's ray this carry)");
        Assert.IsTrue(Owed(tray, 1));
        Assert.IsTrue(tray.HandBack(), "the stamped papers handed back mid-stroke");
        Assert.IsFalse(Owed(tray, 1), "the hand-back lands the owed print first: the papers leave with the verdict's ink");
    }

    /// <summary>One dater's parts as the test drives them.</summary>
    private struct Dater
    {
        public Transform Root, Die;
        public DeskDraggable Drag;
        public Vector3 Home;
    }

    /// <summary>A bare tray, live: a rack with a 4 cm brass box (no collider, as built) at the origin and its two daters (3 cm click boxes) at home beside it.</summary>
    private DeskStampTray Tray(out Dater approved, out Dater denied)
    {
        _rack = new GameObject("StampRack");
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Box";
        Object.DestroyImmediate(box.GetComponent<BoxCollider>());
        box.transform.SetParent(_rack.transform, false);
        box.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        box.transform.localScale = new Vector3(0.2f, 0.04f, 0.08f);
        approved = MakeDater("Approved", new Vector3(0.3f, 0.03f, 0f), out Clickable approvedClick);
        denied = MakeDater("Denied", new Vector3(0.35f, 0.03f, 0f), out Clickable deniedClick);

        _host = new GameObject("StampBar");
        DeskStampTray tray = _host.AddComponent<DeskStampTray>();
        var so = new SerializedObject(tray);
        so.FindProperty("rack").objectReferenceValue = _rack.transform;
        so.FindProperty("approvedStamp").objectReferenceValue = approvedClick;
        so.FindProperty("approvedDie").objectReferenceValue = approved.Die;
        so.FindProperty("deniedStamp").objectReferenceValue = deniedClick;
        so.FindProperty("deniedDie").objectReferenceValue = denied.Die;
        so.ApplyModifiedPropertiesWithoutUndo();
        // Edit mode calls no Awake: the tray wires its daters there.
        typeof(DeskStampTray).GetMethod("Awake", Private).Invoke(tray, null);
        tray.SetLive(true, false);
        return tray;
    }

    private Dater MakeDater(string name, Vector3 home, out Clickable click)
    {
        var dater = new GameObject(name, typeof(BoxCollider));
        dater.transform.SetParent(_rack.transform, false);
        dater.transform.localPosition = home;
        var collider = dater.GetComponent<BoxCollider>();
        collider.size = new Vector3(0.03f, 0.06f, 0.03f);
        collider.center = new Vector3(0f, 0.03f, 0f);
        click = dater.AddComponent<Clickable>();
        var die = new GameObject("Die").transform;
        die.SetParent(dater.transform, false);
        return new Dater { Root = dater.transform, Die = die, Drag = dater.AddComponent<DeskDraggable>(), Home = home };
    }

    /// <summary>A paper (an unbound DeskDocument: paper 0, prints nothing) with a box of <paramref name="size"/> at <paramref name="centre"/>.</summary>
    private void Paper(string name, Vector3 centre, Vector3 size)
    {
        var paper = new GameObject(name, typeof(BoxCollider));
        paper.transform.SetParent(_props.transform, false);
        paper.transform.position = centre;
        paper.GetComponent<BoxCollider>().size = size;
        paper.AddComponent<DeskDocument>();
    }

    /// <summary>True while dater <paramref name="index"/> (0 APPROVED, 1 DENIED) owes a print (its StampInk).</summary>
    private static bool Owed(DeskStampTray tray, int index)
    {
        var handles = (System.Array)typeof(DeskStampTray).GetField("_handles", Private).GetValue(tray);
        object handle = handles.GetValue(index);
        return ((StampInk)handle.GetType().GetField("Ink").GetValue(handle)).Owed;
    }
}
