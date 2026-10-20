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
/// The office builder's booth: the traveller, the traveller wheel and its
/// speech bubble input, the overlay callouts and the office case HUD.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Desk.cs by P0-A (pure move). Owner: FEEDBACK.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>
    /// The traveller: Office/Traveller (TravellerView) with its Figure (a
    /// SortingGroup with one unlit SpriteRenderer per LookLayer, the
    /// LookSpriteStack), the Anchor and the HitZone (a click box). The binder
    /// stands it at the traveller anchor at load. Idempotent.
    /// </summary>
    private static TravellerView BuildTraveller(Transform office, out Clickable hitZone)
    {
        Transform traveller = EnsureChild(office, "Traveller");
        DestroyChildIfPresent(traveller, "Figure");
        Transform figure = EnsureChild(traveller, "Figure");
        SortingGroup group = figure.gameObject.AddComponent<SortingGroup>();
        group.sortingLayerID = GameplaySortingLayerId();
        group.sortingOrder = 0;
        LookSpriteStack stack = figure.gameObject.AddComponent<LookSpriteStack>();
        WireLayers(stack, figure, 0, false);

        Transform anchor = EnsureChild(traveller, "Anchor");
        hitZone = EnsureClickBox(traveller, "HitZone");

        TravellerView view = GetOrAdd<TravellerView>(traveller.gameObject);
        var so = new SerializedObject(view);
        SetRef(so, "figure", stack);
        SetRef(so, "anchor", anchor);
        SetRef(so, "hitZone", hitZone.GetComponent<BoxCollider>());
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>A strip at the top centre of the overlay, <paramref name="top"/> px down, with its text (auto-sized, no raycasts).</summary>
    private static TMP_Text TopStrip(Transform parent, string name, Vector2 size, float top, Color background, ThemeRoleId role, int fontSize, Color ink, out Transform strip)
    {
        strip = Panel(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -top - size.y / 2f), size, background, role);
        ((RectTransform)strip).pivot = Center;
        strip.GetComponent<Image>().raycastTarget = false;
        TMP_Text text = Text(strip, name + "Text", "", fontSize, TextAlignmentOptions.Center, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f), ink, role);
        text.enableAutoSizing = true;
        text.fontSizeMin = 11f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// The office case HUD (piece 10) under the office overlay canvas, rebuilt
    /// each run: an always-active full-screen host (OfficeCaseHud, no graphic)
    /// and its Root, top centre: the office compare strip (CompareBar,
    /// inactive; the CompareController draws it), where the claim tag was (no
    /// claim is printed: the personalities spec's B1). No part takes raycasts.
    /// Returns the HUD and the strip's object and text through out parameters.
    /// </summary>
    private static OfficeCaseHud BuildOfficeCaseHud(Transform overlay, out GameObject compareStrip, out TMP_Text compareText)
    {
        DestroyChildIfPresent(overlay, "OfficeCaseHud");
        Transform host = Panel(overlay, "OfficeCaseHud", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform root = Panel(host, "Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        compareText = TopStrip(root, "CompareStrip", CompareStripSize, TopStripTop, Tooltip, ThemeRoleId.CompareBar, 18, Ink, out Transform compare);
        compareStrip = compare.gameObject;
        compareStrip.SetActive(false);

        OfficeCaseHud hud = host.gameObject.AddComponent<OfficeCaseHud>();
        var so = new SerializedObject(hud);
        SetRef(so, "root", root.gameObject);
        so.ApplyModifiedProperties();
        root.gameObject.SetActive(false);
        return hud;
    }

    /// <summary>
    /// The speech bubble takes input (piece 10): its panel image catches
    /// raycasts (hovering holds the line), carries a transition-free Button
    /// (interactable only while an answer shows, set by the wheel) and a
    /// SpeechBubbleInput, and the bubble moves above the wheel (after it on
    /// the overlay) so an answer can be picked while the wheel is open.
    /// </summary>
    private static void BuildBubbleInput(OverlayCallout bubble, TravellerWheel wheel)
    {
        Transform panel = bubble.transform.Find("Panel");
        Image image = panel.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = panel.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.interactable = false;
        SpeechBubbleInput input = panel.gameObject.AddComponent<SpeechBubbleInput>();
        var so = new SerializedObject(input);
        SetRef(so, "wheel", wheel);
        SetRef(so, "button", button);
        so.ApplyModifiedProperties();

        var soWheel = new SerializedObject(wheel);
        SetRef(soWheel, "bubbleButton", button);
        soWheel.ApplyModifiedProperties();
        bubble.transform.SetSiblingIndex(wheel.transform.GetSiblingIndex() + 1);
    }

    /// <summary>
    /// An overlay callout (a timed label that takes no clicks) under the office
    /// overlay canvas, rebuilt each run: an always-active full-screen host with
    /// no graphic, and its Panel child (anchors and pivot (0.5, 0.5), raycast
    /// targets off, inactive) holding an auto-sized label; with
    /// <paramref name="keepOnScreen"/> it waits at the screen's edge while its
    /// object is out of view, below the case HUD's strips (the speech bubble),
    /// else it hides (the tooltip).
    /// </summary>
    private static OverlayCallout BuildOverlayCallout(Transform overlay, string name, Vector2 size, Color background, ThemeRoleId role, bool keepOnScreen, bool grows = false)
    {
        DestroyChildIfPresent(overlay, name);
        Transform host = Panel(overlay, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform panel = Panel(host, "Panel", Center, Center, Vector2.zero, size, background, role);
        ((RectTransform)panel).pivot = Center;
        panel.GetComponent<Image>().raycastTarget = false;

        TMP_Text label = Text(panel, "Label", "", 24, TextAlignmentOptions.Center, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Ink, role);
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 24f;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        if (grows)
            Grow(panel, label, size);

        OverlayCallout callout = host.gameObject.AddComponent<OverlayCallout>();
        var so = new SerializedObject(callout);
        SetRef(so, "panel", panel);
        SetRef(so, "label", label);
        so.FindProperty("keepOnScreen").boolValue = keepOnScreen;
        so.FindProperty("topInset").floatValue = keepOnScreen ? OverlayTopClearance : 0f;
        so.FindProperty("grows").boolValue = grows;
        so.ApplyModifiedProperties();

        panel.gameObject.SetActive(false);
        return callout;
    }

    /// <summary>
    /// The traveller wheel under the office overlay canvas, rebuilt each run: an
    /// always-active full-screen host (TravellerWheel, no graphic); its Catcher,
    /// a full-screen transparent click-to-close area, inactive; the Ring under it
    /// (anchors and pivot (0.5, 0.5), placed by projection) with the choice
    /// renderer (InteractionPanelController, its template stretched so a centre
    /// clone fills the centre slot), the RadialLayoutGroup and the Centre slot
    /// ("&lt; Back", ignored by the layout). The wheel's traveller is set by
    /// BuildOffice, once the traveller view exists.
    /// </summary>
    private static TravellerWheel BuildTravellerWheel(Transform overlay, DeskConfigSO config, OverlayCallout bubble)
    {
        DestroyChildIfPresent(overlay, "TravellerWheel");
        Transform host = Panel(overlay, "TravellerWheel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform catcher = Panel(host, "Catcher", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), ThemeRoleId.ClickCatcher);

        Transform ring = Panel(catcher, "Ring", Center, Center, Vector2.zero, Vector2.zero, null);
        ((RectTransform)ring).pivot = Center;
        RadialLayoutGroup layout = ring.gameObject.AddComponent<RadialLayoutGroup>();
        layout.Radii = config.wheelRadii;
        layout.ItemSize = config.wheelItemSize;

        Transform centre = Panel(ring, "Centre", Center, Center, Vector2.zero, config.wheelCentreSize, null);
        ((RectTransform)centre).pivot = Center;
        centre.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        Button template = MakeButton(ring, "ActionButtonTemplate", "Choice", Vector2.zero, Vector2.one, new Color(0.16f, 0.28f, 0.42f, 0.95f), ThemeRoleId.WheelButton);
        TMP_Text choice = template.transform.Find("Label").GetComponent<TMP_Text>();
        choice.enableAutoSizing = true;
        choice.fontSizeMin = 12f;
        choice.fontSizeMax = 20f;
        choice.textWrappingMode = TextWrappingModes.Normal;
        template.gameObject.SetActive(false);

        InteractionPanelController panel = ring.gameObject.AddComponent<InteractionPanelController>();
        var soPanel = new SerializedObject(panel);
        SetRef(soPanel, "actionsRoot", ring);
        SetRef(soPanel, "actionButtonTemplate", template);
        SetRef(soPanel, "centreSlot", centre);
        soPanel.ApplyModifiedProperties();

        TravellerWheel wheel = host.gameObject.AddComponent<TravellerWheel>();
        var so = new SerializedObject(wheel);
        SetRef(so, "catcher", catcher.gameObject);
        SetRef(so, "ring", ring);
        SetRef(so, "layout", layout);
        SetRef(so, "centreSlot", centre);
        SetRef(so, "bubble", bubble);
        SetRef(so, "config", config);
        SetRef(so, "kit", _kit);
        so.FindProperty("ringTopInset").floatValue = OverlayTopClearance;
        so.ApplyModifiedProperties();

        catcher.gameObject.SetActive(false);
        return wheel;
    }
}
