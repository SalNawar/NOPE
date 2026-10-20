using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The scene integrity gate (BUG-30: "a build gate on missing scripts and
/// null materials"): scans scenes for GameObjects with a missing script,
/// enabled renderers with a null material or the error (magenta) shader
/// (except a Helix River screen, whose material its HelixRiverMonitor makes
/// in Awake: <see cref="MaterialMadeAtRuntime"/>),
/// texts whose font is LiberationSans (the TMP default no culture uses,
/// BUG-3/37: TEXT-LANG's), and objects the builder retired that are still
/// there (<see cref="Retired"/>, BUG-30's Office/GateLever). Used by the
/// Office UI builder after a build, and by QaRigChecks on every scene of the
/// build settings (opened additively, read only, closed unsaved).
/// </summary>
public static class QaSceneScan
{
    /// <summary>
    /// Objects a builder no longer builds and must not be in a shipped scene,
    /// by hierarchy path (BUG-30: faa5391 deleted GateLever.cs but left the
    /// object). OfficeSceneUIBuilder.Build sweeps these out of OfficeGameplay.
    /// </summary>
    public static readonly string[] Retired = { "Office/GateLever" };

    /// <summary>What a scan found, as "scene: path" lines per kind.</summary>
    public sealed class Findings
    {
        /// <summary>GameObjects with a missing script.</summary>
        public readonly List<string> MissingScripts = new List<string>();

        /// <summary>Renderers drawing with no material or the error shader.</summary>
        public readonly List<string> BrokenMaterials = new List<string>();

        /// <summary>Texts set in a LiberationSans font asset.</summary>
        public readonly List<string> LiberationSans = new List<string>();

        /// <summary>Retired objects still present.</summary>
        public readonly List<string> RetiredPresent = new List<string>();

        /// <summary>Adds <paramref name="other"/>'s findings.</summary>
        public void Add(Findings other)
        {
            MissingScripts.AddRange(other.MissingScripts);
            BrokenMaterials.AddRange(other.BrokenMaterials);
            LiberationSans.AddRange(other.LiberationSans);
            RetiredPresent.AddRange(other.RetiredPresent);
        }
    }

    /// <summary>
    /// True when <paramref name="r"/>'s material is made in play mode, so its
    /// empty slot in the saved scene is not a magenta block: the desk monitor's
    /// Helix River quad (Office/DeskRiver), whose HelixRiverMonitor gives it a
    /// runtime material in Awake. In play mode the runtime probe
    /// (QaGame.BrokenMaterials) still checks it once it is shown.
    /// </summary>
    private static bool MaterialMadeAtRuntime(Renderer r) => r != null && r.GetComponent<HelixRiverMonitor>() != null;

    /// <summary>Scans one loaded scene.</summary>
    public static Findings Scan(Scene scene)
    {
        var f = new Findings();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                    f.MissingScripts.Add(QaGame.Where(go));
                foreach (Renderer r in go.GetComponents<Renderer>())
                    if (r.enabled && !(r is ParticleSystemRenderer) && !MaterialMadeAtRuntime(r) && QaGame.IsBroken(r))
                        f.BrokenMaterials.Add(QaGame.Where(go));
                TMP_Text text = go.GetComponent<TMP_Text>();
                if (text != null && QaGame.IsLiberationSans(text.font))
                    f.LiberationSans.Add(QaGame.Where(go));
            }
            foreach (string path in Retired)
            {
                string prefix = root.name + "/";
                if (path.StartsWith(prefix, StringComparison.Ordinal) && root.transform.Find(path.Substring(prefix.Length)) != null)
                    f.RetiredPresent.Add(scene.name + ": " + path);
            }
        }
        return f;
    }

    /// <summary>
    /// Scans each scene of <paramref name="paths"/>: an open one as it is, a
    /// closed one opened additively, read and closed without saving. Never
    /// saves anything.
    /// </summary>
    public static Findings ScanScenes(IEnumerable<string> paths)
    {
        var all = new Findings();
        foreach (string path in paths)
        {
            Scene open = SceneManager.GetSceneByPath(path);
            bool wasLoaded = open.IsValid() && open.isLoaded;
            Scene scene = wasLoaded ? open : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                all.Add(Scan(scene));
            }
            finally
            {
                if (!wasLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
        return all;
    }

    /// <summary>The enabled scenes of the build settings (what the demo ships).</summary>
    public static List<string> ShippedScenes() => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
}
