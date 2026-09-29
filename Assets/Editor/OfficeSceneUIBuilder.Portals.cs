using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's portals (the portals spec v3): the Portals app's
/// window (PA1-PA3: the TC-970 schedule, read-only, across a scroll), the
/// hall's Departure Board (BD1-BD5: its rows, its click box and reaction and
/// the growing board tooltip) and the rings' effects (VX1-VX6: one
/// PortalEffect per DeskConfigSO.hallPortalLayers entry). Part of
/// <see cref="OfficeSceneUIBuilder"/>; the desktop shell builds the window
/// and registers it under DesktopAppIds.Portals; BuildOffice builds the
/// board and the effects and hands them to the office binder.
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

    /// <summary>The board tooltip's least size (it grows with its lines; overlay reference px).</summary>
    private static readonly Vector2 BoardTooltipSize = new Vector2(360f, 60f);

    /// <summary>The board tooltip's line size and padding (overlay reference px: 22 px is 14.7 px at 720p).</summary>
    private const int BoardTooltipText = 22, BoardTooltipPadding = 14;

    /// <summary>The board tooltip hangs under the display (overlay reference px; the board sits high in the view).</summary>
    private static readonly Vector2 BoardTooltipOffset = new Vector2(0f, -130f);

    /// <summary>How long the board tooltip stays (seconds: it has six lines to read).</summary>
    private const float BoardTooltipSeconds = 8f;

    /// <summary>The rows' largest and smallest type (TextMeshPro world sizes; they shrink to the display together).</summary>
    private const float BoardRowsMax = 2f, BoardRowsMin = 0.05f;

    /// <summary>The rings' effects' material (unlit, premultiplied and brightened: they glow through the evening).</summary>
    private static Material PortalGlowMaterial() => EnsureMaterial("PortalGlow", "TimeDesk/PortalGlow", null);

    /// <summary>
    /// Makes an overlay callout grow with its lines (the departure board's
    /// tooltip, BD4): its panel lays out its label with padding and fits it,
    /// never smaller than <paramref name="least"/>; the label keeps one size and
    /// its lines unwrapped, left aligned.
    /// </summary>
    private static void Grow(Transform panel, TMP_Text label, Vector2 least)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(panel.gameObject);
        layout.padding = new RectOffset(BoardTooltipPadding, BoardTooltipPadding, BoardTooltipPadding / 2, BoardTooltipPadding / 2);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(panel.gameObject);
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        LayoutElement min = GetOrAdd<LayoutElement>(label.gameObject);
        min.minWidth = least.x - 2f * BoardTooltipPadding;
        min.minHeight = least.y - BoardTooltipPadding;
        label.enableAutoSizing = false;
        label.fontSize = BoardTooltipText;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.TopLeft;
    }

    /// <summary>
    /// The Departure Board (BD1-BD5) under the Office root, rebuilt fresh and
    /// inactive until the binder puts it on the DepartureBoard anchor: its Rows
    /// (a world-space TextMeshPro on the Gameplay sorting layer, auto-sized,
    /// unwrapped, left aligned, rich text for the state ink), its ClickBox (a
    /// click box with a tooltip-only reaction, Reaction_DepartureBoard, on
    /// <paramref name="tooltip"/>, the growing board tooltip) and its
    /// TooltipPoint, wired to a DepartureBoardView reading
    /// <paramref name="game"/>'s portals.
    /// </summary>
    private static DepartureBoardView BuildDepartureBoard(Transform office, DeskConfigSO config, OverlayCallout tooltip, GameManager game)
    {
        DestroyChildIfPresent(office, "DepartureBoard");
        Transform root = EnsureChild(office, "DepartureBoard");

        var go = new GameObject("Rows", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(root, false);
        TextMeshPro rows = go.GetComponent<TextMeshPro>();
        rows.text = string.Empty;
        rows.enableAutoSizing = true;
        rows.fontSizeMax = BoardRowsMax;
        rows.fontSizeMin = BoardRowsMin;
        rows.textWrappingMode = TextWrappingModes.NoWrap;
        rows.alignment = TextAlignmentOptions.Left;
        rows.richText = true;
        rows.color = config.hallBoardInk;
        rows.sortingLayerID = GameplaySortingLayerId();

        Clickable click = EnsureClickBox(root, "ClickBox");
        bool made = AssetDatabase.LoadAssetAtPath<DeskReactionSO>($"{DeskReactionFolder}/Reaction_DepartureBoard.asset") == null;
        DeskReactionSO reactionAsset = EnsureDeskReaction("Reaction_DepartureBoard", ReactionKind.None, "tooltip.value");
        if (made)
        {
            reactionAsset.tooltipSeconds = BoardTooltipSeconds;
            reactionAsset.tooltipOffset = BoardTooltipOffset;
            EditorUtility.SetDirty(reactionAsset);
        }
        DeskReaction reaction = WireReaction(click, reactionAsset, tooltip, null);
        Transform point = EnsureChild(root, "TooltipPoint");

        DepartureBoardView view = root.gameObject.AddComponent<DepartureBoardView>();
        var so = new SerializedObject(view);
        Wire(so, "rows", rows);
        Wire(so, "box", click.GetComponent<BoxCollider>());
        Wire(so, "reaction", reaction);
        Wire(so, "tooltipPoint", point);
        Wire(so, "config", config);
        Wire(so, "game", game);
        so.ApplyModifiedProperties();

        root.gameObject.SetActive(false);
        return view;
    }

    /// <summary>
    /// The rings' effects (VX1-VX6) under the Office root, rebuilt fresh: one
    /// PortalEffect per DeskConfigSO.hallPortalLayers entry, in its order, each
    /// a disabled SpriteRenderer in the PortalGlow material (the anime hall's
    /// AnimeHallPortalLink places it, sorts it and shows its art).
    /// </summary>
    private static PortalEffect[] BuildPortalEffects(Transform office, DeskConfigSO config)
    {
        DestroyChildIfPresent(office, "PortalEffects");
        Transform root = EnsureChild(office, "PortalEffects");
        Material material = PortalGlowMaterial();
        HallPortalLayers[] layers = config.hallPortalLayers ?? new HallPortalLayers[0];
        var effects = new PortalEffect[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            Transform t = EnsureChild(root, "PortalEffect_" + PortalText.Number(layers[i] != null ? layers[i].portal : i + 1));
            SpriteRenderer glow = t.gameObject.AddComponent<SpriteRenderer>();
            glow.sharedMaterial = material;
            glow.enabled = false;
            effects[i] = t.gameObject.AddComponent<PortalEffect>();
            var so = new SerializedObject(effects[i]);
            Wire(so, "glow", glow);
            so.ApplyModifiedProperties();
        }
        return effects;
    }
}
