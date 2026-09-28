using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class AnimeHallLayerInstaller
{
    const string Folder = "Assets/Art/Office/AnimeHallLayers";
    const string Report = "ArtDeliverables/TimeDesk/HallLayers/AnimeRegistered";
    const string SourceScene = "Assets/Art/Office/TerminalLayered/LayeredTerminal.unity";
    const int ArtLayer = 29;
    [Serializable] public sealed class Entry
    {
        public string id;
        public string file;
        public int order;
        public float[] normal;
    }
    [Serializable] public sealed class Manifest
    {
        public int width, height;
        public string source;
        public Entry[] layers;
    }

    [MenuItem("Tools/Terminal Art/Install Registered Anime Hall")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Install the art in Edit mode.");
        var manifestPath = Folder + "/layers.json";
        if (!File.Exists(manifestPath)) throw new FileNotFoundException("Layer extraction has not completed.",manifestPath);
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
        if (manifest.layers == null || manifest.layers.Length < 8) throw new InvalidDataException("Incomplete layer manifest.");
        foreach (var entry in manifest.layers)
            if (!File.Exists(Folder + "/Textures/" + entry.file)) throw new FileNotFoundException(entry.file);

        Directory.CreateDirectory(Folder + "/Materials");
        Directory.CreateDirectory(Report);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ShaderUtil.allowAsyncCompilation = false;
        var shader = Shader.Find("NOPE/Anime Hall Registered Layers");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Hall shader is unavailable or has errors.");

        // Preserve the previously saved art scene, including its approved 3D desk.
        var source = SceneManager.GetSceneByPath(SourceScene);
        bool openedSource = !source.isLoaded;
        if (openedSource) source = EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Additive);
        var sourceRoot = source.GetRootGameObjects().First(g => g.name == "Layered illustrated terminal");
        var sourceCamera = sourceRoot.GetComponentInChildren<Camera>();
        var sourceDesk = sourceRoot.transform.Cast<Transform>().First(t => t.name.StartsWith("Approved 3D desk"));
        var previousInstallation = SceneManager.GetSceneByPath(Folder + "/AnimeHall.unity");
        if (previousInstallation.isLoaded)
        {
            if (previousInstallation.isDirty)
                throw new InvalidOperationException("Save the current AnimeHall scene before reinstalling its generated art.");
            EditorSceneManager.CloseScene(previousInstallation,true);
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var root = new GameObject("Anime terminal - registered art and preserved desk");
        var desk = UnityEngine.Object.Instantiate(sourceDesk.gameObject,root.transform);
        desk.name = sourceDesk.name;
        var cameraObject = UnityEngine.Object.Instantiate(sourceCamera.gameObject,root.transform);
        cameraObject.name = "Anime hall player preview";
        var camera = cameraObject.GetComponent<Camera>();
        camera.transform.SetPositionAndRotation(new Vector3(0,2.16f,-2.62f),Quaternion.Euler(4,0,0));
        camera.fieldOfView = 55;
        camera.aspect = 16f/9;
        camera.depth = 100;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.07f,.075f,.09f);
        camera.cullingMask = 1 << ArtLayer;
        camera.GetUniversalAdditionalCameraData().SetRenderer(1);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        camera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;
        camera.allowHDR = false;

        var hall = new GameObject("Registered hall layers").transform;
        hall.SetParent(root.transform,false);
        var rig = hall.gameObject.AddComponent<AnimeHallPresentation>();
        const float distance = 20f;
        float viewHeight = 2f * distance * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
        float artHeight = viewHeight * .7f;
        float artWidth = artHeight * manifest.width / manifest.height;
        float viewWidth = viewHeight * camera.aspect;
        var forward = camera.ViewportToWorldPoint(new Vector3(1f - artWidth/viewWidth*.5f,.65f,distance));
        var left = camera.ViewportToWorldPoint(new Vector3(artWidth/viewWidth*.5f,.65f,distance));
        hall.SetPositionAndRotation(forward,camera.transform.rotation);
        rig.forwardLocalPosition = hall.localPosition;
        rig.leftLocalPosition = root.transform.InverseTransformPoint(left);

        var layers = new List<AnimeHallPresentation.Layer>();
        foreach (var entry in manifest.layers.OrderBy(e => e.order))
        {
            string path = Folder + "/Textures/" + entry.file;
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = 9;
            settings.spritePivot = Vector2.one * .5f;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite || sprite.rect.width != manifest.width || sprite.rect.height != manifest.height)
                throw new InvalidDataException("Registration or import size changed: " + entry.id);
            var gameObject = new GameObject(entry.id);
            gameObject.transform.SetParent(hall,false);
            gameObject.transform.localScale = Vector3.one * (artHeight / (manifest.height/100f));
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = entry.order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            string materialPath = Folder + "/Materials/" + entry.id + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material,materialPath); }
            material.shader = shader;
            var n = entry.normal != null && entry.normal.Length == 3
                ? new Vector3(entry.normal[0],entry.normal[1],entry.normal[2]).normalized : Vector3.back;
            material.SetVector("_SurfaceNormal",new Vector4(n.x,n.y,n.z,0));
            material.SetFloat("_LightingAmount",0);
            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(material);
            layers.Add(new AnimeHallPresentation.Layer { id=entry.id, renderer=renderer });
        }
        rig.layers = layers.ToArray();
        var sun = new GameObject("Realtime hall daylight").AddComponent<Light>();
        sun.transform.SetParent(hall,false);
        sun.type = LightType.Directional;
        sun.lightmapBakeType = LightmapBakeType.Realtime;
        sun.transform.rotation = Quaternion.Euler(38,-35,0);
        sun.cullingMask = 1 << ArtLayer;
        sun.shadows = LightShadows.Soft;
        rig.daylight = sun;
        var local = new GameObject("Movable warm maintenance light").AddComponent<Light>();
        local.transform.SetParent(hall,false);
        local.type = LightType.Point;
        local.lightmapBakeType = LightmapBakeType.Realtime;
        local.transform.position = forward - camera.transform.forward * 2f;
        local.range = 12;
        local.intensity = 2;
        local.color = new Color(1,.61f,.32f);
        local.cullingMask = 1 << ArtLayer;
        local.enabled = false;
        rig.localLight = local;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ArtLayer;
        rig.Apply();
        PrefabUtility.SaveAsPrefabAsset(hall.gameObject,Folder + "/AnimeHallArt.prefab");
        foreach (var id in AssetDatabase.FindAssets("",new[]{Folder}))
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(id)));
        EditorSceneManager.SaveScene(scene,Folder + "/AnimeHall.unity");
        if (openedSource || !source.isDirty) EditorSceneManager.CloseScene(source,true);
        Selection.activeGameObject = hall.gameObject;
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.delayCall += () => VerifyAndCapture(camera,rig,desk,scene);
    }

    static void VerifyAndCapture(Camera camera,AnimeHallPresentation rig,GameObject desk,Scene scene)
    {
        try
        {
            rig.lightingAmount = 0;
            rig.Apply();
            Capture(camera,"unity-neutral-reference.png",1920,1080);
            rig.lightingAmount = 1;
            rig.evening = 0;
            rig.Apply();
            Capture(camera,"unity-day.png",1920,1080);
            rig.evening = 1;
            rig.Apply();
            Capture(camera,"unity-evening.png",1920,1080);
            rig.localLight.enabled = true;
            Capture(camera,"unity-evening-local-light.png",1920,1080);
            rig.localLight.enabled = false;
            rig.evening = 0;
            rig.lightingAmount = 0;
            rig.lookLeft = 1;
            rig.Apply();
            Capture(camera,"unity-left-pan.png",1920,1080);
            rig.lookLeft = 0;
            rig.lightingAmount = 1;
            rig.Apply();
            Capture(camera,"unity-installed.png",1920,1080);
            var errors = ShaderUtil.GetShaderMessages(Shader.Find("NOPE/Anime Hall Registered Layers"))
                .Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n",errors.Select(e=>e.message)));
            File.WriteAllText(Report + "/unity-validation.txt",
                "Scene: " + scene.path + "\nRegistered sprite layers: " + rig.layers.Length +
                "\nPreserved desk renderers: " + desk.GetComponentsInChildren<Renderer>().Length +
                "\nShader errors: 0\nRealtime lights only. No lightmap baking was run." +
                "\nCamera rendering: URP forward. Shared camera-facing normals; per-layer material normals remain editable." +
                "\nGameplay source and original art scene were not saved or rebuilt.\n");
            EditorSceneManager.SaveScene(scene);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText(Report + "/installation-error.txt",e.ToString()); }
    }
    static void Capture(Camera camera,string filename,int width,int height)
    {
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var rt = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);
            image.Apply();
            File.WriteAllBytes(Report + "/" + filename,image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
