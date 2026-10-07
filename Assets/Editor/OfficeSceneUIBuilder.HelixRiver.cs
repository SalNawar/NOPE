using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The office builder's Helix River (stability shown without a number;
/// docs/superpowers/specs/2026-10-07-helix-river-design.md): the taskbar's
/// live strip in the tray (where "Stability: 100%" stood; a hover hint names
/// it), the shift report's panel under its body (where the stability line
/// stood), the fallback HUD's cell, and the quad the office binder lays over
/// the art's stability monitor (HelixRiverMonitor.Cover). Each is a
/// HelixRiverMonitor; <see cref="WireHelixRivers"/> gives them all the
/// tuning, the shader and the shift (HelixRiverAuthoring, shared with the
/// Home builder). Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The tray strip's width (desktop units) and its glass's inset from the taskbar's top and bottom.</summary>
    private const float TrayRiverWidth = 176f, TrayRiverInset = 8f;

    /// <summary>How much of the river's glass the small strips show (the tray, the HUD): the banks and the helix, closer.</summary>
    private const float StripRiverZoom = 1.6f;

    /// <summary>How much of the glass the desk monitor shows: it is small on screen (about 90 x 40 px at 1080p), so closer.</summary>
    private const float DeskRiverZoom = 1.5f;

    /// <summary>How much of the glass the shift report's wide panel shows.</summary>
    private const float ReportRiverZoom = 1.8f;

    /// <summary>The glass's corner radius (a share of its height).</summary>
    private const float RiverCorner = 0.18f;

    /// <summary>The tray's river: a strip as wide as <see cref="TrayRiverWidth"/> in the tray's row, its glass inset from the bar's edges, with a hover hint (tray.stability).</summary>
    private static void BuildTrayRiver(Transform tray)
    {
        Transform strip = Panel(tray, "TimelineStrip", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        LayoutElement size = GetOrAdd<LayoutElement>(strip.gameObject);
        size.minWidth = TrayRiverWidth;
        size.preferredWidth = TrayRiverWidth;
        HelixRiverAuthoring.UiRiver(strip, "River", Vector2.zero, Vector2.one, new Vector2(0f, TrayRiverInset), new Vector2(0f, -TrayRiverInset),
                                    StripRiverZoom, RiverCorner, true);
        BuildHoverHint(strip, "tray.stability", null, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f));
    }

    /// <summary>The shift report's river: a caption (results.timeline) and a wide panel under the body, which gives up the room.</summary>
    private static void BuildResultsRiver(Transform results)
    {
        Transform paper = results.Find("Paper");
        var body = (RectTransform)paper.Find("BodyText");
        body.anchorMin = new Vector2(0.08f, 0.25f);
        Text(paper, "TimelineCaption", null, NewsletterBodyMin, TextAlignmentOptions.BottomLeft, new Vector2(0.08f, 0.215f), new Vector2(0.92f, 0.248f), Ink,
             ThemeRoleId.Newsletter, "results.timeline", FontStyles.Bold, ThemeTextKind.Body, true).raycastTarget = false;
        HelixRiverAuthoring.UiRiver(paper, "TimelineRiver", new Vector2(0.08f, 0.125f), new Vector2(0.92f, 0.21f), Vector2.zero, Vector2.zero,
                                    ReportRiverZoom, RiverCorner, false);
    }

    /// <summary>The fallback HUD's river cell (shown when the art office has no stability text).</summary>
    private static HelixRiverMonitor BuildHudRiver(Transform hud)
    {
        Transform cell = Panel(hud, "Stability", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        GetOrAdd<LayoutElement>(cell.gameObject).preferredWidth = 110f;
        HelixRiverMonitor river = HelixRiverAuthoring.UiRiver(cell, "River", Vector2.zero, Vector2.one, new Vector2(0f, 6f), new Vector2(0f, -6f),
                                                              StripRiverZoom, RiverCorner, false);
        return river;
    }

    /// <summary>The desk monitor's river: a quad (the built-in Quad mesh) under the office root, saved inactive; the binder lays it over the art's stability text and shows it.</summary>
    private static HelixRiverMonitor BuildDeskRiver(Transform office)
    {
        Transform t = EnsureChild(office, "DeskRiver");
        GetOrAdd<MeshFilter>(t.gameObject).sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        MeshRenderer screen = GetOrAdd<MeshRenderer>(t.gameObject);
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        screen.receiveShadows = false;
        screen.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        screen.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        HelixRiverMonitor river = GetOrAdd<HelixRiverMonitor>(t.gameObject);
        HelixRiverAuthoring.Shape(river, DeskRiverZoom, 0f);
        t.gameObject.SetActive(false);
        return river;
    }

    /// <summary>Gives every river in the gameplay scene the tuning, the shader and the shift.</summary>
    private static void WireHelixRivers(Scene scene, GameManager game) => HelixRiverAuthoring.Wire(scene, game);
}
