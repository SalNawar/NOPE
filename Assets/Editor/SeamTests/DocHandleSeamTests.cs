using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DOC-HANDLE's seams and Unity-side rules: each DeskEvents Raise* reaches the
/// event's listeners with its arguments; a desk drag holds through the hold
/// registry and the registry's cancel cancels it (BUG-55); a carry that is
/// replaced or stopped runs its cut callback, never its landing (BUG-24: the
/// scanner's lid was left open); the chip reader lays a passport upright to
/// the player and measures its pad along the view's axes (BUG-24). Tests in
/// the "NeedsEditor" category create Unity objects and run only inside Unity
/// (the Unity pass: docs/pending/DOC-HANDLE.md).
/// </summary>
public class DocHandleSeamTests
{
    [Test]
    public void DeskEvents_EachRaiseReachesItsListeners()
    {
        int landed = 0, ended = 0, rank = -1;
        string upgrade = null;
        Action<DeskDocument> onLanded = paper => landed++;
        Action onEnded = () => ended++;
        Action<string> onUpgrade = id => upgrade = id;
        Action<DeskDocument, int> onStack = (paper, r) => rank = r;
        DeskEvents.DocumentLanded += onLanded;
        DeskEvents.CaseEnded += onEnded;
        DeskEvents.UpgradeArrived += onUpgrade;
        DeskEvents.StackChanged += onStack;
        try
        {
            DeskEvents.RaiseDocumentLanded(null);
            DeskEvents.RaiseCaseEnded();
            DeskEvents.RaiseUpgradeArrived("seam.upgrade");
            DeskEvents.RaiseStackChanged(null, 2);
            Assert.AreEqual(1, landed);
            Assert.AreEqual(1, ended);
            Assert.AreEqual("seam.upgrade", upgrade);
            Assert.AreEqual(2, rank);
        }
        finally
        {
            DeskEvents.DocumentLanded -= onLanded;
            DeskEvents.CaseEnded -= onEnded;
            DeskEvents.UpgradeArrived -= onUpgrade;
            DeskEvents.StackChanged -= onStack;
        }

        Assert.DoesNotThrow(() =>
        {
            DeskEvents.RaiseDocumentLanded(null);
            DeskEvents.RaiseCaseEnded();
            DeskEvents.RaiseUpgradeArrived(null);
            DeskEvents.RaiseStackChanged(null, 0);
        }, "raised with no listener");
        Assert.AreEqual(1, landed, "a removed listener hears nothing");
    }

    [Test, Category("NeedsEditor")]
    public void ADeskDrag_HoldsThroughTheRegistry_AndTheRegistrysCancelCancelsIt()
    {
        var go = new GameObject("DocHandle_Drag");
        try
        {
            DeskDraggable drag = go.AddComponent<DeskDraggable>();
            int cancelled = 0;
            drag.DragCancelled += _ => cancelled++;
            drag.OnBeginDrag(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            Assert.IsTrue(drag.IsDragging);
            Assert.AreSame(drag, HoldRegistry.Desk.Current, "a drag is the desk's hold");
            Assert.IsTrue(HoldRegistry.Desk.IsHolding, "CAM and DOC-LOOK read it");

            HoldRegistry.Desk.Cancel(); // Esc, a view key, the focus lost
            Assert.IsFalse(drag.IsDragging);
            Assert.AreEqual(1, cancelled, "the owner puts it back where it was picked up");
            Assert.IsFalse(HoldRegistry.Desk.IsHolding);

            drag.OnBeginDrag(new PointerEventData(null) { button = PointerEventData.InputButton.Right });
            Assert.IsFalse(drag.IsDragging, "only the left button drags (and holds)");
        }
        finally
        {
            HoldRegistry.Desk.Cancel();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test, Category("NeedsEditor")]
    public void ACarry_ReplacedOrStopped_RunsItsCut_NeverItsLanding()
    {
        var go = new GameObject("DocHandle_Carry");
        try
        {
            var carry = new DeskCarry();
            int done = 0, cut = 0;
            carry.Start(go.transform, new[] { Vector3.right }, Quaternion.identity, 0.3f, () => done++, () => cut++);
            carry.Start(go.transform, new[] { Vector3.left }, Quaternion.identity, 0.3f, () => done++, () => cut++);
            Assert.AreEqual(1, cut, "the replaced carry let go of its machine (the scanner's lid)");
            Assert.AreEqual(0, done, "it never landed");
            carry.Stop();
            Assert.AreEqual(2, cut, "stopped: the folder grabbed mid-carry");
            Assert.IsFalse(carry.Running);
            carry.Stop();
            Assert.AreEqual(2, cut, "nothing runs: nothing to cut");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test, Category("NeedsEditor")]
    public void TheChipReader_LaysAPassportUprightToThePlayer_AndMeasuresItsPad()
    {
        var go = new GameObject("DocHandle_Reader");
        try
        {
            ChipReader reader = go.AddComponent<ChipReader>();
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // the reader turned a quarter on the desk
            reader.FaceThePlayer(Vector3.forward);
            Quaternion onPad = reader.DockFacing(MachineDock.Pad);
            Assert.Less(Quaternion.Angle(onPad, Quaternion.LookRotation(Vector3.forward, Vector3.up)), 0.01f, "upright to the player, not along the slot");
            Vector2 pad = reader.PadExtents(Vector3.right, Vector3.forward);
            Assert.AreEqual(0.092f, pad.x, 1e-4f, "turned a quarter, the pad's depth runs across the view");
            Assert.AreEqual(0.12f, pad.y, 1e-4f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
