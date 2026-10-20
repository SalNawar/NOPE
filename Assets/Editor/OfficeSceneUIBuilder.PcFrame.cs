using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// The office builder's PC: the desktop's own place (the World Space desktop,
/// its frame camera) and the PC frame on the overlay with its pixel art.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Desk.cs by P0-A (pure move). Owner: UI-PC.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>Metres per desktop canvas unit (the desktop is 14.4 x 10.8 m, far from the office).</summary>
    private const float DesktopScale = 0.01f;

    /// <summary>Where the desktop and its cameras live: far below the office floor, on their own layer.</summary>
    private static readonly Vector3 DesktopHome = new Vector3(0f, -100f, 0f);

    /// <summary>The PC frame on the overlay canvas (reference px): the whole monitor, left of centre.</summary>
    private static readonly Vector2 FrameSize = new Vector2(1240f, 1060f);

    /// <summary>The frame's distance from the screen's left edge (reference px).</summary>
    private const float FrameLeft = 20f;

    /// <summary>The placeholder bezel art's scale (it is drawn at half the frame's reference size).</summary>
    private const int FrameArtDivisor = 2;

    // -----------------------------
    // The desktop, its cameras and the screen
    // -----------------------------

    /// <summary>
    /// The desktop's own place, far below the office on the PCDesktop layer:
    /// the World Space desktop canvas (1440 x 1080 units at 1 cm each), the
    /// frame camera (draws the desktop into the PC frame's glass; the canvas's
    /// event camera), the clone camera (draws it into the office PC's texture),
    /// and the MonitorScreen and PcScreenClone that own them. Idempotent.
    /// </summary>
    private static MonitorScreen BuildPcDesktop(Canvas desktopCanvas, DeskConfigSO config, out Camera frameCamera)
    {
        GameObject root = GameObject.Find("PcDesktop") is GameObject found ? found : new GameObject("PcDesktop");
        root.transform.SetPositionAndRotation(DesktopHome, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        Transform canvas = desktopCanvas.transform;
        canvas.SetParent(root.transform, false);
        canvas.localPosition = Vector3.zero;
        canvas.localRotation = Quaternion.identity;
        canvas.localScale = Vector3.one * DesktopScale;

        float halfHeight = DesktopSize.y * DesktopScale / 2f;
        frameCamera = EnsureDesktopCamera(root.transform, "FrameCamera", halfHeight, 10f);
        Camera cloneCamera = EnsureDesktopCamera(root.transform, "CloneCamera", halfHeight, -10f);
        frameCamera.enabled = false;
        cloneCamera.enabled = false;
        desktopCanvas.worldCamera = frameCamera;

        Material clone = EnsureMaterial("PcScreenClone", "TimeDesk/PlanarScreen", null);
        PcScreenClone screenClone = GetOrAdd<PcScreenClone>(root.gameObject);
        var soClone = new SerializedObject(screenClone);
        SetRef(soClone, "cloneCamera", cloneCamera);
        SetRef(soClone, "cloneMaterial", clone);
        SetRef(soClone, "config", config);
        soClone.ApplyModifiedProperties();

        MonitorScreen screen = GetOrAdd<MonitorScreen>(root.gameObject);
        var so = new SerializedObject(screen);
        SetRef(so, "desktopCanvas", desktopCanvas);
        SetRef(so, "desktopRaycaster", desktopCanvas.GetComponent<GraphicRaycaster>());
        SetRef(so, "clone", screenClone);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        SetLayer(root.transform, OfficeLayers.PcDesktopLayer);
        return screen;
    }

    /// <summary>An orthographic camera looking at the desktop that draws only its layer (no post-processing, no shadows); its built <paramref name="depth"/> is a placeholder the binder reorders around the art's camera at load (PcFrame.DrawAfter, PcScreenClone.Bind). Idempotent.</summary>
    private static Camera EnsureDesktopCamera(Transform root, string name, float orthoSize, float depth)
    {
        Transform t = EnsureChild(root, name);
        t.localPosition = new Vector3(0f, 0f, -10f);
        t.localRotation = Quaternion.identity;

        Camera cam = GetOrAdd<Camera>(t.gameObject);
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 20f;
        cam.cullingMask = 1 << OfficeLayers.PcDesktopLayer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.depth = depth;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        UniversalAdditionalCameraData data = GetOrAdd<UniversalAdditionalCameraData>(t.gameObject);
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = false;
        data.renderShadows = false;
        data.antialiasing = AntialiasingMode.None;
        data.requiresColorOption = CameraOverrideOption.Off;
        data.requiresDepthOption = CameraOverrideOption.Off;
        return cam;
    }

    // -----------------------------
    // The PC frame (overlay)
    // -----------------------------

    /// <summary>
    /// The PC's device ink (the brand plate): the neutral theme's DiegeticDevice
    /// ink, #5E5446 in world_source.json, the one source of that colour (audit
    /// R6-005: the builder drew its own #6B614D beside it, and the theme never
    /// recolours a diegetic role, so the drawn colour and the checked one
    /// differed). A missing entry is an error; the text then draws black.
    /// </summary>
    private static Color DeviceInk(ContentLibrarySO library)
    {
        PaletteEntry device = library != null && library.NeutralTheme != null ? library.NeutralTheme.Get(ThemeRoleId.DiegeticDevice) : null;
        if (device != null && device.hasInk)
            return device.ink;

        Debug.LogError("[TimeDesk] The neutral theme has no DiegeticDevice ink for the PC's brand plate. Run Tools > TimeDesk > Generate World, then build again.");
        return Color.black;
    }

    /// <summary>
    /// The PC frame on the office overlay canvas, rebuilt each run: an
    /// always-active host with the PcFrame; its Root (inactive until opened)
    /// holds the full-screen exit catcher (a click outside the frame closes
    /// it and goes on to what it lands on: the traveller, the intercom, the
    /// desk), the bezel (placeholder art; a click on it leaves the PC, with a
    /// "Back to desk" hint: BUG-50a, Saleh's R3/R4 "to leave the PC, press Q
    /// or click the bezel"), the Glass the
    /// frame camera draws into (4:3; the catcher and the bezel let clicks
    /// through there), the red close X, the power button and LED, and the
    /// brand plate, in the neutral theme's DiegeticDevice ink. Returns the power LED and button through out parameters.
    /// </summary>
    private static PcFrame BuildPcFrame(Transform overlay, Camera frameCamera, OfficeViewController view, ContentLibrarySO library, out Image powerLed, out Button powerButton)
    {
        DestroyChildIfPresent(overlay, "PcFrame");
        Transform host = Panel(overlay, "PcFrame", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform root = Panel(host, "Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        Transform catcher = Panel(root, "ExitCatcher", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), ThemeRoleId.ClickCatcher);
        ClickCatcher exit = catcher.gameObject.AddComponent<ClickCatcher>();
        WirePersistentVoid(exit, "onClick", view, nameof(OfficeViewController.FocusOffice));
        var soExit = new SerializedObject(exit);
        soExit.FindProperty("passThrough").boolValue = true;
        soExit.ApplyModifiedProperties();

        Transform frame = Panel(root, "Frame", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, FrameSize, Color.white, ThemeRoleId.DiegeticDevice);
        var frameRect = (RectTransform)frame;
        frameRect.pivot = new Vector2(0f, 0.5f);
        frameRect.anchoredPosition = new Vector2(FrameLeft, 0f);
        Image bezel = frame.GetComponent<Image>();
        bezel.sprite = EnsureOfficeShape("pc_frame", (int)FrameSize.x / FrameArtDivisor, (int)FrameSize.y / FrameArtDivisor, Center, FramePixel);
        bezel.type = Image.Type.Simple;
        ClickCatcher leave = frame.gameObject.AddComponent<ClickCatcher>();
        WirePersistentVoid(leave, "onClick", view, nameof(OfficeViewController.FocusOffice));
        BuildHoverHint(leave, "taskbar.desk", null, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        Transform glass = Panel(frame, "Glass", Vector2.zero, Vector2.zero, Vector2.zero, FrameGlass.size, null);
        var glassRect = (RectTransform)glass;
        glassRect.pivot = Vector2.zero;
        glassRect.anchoredPosition = FrameGlass.position;

        catcher.gameObject.AddComponent<RectHoleRaycastFilter>();
        frame.gameObject.AddComponent<RectHoleRaycastFilter>();
        foreach (RectHoleRaycastFilter filter in new[] { catcher.GetComponent<RectHoleRaycastFilter>(), frame.GetComponent<RectHoleRaycastFilter>() })
        {
            var soFilter = new SerializedObject(filter);
            SetRef(soFilter, "hole", glassRect);
            soFilter.ApplyModifiedProperties();
        }

        Button close = MakeButton(frame, "CloseButton", "", new Vector2(1f, 1f), new Vector2(1f, 1f), Color.white, ThemeRoleId.DiegeticDevice);
        var closeRect = (RectTransform)close.transform;
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(64f, 64f);
        closeRect.anchoredPosition = new Vector2(-14f, -14f);
        close.GetComponent<Image>().sprite = EnsureOfficeShape("pc_close", 48, 48, Center, CloseButtonPixel);
        Object.DestroyImmediate(close.transform.Find("Label").gameObject); // its cross is drawn in the sprite: no text
        WirePersistentVoid(close, "m_OnClick", view, nameof(OfficeViewController.FocusOffice));

        powerButton = MakeButton(frame, "PowerButton", "", new Vector2(1f, 0f), new Vector2(1f, 0f), Color.white, ThemeRoleId.DiegeticDevice);
        var powerRect = (RectTransform)powerButton.transform;
        powerRect.pivot = new Vector2(1f, 0f);
        powerRect.sizeDelta = new Vector2(64f, 64f);
        powerRect.anchoredPosition = new Vector2(-70f, 46f);
        powerButton.GetComponent<Image>().sprite = EnsureOfficeShape("crt_power", 28, 28, Center, PowerButtonPixel);
        Object.DestroyImmediate(powerButton.transform.Find("Label").gameObject); // its symbol is drawn in the sprite: no text

        powerLed = Panel(frame, "PowerLed", new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(18f, 18f), Color.white, ThemeRoleId.DiegeticDevice).GetComponent<Image>();
        ((RectTransform)powerLed.transform).anchoredPosition = new Vector2(-170f, 78f);
        powerLed.sprite = EnsureOfficeShape("crt_led", 8, 8, Center, LedPixel);
        powerLed.raycastTarget = false;

        TMP_Text brand = Text(frame, "Brand", PcBrand, 34, TextAlignmentOptions.Center, new Vector2(0.3f, 0f), new Vector2(0.7f, 0f), DeviceInk(library),
                              ThemeRoleId.DiegeticDevice, style: FontStyles.Bold);
        var brandRect = (RectTransform)brand.transform;
        brandRect.sizeDelta = new Vector2(0f, 60f);
        brandRect.anchoredPosition = new Vector2(0f, 78f);
        brand.raycastTarget = false;

        PcFrame pcFrame = host.gameObject.AddComponent<PcFrame>();
        var so = new SerializedObject(pcFrame);
        SetRef(so, "root", root.gameObject);
        SetRef(so, "glass", glassRect);
        SetRef(so, "frameCamera", frameCamera);
        so.ApplyModifiedProperties();

        root.gameObject.SetActive(false);
        return pcFrame;
    }

    /// <summary>Placeholder bezel (half the frame's reference size): warm ivory plastic with rounded corners, a dark gasket round a transparent 4:3 glass, a darker chin.</summary>
    private static Color32 FramePixel(int x, int y)
    {
        int w = (int)FrameSize.x / FrameArtDivisor;
        int h = (int)FrameSize.y / FrameArtDivisor;
        float gx0 = FrameGlass.xMin / FrameArtDivisor, gx1 = FrameGlass.xMax / FrameArtDivisor;
        float gy0 = FrameGlass.yMin / FrameArtDivisor, gy1 = FrameGlass.yMax / FrameArtDivisor;
        const float radius = 34f;

        float cx = Mathf.Clamp(x + 0.5f, radius, w - radius);
        float cy = Mathf.Clamp(y + 0.5f, radius, h - radius);
        float corner = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
        if (corner > radius)
            return new Color32(0, 0, 0, 0);
        if (x + 0.5f >= gx0 && x + 0.5f <= gx1 && y + 0.5f >= gy0 && y + 0.5f <= gy1)
            return new Color32(0, 0, 0, 0);

        float dx = Mathf.Max(gx0 - (x + 0.5f), 0f, (x + 0.5f) - gx1);
        float dy = Mathf.Max(gy0 - (y + 0.5f), 0f, (y + 0.5f) - gy1);
        float fromGlass = Mathf.Sqrt(dx * dx + dy * dy);
        if (fromGlass < 9f)
            return Color32.Lerp(new Color32(46, 43, 38, 255), new Color32(92, 86, 74, 255), fromGlass / 9f);

        float fromEdge = radius - corner + Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
        if (fromEdge < 3f)
            return new Color32(150, 138, 112, 255);

        float t = y / (float)h;
        var top = new Color32(228, 214, 184, 255);
        var bottom = new Color32(204, 189, 156, 255);
        Color32 plastic = Color32.Lerp(bottom, top, t);
        if (y < gy0 - 2f)
            plastic = Color32.Lerp(plastic, new Color32(186, 171, 140, 255), 0.35f);
        if (Mathf.Abs(y - (gy0 - 2f)) < 1f)
            plastic = new Color32(170, 156, 126, 255);
        if (fromGlass < 16f)
            plastic = Color32.Lerp(new Color32(176, 163, 134, 255), plastic, (fromGlass - 9f) / 7f);
        return plastic;
    }

    /// <summary>Placeholder close button: a red rounded square with a white X.</summary>
    private static Color32 CloseButtonPixel(int x, int y)
    {
        float cx = Mathf.Clamp(x + 0.5f, 8f, 40f), cy = Mathf.Clamp(y + 0.5f, 8f, 40f);
        float corner = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
        if (corner > 8f)
            return new Color32(0, 0, 0, 0);
        if (corner > 6f || x < 2 || y < 2 || x > 45 || y > 45)
            return new Color32(120, 28, 22, 255);
        float u = x - 23.5f, v = y - 23.5f;
        bool cross = (Mathf.Abs(u - v) < 3.6f || Mathf.Abs(u + v) < 3.6f) && Mathf.Abs(u) < 12f && Mathf.Abs(v) < 12f;
        return cross ? new Color32(255, 250, 244, 255) : new Color32(214, 64, 50, 255);
    }

    /// <summary>Placeholder power button: a cream round face on a dark rim, with a power glyph.</summary>
    private static Color32 PowerButtonPixel(int x, int y)
    {
        float dx = x - 13.5f;
        float dy = y - 13.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 13.5f)
            return new Color32(0, 0, 0, 0);
        if (d > 11.5f)
            return new Color32(74, 70, 62, 255);

        var glyph = new Color32(90, 86, 78, 255);
        bool bar = Mathf.Abs(dx) < 1.3f && dy > 0f && dy < 7.5f;
        bool ring = d > 5f && d < 7f && !(dy > 0f && Mathf.Abs(dx) < 3f);
        return bar || ring ? glyph : new Color32(214, 208, 190, 255);
    }

    /// <summary>Placeholder LED: a white disc, tinted on and off by MonitorScreen.</summary>
    private static Color32 LedPixel(int x, int y)
    {
        float dx = x - 3.5f;
        float dy = y - 3.5f;
        return dx * dx + dy * dy <= 14.5f ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
    }
}
