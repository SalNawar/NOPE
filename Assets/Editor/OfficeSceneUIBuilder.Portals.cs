using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's portals (the portals spec v3): the Portals app's
/// window (PA1-PA3: the TC-970 schedule, read-only, across a scroll). Part
/// of <see cref="OfficeSceneUIBuilder"/>; the desktop shell builds the window
/// and registers it under DesktopAppIds.Portals.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The Portals app's page kind (TC-970).</summary>
    private const string PortalScheduleFormPath = "Assets/Data/Forms/Form_PortalSchedule.asset";

    /// <summary>
    /// The Portals window (PA3: 880 x 600 u restored, DesktopConfigSO): one
    /// scroll whose content is the schedule, a FormView across the scroll
    /// (Form_PortalSchedule's landscape page, so its five columns keep their
    /// type sizes), drawn by PortalsWindow from <paramref name="game"/>'s day.
    /// Rebuilt fresh.
    /// </summary>
    private static DesktopWindow BuildPortalsWindow(Transform windowLayer, DesktopConfigSO config, GameManager game)
    {
        DestroyChildIfPresent(windowLayer, "PortalsWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "PortalsWindow", "window.portals", null, null, config.portalsWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);

        ScrollRect scroll = BuildScrollArea(win, "Sheet", out RectTransform viewport);
        PlaceRect(scroll.transform, Vector2.zero, Vector2.one, new Vector2(DocMargin, DocMargin), new Vector2(-DocMargin, -(config.titleBarHeight + DocGap)));
        var content = (RectTransform)Panel(viewport, "Content", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, null);
        content.pivot = new Vector2(0.5f, 1f);
        scroll.content = content;
        FormView page = BuildFormView(content, "Schedule", config.portalsWindowSize.x - 2f * DocMargin - DocGap - DocScrollbar);

        PortalsWindow component = win.gameObject.AddComponent<PortalsWindow>();
        var so = new SerializedObject(component);
        Wire(so, "scroll", scroll);
        Wire(so, "page", page);
        Wire(so, "form", AssetDatabase.LoadAssetAtPath<FormSpecSO>(PortalScheduleFormPath));
        Wire(so, "game", game);
        so.ApplyModifiedProperties();

        win.gameObject.SetActive(false);
        return chrome;
    }
}
