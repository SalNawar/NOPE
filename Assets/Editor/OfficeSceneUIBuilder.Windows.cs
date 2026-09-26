using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's desktop window parts (the PC redesign WN1-WN3, DK8,
/// DK9, CM2): the desktop's knobs (DesktopConfigSO), the compare dock above
/// the taskbar (outside every window: the window layer, the icon area above
/// it, draws over the desktop's icons and shows with or without a case; the
/// dock draws over every window), the
/// taskbar's window buttons, and the window manager on the desktop canvas,
/// wired to every window's chrome and to the frame's Escape. Part of
/// <see cref="OfficeSceneUIBuilder"/>; Build() calls these in its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
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

    /// <summary>The Start menu's centre above the desktop's bottom: it opens right above the dock, so the dock is never covered.</summary>
    private static float StartMenuCentre(float menuHeight)
    {
        DesktopConfigSO config = EnsureDesktopConfig();
        return config.MaximisedBottom + config.startMenuGap + menuHeight / 2f;
    }

    /// <summary>
    /// The compare dock (DK9, CM2): a strip the width of the desktop right
    /// above the taskbar, built hidden and shown with the case (<paramref name="dock"/>,
    /// the façade's). It is the investigation host's last child, so it draws
    /// over the window layer and the scan toast. Empty, it reads the keyed
    /// hint; its Pair (the CompareController's bar, shown while a value is
    /// picked) covers the hint with the compare text and a clear button
    /// (CompareController.Clear). Returns the pair; its text is <paramref name="text"/>.
    /// </summary>
    private static Transform BuildCompareDock(Transform investHost, CompareController compare, out TMP_Text text, out GameObject dock)
    {
        DesktopConfigSO config = EnsureDesktopConfig();

        Transform strip = Panel(investHost, "CompareDock", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, config.taskbarHeight + config.dockHeight / 2f),
                                new Vector2(0f, config.dockHeight), Tooltip, ThemeRoleId.CompareBar);
        TMP_Text hint = Text(strip, "Hint", null, 20, TextAlignmentOptions.Left, new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), Ink,
                             ThemeRoleId.CompareBar, "compare.dockHint", FontStyles.Italic);
        hint.raycastTarget = false;

        Transform pair = Panel(strip, "Pair", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Tooltip, ThemeRoleId.CompareBar);
        text = Text(pair, "CompareText", "", 22, TextAlignmentOptions.Center, new Vector2(0.02f, 0f), new Vector2(0.95f, 1f), Ink, ThemeRoleId.CompareBar);
        SetAnchors(text.transform, new Vector2(0.02f, 0f), new Vector2(0.95f, 1f));
        text.enableAutoSizing = true;
        text.fontSizeMin = 11f;
        text.fontSizeMax = 22f;
        text.raycastTarget = false;

        Button clear = MakeButton(pair, "ClearButton", null, new Vector2(0.955f, 0.15f), new Vector2(0.99f, 0.85f), XpRed, ThemeRoleId.CloseButton, "window.close");
        SetAnchors(clear.transform, new Vector2(0.955f, 0.15f), new Vector2(0.99f, 0.85f));
        WirePersistentVoid(clear, "m_OnClick", compare, nameof(CompareController.Clear));
        pair.gameObject.SetActive(false);

        strip.SetAsLastSibling();
        strip.gameObject.SetActive(false);
        dock = strip.gameObject;
        return pair;
    }

    /// <summary>
    /// The window manager on the desktop canvas (WN1-WN3, DK8): the taskbar's
    /// strip of window buttons between "&lt; Desk" and the tray (a template
    /// shrinking from the widest to the narrowest button knob as windows
    /// open) and the desktop's raycaster (the shell, the context menu and the
    /// empty desktop are wired with the icons: BuildDesktopShell); every
    /// window's chrome gets the manager, its title text and the title size,
    /// and the office view defers its Escape to the manager's stamp. Idempotent.
    /// </summary>
    private static DesktopWindowManager BuildWindowManager(Canvas canvas, OfficeViewController view)
    {
        DesktopConfigSO config = EnsureDesktopConfig();
        Transform root = canvas.transform;
        DesktopWindowManager manager = GetOrAdd<DesktopWindowManager>(root.gameObject);

        Transform taskbar = root.Find("Taskbar");
        Transform strip = Panel(taskbar, "WindowButtons", new Vector2(0.25f, 0.1f), new Vector2(0.635f, 0.9f), Vector2.zero, Vector2.zero, null);
        SetAnchors(strip, new Vector2(0.25f, 0.1f), new Vector2(0.635f, 0.9f));
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(strip.gameObject);
        row.spacing = 4f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;

        Button template = MakeButton(strip, "WindowButtonTemplate", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.5f, 0.95f), ThemeRoleId.DeskButton);
        LayoutElement size = GetOrAdd<LayoutElement>(template.gameObject);
        size.minWidth = config.taskbarButtonMinWidth;
        size.preferredWidth = config.taskbarButtonMaxWidth;
        size.flexibleWidth = 0f;
        TMP_Text label = template.transform.Find("Label").GetComponent<TMP_Text>();
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 18f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = new Vector4(6f, 0f, 6f, 0f);
        template.gameObject.SetActive(false);

        var so = new SerializedObject(manager);
        SetRef(so, "config", config);
        SetRef(so, "raycaster", canvas.GetComponent<GraphicRaycaster>());
        SetRef(so, "taskbarButtons", strip);
        SetRef(so, "taskbarButtonTemplate", template);
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
        }

        var soView = new SerializedObject(view);
        SetRef(soView, "desktop", manager);
        soView.ApplyModifiedProperties();
        return manager;
    }
}
