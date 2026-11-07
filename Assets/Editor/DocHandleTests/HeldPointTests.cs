using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// FOLD-01 round 4 (Papers, Please: the point pressed on a paper stays under
/// the pointer). G1: the desk surface's clamp holds the pressed point, not the
/// root, so a paper pressed on its near half can hang past the clamp's far
/// edge while it grows at the counter's edge (round 3 clamped the root, and a
/// passport stood still for 11 frames while the pointer moved 43.5 px on). A
/// stamp keeps its root clamp (its carry is DeskStamps'). G4: a paper let go
/// while its size still eases keeps growing round the pressed point, not its
/// root (round 3: the pressed point drifted 8.7 px, 39.5 px in the reading
/// view), and the drop is judged where its root lies once the size ends.
/// Makes Unity objects, so it runs in Unity's EditMode suite only
/// (NeedsEditor).
/// </summary>
public class HeldPointTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>The root's offset from the pressed point at the grab (the passport pressed on its closed cover), and how much it grows.</summary>
    private static readonly Vector3 Offset = new Vector3(0f, 0f, 0.0325f);

    private const float Grows = 3.39f;

    private GameObject _surfaceObject, _cameraObject, _dragObject;
    private DeskSurface _surface;
    private Camera _camera;

    [SetUp]
    public void Build()
    {
        _surfaceObject = new GameObject("Surface");
        _surface = _surfaceObject.AddComponent<DeskSurface>();
        _surface.Configure(new Vector2(1.9925f, 1.015f), Vector2.zero, Vector2.one); // the clamp's far edge at z 0.5075
        _cameraObject = new GameObject("Camera");
        _cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3f, 0f), Quaternion.Euler(90f, 0f, 0f));
        _camera = _cameraObject.AddComponent<Camera>();
        _dragObject = new GameObject("Paper");
    }

    [TearDown]
    public void Clear()
    {
        foreach (GameObject go in new[] { _dragObject, _cameraObject, _surfaceObject })
            if (go != null)
                Object.DestroyImmediate(go);
    }

    /// <summary>A drag held by its pressed point (no anchor), mid-drag, its grab offset <see cref="Offset"/> grown <paramref name="scale"/> times, followed to the pointer over <paramref name="pointer"/>.</summary>
    private DeskDraggable Follow(Vector3 pointer, float scale, Transform anchor = null)
    {
        DeskDraggable drag = _dragObject.GetComponent<DeskDraggable>();
        if (drag == null)
            drag = _dragObject.AddComponent<DeskDraggable>();
        drag.Init(_surface, 0f, anchor);
        typeof(DeskDraggable).GetField("_dragging", Private).SetValue(drag, true);
        typeof(DeskDraggable).GetField("_camera", Private).SetValue(drag, _camera);
        typeof(DeskDraggable).GetField("_grabOffset", Private).SetValue(drag, Offset);
        Vector3 screen = _camera.WorldToScreenPoint(pointer);
        typeof(DeskDraggable).GetField("_pointer", Private).SetValue(drag, new Vector2(screen.x, screen.y));
        drag.GrabScale = scale;
        typeof(DeskDraggable).GetMethod("Follow", Private).Invoke(drag, null);
        return drag;
    }

    [Test, Category("NeedsEditor")]
    public void G1_APaperPressedOnItsNearHalf_HangsPastTheClamp_ThePressedPointUnderThePointer()
    {
        var pointer = new Vector3(-0.2f, 0f, 0.44f);
        DeskDraggable drag = Follow(pointer, Grows);
        Vector3 root = _dragObject.transform.position;
        Assert.AreEqual(0.44f + Offset.z * Grows, root.z, 1e-4f, "the root goes where the grown offset puts it, 4 cm past the clamp's far edge (round 3 held it at 0.5075)");
        Assert.Less(Vector3.Distance(root - drag.GrabOffset, pointer), 1e-4f, "the pressed point stays under the pointer");
    }

    [Test, Category("NeedsEditor")]
    public void G1_APointerOffTheDesk_StopsThePressedPointAtTheClampsEdge()
    {
        DeskDraggable drag = Follow(new Vector3(-0.2f, 0f, 0.62f), Grows);
        Vector3 held = _dragObject.transform.position - drag.GrabOffset;
        Assert.AreEqual(0.5075f, held.z, 1e-4f, "the pressed point stops at the clamp's edge; the paper hangs past it by its offset");
    }

    [Test, Category("NeedsEditor")]
    public void G1_AStampCarriedByItsDie_KeepsItsRootClamp()
    {
        var die = new GameObject("Die").transform;
        die.SetParent(_dragObject.transform, false);
        Follow(new Vector3(0f, 0f, 0.49f), 1f, die);
        Assert.AreEqual(0.5075f, _dragObject.transform.position.z, 1e-4f, "a stamp's root stays clamped to the desk, as before (DeskStamps' carry)");
    }

    [Test, Category("NeedsEditor")]
    public void G4_TheRootADropIsJudgedAt_KeepsThePressedPointUnderThePointerOnceTheSizeEnds()
    {
        var pointer = new Vector3(0.1f, 0f, 0.2f);
        DeskDraggable drag = Follow(pointer, 1.58f);
        Vector3 settled = drag.Regrown(_dragObject.transform.position, Grows);
        Assert.Less(Vector3.Distance(settled, pointer + Offset * Grows), 1e-4f, "the settled root: the pressed point under the pointer at the full size");
    }

    [Test, Category("NeedsEditor")]
    public void G4_APaperLetGoWhileItsSizeEases_GrowsRoundThePressedPoint_NotItsRoot()
    {
        var pointer = new Vector3(0.1f, 0f, 0.2f);
        DeskDraggable drag = Follow(pointer, 1.47f);
        typeof(DeskDraggable).GetField("_dragging", Private).SetValue(drag, false); // let go after one frame of the ease (the verifier's c0, P-FLICK)
        DeskDocument paper = _dragObject.AddComponent<DeskDocument>();
        typeof(DeskDocument).GetField("drag", Private).SetValue(paper, drag);
        const float atGrab = 0.3f;
        SetSize(paper, atGrab, atGrab * 1.47f, atGrab * Grows);
        Assert.Less(Vector3.Distance(paper.SettledRoot, pointer + Offset * Grows), 1e-4f, "the drop is judged where the root lies once the size ends");

        paper.GrowRoundHeldPoint();
        foreach (float ratio in new[] { 2.5f, 3.1f, Grows })
        {
            typeof(DeskDocument).GetField("_sizeNow", Private).SetValue(paper, atGrab * ratio);
            typeof(DeskDocument).GetMethod("KeepHeldPointWhileGrowing", Private).Invoke(paper, null);
            Vector3 held = _dragObject.transform.position - Offset * ratio;
            Assert.Less(Vector3.Distance(held, pointer), 1e-4f, $"at {ratio} times its grab size the pressed point is still where the pointer let it go");
        }
        Vector3 rest = _dragObject.transform.position;
        Assert.Less(Vector3.Distance(rest, pointer + Offset * Grows), 1e-4f, "it ends at the root the drop was judged at");

        typeof(DeskDocument).GetField("_sizeNow", Private).SetValue(paper, atGrab * 2f);
        typeof(DeskDocument).GetMethod("KeepHeldPointWhileGrowing", Private).Invoke(paper, null);
        Assert.AreEqual(rest, _dragObject.transform.position, "once the ease has ended, a later change of size does not move it");
    }

    /// <summary>Sets the paper's size at the grab, its size now and the size it eases to (DeskDocument's sheet scales).</summary>
    private static void SetSize(DeskDocument paper, float atGrab, float now, float target)
    {
        typeof(DeskDocument).GetField("_sizeAtGrab", Private).SetValue(paper, atGrab);
        typeof(DeskDocument).GetField("_sizeNow", Private).SetValue(paper, now);
        typeof(DeskDocument).GetField("_sizeTarget", Private).SetValue(paper, target);
    }
}
