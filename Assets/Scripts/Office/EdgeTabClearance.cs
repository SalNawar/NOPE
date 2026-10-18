using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Keeps the office's edge pull tabs (the PC tab at the left edge, which reads
/// DESK while the PC frame is open, and the CITY tab above it) clear of the
/// calendar on the corkboard (Saleh 2026-10-18: "the pc ui label is over the
/// calander"; at 16:9 the PC tab hid the date card's DAY line). Each frame it
/// projects the calendar's click box through the office view's own camera
/// pose (the office Cinemachine camera, never the reading view's tilt or a
/// blend, so a tab does not wander while the camera moves) and moves each tab
/// that shares the calendar's columns up or down just clear of it
/// (ScreenMapping.ClearShift, DeskConfigSO.edgeTabCalendarClearance), never
/// off the screen; at any aspect where nothing overlaps, the tabs stay where
/// the builder put them. The move is applied as a change of the tab's place,
/// so the kit's pull-out on hover and the appear motion keep working.
/// OfficeSceneBinder binds it to the art office's cameras.
/// </summary>
[DefaultExecutionOrder(-10)]
public sealed class EdgeTabClearance : MonoBehaviour
{
    /// <summary>The desk tuning (the clearance kept round the calendar).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The edge pull tabs kept clear (the PC tab, the CITY tab).</summary>
    [SerializeField] private RectTransform[] tabs = new RectTransform[0];

    /// <summary>The calendar prop's click box (its collider's bounds are what the tabs keep clear of).</summary>
    [SerializeField] private Transform calendar;

    private Camera _office;
    private CinemachineCamera _view;
    private float[] _applied = new float[0];
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>The office camera and the office view's Cinemachine camera (null: the office camera's own pose), from the office binder.</summary>
    public void Bind(Camera office, CinemachineCamera view)
    {
        _office = office;
        _view = view;
    }

    private void LateUpdate()
    {
        if (tabs == null)
            return;
        if (_applied.Length != tabs.Length)
            _applied = new float[tabs.Length];

        bool found = TryCalendarRect(out Rect cal);
        float margin = config != null ? config.edgeTabCalendarClearance : 0f;
        for (int i = 0; i < tabs.Length; i++)
        {
            RectTransform tab = tabs[i];
            if (tab == null || !tab.gameObject.activeInHierarchy)
                continue;
            Canvas canvas = tab.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            if (scale <= 0f)
                continue;

            float want = 0f;
            if (found)
            {
                tab.GetWorldCorners(_corners); // an overlay canvas: its world space is the screen's pixels
                float shift = _applied[i] * scale;
                float x0 = _corners[0].x, x1 = _corners[2].x, y0 = _corners[0].y - shift, y1 = _corners[2].y - shift;
                if (x1 > cal.xMin - margin * scale && x0 < cal.xMax + margin * scale)
                    want = ScreenMapping.ClearShift(y0, y1, cal.yMin, cal.yMax, margin * scale, 0f, Screen.height) / scale;
            }
            if (Mathf.Abs(want - _applied[i]) < 0.01f)
                continue;
            tab.anchoredPosition += new Vector2(0f, want - _applied[i]);
            _applied[i] = want;
        }
    }

    /// <summary>The calendar's click box on the screen (pixels from the bottom left) as the office view shows it; false when there is none, it is off, or a corner lies behind the view.</summary>
    private bool TryCalendarRect(out Rect rect)
    {
        rect = default;
        if (_office == null || calendar == null || !calendar.gameObject.activeInHierarchy || !calendar.TryGetComponent(out Collider box) || Screen.width <= 0 || Screen.height <= 0)
            return false;

        Matrix4x4 viewProjection;
        if (_view != null)
        {
            LensSettings lens = _view.Lens;
            float aspect = (float)Screen.width / Screen.height;
            Matrix4x4 projection = lens.Orthographic
                ? Matrix4x4.Ortho(-lens.OrthographicSize * aspect, lens.OrthographicSize * aspect, -lens.OrthographicSize, lens.OrthographicSize, lens.NearClipPlane, lens.FarClipPlane)
                : Matrix4x4.Perspective(lens.FieldOfView, aspect, lens.NearClipPlane, lens.FarClipPlane);
            Transform t = _view.transform;
            viewProjection = projection * Matrix4x4.TRS(t.position, t.rotation, new Vector3(1f, 1f, -1f)).inverse;
        }
        else
        {
            viewProjection = _office.projectionMatrix * _office.worldToCameraMatrix;
        }

        Bounds b = box.bounds;
        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3(b.center.x + ((i & 1) == 0 ? -b.extents.x : b.extents.x),
                                     b.center.y + ((i & 2) == 0 ? -b.extents.y : b.extents.y),
                                     b.center.z + ((i & 4) == 0 ? -b.extents.z : b.extents.z));
            Vector4 clip = viewProjection * new Vector4(corner.x, corner.y, corner.z, 1f);
            if (clip.w <= 1e-5f)
                return false;
            float x = (clip.x / clip.w * 0.5f + 0.5f) * Screen.width, y = (clip.y / clip.w * 0.5f + 0.5f) * Screen.height;
            xMin = Mathf.Min(xMin, x);
            xMax = Mathf.Max(xMax, x);
            yMin = Mathf.Min(yMin, y);
            yMax = Mathf.Max(yMax, y);
        }
        rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        return true;
    }
}
