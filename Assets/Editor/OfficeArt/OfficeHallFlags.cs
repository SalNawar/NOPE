using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Converts only the four hall cloth renderers into authored 2D sprites.</summary>
public static class OfficeHallFlags
{
    const string Folder = "Assets/Art/Office/HallFlags";
    const string Report = "ArtDeliverables/TimeDesk/HallFlags";
    const string TexturePath = Folder + "/HallFlag_Ochre.png";
    const string SpritePath = Folder + "/HallFlag_Ochre.asset";
    const string MaterialPath = Folder + "/HallFlag_Sprite.mat";
    const string SpriteName = "Flag Artwork 2D";
    const string ClothName = "Hall_Banner__Banner_Cloth";

    [Serializable] public class FlagCheck
    {
        public string path;
        public bool clothDisabled, hardwareRetained, spriteAssigned;
        public Vector3 originalSize, spriteSize;
    }
    [Serializable] public class Audit
    {
        public bool success;
        public string note = "Real SpriteRenderers in OfficeScene; supports and original floor are preserved.";
        public FlagCheck[] flags;
    }
    static Transform[] Roots()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/OfficeScene.unity")
            throw new InvalidOperationException("Open OfficeScene in Edit mode.");
        var hall = GameObject.Find("HybridOffice/Hall");
        if (!hall) throw new InvalidOperationException("Hall not found.");
        var roots = hall.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("Blender_Banner_", StringComparison.Ordinal)).ToArray();
        if (roots.Length != 4) throw new InvalidOperationException("Expected four hall banner roots.");
        return roots;
    }
    static Sprite ImportSprite()
    {
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var pixels = texture.GetPixels32();
        int x0 = texture.width, x1 = -1, y0 = texture.height, y1 = -1;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            if (pixels[y * texture.width + x].a < 128) continue;
            x0 = Math.Min(x0, x); x1 = Math.Max(x1, x);
            y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
        }
        if (x1 < x0) throw new InvalidOperationException("Flag has no opaque artwork.");
        // Sprite rect trims empty import margins; source PNG stays unchanged.
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (!sprite)
        {
            sprite = Sprite.Create(texture, new Rect(x0, y0, x1-x0+1, y1-y0+1),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = "HallFlag_Ochre";
            AssetDatabase.CreateAsset(sprite, SpritePath);
        }
        return sprite;
    }

    [MenuItem("Tools/Office Art/Install Hall Flag Sprites")]
    public static void Install()
    {
        var roots = Roots();
        foreach (var root in roots)
            if (!root.Find(ClothName)?.GetComponent<MeshFilter>())
                throw new InvalidOperationException("Missing source cloth: " + root.name);
        var sprite = ImportSprite();
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (!shader || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("URP sprite shader unavailable.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            material = new Material(shader) { name = "HallFlag_Sprite" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Convert hall flags to 2D sprites");
        foreach (var root in roots)
        {
            var cloth = root.Find(ClothName);
            var mesh = cloth.GetComponent<MeshFilter>().sharedMesh;
            var bounds = mesh.bounds;
            var existing = root.Find(SpriteName);
            GameObject art;
            if (existing) art = existing.gameObject;
            else
            {
                art = new GameObject(SpriteName);
                Undo.RegisterCreatedObjectUndo(art, "Create flag sprite");
                art.transform.SetParent(root, false);
            }
            Undo.RecordObject(art.transform, "Fit flag sprite to original cloth");
            // Original cloth lies in local XY; preserve its exact physical envelope.
            art.transform.position = cloth.TransformPoint(bounds.center);
            art.transform.rotation = cloth.rotation;
            art.transform.localScale = new Vector3(
                bounds.size.x * cloth.localScale.x / sprite.bounds.size.x,
                bounds.size.y * cloth.localScale.y / sprite.bounds.size.y, 1);
            art.layer = cloth.gameObject.layer;
            var renderer = art.GetComponent<SpriteRenderer>();
            if (!renderer) renderer = Undo.AddComponent<SpriteRenderer>(art);
            Undo.RecordObject(renderer, "Assign flag sprite");
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = Color.white;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            Undo.RecordObject(cloth.gameObject, "Disable modelled flag cloth");
            cloth.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cloth.gameObject);
        }
        Undo.CollapseUndoOperations(group);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Validate();
        if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()))
            throw new IOException("Could not save OfficeScene.");
    }

    [MenuItem("Tools/Office Art/Validate Hall Flag Sprites")]
    public static void Validate()
    {
        var report = new Audit { flags = Roots().Select(root =>
        {
            var cloth = root.Find(ClothName);
            var sprite = root.Find(SpriteName)?.GetComponent<SpriteRenderer>();
            var hardware = root.Find("Hall_Banner__Hall_DarkMetal")?.GetComponent<MeshRenderer>();
            return new FlagCheck {
                path = "HybridOffice/Hall/" + root.name,
                clothDisabled = !cloth.gameObject.activeSelf,
                hardwareRetained = hardware && hardware.enabled && hardware.gameObject.activeInHierarchy,
                spriteAssigned = sprite && sprite.sprite && sprite.enabled && sprite.gameObject.activeInHierarchy,
                originalSize = cloth.GetComponent<MeshRenderer>().bounds.size,
                spriteSize = sprite ? sprite.bounds.size : Vector3.zero
            };
        }).ToArray() };
        report.success = report.flags.All(f => f.clothDisabled && f.hardwareRetained && f.spriteAssigned &&
            Mathf.Abs(f.originalSize.x-f.spriteSize.x) < .03f &&
            Mathf.Abs(f.originalSize.y-f.spriteSize.y) < .03f);
        Directory.CreateDirectory(Report);
        File.WriteAllText(Report + "/validation.json", JsonUtility.ToJson(report, true));
        if (!report.success) throw new InvalidOperationException("Flag validation failed. Read report.");
        Debug.Log("Hall flags: 4 real 2D sprites, all cloth meshes disabled, supports retained.");
    }
}
