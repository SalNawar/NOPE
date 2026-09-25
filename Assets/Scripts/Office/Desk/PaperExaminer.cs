using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Poses the papers held in the hand (piece 10 X1, X2, X9, X10): each held
/// paper's Sheet leaves the desk and turns to face the office camera in its
/// examine slot (ExamineLayout.OfficeSlot, low beside the screen's centre),
/// dipped under the open wheel, or beside the open PC frame
/// (ExamineLayout.InRegion) with the frame's examine hole sized over it so its
/// rows stay clickable; where the region cannot hold it, it waits behind the
/// frame with no hole (the frame's surround takes the click). The pose is in
/// the camera's space at a distance kept out of the near clip and above the
/// desk (ExamineLayout.SafeDistance, the desk plane from DeskSurface). The
/// paper's root stays where it lay, so a put-back lands there. Rises and
/// returns are eased (ExamineLayout.Ease); it works in LateUpdate only while a
/// pose moves, and re-poses when the screen's size, the camera's pose or its
/// field of view, or the open frame's edge changes. DeskController holds and
/// releases; BoothCoordinator sets the mode; the office binder hands it the
/// camera.
/// </summary>
public sealed class PaperExaminer : MonoBehaviour
{
    /// <summary>The desk tuning (the examine knobs, the paper's size).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The desk plane (a held paper never dips into it).</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The PC frame (its right edge bounds the region; its examine hole lets clicks through to the papers).</summary>
    [SerializeField] private PcFrame frame;

    /// <summary>One paper in the hand, or on its way back.</summary>
    private sealed class Entry
    {
        public DeskDocument Paper;
        public ExamineSlot Slot;
        public bool Releasing;
        public Action Landed;
        public Vector3 RestPosition;
        public Quaternion RestRotation;
        public Vector3 RestScale;
        public Vector3 FromPosition;
        public Quaternion FromRotation;
        public Vector3 FromScale;
        public float Elapsed;
        public bool Moving;
    }

    private readonly List<Entry> _entries = new List<Entry>();
    private Camera _camera;
    private bool _frameOpen;
    private bool _dipped;
    private bool _holeOpen;

    // What the poses were computed for (a change re-poses).
    private int _screenWidth;
    private int _screenHeight;
    private Vector3 _cameraPosition;
    private Quaternion _cameraRotation;
    private float _cameraFov;
    private float _frameEdge;

    /// <summary>The office camera the papers are posed in front of (the office binder's, from the art office).</summary>
    public void SetCamera(Camera office) => _camera = office;

    /// <summary>True when a world point is right of the screen's centre through the office camera (a paper there prefers the right slot).</summary>
    public bool RightOfCentre(Vector3 world) => _camera != null && _camera.WorldToViewportPoint(world).x > 0.5f;

    /// <summary>True when the office slot on that side (not dipped) would cover a world point on the screen (a paper lying there would be hidden).</summary>
    public bool SlotCovers(bool right, Vector3 world) =>
        _camera != null && config != null && Covers(ExamineLayout.OfficeSlot(right, PaperAspect, ScreenAspect, false, config.examine), world);

    /// <summary>A world quad's rectangle on the screen through the office camera, in pixels (empty without a camera or with a corner behind it): where a paper lying there shows.</summary>
    public ScreenRect ScreenRectOf(IReadOnlyList<Vector3> corners)
    {
        if (_camera == null || corners == null || corners.Count == 0)
            return default;

        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
        foreach (Vector3 corner in corners)
        {
            Vector3 s = _camera.WorldToScreenPoint(corner);
            if (s.z <= 0f)
                return default;
            xMin = Mathf.Min(xMin, s.x);
            yMin = Mathf.Min(yMin, s.y);
            xMax = Mathf.Max(xMax, s.x);
            yMax = Mathf.Max(yMax, s.y);
        }
        return new ScreenRect(xMin, yMin, xMax, yMax);
    }

    /// <summary>Where a held paper can sit in the office, in pixels: its office slot raised and dipped under the wheel, together (a paper on the desk hidden there now or once the wheel closes); empty for a paper that is not held, or on its way back.</summary>
    public ScreenRect HeldPlaces(DeskDocument paper)
    {
        Entry entry = Find(paper);
        if (entry == null || entry.Releasing || config == null)
            return default;

        bool right = entry.Slot == ExamineSlot.Right;
        return ScreenRect.Enclosing(Pixels(ExamineLayout.OfficeSlot(right, PaperAspect, ScreenAspect, false, config.examine)),
                                    Pixels(ExamineLayout.OfficeSlot(right, PaperAspect, ScreenAspect, true, config.examine)));
    }

    /// <summary>A box in screen heights as a rectangle in pixels.</summary>
    private ScreenRect Pixels(ScreenBox box)
    {
        float h = Screen.height, halfWidth = Screen.width / 2f, w = box.Height * PaperAspect;
        return new ScreenRect(halfWidth + (box.CentreX - w / 2f) * h, (box.CentreY - box.Height / 2f) * h,
                              halfWidth + (box.CentreX + w / 2f) * h, (box.CentreY + box.Height / 2f) * h);
    }

    /// <summary>Takes a paper into the hand at <paramref name="slot"/>: its sheet rises to the slot (a paper on its way back turns round).</summary>
    public void Hold(DeskDocument paper, ExamineSlot slot)
    {
        if (paper == null || paper.Sheet == null)
            return;

        Entry entry = Find(paper);
        if (entry == null)
        {
            Transform sheet = paper.Sheet;
            entry = new Entry
            {
                Paper = paper,
                RestPosition = sheet.localPosition,
                RestRotation = sheet.localRotation,
                RestScale = sheet.localScale
            };
            _entries.Add(entry);
        }

        entry.Slot = slot;
        entry.Releasing = false;
        entry.Landed = null;
        StartMoving(entry);
        RetargetHeld();
    }

    /// <summary>
    /// Sends a held paper back to where it lies on the desk (at once when
    /// <paramref name="instant"/>), then calls <paramref name="landed"/>; a
    /// paper that is not held lands at once.
    /// </summary>
    public void Release(DeskDocument paper, bool instant, Action landed)
    {
        Entry entry = Find(paper);
        if (entry == null)
        {
            landed?.Invoke();
            return;
        }

        entry.Releasing = true;
        entry.Landed = landed;
        if (instant || _camera == null || config == null)
            Land(entry);
        else
            StartMoving(entry);
        RetargetHeld();
    }

    /// <summary>Where held papers sit: beside the open frame (<paramref name="frameOpen"/>), else in their office slots, dipped under the open wheel (<paramref name="dipped"/>).</summary>
    public void SetMode(bool frameOpen, bool dipped)
    {
        if (frameOpen == _frameOpen && dipped == _dipped)
            return;

        _frameOpen = frameOpen;
        _dipped = dipped;
        RetargetHeld();
    }

    /// <summary>Drops every paper at once, back on its rest pose, with no callbacks (the case ends).</summary>
    public void Clear()
    {
        foreach (Entry entry in _entries)
            if (entry.Paper != null && entry.Paper.Sheet != null)
                Rest(entry);
        _entries.Clear();
        UpdateHole();
    }

    /// <summary>Only while a paper is held or on its way back: re-poses after a screen or camera change, moves the moving poses and keeps the frame's hole over the papers.</summary>
    private void LateUpdate()
    {
        if (_entries.Count == 0)
        {
            if (_holeOpen)
                UpdateHole();
            return;
        }
        if (_camera == null || config == null)
            return;

        if (ViewChanged())
            foreach (Entry entry in _entries)
                if (!entry.Moving && !entry.Releasing)
                    Snap(entry);

        float seconds = config.examine.seconds;
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i];
            if (entry.Paper == null || entry.Paper.Sheet == null)
            {
                _entries.RemoveAt(i);
                continue;
            }
            if (!entry.Moving)
                continue;

            entry.Elapsed += Time.deltaTime;
            float t = seconds > 0f ? Mathf.Clamp01(entry.Elapsed / seconds) : 1f;
            if (entry.Releasing && t >= 1f)
            {
                Land(entry);
                continue;
            }

            (Vector3 position, Quaternion rotation, Vector3 scale) = entry.Releasing ? RestPose(entry) : Target(entry);
            float e = ExamineLayout.Ease(t);
            Transform sheet = entry.Paper.Sheet;
            sheet.SetPositionAndRotation(Vector3.Lerp(entry.FromPosition, position, e), Quaternion.Slerp(entry.FromRotation, rotation, e));
            sheet.localScale = Vector3.Lerp(entry.FromScale, scale, e);
            entry.Moving = t < 1f;
        }

        UpdateHole();
    }

    /// <summary>Every held paper moves from where it is to its (new) target.</summary>
    private void RetargetHeld()
    {
        foreach (Entry entry in _entries)
            if (!entry.Releasing)
                StartMoving(entry);
        UpdateHole();
    }

    /// <summary>Starts a move from the sheet's current pose.</summary>
    private static void StartMoving(Entry entry)
    {
        Transform sheet = entry.Paper.Sheet;
        entry.FromPosition = sheet.position;
        entry.FromRotation = sheet.rotation;
        entry.FromScale = sheet.localScale;
        entry.Elapsed = 0f;
        entry.Moving = true;
    }

    /// <summary>Puts a held paper on its target at once (a screen or camera change).</summary>
    private void Snap(Entry entry)
    {
        (Vector3 position, Quaternion rotation, Vector3 scale) = Target(entry);
        entry.Paper.Sheet.SetPositionAndRotation(position, rotation);
        entry.Paper.Sheet.localScale = scale;
    }

    /// <summary>A returning paper is back: its sheet on its rest pose, the entry gone, its callback called.</summary>
    private void Land(Entry entry)
    {
        Rest(entry);
        _entries.Remove(entry);
        UpdateHole();
        Action landed = entry.Landed;
        entry.Landed = null;
        landed?.Invoke();
    }

    /// <summary>The sheet back on the pose it had on the desk.</summary>
    private static void Rest(Entry entry)
    {
        Transform sheet = entry.Paper.Sheet;
        sheet.localPosition = entry.RestPosition;
        sheet.localRotation = entry.RestRotation;
        sheet.localScale = entry.RestScale;
    }

    /// <summary>The rest pose in world space (position, rotation) and its local scale.</summary>
    private static (Vector3, Quaternion, Vector3) RestPose(Entry entry)
    {
        Transform parent = entry.Paper.Sheet.parent;
        return parent != null
            ? (parent.TransformPoint(entry.RestPosition), parent.rotation * entry.RestRotation, entry.RestScale)
            : (entry.RestPosition, entry.RestRotation, entry.RestScale);
    }

    /// <summary>A held paper's target: its box (the office slot, or its place beside the open frame) at a safe distance, facing the camera, in world space, and its local scale.</summary>
    private (Vector3, Quaternion, Vector3) Target(Entry entry)
    {
        ScreenBox box = BoxOf(entry);
        ExamineTuning t = config.examine;
        float paperAspect = PaperAspect;
        Transform cam = _camera.transform;
        Vector3 normal = surface != null ? surface.transform.up : Vector3.up;
        float heightAboveDesk = surface != null ? Vector3.Dot(cam.position - surface.transform.position, normal) : 1f;
        float distance = ExamineLayout.SafeDistance(box, paperAspect, _camera.fieldOfView, _camera.nearClipPlane, heightAboveDesk,
                                                    Vector3.Dot(normal, cam.right), Vector3.Dot(normal, cam.up), Vector3.Dot(normal, cam.forward), t);
        (float x, float y, float z, float height) = ExamineLayout.Pose(box, _camera.fieldOfView, distance);

        Vector3 position = cam.position + cam.rotation * new Vector3(x, y, z);
        Quaternion rotation = cam.rotation * Quaternion.Euler(0f, 0f, entry.Slot == ExamineSlot.Left ? -t.roll : t.roll);
        Transform parent = entry.Paper.Sheet.parent;
        float parentScale = parent != null && parent.lossyScale.x > 0f ? parent.lossyScale.x : 1f;
        return (position, rotation, Vector3.one * (height / config.paperSize.y / parentScale));
    }

    /// <summary>A held paper's box on the screen: beside the open frame when the papers fit there, else its office slot (dipped under the wheel).</summary>
    private ScreenBox BoxOf(Entry entry) => BoxOf(entry, out _);

    /// <summary>A held paper's box on the screen, and whether it sits beside the open frame (<paramref name="beside"/>) or in its office slot.</summary>
    private ScreenBox BoxOf(Entry entry, out bool beside)
    {
        beside = false;
        if (_frameOpen && TryRegion(out float left, out float right))
        {
            (int index, int count) = PlaceAmongHeld(entry);
            ScreenBox inRegion = ExamineLayout.InRegion(index, count, left, right, PaperAspect, config.examine, out bool fits);
            if (fits)
            {
                beside = true;
                return inRegion;
            }
        }

        return ExamineLayout.OfficeSlot(entry.Slot == ExamineSlot.Right, PaperAspect, ScreenAspect, _dipped, config.examine);
    }

    /// <summary>The region right of the frame, in screen heights from the screen's centre; false without a frame.</summary>
    private bool TryRegion(out float left, out float right)
    {
        left = right = 0f;
        if (frame == null || Screen.height <= 0)
            return false;
        float h = Screen.height;
        left = (frame.RightEdgePixels - Screen.width / 2f) / h;
        right = Screen.width / 2f / h;
        return true;
    }

    /// <summary>A held paper's place among the held papers (left slot first) and their count.</summary>
    private (int index, int count) PlaceAmongHeld(Entry entry)
    {
        int count = 0, index = 0;
        foreach (Entry e in _entries)
        {
            if (e.Releasing)
                continue;
            if (e != entry && e.Slot == ExamineSlot.Left && entry.Slot == ExamineSlot.Right)
                index++;
            count++;
        }
        return (index, count);
    }

    /// <summary>Opens the frame's examine hole over the held papers while they sit beside the open frame, else closes it.</summary>
    private void UpdateHole()
    {
        if (frame == null || config == null)
            return;

        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
        bool any = false;
        if (_frameOpen)
        {
            float h = Screen.height, halfWidth = Screen.width / 2f;
            foreach (Entry entry in _entries)
            {
                if (entry.Releasing)
                    continue;
                ScreenBox box = BoxOf(entry, out bool beside);
                if (!beside)
                    continue; // the papers do not fit beside the frame: they wait behind it
                float w = box.Height * PaperAspect;
                xMin = Mathf.Min(xMin, halfWidth + (box.CentreX - w / 2f) * h);
                xMax = Mathf.Max(xMax, halfWidth + (box.CentreX + w / 2f) * h);
                yMin = Mathf.Min(yMin, (box.CentreY - box.Height / 2f) * h);
                yMax = Mathf.Max(yMax, (box.CentreY + box.Height / 2f) * h);
                any = true;
            }
        }

        if (any)
            frame.SetExamineHole(xMin, yMin, xMax, yMax);
        else if (_holeOpen)
            frame.SetExamineHole(0f, 0f, 0f, 0f);
        _holeOpen = any;
    }

    /// <summary>
    /// True (and remembered) when the screen's size, the camera's pose or field
    /// of view, or (while the frame is open) the frame's right edge changed
    /// since the last call (the overlay's scaler can follow a new screen size a
    /// frame late, so the region beside the frame is re-read until it settles).
    /// </summary>
    private bool ViewChanged()
    {
        Transform cam = _camera.transform;
        float edge = _frameOpen && frame != null ? frame.RightEdgePixels : 0f;
        if (Screen.width == _screenWidth && Screen.height == _screenHeight && cam.position == _cameraPosition &&
            cam.rotation == _cameraRotation && Mathf.Approximately(_camera.fieldOfView, _cameraFov) && Mathf.Approximately(edge, _frameEdge))
            return false;

        _frameEdge = edge;
        _screenWidth = Screen.width;
        _screenHeight = Screen.height;
        _cameraPosition = cam.position;
        _cameraRotation = cam.rotation;
        _cameraFov = _camera.fieldOfView;
        return true;
    }

    /// <summary>True when a box covers a world point's projection through the office camera (a point behind the camera is never covered).</summary>
    private bool Covers(ScreenBox box, Vector3 world)
    {
        Vector3 screen = _camera.WorldToScreenPoint(world);
        if (screen.z <= 0f || Screen.height <= 0)
            return false;
        float h = Screen.height, halfWidth = Screen.width / 2f, w = box.Height * PaperAspect;
        float x = (screen.x - halfWidth) / h, y = screen.y / h;
        return Mathf.Abs(x - box.CentreX) <= w / 2f && Mathf.Abs(y - box.CentreY) <= box.Height / 2f;
    }

    private Entry Find(DeskDocument paper)
    {
        foreach (Entry entry in _entries)
            if (entry.Paper == paper)
                return entry;
        return null;
    }

    private float PaperAspect => config.paperSize.x / config.paperSize.y;

    private static float ScreenAspect => Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
}
