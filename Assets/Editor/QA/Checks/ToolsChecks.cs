using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TOOLS' gate checks (FIX_PLAN TOOLS and its v2 delta; QA-RIG's DemoGate
/// finds them by their <see cref="GateCheckAttribute"/>). Static: the UV
/// torch's click box in the built OfficeGameplay hugs the torch (BUG-4b).
/// Play: the recipes of docs/pending/TOOLS.md, measured by TOOLS' scenario
/// ops (ToolsOps): the torch's reveal unoccluded and its pool on the desk
/// (BUG-2c), the chip reader's clicks (BUG-4b), the Translation Lens's
/// phantoms, PC texts and revert (BUG-52), and the scan view's microprint
/// (BUG-2b; measurable once UI-PC's scan view draws it, FIX_PLAN_v2 #13).
/// </summary>
public static class ToolsChecks
{
    private const string Owner = "TOOLS";

    /// <summary>The gameplay scene Build Office UI writes.</summary>
    public const string GameplayScene = "Assets/Scenes/OfficeGameplay.unity";

    /// <summary>How much larger than the torch's own meshes its click box may be on any axis (the builder's 5 mm margin on a 23 cm torch, with room).</summary>
    public const float ClickBoxSlack = 1.25f;

    /// <summary>BUG-4b: the torch's click box is the torch's own size (1019a's took in the beam's 32 cm cone, 2.6 times the torch, and swallowed the chip reader's clicks).</summary>
    [GateCheck("tools.torch.clickBox", Owner)]
    public static GateResult TorchClickBox()
    {
        Scene open = SceneManager.GetSceneByPath(GameplayScene);
        bool wasLoaded = open.IsValid() && open.isLoaded;
        if (!wasLoaded && AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScene) == null)
            return GateResult.Fail("tools.torch.clickBox", Owner, "no scene", $"{GameplayScene} is missing.");
        Scene scene = wasLoaded ? open : EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Additive);
        try
        {
            UvTorch torch = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UvTorch>(true)).FirstOrDefault();
            if (torch == null)
                return GateResult.Fail("tools.torch.clickBox", Owner, "no UV torch", "OfficeGameplay has no UvTorch (Build Office UI builds it with its art).");
            BoxCollider box = torch.GetComponent<BoxCollider>();
            Transform body = torch.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != torch.transform && t.name == "UvTorch");
            if (box == null || body == null)
                return GateResult.Fail("tools.torch.clickBox", Owner, box == null ? "no click box" : "no torch body", "Rebuild OfficeGameplay with Build Office UI.");
            Bounds own = default;
            bool any = false;
            foreach (MeshFilter f in body.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || f.GetComponentsInParent<Transform>(true).Any(t => t.name == "BeamFX"))
                    continue;
                Bounds b = f.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 p = torch.transform.InverseTransformPoint(f.transform.TransformPoint(corner));
                    if (any)
                        own.Encapsulate(p);
                    else
                        own = new Bounds(p, Vector3.zero);
                    any = true;
                }
            }
            Vector3 ratio = new Vector3(box.size.x / Mathf.Max(1e-4f, own.size.x), box.size.y / Mathf.Max(1e-4f, own.size.y), box.size.z / Mathf.Max(1e-4f, own.size.z));
            float worst = Mathf.Max(ratio.x, ratio.y, ratio.z);
            bool covers = box.size.x + 1e-4f >= own.size.x && box.size.y + 1e-4f >= own.size.y && box.size.z + 1e-4f >= own.size.z;
            bool pass = any && covers && worst <= ClickBoxSlack;
            return GateResult.Of("tools.torch.clickBox", Owner, pass,
                $"box {box.size.x:0.000} x {box.size.y:0.000} x {box.size.z:0.000} m, torch {own.size.x:0.000} x {own.size.y:0.000} x {own.size.z:0.000} m (largest ratio {worst:0.00})",
                pass ? string.Empty : covers ? $"The box is more than {ClickBoxSlack:0.00}x the torch: rebuild OfficeGameplay (BuildUvTorch leaves BeamFX out)." : "The box does not cover the torch.");
        }
        finally
        {
            if (!wasLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>BUG-2c: the torch swept over a planted paper, P-FAST, P-SLOW and P-WOBBLE, in the office and reading views (tools_torch.json): the reveal ≥95% unoccluded on every held frame, the pool never off the desk.</summary>
    [GateCheck("tools.torch", Owner, GateKind.Play)]
    public static GateResult Torch() => GateResults.Require("tools.torch", Owner, "tools_torch.tools.torch.*");

    /// <summary>BUG-4b: at A1 the chip reader's centre presses the reader 10/10 (tools_clicks.json).</summary>
    [GateCheck("tools.clicks", Owner, GateKind.Play)]
    public static GateResult Clicks() => GateResults.Require("tools.clicks", Owner, "tools_clicks.tools.clicks.*");

    /// <summary>BUG-52: day 8 with the PC closed, hovering papers and an answer: 0 phantom translations, 0 PC texts, revert ≤0.2 s (tools_lens.json).</summary>
    [GateCheck("tools.lens", Owner, GateKind.Play)]
    public static GateResult Lens() => GateResults.Require("tools.lens", Owner, "tools_lens.tools.lens.*");

    /// <summary>BUG-2b: the scan view's microprint at A2 has capitals ≥12 px and is credited once drawn (tools_microprint.json; known red until UI-PC's scan view draws it, FIX_PLAN_v2 #13).</summary>
    [GateCheck("tools.micro", Owner, GateKind.Play)]
    public static GateResult Micro() => GateResults.Require("tools.micro", Owner, "tools_microprint.tools.micro.*");
}
