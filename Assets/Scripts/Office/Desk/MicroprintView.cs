using TMPro;
using UnityEngine;

/// <summary>
/// The microprint as the scan view draws it (FIX_PLAN_v2 TOOLS, BUG-2b; Saleh
/// D5: "the scanner's hidden-layer view reveals the microprint"): TOOLS'
/// content and readability rule for the view UI-PC builds (ScanLayerSwitch,
/// FIX_PLAN_v2 #13). <see cref="Print"/> writes a paper's line into a text of
/// the hidden layer at the size whose capitals reach ScanMicroprint's cap
/// where that text is drawn; <see cref="CapPx"/> measures the capitals as
/// drawn, which MicroLens.Readable credits (never the geometry). TOOLS' QA op
/// measures the same way (tools/qa/scenarios/tools_microprint.json).
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
    /// 0 when it is not drawn (inactive, its canvas off, its camera off) or
    /// shows no capital or digit. Call it after the text has been drawn (the
    /// frame after Print).
    /// </summary>
    public static float CapPx(TMP_Text text)
    {
        if (text == null || !text.isActiveAndEnabled || !Drawn(text, out Camera cam))
            return 0f;
        TMP_TextInfo info = text.textInfo;
        if (info == null)
            return 0f;
        float best = 0f;
        Transform t = text.transform;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible || !(char.IsUpper(c.character) || char.IsDigit(c.character)) || c.textElement == null || c.textElement.glyph == null)
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
