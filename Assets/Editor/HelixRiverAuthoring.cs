using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The builders' shared Helix River parts (Build Office UI and Build Home
/// UI): the tuning asset (HelixRiver_Default, created once with the
/// defaults, never overwritten), the shader, a uGUI river (a RawImage, which
/// the culture themes never recolour, tagged DiegeticDevice: the river is the
/// office's hardware whatever the culture) and the wiring every
/// HelixRiverMonitor in a scene gets (the tuning, the shader, the shift).
/// </summary>
public static class HelixRiverAuthoring
{
    /// <summary>The river's tuning asset.</summary>
    public const string SettingsPath = "Assets/Data/Config/HelixRiver_Default.asset";

    /// <summary>TimeDesk/HelixRiver's file.</summary>
    public const string ShaderPath = "Assets/Shaders/HelixRiver.shader";

    /// <summary>The tuning asset, created with the defaults when missing (an Inspector knob: never overwritten).</summary>
    public static HelixRiverSO EnsureSettings()
    {
        HelixRiverSO settings = AssetDatabase.LoadAssetAtPath<HelixRiverSO>(SettingsPath);
        if (settings != null)
            return settings;
        PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
        settings = ScriptableObject.CreateInstance<HelixRiverSO>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    /// <summary>
    /// A uGUI river under <paramref name="parent"/> named <paramref name="name"/>
    /// (found or created; its place re-applied): a RawImage stretched over
    /// the anchors with the offsets, tagged DiegeticDevice, with a
    /// HelixRiverMonitor showing <paramref name="zoom"/> of the glass with
    /// rounded corners (<paramref name="corner"/>, a share of its height).
    /// <paramref name="hoverable"/> lets it catch the pointer (for a hover hint).
    /// </summary>
    public static HelixRiverMonitor UiRiver(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
                                            float zoom, float corner, bool hoverable)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null)
            go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        RawImage glass = go.GetComponent<RawImage>();
        if (glass == null)
            glass = go.AddComponent<RawImage>();
        glass.color = Color.white;
        glass.raycastTarget = hoverable;
        SceneUiKit.Tag(glass, ThemeRoleId.DiegeticDevice, ThemePart.Fill);

        HelixRiverMonitor river = go.GetComponent<HelixRiverMonitor>();
        if (river == null)
            river = go.AddComponent<HelixRiverMonitor>();
        Shape(river, zoom, corner);
        return river;
    }

    /// <summary>Sets how much of the glass <paramref name="river"/> shows (<paramref name="zoom"/>) and its corner radius (<paramref name="corner"/>, a share of its height).</summary>
    public static void Shape(HelixRiverMonitor river, float zoom, float corner)
    {
        var so = new SerializedObject(river);
        so.FindProperty("zoom").floatValue = zoom;
        so.FindProperty("corner").floatValue = corner;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Gives every HelixRiverMonitor in <paramref name="scene"/> the tuning, the shader and <paramref name="game"/> (the shift; null in Home).</summary>
    public static void Wire(Scene scene, GameManager game)
    {
        HelixRiverSO settings = EnsureSettings();
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
            Debug.LogError($"[TimeDesk] {ShaderPath} is missing: the Helix River cannot be drawn.");
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (HelixRiverMonitor river in root.GetComponentsInChildren<HelixRiverMonitor>(true))
            {
                var so = new SerializedObject(river);
                so.FindProperty("settings").objectReferenceValue = settings;
                so.FindProperty("shader").objectReferenceValue = shader;
                so.FindProperty("game").objectReferenceValue = game;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
