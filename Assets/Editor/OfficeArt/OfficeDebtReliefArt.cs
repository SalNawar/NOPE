using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Art-side editor tools for the Debt Relief palette and desk-layout passes on
/// OfficeScene (Tools > Office Art > Debt Relief). Four files: this one holds the
/// baseline capture and the protection hashes; .Apply the palette, textures and
/// portal clearance; .LayoutWear the five desk notes and the worn surfaces;
/// .Preview the play-mode capture helpers. Each pass refuses to save when the
/// protected PC and floor or any gameplay component data changed during it, and
/// writes after.json and validation.json next to its baseline
/// (ArtDeliverables/TimeDesk/PaletteExploration/Applied, .../LayoutWear).
/// Editor only: no runtime component.
/// </summary>
public static partial class OfficeDebtReliefArt
{
    const string ArtFolder = "Assets/Art/Office/DebtRelief";
    const string ReportFolder = "ArtDeliverables/TimeDesk/PaletteExploration/Applied";
    const string ScenePath = "Assets/Scenes/OfficeScene.unity";
    /// <summary>
    /// Objects no art pass may change: the PC (the scene contract's PCScreen fallback and
    /// the booth PC model), the contract's gameplay anchors, the original hall floor and
    /// the pack floor kept for comparison. Their subtrees, components and material files
    /// are hashed, and the passes skip their renderers.
    /// </summary>
    static readonly string[] ProtectedRoots = {
        "ImportedOfficeDress/Desk/Retro CRT", "HybridOffice/Booth/Finish_PC",
        OfficeContract.AnchorRoot, "HybridOffice/Hall/Blender_HallFloor",
        "ImportedOfficeDress/Hall floor"
    };
    /// <summary>One material slot as captured.</summary>
    [Serializable] public class MaterialInfo
    {
        /// <summary>The material's name.</summary>
        public string name;

        /// <summary>Its asset path (Apply Colours reloads the original material from it).</summary>
        public string path;

        /// <summary>Its shader's name.</summary>
        public string shader;

        /// <summary>Its _BaseColor (else _Color) as RRGGBBAA; empty when it has neither.</summary>
        public string colour;

        /// <summary>The asset path of its _BaseMap (else _MainTex); empty when none.</summary>
        public string texture;
    }

    /// <summary>One enabled art renderer as captured.</summary>
    [Serializable] public class RenderInfo
    {
        /// <summary>Its hierarchy path from the scene root.</summary>
        public string path;

        /// <summary>World position.</summary>
        public Vector3 position;

        /// <summary>World Euler angles.</summary>
        public Vector3 rotation;

        /// <summary>World (lossy) scale.</summary>
        public Vector3 scale;

        /// <summary>World bounds minimum.</summary>
        public Vector3 min;

        /// <summary>World bounds maximum.</summary>
        public Vector3 max;

        /// <summary>Its material slots, in order.</summary>
        public MaterialInfo[] materials;
    }

    /// <summary>A scene capture: a pass's baseline (before.json) or its result (after.json).</summary>
    [Serializable] public class AuditReport
    {
        /// <summary>The captured scene's path.</summary>
        public string scene;

        /// <summary>The hash of the protected roots (<see cref="ProtectedHash"/>).</summary>
        public string protectedHash;

        /// <summary>The hash of every gameplay component's data (<see cref="GameplayHash"/>).</summary>
        public string gameplayHash;

        /// <summary>The main camera's world position.</summary>
        public Vector3 cameraPosition;

        /// <summary>The main camera's world Euler angles.</summary>
        public Vector3 cameraRotation;

        /// <summary>Every enabled art renderer.</summary>
        public RenderInfo[] renderers;
    }

    /// <summary>The active scene (the art office when a pass runs).</summary>
    static Scene Scene => SceneManager.GetActiveScene();
    /// <summary>A transform's hierarchy path from its scene root ("HybridOffice/Booth/Finish_Mat").</summary>
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
    /// <summary>Every transform of the active scene, inactive ones included.</summary>
    static Transform[] Transforms() => Scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
    /// <summary>The transform at a hierarchy path, or null.</summary>
    static Transform Find(string path) => Transforms().FirstOrDefault(t => PathOf(t) == path);
    /// <summary>The transform at a hierarchy path; throws when the art has moved or renamed it.</summary>
    static Transform Require(string path) => Find(path) ?? throw new InvalidOperationException("Missing art target: " + path);
    /// <summary>Refuses unless OfficeScene is the active scene in edit mode.</summary>
    static void CheckScene()
    {
        if (EditorApplication.isPlaying || Scene.path != ScenePath)
            throw new InvalidOperationException("Open OfficeScene in Edit mode.");
    }
    /// <summary>An enabled, visible renderer that is not a TMP text (the readouts are the game's to write).</summary>
    static bool IsArt(Renderer r) => r.enabled && r.gameObject.activeInHierarchy && !r.GetComponent<TMPro.TMP_Text>();
    /// <summary>SHA-256 of a string, as hex.</summary>
    static string Hash(string s)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "");
    }
    /// <summary>
    /// Hash of the protected roots: each one present or absent; every transform under it
    /// (path, active state, every component's serialized data); and the text of every
    /// material file its renderers use, disabled ones included. So it also changes
    /// when such a material file is edited, pruned or re-pointed (then run Refresh
    /// Baseline Hashes).
    /// </summary>
    static string ProtectedHash()
    {
        var s = new StringBuilder(); var assets = new SortedSet<string>();
        foreach (var root in ProtectedRoots)
        {
            var t = Find(root); s.AppendLine(root + ":" + (t ? "present" : "absent"));
            if (!t) continue;
            foreach (var x in t.GetComponentsInChildren<Transform>(true).OrderBy(PathOf))
            {
                s.AppendLine(PathOf(x)); s.AppendLine(x.gameObject.activeSelf.ToString());
                foreach (var c in x.GetComponents<Component>())
                    if (c) s.AppendLine(c.GetType().FullName + EditorJsonUtility.ToJson(c));
                foreach (var r in x.GetComponents<Renderer>())
                foreach (var m in r.sharedMaterials)
                    if (m) assets.Add(AssetDatabase.GetAssetPath(m));
            }
        }
        foreach (var path in assets)
            if (File.Exists(path)) s.AppendLine(path + File.ReadAllText(path));
        return Hash(s.ToString());
    }
    /// <summary>
    /// Hash of every Assembly-CSharp MonoBehaviour's serialized data in the scene. That
    /// covers the leftover gameplay objects and the art's own runtime components
    /// (crowd palette, traffic, window glass), so a change to one of those classes'
    /// fields also changes it.
    /// </summary>
    static string GameplayHash()
    {
        var s = new StringBuilder();
        foreach (var t in Transforms().OrderBy(PathOf))
        foreach (var c in t.GetComponents<MonoBehaviour>())
            if (c && c.GetType().Assembly.GetName().Name == "Assembly-CSharp")
                s.AppendLine(PathOf(t) + c.GetType().FullName + EditorJsonUtility.ToJson(c));
        return Hash(s.ToString());
    }
    /// <summary>Captures the scene: both hashes, the main camera and every enabled art renderer.</summary>
    static AuditReport Capture()
    {
        var cam = Camera.main;
        return new AuditReport {
            scene = Scene.path, protectedHash = ProtectedHash(), gameplayHash = GameplayHash(),
            cameraPosition = cam ? cam.transform.position : Vector3.zero,
            cameraRotation = cam ? cam.transform.eulerAngles : Vector3.zero,
            renderers = Transforms().SelectMany(t => t.GetComponents<Renderer>()).Where(IsArt).Select(r => new RenderInfo {
                path = PathOf(r.transform), position = r.transform.position, rotation = r.transform.eulerAngles,
                scale = r.transform.lossyScale, min = r.bounds.min, max = r.bounds.max,
                materials = r.sharedMaterials.Select(m => m ? new MaterialInfo {
                    name = m.name, path = AssetDatabase.GetAssetPath(m), shader = m.shader.name,
                    colour = m.HasProperty("_BaseColor") ? ColorUtility.ToHtmlStringRGBA(m.GetColor("_BaseColor")) : m.HasProperty("_Color") ? ColorUtility.ToHtmlStringRGBA(m.GetColor("_Color")) : "",
                    texture = m.HasProperty("_BaseMap") ? AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")) : m.HasProperty("_MainTex") ? AssetDatabase.GetAssetPath(m.GetTexture("_MainTex")) : ""
                } : new MaterialInfo()).ToArray()
            }).ToArray()
        };
    }
    /// <summary>Writes the palette pass's baseline (Applied/before.json) once; refuses to overwrite it.</summary>
    [MenuItem("Tools/Office Art/Debt Relief/Capture Before")]
    public static void CaptureBefore()
    {
        CheckScene(); Directory.CreateDirectory(ReportFolder);
        if (File.Exists(ReportFolder + "/before.json")) throw new InvalidOperationException("Before capture already exists; preserve the baseline.");
        File.WriteAllText(ReportFolder + "/before.json", JsonUtility.ToJson(Capture(), true));
        Debug.Log("Debt-relief art baseline captured; scene unchanged.");
    }
    /// <summary>
    /// Re-stamps the protected and gameplay hashes of both baselines (Applied/before.json and
    /// LayoutWear/before.json) from the open scene, keeping their renderer records: Apply Colours
    /// still reads the original materials from them, so never re-capture instead. Run it once after
    /// an intentional change outside the art passes (the 2026-09-26 code cleanup, deleting the legacy
    /// gameplay objects the scene contract lists, pruning disabled pack children); the validators
    /// then guard against the next unintended change.
    /// </summary>
    [MenuItem("Tools/Office Art/Debt Relief/Refresh Baseline Hashes")]
    public static void RefreshBaselineHashes()
    {
        CheckScene();
        string protectedNow = ProtectedHash(), gameplayNow = GameplayHash();
        var refreshed = new List<string>();
        foreach (var path in new[] { ReportFolder + "/before.json", LayoutReport + "/before.json" })
        {
            if (!File.Exists(path)) continue;
            var baseline = JsonUtility.FromJson<AuditReport>(File.ReadAllText(path));
            baseline.protectedHash = protectedNow; baseline.gameplayHash = gameplayNow;
            File.WriteAllText(path, JsonUtility.ToJson(baseline, true));
            refreshed.Add(path);
        }
        if (refreshed.Count == 0) throw new InvalidOperationException("No baseline to refresh: run Capture Before first.");
        Debug.Log("Debt-relief baseline hashes refreshed from the open scene: " + string.Join(", ", refreshed));
    }
}
