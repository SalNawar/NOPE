using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

/// <summary>
/// FOLD-01 round 4 (Papers, Please: the point pressed on a paper stays under
/// the pointer). G1: the desk surface's clamp holds the pressed point, not the
/// root, so a paper pressed on its near half can hang past the clamp's far
/// edge while it grows at the counter's edge (round 3 clamped the root, and a
/// passport stood still for 11 frames while the pointer moved 43.5 px on). A
/// stamp keeps its root clamp (its carry is DeskStamps'). P1 (the round 4
/// verifier): a paper resting with a part past the clamp area, pressed on that
/// part, does not move at the drag's first follow (the clamp area grows for
/// the drag to hold the point the press met; it jumped 45.8 px). G4: a paper
/// let go while its size still eases keeps growing round the pressed point,
/// not its root (round 3: the pressed point drifted 8.7 px, 39.5 px in the
/// reading view), and the drop is judged where its root lies once the size
/// ends. Each drag runs through DeskDraggable's own begin and move (the press
/// seen by a PhysicsRaycaster on the camera). Makes Unity objects, so it runs
/// in Unity's EditMode suite only (NeedsEditor).
/// </summary>
public class HeldPointTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>The root's offset from the pressed point at the grab (the passport pressed on its closed cover), and how much it grows.</summary>
    private static readonly Vector3 Offset = new Vector3(0f, 0f, 0.0325f);

    private const float Grows = 3.39f;

    /// <summary>The clamp area's far and near edges (DeskSurface, 1.9925 x 1.015 m centred on the origin, as the art office sizes it).</summary>
    private const float FarEdge = 0.5075f, NearEdge = -0.5075f;

    private GameObject _surfaceObject, _cameraObject, _dragObject;
    private DeskSurface _surface;
    private Camera _camera;
    private PhysicsRaycaster _raycaster;
    private DeskDraggable _drag;

    [SetUp]
    public void Build()
    {
        _surfaceObject = new GameObject("Surface");
        _surface = _surfaceObject.AddComponent<DeskSurface>();
        _surface.Configure(new Vector2(1.9925f, 1.015f), Vector2.zero, Vector2.one);
        _cameraObject = new GameObject("Camera");
        _cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3f, 0f), Quaternion.Euler(90f, 0f, 0f));
        _camera = _cameraObject.AddComponent<Camera>();
        _raycaster = _cameraObject.AddComponent<PhysicsRaycaster>();
        _dragObject = new GameObject("Paper");
    }

    [TearDown]
    public void Clear()
    {
        if (_drag != null)
            HoldRegistry.Desk.End(_drag);
        foreach (GameObject go in new[] { _dragObject, _cameraObject, _surfaceObject })
            if (go != null)
                Object.DestroyImmediate(go);
    }

    /// <summary>
    /// Lays the paper's root at <paramref name="root"/>, presses it at
    /// <paramref name="pressed"/> (the press's hit on the paper, as the office
    /// camera's PhysicsRaycaster gives it), grows the grab offset
    /// <paramref name="scale"/> times and moves the pointer over
    /// <paramref name="pointer"/> (the drag's first follow). With
    /// <paramref name="anchor"/> the drag carries that point, as a stamp's die.
    /// </summary>
    private DeskDraggable Drag(Vector3 root, Vector3 pressed, Vector3 pointer, float scale = 1f, Transform anchor = null)
    {
        _dragObject.transform.position = root;
        _drag = _dragObject.GetComponent<DeskDraggable>();
        if (_drag == null)
            _drag = _dragObject.AddComponent<DeskDraggable>();
        _drag.Init(_surface, 0f, anchor);
        var press = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left,
            position = OnScreen(pressed),
            pointerPressRaycast = new RaycastResult { gameObject = _dragObject, module = _raycaster, worldPosition = pressed },
        };
        _drag.OnBeginDrag(press);
        _drag.GrabScale = scale;
        press.position = OnScreen(pointer);
        _drag.OnDrag(press);
        return _drag;
    }

    /// <summary>A drag pressed at <paramref name="pressed"/> with the root <see cref="Offset"/> past it, grown <paramref name="scale"/> times and followed to <paramref name="pointer"/>.</summary>
    private DeskDraggable Follow(Vector3 pressed, Vector3 pointer, float scale) => Drag(pressed + Offset, pressed, pointer, scale);

    /// <summary>Where <paramref name="world"/> lies on the camera's screen (pixels).</summary>
    private Vector2 OnScreen(Vector3 world)
    {
        Vector3 screen = _camera.WorldToScreenPoint(world);
        return new Vector2(screen.x, screen.y);
    }

    [Test, Category("NeedsEditor")]
    public void G1_APaperPressedOnItsNearHalf_HangsPastTheClamp_ThePressedPointUnderThePointer()
    {
        var pointer = new Vector3(-0.2f, 0f, 0.44f);
        DeskDraggable drag = Follow(new Vector3(-0.2f, 0f, 0.43f), pointer, Grows);
        Vector3 root = _dragObject.transform.position;
        Assert.AreEqual(0.44f + Offset.z * Grows, root.z, 1e-4f, "the root goes where the grown offset puts it, 4 cm past the clamp's far edge (round 3 held it at 0.5075)");
        Assert.Less(Vector3.Distance(root - drag.GrabOffset, pointer), 1e-4f, "the pressed point stays under the pointer");
    }

    [Test, Category("NeedsEditor")]
    public void G1_APointerOffTheDesk_StopsThePressedPointAtTheClampsEdge()
    {
        DeskDraggable drag = Follow(new Vector3(-0.2f, 0f, 0.43f), new Vector3(-0.2f, 0f, 0.62f), Grows);
        Vector3 held = _dragObject.transform.position - drag.GrabOffset;
        Assert.AreEqual(FarEdge, held.z, 1e-4f, "the pressed point stops at the clamp's edge; the paper hangs past it by its offset");
    }

    [Test, Category("NeedsEditor")]
    public void G1_AStampCarriedByItsDie_KeepsItsRootClamp()
    {
        var die = new GameObject("Die").transform;
        die.SetParent(_dragObject.transform, false);
        die.localPosition = -Offset;
        Drag(new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, 0.4f) - Offset, new Vector3(0f, 0f, 0.49f), 1f, die);
        Assert.AreEqual(FarEdge, _dragObject.transform.position.z, 1e-4f, "a stamp's root stays clamped to the desk, as before (DeskStamps' carry)");
    }

    /// <summary>
    /// The round 4 verifier's P1: a paper may rest with a part past the clamp
    /// area (the rest area is the clamp grown by the footprint and cut by the
    /// reading view). Pressed on that part, its first follow clamped the
    /// pressed point and it jumped toward the clamp (the passport on day 1,
    /// 45.8 px; the day 12 large form's far part on the counter, up to 15.1
    /// px). The cases: the verifier's e2 (the near band), f1 (the far part on
    /// the counter) and the left band; the sheet lies 14.6 mm over the desk.
    /// </summary>
    [TestCase(0f, -0.3669f, 0f, -0.5673f, 0f, -0.5561f, TestName = "P1_APressOnAPartPastTheClampsNearEdge_MovesThePaperOnlyAsThePointerMoves")]
    [TestCase(0f, 0.4375f, 0f, 0.5413f, 0f, 0.5272f, TestName = "P1_APressOnAPartPastTheClampsFarEdge_MovesThePaperOnlyAsThePointerMoves")]
    [TestCase(-0.9f, 0f, -1.05f, 0f, -1.04f, 0.01f, TestName = "P1_APressOnAPartPastTheClampsLeftEdge_MovesThePaperOnlyAsThePointerMoves")]
    [Category("NeedsEditor")]
    public void P1_APressPastTheClampArea_DoesNotMoveThePaperAtTheFirstFollow(float rootX, float rootZ, float pressX, float pressZ, float pointerX, float pointerZ)
    {
        const float sheet = 0.0146f;
        var root = new Vector3(rootX, 0f, rootZ);
        var pressed = new Vector3(pressX, sheet, pressZ);
        var pointer = new Vector3(pointerX, sheet, pointerZ);
        Drag(root, pressed, pointer);
        Vector3 moved = _dragObject.transform.position - root, asked = pointer - pressed;
        Assert.AreEqual(asked.x, moved.x, 1e-4f, "the paper moves across only as far as the pointer moved");
        Assert.AreEqual(asked.z, moved.z, 1e-4f, "the paper moves along only as far as the pointer moved: no jump toward the clamp area");
    }

    [Test, Category("NeedsEditor")]
    public void P1_APointerGoingFurtherOutThanThePress_StopsThePressedPointWhereItWasPressed()
    {
        var root = new Vector3(0f, 0f, -0.3669f);
        var pressed = new Vector3(0f, 0f, -0.5673f);
        Assert.Less(pressed.z, NearEdge - 0.05f, "the press lies 6 cm past the clamp's near edge");
        Drag(root, pressed, new Vector3(0.05f, 0f, -0.62f));
        Vector3 at = _dragObject.transform.position;
        Assert.AreEqual(root.z, at.z, 1e-4f, "the pressed point stops where it was pressed: the paper goes no further out, and never jumps in");
        Assert.AreEqual(0.05f, at.x, 1e-4f, "it still follows the pointer across");
    }

    [Test, Category("NeedsEditor")]
    public void G4_TheRootADropIsJudgedAt_KeepsThePressedPointUnderThePointerOnceTheSizeEnds()
    {
        var pointer = new Vector3(0.1f, 0f, 0.2f);
        DeskDraggable drag = Follow(new Vector3(0.1f, 0f, 0.19f), pointer, 1.58f);
        Vector3 settled = drag.Regrown(_dragObject.transform.position, Grows);
        Assert.Less(Vector3.Distance(settled, pointer + Offset * Grows), 1e-4f, "the settled root: the pressed point under the pointer at the full size");
    }

    [Test, Category("NeedsEditor")]
    public void G4_APaperLetGoWhileItsSizeEases_GrowsRoundThePressedPoint_NotItsRoot()
    {
        var pointer = new Vector3(0.1f, 0f, 0.2f);
        DeskDraggable drag = Follow(new Vector3(0.1f, 0f, 0.19f), pointer, 1.47f);
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
