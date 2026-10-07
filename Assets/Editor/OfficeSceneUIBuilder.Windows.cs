using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's desktop window parts (the PC redesign WN1-WN3, DK8):
/// the desktop's knobs (DesktopConfigSO), the taskbar's window buttons, and
/// the window manager on the desktop canvas,
/// wired to every window's chrome and to the frame's Escape. Part of
/// <see cref="OfficeSceneUIBuilder"/>; Build() calls these in its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The least a taskbar window button shrinks to (desktop units) when many windows are open: eight fit beside the tray.</summary>
    private const float TaskbarButtonLeast = 30f;

    /// <summary>The desktop's knobs, created by the builder when missing (a designer's edits are kept).</summary>
    private const string DesktopConfigPath = "Assets/Data/Config/Desktop_Default.asset";

    /// <summary>Returns the desktop's knobs, creating them with the defaults when missing.</summary>
    private static DesktopConfigSO EnsureDesktopConfig() => EnsureConfigAsset<DesktopConfigSO>(DesktopConfigPath);

    /// <summary>Returns the knobs asset at <paramref name="path"/> (under Assets/Data/Config), creating it with the defaults when missing (a designer's edits are kept).</summary>
    private static T EnsureConfigAsset<T>(string path) where T : ScriptableObject
    {
        T config = AssetDatabase.LoadAssetAtPath<T>(path);
        if (config != null)
            return config;

        PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
        config = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        return config;
    }

    /// <summary>The taskbar's height (desktop units).</summary>
    private static float TaskbarHeight => EnsureDesktopConfig().taskbarHeight;

    /// <summary>A window title bar's centre below the window's top (a Panel's position under top-stretch anchors).</summary>
    private static Vector2 TitleBarPos => new Vector2(0f, -EnsureDesktopConfig().titleBarHeight / 2f);

    /// <summary>A window title bar's size under top-stretch anchors.</summary>
    private static Vector2 TitleBarSize => new Vector2(0f, EnsureDesktopConfig().titleBarHeight);

    /// <summary>A window title's size (a new title text; the window manager's wiring re-applies it to existing ones).</summary>
    private static int TitleFontSize => Mathf.RoundToInt(EnsureDesktopConfig().titleFontSize);

    /// <summary>The Start menu's centre above the desktop's bottom: it opens right above the taskbar.</summary>
    private static float StartMenuCentre(float menuHeight)
    {
        DesktopConfigSO config = EnsureDesktopConfig();
        return config.MaximisedBottom + config.startMenuGap + menuHeight / 2f;
    }

    /// <summary>
    /// The window manager on the desktop canvas (WN1-WN3, DK8): the taskbar's
    /// strip of window buttons between "&lt; Desk" and the tray (a template
    /// shrinking from the widest to the narrowest button knob as windows
    /// open) and the desktop's raycaster (the shell, the context menu and the
    /// empty desktop are wired with the icons: BuildDesktopShell); every
    /// window's chrome gets the manager, its title text and the title size
    /// (the office view defers its Escape to the keyboard poller's stamp:
    /// BuildDesktopKeys). Idempotent.
    /// </summary>
    private static DesktopWindowManager BuildWindowManager(Canvas canvas)
    {
        DesktopConfigSO config = EnsureDesktopConfig();
        Transform root = canvas.transform;
        DesktopWindowManager manager = GetOrAdd<DesktopWindowManager>(root.gameObject);

        Transform taskbar = root.Find("Taskbar");
        float from = PcSize.S + MenuButtonWidth + PcSize.S + DeskButtonWidth + PcSize.L;
        Transform strip = Panel(taskbar, "WindowButtons", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(strip, Vector2.zero, Vector2.one, new Vector2(from, 4f), new Vector2(-(PcSize.S + TrayWidth + PcSize.L), -4f));
        GetOrAdd<RectMask2D>(strip.gameObject); // many windows: the buttons shrink to their least, and never draw over the tray
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(strip.gameObject);
        row.spacing = 4f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;

        // A window's button (the PC UX redesign IA12, C7): the app's glyph on a Tab plate, the title only when the window has no glyph, an accent bar under the focused one, the title in a hover hint above it.
        Button template = MakeButton(strip, "WindowButtonTemplate", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.5f, 0.95f), ThemeRoleId.Tab);
        LayoutElement size = GetOrAdd<LayoutElement>(template.gameObject);
        size.minWidth = TaskbarButtonLeast;
        size.preferredWidth = config.taskbarButtonWidth;
        size.flexibleWidth = 0f;
        TMP_Text label = template.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(label, PcType.Caption);
        label.margin = new Vector4(PcSize.M, 0f, PcSize.M, 0f);
        label.raycastTarget = false;
        Transform glyph = Panel(template.transform, "Glyph", Center, Center, new Vector2(0f, 2f), new Vector2(30f, 30f), Color.white);
        Image glyphImage = glyph.GetComponent<Image>();
        glyphImage.raycastTarget = false;
        glyphImage.preserveAspect = true;
        SceneUiKit.Tag(glyphImage, ThemeRoleId.Tab, ThemePart.Ink);
        Transform focus = Panel(template.transform, "Focus", new Vector2(0.2f, 0f), new Vector2(0.8f, 0f), new Vector2(0f, 2f), new Vector2(0f, 4f), new Color(0.95f, 0.6f, 0.1f, 1f),
                                ThemeRoleId.FocusRing);
        focus.GetComponent<Image>().raycastTarget = false;
        focus.gameObject.SetActive(false);
        BuildHoverHint(template, null, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f));
        template.gameObject.SetActive(false);

        var so = new SerializedObject(manager);
        SetRef(so, "config", config);
        SetRef(so, "raycaster", canvas.GetComponent<GraphicRaycaster>());
        SetRef(so, "taskbarButtons", strip);
        SetRef(so, "taskbarButtonTemplate", template);
        Transform iconLayer = root.Find("DesktopIcons");
        SetRef(so, "icons", iconLayer != null ? iconLayer.GetComponent<DesktopIcons>() : null);
        so.ApplyModifiedProperties();

        foreach (DesktopWindow window in root.GetComponentsInChildren<DesktopWindow>(true))
        {
            WindowDrag drag = window.GetComponentInChildren<WindowDrag>(true);
            Transform title = drag != null ? drag.transform.Find("TitleText") : null;
            TMP_Text titleText = title != null ? title.GetComponent<TMP_Text>() : null;
            if (titleText != null)
                titleText.fontSize = config.titleFontSize;

            var soWindow = new SerializedObject(window);
            SetRef(soWindow, "manager", manager);
            SetRef(soWindow, "titleText", titleText);
            soWindow.ApplyModifiedProperties();

            // The PC workbench spec §6: a hairline under the title bar and a stronger one round the window, over everything in it.
            if (drag != null)
                HairlineEdge(drag.transform, "Rule", 1);
            HairlineFrame(window.transform, WbLineStrong, ThemeRoleId.HairlineStrong, 1f, "Edge").SetAsLastSibling();
        }

        return manager;
    }
}
