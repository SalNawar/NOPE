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
/// The office builder's view chrome: the office controls
/// (Papers, Please's keys), the desk view and its back button, and the city
/// view.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Desk.cs by P0-A (pure move). Owner: CAM.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The gap (reference px) between the tutorial's prompt plate and the desk view's "▲ Back" control under it.</summary>
    private const float DeskViewBackGap = 12f;

    /// <summary>The city view's edge buttons (reference px) and their height on the screen (a share from the bottom: above the corkboards' calendar and clock).</summary>
    private static readonly Vector2 CityButtonSize = new Vector2(170f, 44f);

    private const float CityButtonHeight = 0.74f;

    /// <summary>The PC's grey tab on the overlay's left edge (reference px) and its height on the screen (a share from the bottom).</summary>
    private static readonly Vector2 PcTabSize = new Vector2(96f, 96f);

    private const float PcTabHeight = 0.4f;

    /// <summary>
    /// The desk view's "▲ Back" control (the readability fix: a visible way
    /// out of the desk view) under the office overlay canvas, rebuilt each run:
    /// a small button at the top left (the art pass, 2026-10-07: off the
    /// traveller's face in the closer counter framing), under the case HUD's
    /// strips and under the tutorial's prompt plate (GuidePlateAt,
    /// GuidePlateSize: both show in the reading view on day 1), in the
    /// "&lt; Desk" button's role, its label keyed (deskView.back); a hover hint
    /// beside it, on its right (deskView.backHint: the other ways back), in the
    /// tooltip's role, shown by HoverHint (beside, not under: the speech bubble
    /// waits right under the Back control in the desk view). Inactive:
    /// DeskView shows it while tilted (BoothRules.DeskViewBackLive). Returns
    /// its button.
    /// </summary>
    private static Button BuildDeskViewBack(Transform overlay)
    {
        DestroyChildIfPresent(overlay, "DeskViewBack");
        Button back = MakeButton(overlay, "DeskViewBack", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.5f, 0.95f), ThemeRoleId.DeskButton, "deskView.back");
        var rt = (RectTransform)back.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, Mathf.Min(-CaseHudClearance, GuidePlateAt.y - GuidePlateSize.y - DeskViewBackGap));
        rt.sizeDelta = DeskViewBackSize;

        Transform hint = Panel(back.transform, "Hint", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(8f, 0f), new Vector2(DeskViewBackSize.x, 34f), Tooltip, ThemeRoleId.Tooltip);
        ((RectTransform)hint).pivot = new Vector2(0f, 0.5f);
        hint.GetComponent<Image>().raycastTarget = false;
        TMP_Text hintText = Text(hint, "Label", null, 18, TextAlignmentOptions.Center, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), Ink,
                                 ThemeRoleId.Tooltip, "deskView.backHint", FontStyles.Normal, ThemeTextKind.Body, true);
        hintText.raycastTarget = false;
        hint.gameObject.SetActive(false);

        HoverHint hover = back.gameObject.AddComponent<HoverHint>();
        var so = new SerializedObject(hover);
        SetRef(so, "hint", hint.gameObject);
        so.ApplyModifiedProperties();

        back.gameObject.SetActive(false);
        return back;
    }

    /// <summary>
    /// The one input model (Saleh 2026-10-06), rebuilt each run: the
    /// OfficeControls on the Office root, wired to the view, the desktop's
    /// keyboard poller, the booth, the wheel, inspect mode, the stamp bar, the
    /// reading and city views and the desk; and the PC's grey tab on the
    /// overlay's left edge ("PC" over the key, Q; inactive until the booth makes
    /// the switch live), last on the overlay so it stays on top of the open PC
    /// frame; and the office's callouts (OfficeCallouts on the Office root:
    /// the speech bubble and the board's tooltip, hidden in the reading view
    /// and the city view). Returns the controls.
    /// </summary>
    private static OfficeControls BuildControls(Transform overlay, OfficeViewController view, BoothCoordinator booth, TravellerWheel wheel, DeskInspect inspect,
                                                DeskStampTray stamps, DeskView deskView, CityView cityView, DeskController desk, DeskConfigSO config)
    {
        DestroyChildIfPresent(overlay, "PcTab");
        Button tab = MakeButton(overlay, "PcTab", null, Vector2.zero, Vector2.one, StampGrey, ThemeRoleId.DiegeticDevice);
        var rt = (RectTransform)tab.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, PcTabHeight);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = PcTabSize;
        TMP_Text label = tab.transform.Find("Label").GetComponent<TMP_Text>();
        label.text = UiText.Format("controls.pcTab", ControlRules.PcKey);
        label.color = Color.white;
        label.enableAutoSizing = false;
        label.fontSize = 22f;
        tab.gameObject.SetActive(false);
        tab.transform.SetAsLastSibling();

        OfficeControls controls = GetOrAdd<OfficeControls>(view.gameObject);
        var so = new SerializedObject(controls);
        SetRef(so, "view", view);
        SetRef(so, "keyboard", Object.FindFirstObjectByType<DesktopKeyboard>(FindObjectsInactive.Include));
        SetRef(so, "booth", booth);
        SetRef(so, "wheel", wheel);
        SetRef(so, "inspect", inspect);
        SetRef(so, "stamps", stamps);
        SetRef(so, "deskView", deskView);
        SetRef(so, "cityView", cityView);
        SetRef(so, "desk", desk);
        SetRef(so, "pcTab", tab);
        SetRef(so, "pcTabLabel", label);
        so.ApplyModifiedProperties();

        // In the city view only the DESK tab shows: the PC and STAMPS tabs fade out (CityView.deskControls),
        // and so do the props' tooltips and the case HUD's strip, which drew over the city.
        var soCity = new SerializedObject(cityView);
        SerializedProperty hidden = soCity.FindProperty("deskControls");
        Transform[] deskControls = { tab.transform, overlay.Find("StampBar/Tab"), overlay.Find("DeskTooltip"), overlay.Find("OfficeCaseHud") };
        hidden.arraySize = deskControls.Length;
        for (int i = 0; i < deskControls.Length; i++)
            hidden.GetArrayElementAtIndex(i).objectReferenceValue = deskControls[i] != null ? GetOrAdd<CanvasGroup>(deskControls[i].gameObject) : null;
        soCity.ApplyModifiedProperties();

        // The office's callouts over the hall (the traveller's speech bubble, the board's tooltip) show only in the office view:
        // the reading view and the city view hide them (Saleh 2026-10-18: "dialogue bubles stay visible when you click on the table").
        OfficeCallouts callouts = GetOrAdd<OfficeCallouts>(view.gameObject);
        var soCallouts = new SerializedObject(callouts);
        SetRef(soCallouts, "config", config);
        SetRef(soCallouts, "deskView", deskView);
        SetRef(soCallouts, "cityView", cityView);
        SerializedArrays.Set(soCallouts, "callouts", new[] { GetOrAdd<CanvasGroup>(overlay.Find("SpeechBubble").gameObject), GetOrAdd<CanvasGroup>(overlay.Find("BoardTooltip").gameObject) });
        soCallouts.ApplyModifiedProperties();
        return controls;
    }

    /// <summary>A world-space text lying face up (top edge away from the chair) under <paramref name="parent"/>, auto-sized up to <paramref name="maxSize"/>.</summary>
    /// <summary>Makes <paramref name="text"/> read its UI string <paramref name="key"/> again as the scene starts (KeyedText; <paramref name="template"/> around it), so a builder-printed word follows the reading language.</summary>
    private static void Keyed(TMP_Text text, string key, string template = "{0}")
    {
        if (text == null)
            return;
        GetOrAdd<KeyedText>(text.gameObject).Configure(key, template);
        EditorUtility.SetDirty(text.gameObject);
    }

    private static TextMeshPro FlatText(Transform parent, string name, Vector3 position, Vector2 box, float maxSize, Color ink, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ((RectTransform)go.transform).sizeDelta = box;
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        tmp.text = string.Empty;
        tmp.fontSize = maxSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = maxSize;
        tmp.fontSizeMin = maxSize * 0.4f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        tmp.color = ink;
        tmp.sortingLayerID = GameplaySortingLayerId();
        return tmp;
    }

    /// <summary>
    /// The desk view (piece 10 section 11), rebuilt each run: Office/DeskView
    /// (DeskView, wired to the desk tuning, the mat's click and the overlay's
    /// "▲ Back" control) and its inactive Camera child, a CinemachineCamera at
    /// priority 0 that the office binder poses from the art's camera and the
    /// mat at load.
    /// </summary>
    private static DeskView BuildDeskView(Transform office, DeskConfigSO config, ClickCatcher mat, Button back)
    {
        DestroyChildIfPresent(office, "DeskView");
        Transform host = EnsureChild(office, "DeskView");
        Transform cameraHost = EnsureChild(host, "Camera");
        CinemachineCamera deskCamera = cameraHost.gameObject.AddComponent<CinemachineCamera>();
        deskCamera.Priority = 0;
        cameraHost.gameObject.SetActive(false);

        DeskView deskView = host.gameObject.AddComponent<DeskView>();
        var so = new SerializedObject(deskView);
        SetRef(so, "config", config);
        SetRef(so, "deskCamera", deskCamera);
        SetRef(so, "mat", mat);
        SetRef(so, "backButton", back);
        so.ApplyModifiedProperties();
        return deskView;
    }

    /// <summary>
    /// The city view (the desk-first redesign, item 6; Saleh 2026-10-07),
    /// rebuilt each run: Office/CityView (the CityView; the binder binds it to
    /// the hall's living city), and on the office overlay, first so the rest
    /// of the overlay draws over it, CityScreen (full screen, inactive until
    /// the fade: its CanvasGroup the fade, its Image the matte in
    /// DeskConfigSO.cityMatte; both in the diegetic DiegeticDevice role, so no
    /// theme recolours the art's view) holding Panorama (a RawImage fitted whole
    /// inside the screen by an AspectRatioFitter; the view gives it the living
    /// city's material at bind); then the CITY pull tab at the left edge and
    /// the DESK one at the right edge (the UI kit's, OfficeSceneUIBuilder.Kit:
    /// the word over its key's keycap; inactive: the view shows them). Returns it.
    /// </summary>
    private static CityView BuildCityView(Transform office, Transform overlay, DeskConfigSO config)
    {
        DestroyChildIfPresent(office, "CityView");
        Transform host = EnsureChild(office, "CityView");

        DestroyChildIfPresent(overlay, "CityScreen");
        // The art's view of the city, never themed (a diegetic role keeps its look).
        Transform screen = Panel(overlay, "CityScreen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, config.cityMatte, ThemeRoleId.DiegeticDevice);
        screen.SetAsFirstSibling();
        CanvasGroup group = screen.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        Transform picture = Panel(screen, "Panorama", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        RawImage panorama = picture.gameObject.AddComponent<RawImage>();
        panorama.raycastTarget = false;
        SceneUiKit.Tag(panorama, ThemeRoleId.DiegeticDevice, ThemePart.Fill);
        AspectRatioFitter fitter = picture.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 3f;
        screen.gameObject.SetActive(false);

        Button Edge(string name, string key, bool left)
        {
            DestroyChildIfPresent(overlay, name);
            Button b = MakeButton(overlay, name, null, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.5f, 0.95f), ThemeRoleId.DeskButton, key);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, CityButtonHeight);
            rt.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rt.anchoredPosition = new Vector2(left ? 12f : -12f, 0f);
            rt.sizeDelta = CityButtonSize;
            b.gameObject.SetActive(false);
            return b;
        }

        CityView view = host.gameObject.AddComponent<CityView>();
        var so = new SerializedObject(view);
        SetRef(so, "config", config);
        SetRef(so, "screen", group);
        SetRef(so, "matte", screen.GetComponent<Image>());
        SetRef(so, "panorama", panorama);
        SetRef(so, "fitter", fitter);
        SetRef(so, "lookButton", Edge("CityLook", "city.look", true));
        SetRef(so, "backButton", Edge("CityBack", "city.back", false));
        so.ApplyModifiedProperties();
        return view;
    }
}
