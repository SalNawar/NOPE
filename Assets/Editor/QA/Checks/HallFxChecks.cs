using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HALL-FX's gate checks (BUG-13, 26a, 54, 56e, 67, 71; FIX_PLAN §3 HALL-FX, "Harness [BP]: the flicker
/// sequences as standing gates"). Play and built player: every sequence of the track's scenarios passes
/// seqtools flicker (no pixel toggling more than twice in 10 s outside the tracked cars), except the AVAILABLE
/// sign's 3 invitation beats, which are meant to be seen. Static: the hall's
/// one portal source is whole (five rings with fitted openings inside the effect's quad, the mask atlas wired
/// and imported as linear data without mips).
/// </summary>
public static class HallFxChecks
{
    /// <summary>The office and city views at every hour and aspect pass flicker (the editor run).</summary>
    [GateCheck("hallfx.city.flicker", "HALL-FX", GateKind.Play)]
    public static GateResult CityFlicker() => GateResults.Require("hallfx.city.flicker", "HALL-FX", "hall_fx_city.seq.*.flicker");

    /// <summary>The Helix monitor before and after a drop, under and out of the warning, passes flicker (the editor run).</summary>
    [GateCheck("hallfx.helix.flicker", "HALL-FX", GateKind.Play)]
    public static GateResult HelixFlicker() => GateResults.Require("hallfx.helix.flicker", "HALL-FX", "hall_fx_helix.seq.*.flicker");

    /// <summary>The city and the office on a running clock through dusk (windows fading in while it records) pass flicker (the editor run).</summary>
    [GateCheck("hallfx.dusk.flicker", "HALL-FX", GateKind.Play)]
    public static GateResult DuskFlicker() => GateResults.Require("hallfx.dusk.flicker", "HALL-FX", "hall_fx_dusk.seq.*.flicker");

    /// <summary>The AVAILABLE sign under the briefing, after its invitation and mid-case passes flicker (the editor run; the invitation's own 3 beats, sign_invite, are motion meant to be seen and are judged by region_luma --dips 3).</summary>
    [GateCheck("hallfx.sign.flicker", "HALL-FX", GateKind.Play)]
    public static GateResult SignFlicker() => GateResults.Require("hallfx.sign.flicker", "HALL-FX", "hall_fx_sign.seq.sign_steady_*.flicker");

    /// <summary>The hall slot crossfade, art to art and under a hit-stop, passes flicker (the editor run; its monotonic blend is region_luma --monotonic).</summary>
    [GateCheck("hallfx.crossfade.flicker", "HALL-FX", GateKind.Play)]
    public static GateResult CrossfadeFlicker() => GateResults.Require("hallfx.crossfade.flicker", "HALL-FX", "hall_fx_crossfade.seq.*.flicker");

    /// <summary>[BP] The same city sequences on the built QA player (the editor renders differently: BUG-53). Demo only, as every QA-RIG built-player check: the QA player opens a window, so it runs before a demo, when Saleh is away.</summary>
    [GateCheck("hallfx.bp.city.flicker", "HALL-FX", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult CityFlickerPlayer() => GateResults.Require("hallfx.bp.city.flicker", "HALL-FX", "bp.hall_fx_city.seq.*.flicker", true);

    /// <summary>[BP] The dusk sequences on the built QA player.</summary>
    [GateCheck("hallfx.bp.dusk.flicker", "HALL-FX", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult DuskFlickerPlayer() => GateResults.Require("hallfx.bp.dusk.flicker", "HALL-FX", "bp.hall_fx_dusk.seq.*.flicker", true);

    /// <summary>[BP] The same Helix sequences on the built QA player.</summary>
    [GateCheck("hallfx.bp.helix.flicker", "HALL-FX", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult HelixFlickerPlayer() => GateResults.Require("hallfx.bp.helix.flicker", "HALL-FX", "bp.hall_fx_helix.seq.*.flicker", true);

    /// <summary>The hall's one portal source (HallPortals.asset) is whole and its masks import as data.</summary>
    [GateCheck("hallfx.portals.source", "HALL-FX")]
    public static GateResult PortalSource()
    {
        const string id = "hallfx.portals.source", path = "Assets/Data/Config/HallPortals.asset";
        var problems = new List<string>();
        HallPortalsSO portals = AssetDatabase.LoadAssetAtPath<HallPortalsSO>(path);
        if (portals == null)
            return GateResult.Fail(id, "HALL-FX", "missing", path + " does not load");
        if (portals.masks == null)
            problems.Add("no mask atlas");
        else if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(portals.masks)) is TextureImporter importer)
        {
            if (importer.sRGBTexture)
                problems.Add("the masks import as sRGB (they are data)");
            if (importer.mipmapEnabled)
                problems.Add("the masks have mips (a low mip bleeds between the crops)");
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                problems.Add("the masks are compressed (block artefacts on the holes' edges)");
        }
        int rings = portals.rings != null ? portals.rings.Length : 0;
        if (rings != 5)
            problems.Add($"{rings} rings (the hall has 5)");
        for (int i = 0; i < rings; i++)
        {
            HallPortalRing r = portals.rings[i];
            if (r == null || r.opening.z <= 0f || r.opening.w <= 0f)
                problems.Add($"ring {i + 1} has no opening");
            else if (r.reach < 1f || r.reach > PortalVortex.Margin)
                problems.Add($"portal {r.portal}: reach {r.reach:0.00} outside 1..{PortalVortex.Margin}");
        }
        return GateResult.Of(id, "HALL-FX", problems.Count == 0, $"{rings} rings", string.Join("; ", problems));
    }
}
