using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// The lighting step of Tools > TimeDesk > Add Anime Hall Hooks
/// (docs/HALL_LIGHTING.md): gives the anime hall its lights, shadows and
/// dust, reproducibly. It makes sure the project has the HallBackdrop layer
/// and the HallSky sorting layer (before Default); puts the hall's 58 painted
/// layers on the layer with URP's Sprite-Lit-Default material (the Light2Ds
/// light them; the office camera, forward, never draws them: HallBackdrop
/// shows the 2D Renderer's picture of them behind the desk) and the exterior
/// on HallSky (the layers are mutually exclusive masks, so it still draws where
/// it did, but only the sky light reaches it); creates the knobs (Assets/Data/Config/HallLighting_Default.asset),
/// the dust's mote and material; and adds the HallLighting root with its
/// HallLightingRig and HallBackdrop, a Plane (following the art's
/// presentation) carrying one Light2D per painted light source of the
/// inventory (the global light, the sky light, the window shafts, the ceiling fixtures in
/// their switching order, the screens, the lit signs, the portal rings), the
/// piers' ShadowCaster2Ds and three dust emitters, and a Desk group with the
/// preserved 3D desk's lamp and PC glow (3D lights). Each light is placed on
/// the canvas pixel the inventory measured (the painted layers' own
/// transform turns a pixel into a place). What exists is left alone, so a
/// second run changes nothing and Saleh's moves and tunings stay.
/// </summary>
public static class AnimeHallLightingHooks
{
    /// <summary>The knobs' asset.</summary>
    public const string SettingsPath = "Assets/Data/Config/HallLighting_Default.asset";

    /// <summary>URP's lit sprite material (its forward pass draws a sprite unlit, the fallback without the 2D pass).</summary>
    private const string SpriteLitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

    private const string TextureFolder = "Assets/Art/Office/Gameplay/Textures";
    private const string MotePath = TextureFolder + "/HallDustMote.png";
    private const string DustMaterialPath = "Assets/Art/Office/Gameplay/Materials/HallDust.mat";
    private const string BackdropShaderPath = "Assets/Art/Office/Gameplay/Shaders/HallBackdrop.shader";
    private const string DustShaderPath = "Assets/Art/Office/Gameplay/Shaders/HallDust.shader";

    /// <summary>The root this step adds.</summary>
    public const string RootName = "HallLighting";

    private const string UndoName = "Add anime hall lighting";

    /// <summary>The painted layer whose transform and sprite turn a canvas pixel into a place (every layer shares them).</summary>
    private const string ReferenceLayer = "00 Structural masonry";

    /// <summary>The painted exterior (the sky and the city through the windows), which goes on the HallSky sorting layer.</summary>
    private const string SkyLayer = "03 Exterior placeholder single layer";

    /// <summary>The dust's sorting order: above the hall's 58 layers (0..57), under everything the office camera draws (the desk, the traveller).</summary>
    private const int DustOrder = 58;

    private static readonly Color Warm = new Color(1f, 0.86f, 0.66f);
    private static readonly Color Cool = new Color(0.72f, 0.86f, 1f);
    private static readonly Color Paper = new Color(1f, 0.96f, 0.9f);

    /// <summary>Turns canvas pixels (x right, y down) into the Plane's space.</summary>
    private sealed class Canvas
    {
        private readonly Transform _layer;
        private readonly Sprite _sprite;

        public Canvas(SpriteRenderer layer)
        {
            _layer = layer.transform;
            _sprite = layer.sprite;
        }

        /// <summary>Metres on the plane per canvas pixel.</summary>
        public float PerPixel => _layer.localScale.x / _sprite.pixelsPerUnit;

        /// <summary>The place of canvas pixel (<paramref name="x"/>, <paramref name="y"/>) in the presentation's (the Plane's) space.</summary>
        public Vector3 At(float x, float y)
        {
            float ppu = _sprite.pixelsPerUnit;
            var local = new Vector3((x - _sprite.pivot.x) / ppu, (_sprite.rect.height - y - _sprite.pivot.y) / ppu, 0f);
            return _layer.localPosition + _layer.localRotation * Vector3.Scale(_layer.localScale, local);
        }
    }

    /// <summary>Adds what the hall lacks (see the class summary); each addition is named in <paramref name="changes"/>.</summary>
    public static void Add(Scene hall, Camera camera, List<string> changes)
    {
        AnimeHallPresentation presentation = hall.GetRootGameObjects()
            .Select(g => g.GetComponentInChildren<AnimeHallPresentation>(true))
            .FirstOrDefault(p => p != null);
        SpriteRenderer reference = presentation != null ? presentation.FindLayer(ReferenceLayer) : null;
        if (presentation == null || reference == null || reference.sprite == null)
        {
            Debug.LogWarning($"[AnimeHallHooks] The hall has no presentation with the layer '{ReferenceLayer}': no lighting was added.");
            return;
        }

        if (OfficeLayers.HallBackdropLayer < 0)
        {
            OfficeSceneUIBuilder.EnsureLayer(OfficeLayers.HallBackdrop);
            changes.Add($"added the layer {OfficeLayers.HallBackdrop}");
        }
        int layer = OfficeLayers.HallBackdropLayer;
        if (layer < 0)
            return;
        if (!SortingLayer.layers.Any(l => l.name == OfficeLayers.SkySortingLayer))
        {
            OfficeSceneUIBuilder.EnsureSortingLayer(OfficeLayers.SkySortingLayer, first: true);
            changes.Add($"added the sorting layer {OfficeLayers.SkySortingLayer} before Default");
        }

        HallLightingSO settings = Settings(changes);
        Material dust = DustMaterial(changes);
        LitLayers(presentation, layer, changes);

        GameObject root = hall.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, UndoName);
            changes.Add($"created {RootName}");
        }

        var canvas = new Canvas(reference);
        Transform plane = Child(root.transform, "Plane", layer, Vector3.zero, changes);
        HallBackdrop backdrop = root.GetComponent<HallBackdrop>();
        if (backdrop == null)
        {
            backdrop = Undo.AddComponent<HallBackdrop>(root);
            var so = new SerializedObject(backdrop);
            so.FindProperty("source").objectReferenceValue = camera;
            so.FindProperty("composite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(BackdropShaderPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add($"added the HallBackdrop on {RootName}");
        }

        AddGlobal(plane, layer, changes);
        AddWindows(plane, canvas, layer, changes);
        AddFixtures(plane, canvas, layer, changes);
        AddScreensAndSigns(plane, canvas, layer, changes);
        AddPortals(plane, canvas, layer, changes);
        AddShadows(plane, canvas, layer, changes);
        List<ParticleSystem> motes = AddDust(plane, canvas, layer, dust, changes);
        AddDeskLights(hall, root.transform, camera, changes);

        HallLightingRig rig = root.GetComponent<HallLightingRig>();
        if (rig == null)
        {
            rig = Undo.AddComponent<HallLightingRig>(root);
            var so = new SerializedObject(rig);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("presentation").objectReferenceValue = presentation;
            so.FindProperty("plane").objectReferenceValue = plane;
            so.FindProperty("backdrop").objectReferenceValue = backdrop;
            SerializedProperty list = so.FindProperty("dust");
            list.arraySize = motes.Count;
            for (int i = 0; i < motes.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = motes[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add($"added the HallLightingRig on {RootName}");
        }
    }

    // -----------------------------
    // Assets
    // -----------------------------

    private static HallLightingSO Settings(List<string> changes)
    {
        var settings = AssetDatabase.LoadAssetAtPath<HallLightingSO>(SettingsPath);
        if (settings != null)
            return settings;
        settings = ScriptableObject.CreateInstance<HallLightingSO>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        changes.Add($"created {SettingsPath}");
        return settings;
    }

    /// <summary>The dust's material (TimeDesk/HallDust) with its soft mote (a 32 px radial falloff), made once.</summary>
    private static Material DustMaterial(List<string> changes)
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(MotePath) == null)
        {
            if (!AssetDatabase.IsValidFolder(TextureFolder))
                AssetDatabase.CreateFolder("Assets/Art/Office/Gameplay", "Textures");
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
                }
            File.WriteAllBytes(MotePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(MotePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(MotePath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            changes.Add($"created {MotePath}");
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        if (material == null)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(DustShaderPath)) { name = "HallDust" };
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(MotePath));
            AssetDatabase.CreateAsset(material, DustMaterialPath);
            AssetDatabase.SaveAssets();
            changes.Add($"created {DustMaterialPath}");
        }
        return material;
    }

    /// <summary>The painted layers on the backdrop layer, with the lit sprite material.</summary>
    private static void LitLayers(AnimeHallPresentation presentation, int layer, List<string> changes)
    {
        var lit = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitPath);
        int sky = SortingLayer.NameToID(OfficeLayers.SkySortingLayer);
        int moved = 0;
        foreach (AnimeHallPresentation.Layer l in presentation.layers)
        {
            SpriteRenderer r = l.renderer;
            bool isSky = l.id == SkyLayer;
            if (r == null || (r.gameObject.layer == layer && r.sharedMaterial == lit && (!isSky || r.sortingLayerID == sky)))
                continue;
            Undo.RecordObject(r.gameObject, UndoName);
            Undo.RecordObject(r, UndoName);
            r.gameObject.layer = layer;
            r.sharedMaterial = lit;
            if (isSky)
                r.sortingLayerID = sky;
            moved++;
        }
        if (moved > 0)
            changes.Add($"{moved} painted layers on {OfficeLayers.HallBackdrop} with Sprite-Lit-Default ('{SkyLayer}' on the sorting layer {OfficeLayers.SkySortingLayer})");
    }

    // -----------------------------
    // Lights
    // -----------------------------

    private static void AddGlobal(Transform plane, int layer, List<string> changes)
    {
        Light2D light = NewLight(plane, "Global light", HallLightKind.Global, 1f, 0, layer, Vector3.zero, changes);
        if (light != null)
        {
            light.lightType = Light2D.LightType.Global;
            light.blendStyleIndex = 0;
        }
        Light2D sky = NewLight(plane, "Sky light", HallLightKind.Sky, 1f, 0, layer, Vector3.zero, changes);
        if (sky != null)
        {
            sky.targetSortingLayers = new[] { SortingLayer.NameToID(OfficeLayers.SkySortingLayer) };
            sky.lightType = Light2D.LightType.Global;
            sky.blendStyleIndex = 0;
        }
    }

    /// <summary>The sun shafts: one beam through each of the three tall panes of the left curtain wall, slanting onto its platform and the concourse (the left pier shadows them), and two through the rear windows onto the rear hall; each turns about its pane's top.</summary>
    private static void AddWindows(Transform plane, Canvas c, int layer, List<string> changes)
    {
        Transform windows = Child(plane, "Windows", layer, Vector3.zero, changes);
        Beam(windows, c, layer, "Shaft - left pane 1", 95f, 260f, 40f, 700f, 0.55f, 1.35f, 0.3f, 50f, changes);
        Beam(windows, c, layer, "Shaft - left pane 2", 310f, 405f, 40f, 700f, 0.55f, 1.35f, 0.26f, 40f, changes);
        Beam(windows, c, layer, "Shaft - left pane 3", 440f, 505f, 40f, 700f, 0.55f, 1.35f, 0.22f, 34f, changes);
        Beam(windows, c, layer, "Shaft - rear windows left", 730f, 900f, 205f, 450f, 0.15f, 1.3f, 0.16f, 30f, changes);
        Beam(windows, c, layer, "Shaft - rear windows right", 1060f, 1240f, 205f, 450f, 0.15f, 1.3f, 0.16f, 30f, changes);
    }

    /// <summary>A beam from a pane's top edge (<paramref name="x0"/>..<paramref name="x1"/> at <paramref name="top"/>) down to <paramref name="bottom"/>, leaning right by <paramref name="lean"/> of its drop and widening by <paramref name="spread"/>.</summary>
    private static void Beam(Transform parent, Canvas c, int layer, string name, float x0, float x1, float top, float bottom, float lean, float spread, float intensity, float falloffPx, List<string> changes)
    {
        float drop = bottom - top, shift = lean * drop, mid = (x0 + x1) * 0.5f, half = (x1 - x0) * 0.5f * spread;
        Shaft(parent, c, layer, name, new Vector2(mid, top), new[]
        {
            new Vector2(mid + shift - half, bottom), new Vector2(mid + shift + half, bottom), new Vector2(x1, top), new Vector2(x0, top),
        }, intensity, falloffPx, changes);
    }

    /// <summary>A shaft pivoting at <paramref name="pivot"/> (the rig turns and lengthens it with the sun), its polygon in canvas pixels (counter-clockwise on screen).</summary>
    private static void Shaft(Transform parent, Canvas c, int layer, string name, Vector2 pivot, Vector2[] polygon, float intensity, float falloffPx, List<string> changes)
    {
        Vector3 at = c.At(pivot.x, pivot.y);
        Light2D light = NewLight(parent, name, HallLightKind.Window, intensity, 0, layer, at, changes);
        if (light == null)
            return;
        light.lightType = Light2D.LightType.Freeform;
        light.blendStyleIndex = 1;
        light.SetShapePath(polygon.Select(p => c.At(p.x, p.y) - at).ToArray());
        light.shapeLightFalloffSize = falloffPx * c.PerPixel;
        light.falloffIntensity = 0.6f;
        light.shadowsEnabled = true;
        light.shadowIntensity = 0.55f;
        light.shadowSoftness = 0.35f;
    }

    /// <summary>The painted ceiling fixtures (layers 01/02), in their dusk switching order (front row first): downward cones.</summary>
    private static void AddFixtures(Transform plane, Canvas c, int layer, List<string> changes)
    {
        Transform fixtures = Child(plane, "Fixtures", layer, Vector3.zero, changes);
        (string name, float x, float y, float radius)[] list =
        {
            ("Fixture 1 - front left", 922f, 35f, 520f),
            ("Fixture 2 - front right", 1068f, 35f, 520f),
            ("Fixture 3 - mid left", 828f, 117f, 420f),
            ("Fixture 4 - mid right", 1208f, 117f, 420f),
            ("Fixture 5 - rear left", 970f, 190f, 300f),
            ("Fixture 6 - rear right", 1059f, 190f, 300f),
        };
        for (int i = 0; i < list.Length; i++)
        {
            Light2D light = NewLight(fixtures, list[i].name, HallLightKind.Fixture, 0.9f, i, layer, c.At(list[i].x, list[i].y), changes);
            if (light == null)
                continue;
            light.lightType = Light2D.LightType.Point;
            light.blendStyleIndex = 0;
            light.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            light.pointLightOuterRadius = list[i].radius * c.PerPixel;
            light.pointLightInnerRadius = list[i].radius * 0.08f * c.PerPixel;
            light.pointLightOuterAngle = 150f;
            light.pointLightInnerAngle = 70f;
            light.falloffIntensity = 0.55f;
        }
    }

    /// <summary>The always-on screens (the Departure Board's display, the two small displays on the bridge, the vending machine's front, the kiosk at the right) and the lit door signs.</summary>
    private static void AddScreensAndSigns(Transform plane, Canvas c, int layer, List<string> changes)
    {
        Transform screens = Child(plane, "Screens", layer, Vector3.zero, changes);
        Panel(screens, c, layer, "Departure Board display", HallLightKind.Screen, new Rect(853f, 74f, 289f, 99f), 0.3f, Cool, 26f, changes);
        Panel(screens, c, layer, "Bridge display left", HallLightKind.Screen, new Rect(855f, 278f, 47f, 21f), 0.6f, Cool, 16f, changes);
        Panel(screens, c, layer, "Bridge display right", HallLightKind.Screen, new Rect(1142f, 278f, 48f, 21f), 0.6f, Cool, 16f, changes);
        Panel(screens, c, layer, "Vending machine", HallLightKind.Screen, new Rect(1720f, 476f, 44f, 106f), 0.75f, Paper, 46f, changes);
        Panel(screens, c, layer, "Kiosk", HallLightKind.Screen, new Rect(2100f, 142f, 40f, 92f), 0.55f, Warm, 30f, changes);

        Transform signs = Child(plane, "Signs", layer, Vector3.zero, changes);
        Panel(signs, c, layer, "Detention sign", HallLightKind.Sign, new Rect(1392f, 358f, 60f, 50f), 0.5f, Paper, 22f, changes);
        Panel(signs, c, layer, "Medbay sign", HallLightKind.Sign, new Rect(1484f, 382f, 72f, 68f), 0.5f, Paper, 22f, changes);
        Panel(signs, c, layer, "Executive suites sign", HallLightKind.Sign, new Rect(1598f, 410f, 82f, 86f), 0.5f, Paper, 22f, changes);
    }

    /// <summary>A lit rectangle (canvas pixels) with a soft halo of <paramref name="falloffPx"/>.</summary>
    private static void Panel(Transform parent, Canvas c, int layer, string name, HallLightKind kind, Rect rect, float intensity, Color colour, float falloffPx, List<string> changes)
    {
        Vector3 at = c.At(rect.center.x, rect.center.y);
        Light2D light = NewLight(parent, name, kind, intensity, 0, layer, at, changes);
        if (light == null)
            return;
        light.lightType = Light2D.LightType.Freeform;
        light.blendStyleIndex = 0;
        light.color = colour;
        light.SetShapePath(new[]
        {
            c.At(rect.xMin, rect.yMax) - at, c.At(rect.xMax, rect.yMax) - at,
            c.At(rect.xMax, rect.yMin) - at, c.At(rect.xMin, rect.yMin) - at,
        });
        light.shapeLightFalloffSize = falloffPx * c.PerPixel;
        light.falloffIntensity = 0.5f;
    }

    /// <summary>A light around each portal ring (the metal rings' layers 39..47), its portal's number its order.</summary>
    private static void AddPortals(Transform plane, Canvas c, int layer, List<string> changes)
    {
        Transform portals = Child(plane, "Portals", layer, Vector3.zero, changes);
        (int number, Rect ring)[] rings =
        {
            (1, Rect.MinMaxRect(938f, 405f, 1117f, 586f)),
            (2, Rect.MinMaxRect(863f, 328f, 946f, 407f)),
            (3, Rect.MinMaxRect(1098f, 329f, 1181f, 406f)),
            (4, Rect.MinMaxRect(1647f, 153f, 1754f, 264f)),
            (5, Rect.MinMaxRect(1877f, 135f, 2022f, 271f)),
        };
        foreach ((int number, Rect ring) in rings)
        {
            Light2D light = NewLight(portals, $"Portal {number:00} ring", HallLightKind.Portal, 0.9f, number, layer, c.At(ring.center.x, ring.center.y), changes);
            if (light == null)
                continue;
            light.lightType = Light2D.LightType.Point;
            light.blendStyleIndex = 0;
            light.pointLightOuterRadius = ring.width * 1.25f * c.PerPixel;
            light.pointLightInnerRadius = ring.width * 0.3f * c.PerPixel;
            light.falloffIntensity = 0.6f;
        }
    }

    /// <summary>The two piers between the windows and the hall shade the sun shafts (ShadowCaster2D rectangles over their painted columns).</summary>
    private static void AddShadows(Transform plane, Canvas c, int layer, List<string> changes)
    {
        Transform shadows = Child(plane, "Shadows", layer, Vector3.zero, changes);
        Caster(shadows, c, layer, "Pier - left elevator", Rect.MinMaxRect(552f, 0f, 692f, 418f), changes);
        Caster(shadows, c, layer, "Pier - right load bearing", Rect.MinMaxRect(1335f, 0f, 1473f, 472f), changes);
    }

    private static void Caster(Transform parent, Canvas c, int layer, string name, Rect rect, List<string> changes)
    {
        if (parent.Find(name) != null)
            return;
        Vector3 at = c.At(rect.center.x, rect.center.y);
        var go = new GameObject(name) { layer = layer };
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        var caster = go.AddComponent<ShadowCaster2D>();
        var so = new SerializedObject(caster);
        Vector3[] path =
        {
            c.At(rect.xMin, rect.yMax) - at, c.At(rect.xMax, rect.yMax) - at,
            c.At(rect.xMax, rect.yMin) - at, c.At(rect.xMin, rect.yMin) - at,
        };
        SerializedProperty shape = so.FindProperty("m_ShapePath");
        shape.arraySize = path.Length;
        for (int i = 0; i < path.Length; i++)
            shape.GetArrayElementAtIndex(i).vector3Value = path[i];
        so.FindProperty("m_ShapePathHash").intValue = name.GetHashCode() ^ 0x5eed;
        so.FindProperty("m_ShadowCastingSource").enumValueIndex = 1;
        so.FindProperty("m_SelfShadows").boolValue = false;
        SerializedProperty layers = so.FindProperty("m_ApplyToSortingLayers");
        layers.arraySize = 1;
        layers.GetArrayElementAtIndex(0).intValue = SortingLayer.NameToID("Default");
        so.ApplyModifiedPropertiesWithoutUndo();
        changes.Add($"added {RootName}/Plane/Shadows/{name}");
    }

    // -----------------------------
    // Dust
    // -----------------------------

    /// <summary>Three dust emitters (the rig applies their knobs): in the left window's shafts, in the rear hall under its windows, and in the right wing's air; each well under the Departure Board, so no mote drifts over its rows.</summary>
    private static List<ParticleSystem> AddDust(Transform plane, Canvas c, int layer, Material material, List<string> changes)
    {
        Transform dust = Child(plane, "Dust", layer, Vector3.zero, changes);
        return new List<ParticleSystem>
        {
            Emitter(dust, c, layer, material, "Dust - left shaft", Rect.MinMaxRect(180f, 300f, 800f, 640f), changes),
            Emitter(dust, c, layer, material, "Dust - rear hall", Rect.MinMaxRect(740f, 290f, 1300f, 440f), changes),
            Emitter(dust, c, layer, material, "Dust - right wing", Rect.MinMaxRect(1480f, 300f, 2100f, 600f), changes),
        }.Where(p => p != null).ToList();
    }

    private static ParticleSystem Emitter(Transform parent, Canvas c, int layer, Material material, string name, Rect area, List<string> changes)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.GetComponent<ParticleSystem>();

        var go = new GameObject(name) { layer = layer };
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = c.At(area.center.x, area.center.y);
        var ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 10f;
        main.startSpeed = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 16f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 40;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 3f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(area.width * c.PerPixel, area.height * c.PerPixel, 0.01f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.024f, 0.032f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.05f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.separateAxes = false;

        ParticleSystem.ColorOverLifetimeModule colour = ps.colorOverLifetime;
        colour.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        colour.color = new ParticleSystem.MinMaxGradient(fade);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerID = SortingLayer.NameToID("Default");
        renderer.sortingOrder = DustOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.maxParticleSize = 0.05f;
        changes.Add($"added {RootName}/Plane/Dust/{name}");
        return ps;
    }

    // -----------------------------
    // The preserved 3D desk
    // -----------------------------

    /// <summary>The desk's green lamp (a downward spot under its shade) and the PC's glow (a point in front of its glass): 3D lights on the art's layer, lighting the desk and the gameplay's papers as the daylight does.</summary>
    private static void AddDeskLights(Scene hall, Transform root, Camera camera, List<string> changes)
    {
        Transform desk = Child(root, "Desk", camera.gameObject.layer, Vector3.zero, changes);
        Transform shade = OfficeAnchors.Find(hall, "Clean_Lamp__DeskClean_Paper", includeInactive: false);
        Transform glass = OfficeAnchors.Find(hall, "CRT2_Glass", includeInactive: false);
        int mask = camera.cullingMask | OfficeLayers.GameplayMask;

        if (shade != null && shade.TryGetComponent(out Renderer lampShade) && desk.Find("Desk lamp") == null)
        {
            Bounds b = lampShade.bounds;
            Light lamp = NewLight3D(desk, "Desk lamp", HallLightKind.DeskLamp, 3.5f, new Vector3(b.center.x, b.min.y - 0.02f, b.center.z), LightType.Spot, mask, changes);
            lamp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            lamp.spotAngle = 115f;
            lamp.innerSpotAngle = 60f;
            lamp.range = 1.8f;
            lamp.color = new Color(1f, 0.82f, 0.55f);
        }

        if (glass != null && glass.TryGetComponent(out Renderer screen) && desk.Find("PC screen glow") == null)
        {
            Bounds b = screen.bounds;
            Vector3 toward = (camera.transform.position - b.center).normalized;
            Light glow = NewLight3D(desk, "PC screen glow", HallLightKind.DeskScreen, 1.6f, b.center + toward * 0.22f, LightType.Point, mask, changes);
            glow.range = 1.0f;
            glow.color = new Color(0.62f, 0.8f, 1f);
        }
    }

    private static Light NewLight3D(Transform parent, string name, HallLightKind kind, float intensity, Vector3 position, LightType type, int mask, List<string> changes)
    {
        var go = new GameObject(name) { layer = parent.gameObject.layer };
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.type = type;
        light.shadows = LightShadows.None;
        light.cullingMask = mask;
        light.intensity = intensity;
        var role = go.AddComponent<HallLight>();
        role.kind = kind;
        role.intensity = intensity;
        changes.Add($"added {RootName}/Desk/{name}");
        return light;
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    /// <summary>A Light2D named <paramref name="name"/> under <paramref name="parent"/> with its HallLight, or null when one exists (left as it is).</summary>
    private static Light2D NewLight(Transform parent, string name, HallLightKind kind, float intensity, int order, int layer, Vector3 localPosition, List<string> changes)
    {
        if (parent.Find(name) != null)
            return null;
        var go = new GameObject(name) { layer = layer };
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var light = go.AddComponent<Light2D>();
        light.targetSortingLayers = new[] { SortingLayer.NameToID("Default") };
        light.volumeIntensityEnabled = false;
        light.intensity = intensity;
        var role = go.AddComponent<HallLight>();
        role.kind = kind;
        role.intensity = intensity;
        role.order = order;
        changes.Add($"added {PathOf(go.transform)}");
        return light;
    }

    /// <summary>The child <paramref name="name"/> (made on <paramref name="layer"/> at <paramref name="localPosition"/> when missing).</summary>
    private static Transform Child(Transform parent, string name, int layer, Vector3 localPosition, List<string> changes)
    {
        Transform t = parent.Find(name);
        if (t != null)
            return t;
        var go = new GameObject(name) { layer = layer };
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        changes.Add($"added {PathOf(go.transform)}");
        return go.transform;
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
