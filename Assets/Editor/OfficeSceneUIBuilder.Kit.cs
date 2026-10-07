using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's UI kit pass (docs/UI_KIT.md, run 7: "the UI to be
/// done, new art in the game"), run at the end of Build on both canvases and
/// re-applied on every build: the desk's own pieces first (the pull tabs with
/// their keycaps, the red inspect button over its SPACE key, the speech
/// bubble, the verdict ribbon, the citation slip, the morning paper's and
/// the ledger's plates), then every other control and panel by its theme
/// role (<see cref="RolePiece"/>: a default button is a bone mini plate, a
/// desk button a slate plate, a wheel choice a pill, the taskbar, title bars,
/// window bodies, menus, tooltips, toasts, badges and fields their kit
/// pieces), each through SceneUiKit.Skin (the sprite on a KitFace child, the
/// control's states swapped) with its label in the kit's face and ink. Kit
/// graphics take the ThemePart.Kit tag: a culture's theme keeps their
/// colours and changes only their font when its labels are in its own script.
/// Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The kit for this build (UiKitAssets.Ensure at the start of Build; null without the kit: the UI keeps its flat look).</summary>
    private static UiKitSO _kit;

    /// <summary>The graphics the desk's own pieces skinned this build (the role pass leaves them).</summary>
    private static readonly HashSet<Image> KitDone = new HashSet<Image>();

    /// <summary>Every pull tab's size (overlay units; sheet 01 B1: a tab a little taller than wide).</summary>
    private static readonly Vector2 PullTabSize = new Vector2(112f, 124f);

    /// <summary>The speech bubble's tail (overlay units; the kit's sprite with its pad).</summary>
    private static readonly Vector2 SpeechTailSize = new Vector2(46f, 42f);

    /// <summary>A pull tab's keycap under its word (overlay units).</summary>
    private static readonly Vector2 PullTabKeycap = new Vector2(52f, 30f);

    /// <summary>The inspect button's SPACE keycap under it (overlay units).</summary>
    private static readonly Vector2 InspectKeycap = new Vector2(96f, 34f);

    /// <summary>The kit pass over the desktop and the overlay canvases (see the class summary).</summary>
    private static void ApplyKit(Canvas desktop, Canvas overlay)
    {
        if (_kit == null)
            return;
        Transform o = overlay.transform;

        KitPullTab(o.Find("PcTab"), true, ControlRules.PcKey);
        KitPullTab(o.Find("StampBar/Tab"), false, ControlRules.StampsKey);
        TMP_Text stampsWord = o.Find("StampBar/Tab/Label")?.GetComponent<TMP_Text>();
        if (stampsWord != null)
            stampsWord.text = UiText.Get("controls.stampsTab");
        KitPullTab(o.Find("CityLook"), true, ControlRules.CityKey);
        KitPullTab(o.Find("CityBack"), false, ControlRules.CityBackKey);
        KitInspect(o.Find("InspectButton"));
        KitSpeechBubble(o.Find("SpeechBubble/Panel"));
        KitWheel(o.Find("TravellerWheel/Catcher/Ring"));
        KitDeskProps();

        // The taskbar's window button template at its own width (its row lays it out at run time; at build time it would take the row's rect).
        if (desktop.transform.Find("Taskbar/WindowButtons/WindowButtonTemplate") is RectTransform windowButton)
        {
            windowButton.anchorMin = Vector2.zero;
            windowButton.anchorMax = new Vector2(0f, 1f);
            windowButton.pivot = new Vector2(0f, 0.5f);
            windowButton.sizeDelta = new Vector2(EnsureDesktopConfig().taskbarButtonWidth, 0f);
        }

        ApplyKitByRole(o, _kit.overlayScale);
        ApplyKitByRole(desktop.transform, _kit.desktopScale);
        KitStartBanner(desktop.transform.Find("StartMenu"));
    }

    /// <summary>Skins a host and remembers it (the role pass leaves it); returns the face.</summary>
    private static Image KitSkin(Component host, string piece, float scale)
    {
        Image image = host != null ? host.GetComponent<Image>() : null;
        if (image == null)
            return null;
        KitDone.Add(image);
        return SceneUiKit.Skin(image, _kit, piece, scale);
    }

    /// <summary>How far a plate's label keeps inside its plate's edges (units): the label lies on the flat face, clear of the bevel and the ink line.</summary>
    private static readonly Vector2 KitLabelInset = new Vector2(8f, 6f);

    /// <summary>A button's "Label" child in the kit's label face, its ink the piece's (bone on a dark face, ink on a light one), in capitals unless <paramref name="upper"/> is false, kept inside the plate's flat face.</summary>
    private static void KitLabel(Component button, string piece, bool upper = true, TMP_FontAsset face = null)
    {
        Transform label = button != null ? button.transform.Find("Label") : null;
        if (label == null)
            return;
        var rt = (RectTransform)label;
        if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
        {
            rt.offsetMin = KitLabelInset;
            rt.offsetMax = -KitLabelInset;
        }
        SceneUiKit.SkinText(label.GetComponent<TMP_Text>(), _kit.InkOn(piece), face != null ? face : _kit.labelFont, upper);
    }

    /// <summary>
    /// A screen-edge pull tab (sheet 01 B1): the slate tab flush to its edge
    /// (<paramref name="left"/> or right), its word in the upper part and a bone
    /// keycap printed with <paramref name="key"/> under it.
    /// </summary>
    private static void KitPullTab(Transform tab, bool left, string key)
    {
        if (tab == null)
            return;
        string piece = left ? "pulltab_left" : "pulltab_right";
        ((RectTransform)tab).sizeDelta = PullTabSize;
        KitSkin(tab, piece, _kit.overlayScale);

        Transform label = tab.Find("Label");
        if (label != null)
        {
            var labelRect = (RectTransform)label;
            labelRect.anchorMin = new Vector2(left ? 0.06f : 0.16f, 0.46f);
            labelRect.anchorMax = new Vector2(left ? 0.84f : 0.94f, 0.92f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TMP_Text text = label.GetComponent<TMP_Text>();
            text.enableAutoSizing = true;
            text.fontSizeMax = 30f;
            text.fontSizeMin = 16f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.Center;
            text.characterSpacing = 4f;
            SceneUiKit.SkinText(text, _kit.inkOnDark, _kit.labelFont, true);
        }
        KitKeycap(tab, "Keycap", "keycap_bone", key, new Vector2(left ? 0.45f : 0.55f, 0.27f), PullTabKeycap);
    }

    /// <summary>A keycap printed with <paramref name="key"/> (a child plate in <paramref name="piece"/>, its word in the label face), centred at <paramref name="at"/> of its parent, rebuilt each build.</summary>
    private static void KitKeycap(Transform parent, string name, string piece, string key, Vector2 at, Vector2 size)
    {
        DestroyChildIfPresent(parent, name);
        Transform cap = Panel(parent, name, at, at, Vector2.zero, size, Color.white, ThemeRoleId.DiegeticDevice);
        Image capImage = cap.GetComponent<Image>();
        capImage.raycastTarget = false;
        KitSkin(cap, piece, _kit.overlayScale);
        TMP_Text word = Text(cap, "Key", key, 20, TextAlignmentOptions.Center, new Vector2(0.14f, 0.22f), new Vector2(0.86f, 0.86f), _kit.InkOn(piece), ThemeRoleId.DiegeticDevice);
        word.raycastTarget = false;
        word.enableAutoSizing = true;
        word.fontSizeMax = 20f;
        word.fontSizeMin = 16f;
        word.textWrappingMode = TextWrappingModes.NoWrap;
        SceneUiKit.SkinText(word, _kit.InkOn(piece), _kit.labelFont, true);
    }

    /// <summary>The red inspect button (sheet 01 B2): the domed button in its housing with the magnifier in the art, the code-drawn glyph and key label dropped, the lavender SPACE keycap under it.</summary>
    private static void KitInspect(Transform button)
    {
        if (button == null)
            return;
        KitSkin(button, "inspect", _kit.overlayScale);
        DestroyChildIfPresent(button, "Magnifier");
        Transform label = button.Find("Label");
        if (label != null)
            label.gameObject.SetActive(false);
        KitKeycap(button, "Keycap", "keycap_bone", ControlRules.InspectKey, new Vector2(0.5f, -0.06f), InspectKeycap);

        // Inspect mode on: the button stays lit (its hover face, the halo and the glint) instead of the old square ring.
        Transform on = button.Find("On");
        if (on == null)
            return;
        foreach (string side in new[] { "Top", "Bottom", "Left", "Right" })
            DestroyChildIfPresent(on, side);
        Transform lit = Panel(on, "Lit", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white, ThemeRoleId.DiegeticDevice);
        lit.SetAsFirstSibling();
        lit.GetComponent<Image>().raycastTarget = false;
        KitSkin(lit, "inspect_hover", _kit.overlayScale);
    }

    /// <summary>The counter strip's three looks (sheet 05: slate COUNTER, green HAND BACK, red STAMP THE PASSPORT FIRST) and the rulebook's folder tabs (manila, the open one lighter).</summary>
    private static readonly Color KitSlate = new Color(0.29f, 0.345f, 0.447f, 0.92f), KitGreen = new Color(0.373f, 0.522f, 0.314f, 0.92f),
                                  KitGreenLit = new Color(0.373f, 0.522f, 0.314f, 1f), KitRed = new Color(0.761f, 0.227f, 0.18f, 0.92f),
                                  KitManila = new Color(0.886f, 0.788f, 0.58f), KitManilaShut = new Color(0.79f, 0.69f, 0.48f);

    /// <summary>
    /// The desk's 3D pieces in the kit's colours (they are lit quads and flat
    /// texts on the desk, not UI): the counter strip's tints and its label in
    /// the label face (bone), the rulebook's tab plates manila and their words
    /// in the label face.
    /// </summary>
    private static void KitDeskProps()
    {
        DeskCounter counter = Object.FindFirstObjectByType<DeskCounter>(FindObjectsInactive.Include);
        if (counter != null)
        {
            var so = new SerializedObject(counter);
            so.FindProperty("plainTint").colorValue = KitSlate;
            so.FindProperty("handBackTint").colorValue = KitGreen;
            so.FindProperty("hoverHandBackTint").colorValue = KitGreenLit;
            so.FindProperty("hoverRefusedTint").colorValue = KitRed;
            so.ApplyModifiedProperties();
            if (so.FindProperty("label").objectReferenceValue is TMP_Text label)
            {
                label.font = _kit.labelFont;
                label.color = _kit.inkOnDark;
                label.characterSpacing = 6f;
            }
        }

        DeskRulebook rulebook = Object.FindFirstObjectByType<DeskRulebook>(FindObjectsInactive.Include);
        if (rulebook != null)
        {
            var so = new SerializedObject(rulebook);
            so.FindProperty("openTab").colorValue = KitManila;
            so.FindProperty("closedTab").colorValue = KitManilaShut;
            so.ApplyModifiedProperties();
            SerializedProperty tabs = so.FindProperty("tabs");
            for (int i = 0; i < tabs.arraySize; i++)
                if (tabs.GetArrayElementAtIndex(i).objectReferenceValue is Component tab && tab.transform.Find("Text") is Transform word)
                {
                    TMP_Text text = word.GetComponent<TMP_Text>();
                    text.font = _kit.labelFont;
                    text.color = _kit.inkOnLight;
                    text.fontStyle = FontStyles.UpperCase;
                }
        }
    }

    /// <summary>The traveller's speech bubble (sheet 05): the kit's cream bubble with the body face in ink.</summary>
    private static void KitSpeechBubble(Transform panel)
    {
        if (panel == null)
            return;
        KitSkin(panel, "speech_bubble", _kit.overlayScale);
        // The tail under the bubble's bottom, pointing down at the traveller.
        Transform tail = Panel(panel, "Tail", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), SpeechTailSize, Color.white, ThemeRoleId.DiegeticBubble);
        ((RectTransform)tail).pivot = new Vector2(0.5f, 1f);
        Image tailImage = tail.GetComponent<Image>();
        tailImage.sprite = _kit.Get("speech_tail");
        tailImage.raycastTarget = false;
        tailImage.preserveAspect = true;
        SceneUiKit.Tag(tailImage, ThemeRoleId.DiegeticBubble, ThemePart.Kit);
        Transform label = panel.Find("Label");
        if (label != null)
            SceneUiKit.SkinText(label.GetComponent<TMP_Text>(), _kit.inkOnLight, _kit.bodyFont, false);
    }

    /// <summary>The dialogue wheel (sheet 05): each choice a pill (bone at rest, slate under the pointer) and its label in capitals.</summary>
    private static void KitWheel(Transform ring)
    {
        Transform template = ring != null ? ring.Find("ActionButtonTemplate") : null;
        if (template == null)
            return;
        KitSkin(template, "wheelpill", _kit.overlayScale);
        KitLabel(template, "wheelpill_rest");
        // The pill's pictogram tile at its left (TravellerWheel.IconFor), its drawn tile as tall as the pill.
        var so = new SerializedObject(ring.GetComponent<InteractionPanelController>());
        so.FindProperty("iconSize").floatValue = WheelTileSize;
        so.FindProperty("iconPadding").floatValue = 2f;
        so.ApplyModifiedProperties();
    }

    /// <summary>A wheel pill's pictogram tile (overlay units; its sprite's pad round a pill-tall tile).</summary>
    private const float WheelTileSize = 52f;

    /// <summary>The PC's brand, printed on the frame's chin and up the Menu's side banner (a name, never translated).</summary>
    private const string PcBrand = "CHRONODESK 2150";

    /// <summary>The Menu's side banner's width (desktop units).</summary>
    private const float StartBannerWidth = 44f;

    /// <summary>The Menu's side banner (sheet 02): an oxblood band up its left side with the PC's brand reading upwards; the entries move right of it.</summary>
    private static void KitStartBanner(Transform menu)
    {
        if (menu == null || !menu.TryGetComponent(out VerticalLayoutGroup list))
            return;
        list.padding = new RectOffset((int)StartBannerWidth + 12, list.padding.right, list.padding.top, list.padding.bottom);
        Transform banner = Panel(menu, "Banner", Vector2.zero, new Vector2(0f, 1f), new Vector2(StartBannerWidth / 2f + 6f, 0f), new Vector2(StartBannerWidth, -12f),
                                 new Color(0.541f, 0.184f, 0.231f, 1f), ThemeRoleId.StartMenu);
        Image band = banner.GetComponent<Image>();
        band.raycastTarget = false;
        SceneUiKit.Tag(band, ThemeRoleId.StartMenu, ThemePart.Kit);
        GetOrAdd<LayoutElement>(banner.gameObject).ignoreLayout = true;
        float length = ((RectTransform)menu).sizeDelta.y - 40f;
        Transform word = Panel(banner, "Brand", Center, Center, Vector2.zero, new Vector2(length, StartBannerWidth), null);
        word.localRotation = Quaternion.Euler(0f, 0f, 90f);
        TMP_Text brand = Text(word, "Text", PcBrand, 28, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, _kit.inkOnDark, ThemeRoleId.StartMenu);
        brand.raycastTarget = false;
        brand.textWrappingMode = TextWrappingModes.NoWrap;
        brand.characterSpacing = 6f;
        SceneUiKit.SkinText(brand, _kit.inkOnDark, _kit.labelFont, true);
    }

    /// <summary>
    /// The role pass: every themed image under <paramref name="root"/> the desk
    /// pieces did not take gets its role's kit piece (<see cref="RolePiece"/>)
    /// at <paramref name="scale"/>, a button's label the piece's ink in the
    /// label face; then the chrome texts (title bars, the tray, the Menu
    /// button, toasts) their kit inks and faces.
    /// </summary>
    private static void ApplyKitByRole(Transform root, float scale)
    {
        foreach (ThemeTag tag in root.GetComponentsInChildren<ThemeTag>(true))
        {
            if (tag.name == UiKitSO.FaceName || tag.Part == ThemePart.Ink || !tag.TryGetComponent(out Image image) || KitDone.Contains(image))
                continue;
            string piece = RolePiece(tag, image);
            if (piece == null)
                continue;
            Image face = SceneUiKit.Skin(image, _kit, piece, scale);
            if (face == null)
                continue;

            if (tag.Role == ThemeRoleId.MenuEntry || tag.Role == ThemeRoleId.QuitEntry)
            {
                GhostRow(image.GetComponent<Button>(), tag.Role == ThemeRoleId.QuitEntry ? _kit.inkAlert : _kit.inkOnLight);
                continue;
            }

            if (image.TryGetComponent(out TMP_InputField field))
            {
                // A field shows its focus ring while it types.
                field.transition = Selectable.Transition.SpriteSwap;
                field.spriteState = new SpriteState { selectedSprite = _kit.Get("field_focus"), pressedSprite = _kit.Get("field_focus") };
                continue;
            }

            if (image.GetComponent<Button>() != null)
                KitLabel(image, UiKitNames.Of(piece, KitState.Rest));
        }

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            ThemeTag tag = text.GetComponent<ThemeTag>();
            if (tag == null || tag.Part == ThemePart.Kit)
                continue;
            // A window's own lines lie on its bone body (the kit's): ink, whatever the culture's window colours.
            if (tag.Role == ThemeRoleId.WindowBody && text.transform.parent.GetComponent<DesktopWindow>() != null)
            {
                SceneUiKit.SkinText(text, _kit.inkOnLight, null, false);
                continue;
            }
            // A quiet caption on a kit face (a drop-down's note) keeps its quiet, in the kit's muted ink.
            if (tag.Role == ThemeRoleId.SurfaceMuted && OnKitFace(text.transform))
            {
                SceneUiKit.SkinText(text, _kit.inkOnLight, null, false);
                continue;
            }
            switch (tag.Role)
            {
                case ThemeRoleId.TitleBar:
                case ThemeRoleId.StartButton:
                    SceneUiKit.SkinText(text, _kit.inkOnDark, _kit.labelFont, true);
                    break;
                case ThemeRoleId.Tray:
                    SceneUiKit.SkinText(text, _kit.phosphorInk, _kit.readoutFont, true);
                    break;
                case ThemeRoleId.Toast:
                case ThemeRoleId.Badge:
                    SceneUiKit.SkinText(text, _kit.inkOnDark, _kit.labelFont, false);
                    break;
                case ThemeRoleId.Tooltip:
                    SceneUiKit.SkinText(text, _kit.inkOnLight, _kit.bodyFont, false);
                    break;
            }
        }
    }

    /// <summary>
    /// A menu row (a Menu entry, a drop-down's row) on the kit's light row,
    /// bare until the pointer is on it (the row fades in), its label in
    /// <paramref name="ink"/> in the body face. The row must already be skinned.
    /// </summary>
    private static void GhostRow(Button row, Color ink)
    {
        if (row == null)
            return;
        row.transition = Selectable.Transition.ColorTint;
        ColorBlock tint = row.colors;
        tint.normalColor = Color.clear;
        tint.selectedColor = Color.clear;
        tint.disabledColor = Color.clear;
        tint.highlightedColor = Color.white;
        tint.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        tint.colorMultiplier = 1f;
        tint.fadeDuration = 0.06f;
        row.colors = tint;
        Transform label = row.transform.Find("Label");
        if (label != null)
            SceneUiKit.SkinText(label.GetComponent<TMP_Text>(), ink, _kit.bodyFont, false);
    }

    /// <summary>True when one of <paramref name="t"/>'s parents is a kit-skinned image (its text is drawn on a kit face).</summary>
    private static bool OnKitFace(Transform t)
    {
        for (Transform p = t.parent; p != null && p.GetComponent<Canvas>() == null; p = p.parent)
            if (p.GetComponent<Image>() != null && p.TryGetComponent(out ThemeTag tag) && tag.Part == ThemePart.Kit)
                return true;
        return false;
    }

    /// <summary>
    /// The kit piece a themed image takes by its role, or null to keep its
    /// look: a control's plate (a default button a bone mini plate, a desk
    /// button a slate plate, a newsletter's an oxblood plate, a wheel choice a
    /// pill, Search a slate mini plate, a window control an icon key, a menu
    /// row the light row), and the chrome's panels (the taskbar, the Menu
    /// button, the tray's phosphor glass, a window's title bar and body, the
    /// menus, tooltips, toasts, badges, fields).
    /// </summary>
    private static string RolePiece(ThemeTag tag, Image image)
    {
        bool button = image.GetComponent<Button>() != null;
        switch (tag.Role)
        {
            case ThemeRoleId.Button:
                return button ? "miniplate_bone" : null;
            case ThemeRoleId.NewsletterButton:
                return button ? "plate_ox" : null;
            case ThemeRoleId.DeskButton:
                return button ? "plate_slate" : null;
            case ThemeRoleId.WheelButton:
                return button ? "wheelpill" : null;
            case ThemeRoleId.SearchButton:
                return button ? "miniplate_slate" : null;
            case ThemeRoleId.AcceptButton:
                return button ? "plate_green" : null;
            case ThemeRoleId.DenyButton:
                return button ? "plate_red" : null;
            case ThemeRoleId.MenuEntry:
            case ThemeRoleId.QuitEntry:
                return button ? "listrow_hover" : null;
            case ThemeRoleId.Tab:
                return button ? (image.name == "MinBtn" || image.name == "MaxBtn" ? "iconkey" : "miniplate_bone") : null;
            case ThemeRoleId.CloseButton:
                return button ? "iconkey" : null;
            case ThemeRoleId.Taskbar:
                return image.name == "Taskbar" ? "taskbar" : null;
            case ThemeRoleId.StartButton:
                return "plate_ox";
            case ThemeRoleId.Tray:
                return image.name == "Tray" ? "lcd_glass" : null;
            case ThemeRoleId.TitleBar:
                return image.GetComponent<WindowDrag>() != null ? "titlebar_slate" : null;
            case ThemeRoleId.WindowBody:
                return image.GetComponent<DesktopWindow>() != null ? "panel_bone" : null;
            case ThemeRoleId.StartMenu:
                return "dropdown";
            case ThemeRoleId.Tooltip:
            case ThemeRoleId.CompareBar:
                return "tooltip";
            case ThemeRoleId.Toast:
                return image.GetComponent<Button>() == null ? "panel_dark" : null;
            case ThemeRoleId.Badge:
                return "badge_red";
            case ThemeRoleId.InputField:
                return image.GetComponent<TMP_InputField>() != null ? "field" : null;
            default:
                return null;
        }
    }

    /// <summary>The citation slip's size (overlay units): the kit's slip at 1.3 times its design size (340 x 400).</summary>
    private static readonly Vector2 CitationSlipSize = new Vector2(442f, 520f);

    /// <summary>The slip's ACKNOWLEDGE plate under it (overlay units).</summary>
    private static readonly Vector2 CitationAcknowledgeSize = new Vector2(330f, 72f);

    /// <summary>The CITED stamp on the slip (overlay units) and its tilt (degrees).</summary>
    private static readonly Vector2 CitedStampSize = new Vector2(250f, 102f);
    private const float CitedStampTilt = 12f;

    /// <summary>
    /// The verdict ribbon (sheet 05), rebuilt each run: top centre, the case
    /// HUD's compare strip's place (they never show together), inactive; its
    /// face is the kit's ribbon (OfficeUIController picks green, red or
    /// brass per verdict) and its line in bone capitals. Returns the strip.
    /// </summary>
    private static Transform BuildVerdictRibbon(Transform overlay, out Image ribbon, out TMP_Text line)
    {
        DestroyChildIfPresent(overlay, "VerdictStrip");
        Transform strip = Panel(overlay, "VerdictStrip", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -TopStripTop - VerdictStripSize.y / 2f),
                                VerdictStripSize, ScreenStripColor, ThemeRoleId.ScreenStrip);
        ((RectTransform)strip).pivot = Center;
        strip.GetComponent<Image>().raycastTarget = false;
        ribbon = KitSkin(strip, UiKitNames.VerdictRibbon(null, false), _kit != null ? _kit.overlayScale : 2f);
        line = Text(strip, "VerdictText", "", 30, TextAlignmentOptions.Center, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.86f), Color.white, ThemeRoleId.ScreenStrip, fit: true);
        line.raycastTarget = false;
        if (_kit != null)
            SceneUiKit.SkinText(line, _kit.InkOn(UiKitNames.VerdictRibbon(null, false)), _kit.labelFont, true);
        strip.gameObject.SetActive(false);
        return strip;
    }

    /// <summary>
    /// The citation slip (sheet 05: "prints from the desk, stamped, one
    /// button"), rebuilt each run over the office and the frame (it holds the
    /// day until ACKNOWLEDGE): the kit's printed slip with its perforated top,
    /// the red header band with the notice's title, the reason line (the
    /// mistake, bold), a red rule, the rule broken and the exact values
    /// (lesson 6), the warning or penalty in red capitals, the CITED stamp
    /// across its foot, and the oxblood ACKNOWLEDGE plate under it. Inactive.
    /// </summary>
    private static Transform BuildCitationSlip(Transform overlay, out TMP_Text reason, out TMP_Text detail, out TMP_Text consequence, out Button acknowledge)
    {
        DestroyChildIfPresent(overlay, "CitationPanel");
        Transform slip = Panel(overlay, "CitationPanel", Center, Center, new Vector2(0f, CitationAcknowledgeSize.y / 2f), CitationSlipSize, new Color(0.96f, 0.9f, 0.88f, 1f),
                               ThemeRoleId.Alert);
        float scale = _kit != null ? _kit.overlayScale : 2f;
        KitSkin(slip, "citation_slip", scale);

        TMP_Text Line(string name, string key, int size, int min, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Color ink, TMP_FontAsset face, bool upper)
        {
            TMP_Text t = Text(slip, name, key != null ? null : string.Empty, size, align, aMin, aMax, ink, ThemeRoleId.Alert, key);
            t.raycastTarget = false;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = min;
            t.textWrappingMode = TextWrappingModes.Normal;
            if (_kit != null)
                SceneUiKit.SkinText(t, ink, face, upper);
            return t;
        }

        Color ink = _kit != null ? _kit.inkOnLight : Ink, bone = _kit != null ? _kit.inkOnDark : Color.white, red = _kit != null ? _kit.inkAlert : Color.red;
        Line("Title", "citation.title", 30, 24, TextAlignmentOptions.Center, new Vector2(0.07f, 0.80f), new Vector2(0.93f, 0.915f), bone, _kit?.labelFont, true);
        reason = Line("Reason", null, 27, 18, TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.6f), new Vector2(0.92f, 0.765f), ink, _kit?.bodyBoldFont, false);
        Transform rule = Panel(slip, "Rule", new Vector2(0.08f, 0.585f), new Vector2(0.92f, 0.585f), Vector2.zero, new Vector2(0f, 2f), red, ThemeRoleId.Alert);
        rule.GetComponent<Image>().raycastTarget = false;
        detail = Line("Detail", null, 20, 15, TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.565f), ink, _kit?.bodyFont, false);
        consequence = Line("Consequence", null, 30, 22, TextAlignmentOptions.MidlineLeft, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.335f), red, _kit?.labelFont, true);

        Transform stamp = Panel(slip, "Cited", new Vector2(0.66f, 0.13f), new Vector2(0.66f, 0.13f), Vector2.zero, CitedStampSize, Color.white, ThemeRoleId.Alert);
        stamp.localRotation = Quaternion.Euler(0f, 0f, CitedStampTilt);
        Image stampImage = stamp.GetComponent<Image>();
        stampImage.raycastTarget = false;
        stampImage.preserveAspect = true;
        stampImage.sprite = _kit != null ? _kit.Get("stamp_cited") : null;
        if (_kit != null)
            SceneUiKit.Tag(stampImage, ThemeRoleId.Alert, ThemePart.Kit);

        acknowledge = MakeButton(slip, "ContinueButton", null, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), null, ThemeRoleId.Button, "citation.acknowledge");
        var rt = (RectTransform)acknowledge.transform;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -10f);
        rt.sizeDelta = CitationAcknowledgeSize;
        KitSkin(acknowledge, "plate_ox", scale);
        KitLabel(acknowledge, "plate_ox_rest");

        slip.gameObject.SetActive(false);
        return slip;
    }
}
