using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// The office builder's verdict feedback: the verdict ribbon and the
/// citation slip.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Kit.cs by P0-A (pure move). Owner: FEEDBACK.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
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
            SceneUiKit.SkinText(line, _kit, KitText.Ribbon, VerdictStripSize.y, _kit.InkOn(UiKitNames.VerdictRibbon(null, false)));
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
    /// across its foot (inactive: it slams on once the slip has printed,
    /// OfficeUIController), and the oxblood ACKNOWLEDGE plate under it. Inactive.
    /// </summary>
    private static Transform BuildCitationSlip(Transform overlay, out TMP_Text reason, out TMP_Text detail, out TMP_Text consequence, out Button acknowledge, out GameObject cited)
    {
        DestroyChildIfPresent(overlay, "CitationPanel");
        Transform slip = Panel(overlay, "CitationPanel", Center, Center, new Vector2(0f, CitationAcknowledgeSize.y / 2f), CitationSlipSize, new Color(0.96f, 0.9f, 0.88f, 1f),
                               ThemeRoleId.Alert);
        float scale = _kit != null ? _kit.overlayScale : 2f;
        KitSkin(slip, "citation_slip", scale);

        // Each line by the type scale: its role, and the height of its band on the slip.
        TMP_Text Line(string name, string key, KitText kind, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Color ink)
        {
            TMP_Text t = Text(slip, name, key != null ? null : string.Empty, 22, align, aMin, aMax, ink, ThemeRoleId.Alert, key);
            t.raycastTarget = false;
            if (_kit != null)
                SceneUiKit.SkinText(t, _kit, kind, (aMax.y - aMin.y) * CitationSlipSize.y, ink);
            return t;
        }

        Color ink = _kit != null ? _kit.inkOnLight : Ink, bone = _kit != null ? _kit.inkOnDark : Color.white, red = _kit != null ? _kit.inkAlert : Color.red;
        Line("Title", "citation.title", KitText.PanelHeading, TextAlignmentOptions.Center, new Vector2(0.07f, 0.80f), new Vector2(0.93f, 0.915f), bone);
        reason = Line("Reason", null, KitText.BodyLarge, TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.6f), new Vector2(0.92f, 0.765f), ink);
        if (_kit != null)
        {
            reason.font = _kit.bodyBoldFont; // the mistake reads bold over the rule and the values
            reason.GetComponent<ThemeTag>().SetFace(_kit.bodyBoldFont);
        }
        Transform rule = Panel(slip, "Rule", new Vector2(0.08f, 0.585f), new Vector2(0.92f, 0.585f), Vector2.zero, new Vector2(0f, 2f), red, ThemeRoleId.Alert);
        rule.GetComponent<Image>().raycastTarget = false;
        detail = Line("Detail", null, KitText.Body, TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.565f), ink);
        consequence = Line("Consequence", null, KitText.Ribbon, TextAlignmentOptions.MidlineLeft, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.335f), red);

        Transform stamp = Panel(slip, "Cited", new Vector2(0.66f, 0.13f), new Vector2(0.66f, 0.13f), Vector2.zero, CitedStampSize, Color.white, ThemeRoleId.Alert);
        stamp.localRotation = Quaternion.Euler(0f, 0f, CitedStampTilt);
        Image stampImage = stamp.GetComponent<Image>();
        stampImage.raycastTarget = false;
        stampImage.preserveAspect = true;
        stampImage.sprite = _kit != null ? _kit.Get("stamp_cited") : null;
        if (_kit != null)
            SceneUiKit.Tag(stampImage, ThemeRoleId.Alert, ThemePart.Kit);
        stamp.gameObject.SetActive(false);
        cited = stamp.gameObject;

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
