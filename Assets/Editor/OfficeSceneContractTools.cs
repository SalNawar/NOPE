using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The office scene contract's editor tools (docs/SCENE_CONTRACT_GAMEPLAY.md):
/// Check Office Scene Contract lists how each anchor resolves in the art office
/// (read-only: the scene is never saved); Add Gameplay Anchors is for the art
/// side: in the open art office it creates GameplayAnchors/Anchor_{id} for
/// each place the gameplay would otherwise put on its default pose (the
/// scanner, the traveller, the hand-over point, the PC's power knob), at that
/// pose, to be moved to where the art wants them.
/// </summary>
public static class OfficeSceneContractTools
{
    /// <summary>The art office.</summary>
    private const string ArtScenePath = "Assets/Scenes/OfficeScene.unity";

    /// <summary>The contract asset.</summary>
    private const string ContractPath = "Assets/Data/Config/OfficeSceneContract.asset";

    /// <summary>Opens the art office (when it is not open and nothing is unsaved) and logs how every anchor resolves.</summary>
    [MenuItem("Tools/TimeDesk/Check Office Scene Contract")]
    public static void Check()
    {
        Scene art = SceneManager.GetSceneByPath(ArtScenePath);
        if (!art.IsValid() || !art.isLoaded)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            art = EditorSceneManager.OpenScene(ArtScenePath, OpenSceneMode.Single);
        }

        Debug.Log(Report(art));
    }

    /// <summary>
    /// How every anchor of the contract resolves in <paramref name="art"/>: one
    /// line each (id, source, what found it, where), then the counts of
    /// explicit anchors, fallbacks, default poses and missing ones.
    /// </summary>
    public static string Report(Scene art)
    {
        OfficeSceneContractSO contract = AssetDatabase.LoadAssetAtPath<OfficeSceneContractSO>(ContractPath);
        if (contract == null)
            return $"[OfficeSceneContract] No contract at {ContractPath}: run Tools > TimeDesk > Build Office UI.";

        Dictionary<OfficeAnchorId, ResolvedAnchor> anchors = OfficeAnchors.Resolve(art, contract);
        var sb = new StringBuilder($"[OfficeSceneContract] {art.path}\n");
        foreach (ResolvedAnchor a in anchors.Values.OrderBy(a => (int)a.Id))
        {
            string where = a.HasBounds ? $"bounds centre {a.Bounds.center:F3} size {a.Bounds.size:F3}"
                : a.Id == OfficeAnchorId.PCPower && a.Source == AnchorSource.Default ? "derived from the PC glass at load (bottom right of the bezel)"
                : $"at {a.Position:F3}";
            sb.AppendLine($"  {a.Id,-17} {a.Source,-8} {(a.Path.Length > 0 ? a.Path : "-"),-45} {(a.Found ? where : "")}");
        }

        int Count(AnchorSource s) => anchors.Values.Count(a => a.Source == s);
        sb.AppendLine($"  anchors {Count(AnchorSource.Anchor)}, fallbacks {Count(AnchorSource.Fallback)}, defaults {Count(AnchorSource.Default)}, missing {Count(AnchorSource.Missing)}");
        return sb.ToString();
    }

    /// <summary>
    /// For the art side, in the open art office: creates the GameplayAnchors
    /// root (top level, so no art builder rebuilds it away) and an empty
    /// Anchor_{id} for every place now on its default pose, at that pose (the
    /// PC's power knob on the knob the gameplay measures from the glass).
    /// Existing anchors are left alone. The scene is marked dirty, not saved.
    /// </summary>
    [MenuItem("Tools/TimeDesk/Add Gameplay Anchors (art office)")]
    public static void AddAnchors()
    {
        Scene art = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || art.path != ArtScenePath)
        {
            Debug.LogError($"[OfficeSceneContract] Open {ArtScenePath} (edit mode, active scene) to add its gameplay anchors. Nothing was changed.");
            return;
        }

        OfficeSceneContractSO contract = AssetDatabase.LoadAssetAtPath<OfficeSceneContractSO>(ContractPath);
        if (contract == null)
        {
            Debug.LogError($"[OfficeSceneContract] No contract at {ContractPath}: run Tools > TimeDesk > Build Office UI first. Nothing was changed.");
            return;
        }

        Dictionary<OfficeAnchorId, ResolvedAnchor> anchors = OfficeAnchors.Resolve(art, contract);
        var added = new List<string>();
        foreach (ResolvedAnchor a in anchors.Values.Where(a => a.Source == AnchorSource.Default).OrderBy(a => (int)a.Id))
        {
            Vector3 position = a.Position;
            if (a.Id == OfficeAnchorId.PCPower && !TryKnob(anchors[OfficeAnchorId.PCScreen], anchors[OfficeAnchorId.OfficeCamera], out position))
                continue;

            GameObject root = art.GetRootGameObjects().FirstOrDefault(g => g.name == OfficeContract.AnchorRoot);
            if (root == null)
            {
                root = new GameObject(OfficeContract.AnchorRoot);
                Undo.RegisterCreatedObjectUndo(root, "Add gameplay anchors");
            }

            var anchor = new GameObject(OfficeContract.AnchorName(a.Id));
            Undo.RegisterCreatedObjectUndo(anchor, "Add gameplay anchors");
            anchor.transform.SetParent(root.transform, false);
            anchor.transform.SetPositionAndRotation(position, a.Rotation);
            added.Add(anchor.name);
        }

        if (added.Count > 0)
            EditorSceneManager.MarkSceneDirty(art);
        Debug.Log(added.Count > 0
            ? $"[OfficeSceneContract] Added {string.Join(", ", added)} under {OfficeContract.AnchorRoot}: move them where the art wants them and save the scene."
            : "[OfficeSceneContract] Every anchor with a default pose already has an object; nothing was added.");
    }

    /// <summary>The PC's power knob, measured from its glass as the office camera sees it (GlassFrame.PowerKnob); false without a glass or a camera.</summary>
    private static bool TryKnob(ResolvedAnchor pc, ResolvedAnchor camera, out Vector3 knob)
    {
        knob = Vector3.zero;
        if (pc.Transform == null || camera.Transform == null)
            return false;

        (Renderer glass, int submesh) = OfficeAnchors.FindGlass(pc.Transform);
        MeshFilter filter = glass != null ? glass.GetComponent<MeshFilter>() : null;
        if (filter == null || filter.sharedMesh == null)
            return false;

        Bounds local = filter.sharedMesh.GetSubMesh(Mathf.Clamp(submesh, 0, filter.sharedMesh.subMeshCount - 1)).bounds;
        knob = GlassFrame.Measure(glass.transform, local, camera.Transform.position).PowerKnob;
        return true;
    }
}
