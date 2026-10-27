/// <summary>STAMP's gate checks (BUG-15a, 16a, 40a; QA-RIG §5).</summary>
public static class StampChecks
{
    /// <summary>30/30 dater strokes land on the aimed paper, no sink, no ink/verdict mismatch (stamp_aim.json).</summary>
    [GateCheck("stamp.aim", "STAMP", GateKind.Play)]
    public static GateResult Aim() => GateResults.Require("stamp.aim", "STAMP", "stamp_aim.aim.*");

    /// <summary>Rack pixels over any paper = 0 in every frame of a full case at A1 and A2 (stamp_case.json, the rackOverPaper op).</summary>
    [GateCheck("stamp.rackClear", "STAMP", GateKind.Play)]
    public static GateResult RackClear() => GateResults.Require("stamp.rackClear", "STAMP", "stamp_case.rackOverPaper.*");

    /// <summary>10 s on the rack at H2 and H5 on the QA player: flicker pass (stamp_rack.json).</summary>
    [GateCheck("stamp.rackNoise", "STAMP", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult RackNoise() => GateResults.Require("stamp.rackNoise", "STAMP", "bp.stamp_rack.seq.rack*.flicker", true);

    /// <summary>The rack's brass is cel (NOPE/Desk Anime, no texture) and the old brushed texture is gone.</summary>
    [GateCheck("stamp.brass", "STAMP", GateKind.Static)]
    public static GateResult Brass()
    {
        string[] mats = { "Assets/Art/Office/Gameplay/Materials/Drawer_Brass.mat", "Assets/Art/Office/Gameplay/Materials/Drawer_BrassDark.mat" };
        var bad = new System.Collections.Generic.List<string>();
        foreach (string path in mats)
        {
            var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (m == null || m.shader == null || m.shader.name != "NOPE/Desk Anime" || m.GetTexture("_BaseMap") != null)
                bad.Add(path);
        }
        if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Art/Office/Props/BrassDrawer/BrassDrawer_Brass.png") != null)
            bad.Add("BrassDrawer_Brass.png present");
        return GateResult.Of("stamp.brass", "STAMP", bad.Count == 0, bad.Count.ToString(), bad.Count == 0 ? "both rack materials are NOPE/Desk Anime with no _BaseMap; the brushed texture is gone" : string.Join(", ", bad));
    }
}
