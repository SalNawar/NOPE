using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The microprint as the scan view draws it (FIX_PLAN_v2 TOOLS, BUG-2b; Saleh
/// D5: "the scanner's hidden-layer view reveals the microprint"): TOOLS'
/// content and readability rule for the view UI-PC builds (ScanLayerSwitch,
/// FIX_PLAN_v2 #13). <see cref="Print"/> writes a paper's line into a text of
/// the hidden layer at the size whose capitals reach ScanMicroprint's cap
/// where that text is drawn; <see cref="CapPx"/> measures the capitals as
/// drawn and seen whole, which MicroLens.Readable credits (never the
/// geometry). TOOLS' QA op checks it against an independent measure, the
/// line's ink in a screenshot (tools/qa/scenarios/tools_microprint.json).
/// </summary>
public static class MicroprintView
{
    /// <summary>
    /// Prints <paramref name="line"/> (form <paramref name="formNumber"/>) into
    /// <paramref name="text"/> on one line, at the size whose capitals reach
    /// ScanMicroprint.MinCapPx × Headroom on the screen where the text is
    /// drawn now (its canvas's camera; auto-size off, so nothing shrinks it).
    /// </summary>
    public static void Print(TMP_Text text, MicroLine line, string formNumber)
    {
        if (text == null)
            return;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.fontSize = ScanMicroprint.FontSize(CapShare(text), PixelsPerUnit(text));
        text.text = ScanMicroprint.Text(line, formNumber);
    }

    /// <summary>
    /// The height in screen pixels of <paramref name="text"/>'s capitals (and
    /// digits) as last drawn: the tallest such glyph of its laid-out text,
    /// carried to the screen through its transform and its canvas's camera.
    /// 0 when the player cannot see the whole line: the text is not drawn
    /// (inactive, its canvas off, its camera off or drawing into a texture),
    /// any of its visible glyphs lies partly off the screen (its camera's
    /// viewport) or outside a clip of its parents (a RectMask2D or Mask, a
    /// scroll view's viewport), or a visible graphic of another window or
    /// panel is over any glyph's centre (the event system's top UI hit there);
    /// also 0 when it shows no capital or digit. Call it after the text has
    /// been drawn (the frame after Print); it raycasts once per glyph, so call
    /// it until the read is credited, not every frame after.
    /// </summary>
    public static float CapPx(TMP_Text text)
    {
        if (text == null || !text.isActiveAndEnabled || !Drawn(text, out Camera cam) || (cam != null && cam.targetTexture != null))
            return 0f;
        TMP_TextInfo info = text.textInfo;
        if (info == null)
            return 0f;
        Rect view = cam != null ? cam.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
        Transform t = text.transform;
        Transform panel = PanelOf(t);
        float best = 0f;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible)
                continue;
            Vector2 bl = ToScreen(cam, t.TransformPoint(c.bottomLeft));
            Vector2 tr = ToScreen(cam, t.TransformPoint(c.topRight));
            Rect glyph = Rect.MinMaxRect(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y), Mathf.Max(bl.x, tr.x), Mathf.Max(bl.y, tr.y));
            if (!Holds(view, glyph) || Clipped(t, glyph, cam) || Covered(panel, glyph.center))
                return 0f;
            if (!(char.IsUpper(c.character) || char.IsDigit(c.character)) || c.textElement == null || c.textElement.glyph == null)
                continue;
            float height = c.textElement.glyph.metrics.height * c.scale;
            Vector2 foot = ToScreen(cam, t.TransformPoint(new Vector3(c.origin, c.baseLine, 0f)));
            Vector2 top = ToScreen(cam, t.TransformPoint(new Vector3(c.origin, c.baseLine + height, 0f)));
            float px = Vector2.Distance(foot, top);
            if (px > best)
                best = px;
        }
        return best;
    }

    /// <summary>How far (screen pixels) a glyph may stand past a clip edge and still count as inside it (rounding).</summary>
    private const float ClipSlack = 0.5f;

    /// <summary>A graphic this faint (its alpha, inherited) is not over anything.</summary>
    private const float SeenAlpha = 0.05f;

    private static readonly Vector3[] Corners = new Vector3[4];
    private static readonly List<RaycastResult> Hits = new List<RaycastResult>();
    private static PointerEventData _pointer;
    private static EventSystem _pointerSystem;

    /// <summary>True when <paramref name="outer"/> holds <paramref name="inner"/> whole (with ClipSlack).</summary>
    private static bool Holds(Rect outer, Rect inner) =>
        inner.xMin >= outer.xMin - ClipSlack && inner.yMin >= outer.yMin - ClipSlack && inner.xMax <= outer.xMax + ClipSlack && inner.yMax <= outer.yMax + ClipSlack;

    /// <summary>True when a clip of the text's parents (an enabled RectMask2D, or a Mask that masks) leaves part of the glyph out.</summary>
    private static bool Clipped(Transform text, Rect glyph, Camera cam)
    {
        for (Transform w = text.parent; w != null; w = w.parent)
        {
            bool clips = (w.TryGetComponent(out RectMask2D rect) && rect.isActiveAndEnabled) || (w.TryGetComponent(out Mask mask) && mask.MaskEnabled());
            if (!clips || !(w is RectTransform rt))
                continue;
            rt.GetWorldCorners(Corners);
            Vector2 a = ToScreen(cam, Corners[0]), b = ToScreen(cam, Corners[2]);
            if (!Holds(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)), glyph))
                return true;
        }
        return false;
    }

    /// <summary>True when the topmost visible UI graphic at <paramref name="point"/> belongs to another window or panel than <paramref name="panel"/> (the text's).</summary>
    private static bool Covered(Transform panel, Vector2 point)
    {
        EventSystem system = EventSystem.current;
        if (system == null)
            return false;
        if (_pointer == null || _pointerSystem != system)
        {
            _pointer = new PointerEventData(system);
            _pointerSystem = system;
        }
        _pointer.position = point;
        Hits.Clear();
        system.RaycastAll(_pointer, Hits);
        foreach (RaycastResult hit in Hits)
        {
            if (hit.gameObject == null || !(hit.module is GraphicRaycaster) || !hit.gameObject.TryGetComponent(out Graphic graphic))
                continue;
            if (graphic.color.a * graphic.canvasRenderer.GetInheritedAlpha() < SeenAlpha)
                continue; // an invisible catcher hides nothing
            return !hit.gameObject.transform.IsChildOf(panel);
        }
        return false;
    }

    /// <summary>The window (DesktopWindow) or top panel (the child of its root canvas) a UI text belongs to; a text with no canvas, its top object.</summary>
    private static Transform PanelOf(Transform t)
    {
        Canvas canvas = t.GetComponentInParent<Canvas>();
        if (canvas == null)
            return t.root;
        Transform root = canvas.rootCanvas.transform;
        for (Transform w = t; w != null; w = w.parent)
        {
            if (w.TryGetComponent(out DesktopWindow _) || w.parent == root || w.parent == null)
                return w;
        }
        return t;
    }

    /// <summary>The font's capital height per unit of size (its face's cap line over its point size, with TMP's own scale; a 3D text's tenth).</summary>
    private static float CapShare(TMP_Text text)
    {
        TMP_FontAsset font = text.font;
        if (font == null || font.faceInfo.pointSize <= 0f)
            return 0f;
        float share = font.faceInfo.capLine * font.faceInfo.scale / font.faceInfo.pointSize;
        return text.isOrthographic ? share : share * 0.1f;
    }

    /// <summary>The screen pixels one unit of the text's own space covers vertically where it is drawn now (0 when it is not drawn).</summary>
    private static float PixelsPerUnit(TMP_Text text)
    {
        if (!Drawn(text, out Camera cam))
            return 0f;
        Transform t = text.transform;
        return Vector2.Distance(ToScreen(cam, t.TransformPoint(Vector3.zero)), ToScreen(cam, t.TransformPoint(Vector3.up)));
    }

    /// <summary>True when the text's canvas is drawn: enabled, and an overlay or drawn by an enabled camera (<paramref name="cam"/>; null for an overlay).</summary>
    private static bool Drawn(TMP_Text text, out Camera cam)
    {
        cam = null;
        Canvas canvas = text.canvas;
        if (canvas == null)
        {
            cam = Camera.main;
            return cam != null && cam.isActiveAndEnabled;
        }
        Canvas root = canvas.rootCanvas;
        if (!root.isActiveAndEnabled)
            return false;
        if (root.renderMode == RenderMode.ScreenSpaceOverlay)
            return true;
        cam = root.worldCamera;
        return cam != null && cam.isActiveAndEnabled;
    }

    private static Vector2 ToScreen(Camera cam, Vector3 world) => RectTransformUtility.WorldToScreenPoint(cam, world);
}
