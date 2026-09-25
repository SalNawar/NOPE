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

// Offline scene-art authoring only. No runtime component or gameplay dependency.
public static partial class OfficeDebtReliefArt
{
    const string ArtFolder = "Assets/Art/Office/DebtRelief";
    const string ReportFolder = "ArtDeliverables/TimeDesk/PaletteExploration/Applied";
    const string ScenePath = "Assets/Scenes/OfficeScene.unity";
    static readonly string[] ProtectedRoots = {
        "ImportedOfficeDress/Desk/Retro CRT", "HybridOffice/Booth/Finish_PC",
        "OfficeRoot/CRTMonitor", "HybridOffice/Hall/Blender_HallFloor",
        "ImportedOfficeDress/Hall floor"
    };
    [Serializable] public class MaterialInfo
    {
        public string name, path, shader, colour, texture;
    }
    [Serializable] public class RenderInfo
    {
        public string path; public Vector3 position, rotation, scale, min, max;
        public MaterialInfo[] materials;
    }
    [Serializable] public class AuditReport
    {
        public string scene, protectedHash, gameplayHash;
        public Vector3 cameraPosition, cameraRotation;
        public RenderInfo[] renderers;
    }
    static Scene Scene => SceneManager.GetActiveScene();
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
    static Transform[] Transforms() => Scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
    static Transform Find(string path) => Transforms().FirstOrDefault(t => PathOf(t) == path);
    static Transform Require(string path) => Find(path) ?? throw new InvalidOperationException("Missing art target: " + path);
    static void CheckScene()
    {
        if (EditorApplication.isPlaying || Scene.path != ScenePath)
            throw new InvalidOperationException("Open OfficeScene in Edit mode.");
    }
    static bool IsArt(Renderer r) => r.enabled && r.gameObject.activeInHierarchy && !r.GetComponent<TMPro.TMP_Text>();
    static string Hash(string s)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "");
    }
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
    static string GameplayHash()
    {
        var s = new StringBuilder();
        foreach (var t in Transforms().OrderBy(PathOf))
        foreach (var c in t.GetComponents<MonoBehaviour>())
            if (c && c.GetType().Assembly.GetName().Name == "Assembly-CSharp")
                s.AppendLine(PathOf(t) + c.GetType().FullName + EditorJsonUtility.ToJson(c));
        return Hash(s.ToString());
    }
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
    [MenuItem("Tools/Office Art/Debt Relief/Capture Before")]
    public static void CaptureBefore()
    {
        CheckScene(); Directory.CreateDirectory(ReportFolder);
        if (File.Exists(ReportFolder + "/before.json")) throw new InvalidOperationException("Before capture already exists; preserve the baseline.");
        File.WriteAllText(ReportFolder + "/before.json", JsonUtility.ToJson(Capture(), true));
        Debug.Log("Debt-relief art baseline captured; scene unchanged.");
    }
}
