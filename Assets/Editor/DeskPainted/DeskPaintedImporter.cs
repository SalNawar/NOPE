using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The painted desk's importer (desk v3, DESK-ART; UNITY_PLAN 9.4): the
/// offline bake (E:\unity\NOPE-tools\design\desk\pipeline\export\v2, copied
/// into Assets/Art/Office/DeskPainted: Meshes, Textures, Data/anchors.json)
/// as one prefab. On import it sets the folder's texture settings (paint in
/// sRGB, masks linear, BC7 with mips at their own size, clamped, trilinear)
/// and model settings (metres as baked, no materials, animation, cameras or
/// lights, normals as baked). Tools > TimeDesk > Import Painted Desk (Build)
/// makes one material per texture on NOPE/Desk Painted (the paint, the mask
/// with its glow, time of day and locked channels; the locked homes'
/// inserts desaturated), the glass's sheen, the lamps' jewels and the PC's
/// screen, then the prefab Prefabs/DeskPainted.prefab: the shell and the
/// fixed objects (they cast no shadows: theirs are in the paint; only what
/// moves casts, UNITY_PLAN 4.4), every movable object as home root (at its
/// pivot), Slide child, mesh (the slide-in moves only the Slide; one that
/// slides on its own with a click box on the Interactable layer), the six
/// lamps (one bezel each and a jewel facing the office eye, from
/// anchors.json) and the PC's screen quad on its cut-out (anchors.json's
/// pc_screen). It lays out every painted home (DeskPainted's Homes and
/// FixedArt, through DeskPaintedMap.Homes: the rectangles of desk v3's
/// layout, Data/desk_layout_v3.json, the heights measured on the meshes) and
/// checks every pivot against anchors.json. It touches no scene (Build Office UI puts the prefab
/// into the gameplay scene; the office binder installs it).
/// </summary>
public sealed class DeskPaintedImporter : AssetPostprocessor
{
    /// <summary>The painted desk's art folder (DESK-ART owns it).</summary>
    public const string Folder = "Assets/Art/Office/DeskPainted";

    /// <summary>The prefab Build writes and Build Office UI places.</summary>
    public const string PrefabPath = Folder + "/Prefabs/DeskPainted.prefab";

    private const string MeshFolder = Folder + "/Meshes", TextureFolder = Folder + "/Textures", MaterialFolder = Folder + "/Materials";
    private const string AnchorsPath = Folder + "/Data/anchors.json";

    /// <summary>Desk v3's layout as data (E:\unity\NOPE-tools\design\desk\pipeline\scripts\export_layout.py from layout.py; UNITY_PLAN 9.3).</summary>
    private const string LayoutPath = Folder + "/Data/desk_layout_v3.json";
    private const string PaintedShader = "NOPE/Desk Painted", UnlitShader = "Universal Render Pipeline/Unlit";

    /// <summary>The bake's FBX files, in the order their objects are laid.</summary>
    private static readonly string[] Models = { "DeskShell", "DeskObjects", "Desk_PC" };

    /// <summary>The empty on the PC's screen face in Desk_PC.fbx (its centre; the size and facing are anchors.json's pc_screen).</summary>
    private const string ScreenAnchor = "PC_ScreenAnchor";

    /// <summary>The office camera's eye in the bake's frame (Unity axes: right, up, away from the chair; layout.CAMERAS office), which the lamps' jewels face.</summary>
    private static readonly Vector3 OfficeEye = new Vector3(0f, 2.16f, -2.10f);

    /// <summary>
    /// Unity's FBX importer reads the bake (exported with forward Z, up Y)
    /// half a turn about the vertical from the bake's frame: (x, y, z) comes in
    /// as (-x, y, -z), every object turned 180 degrees (measured on import,
    /// 2026-10-21; the bake's README expected (x, z, y) of the DeskFrame from
    /// a Blender round trip). The importer turns every object back by this.
    /// </summary>
    private static readonly Quaternion Upright = Quaternion.AngleAxis(180f, Vector3.up);

    /// <summary>How far the PC's screen quad stands in front of its cut-out (metres), toward the chair.</summary>
    private const float ScreenStandOff = 0.002f;

    /// <summary>The largest pivot error the build accepts against anchors.json (metres).</summary>
    private const float PivotTolerance = 0.001f;

    /// <summary>Bumped when the import settings change, so Unity imports the folder again.</summary>
    public override uint GetVersion() => 1;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TextureFolder + "/"))
            return;
        var importer = (TextureImporter)assetImporter;
        string name = Path.GetFileNameWithoutExtension(assetPath);
        bool mask = name.EndsWith("_Mask");
        bool alpha = name == "Glass_Sheen" || name.StartsWith("Lamp_");
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = !mask;
        importer.mipmapEnabled = true;
        importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        importer.alphaIsTransparency = alpha;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 8;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
        standalone.overridden = true;
        standalone.maxTextureSize = 4096;
        standalone.format = TextureImporterFormat.BC7;
        importer.SetPlatformTextureSettings(standalone);
    }

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(MeshFolder + "/"))
            return;
        var importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        // The PC's CRT is its own click shape (OfficeSceneBinder.ClickOnMesh: a MeshCollider made at bind needs a readable mesh in a build).
        importer.isReadable = assetPath.EndsWith("/Desk_PC.fbx", System.StringComparison.Ordinal);
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.importNormals = ModelImporterNormals.Import;
        importer.importTangents = ModelImporterTangents.None;
        importer.generateSecondaryUV = false;
        importer.addCollider = false;
    }

    /// <summary>Builds the materials and the prefab (see the class); logs what it checked and throws when a pivot is off, a model or a texture is missing.</summary>
    [MenuItem("Tools/TimeDesk/Import Painted Desk")]
    public static void Build()
    {
        ContentNode anchors = ContentJson.Parse(File.ReadAllText(AnchorsPath));
        EnsureFolder(MaterialFolder);
        EnsureFolder(Folder + "/Prefabs");
        Dictionary<string, Material> materials = BuildMaterials();

        // Rebuilt inside the existing prefab when there is one, so its root and its DeskPainted keep their ids (the gameplay scene's binder points at that component).
        bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        GameObject root = existing ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("DeskPainted");
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        var log = new StringBuilder();
        try
        {
            Transform shell = Child(root.transform, "Shell"), fixedRoot = Child(root.transform, "Fixed"), homesRoot = Child(root.transform, "Homes");
            var parts = new List<DeskPainted.Part>();
            float worstPivot = 0f;
            Transform screenAnchor = null;
            GameObject bezel = null;
            foreach (string model in Models)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{MeshFolder}/{model}.fbx");
                if (asset == null)
                    throw new FileNotFoundException($"[DeskPainted] {MeshFolder}/{model}.fbx is missing.");
                foreach (Transform source in asset.transform)
                {
                    if (source.name == ScreenAnchor)
                    {
                        screenAnchor = source;
                        continue;
                    }
                    if (source.name == DeskPaintedMap.LampBezel)
                    {
                        bezel = source.gameObject;
                        continue;
                    }
                    PaintedObject painted = DeskPaintedMap.Find(source.name);
                    if (painted == null)
                    {
                        log.AppendLine($"skipped {model}/{source.name} (not in DeskPaintedMap.Objects)");
                        continue;
                    }
                    worstPivot = Mathf.Max(worstPivot, PivotError(anchors, source));
                    parts.Add(Lay(source, painted, shell, fixedRoot, homesRoot, materials, anchors));
                }
            }
            if (worstPivot > PivotTolerance)
                throw new System.InvalidOperationException($"[DeskPainted] A pivot is {worstPivot * 1000f:0.0} mm from anchors.json: the FBX axes are not the bake's (Unity = (x, z, y) of the DeskFrame).");
            foreach (PaintedObject o in DeskPaintedMap.Objects)
                if (!parts.Exists(p => p.name == o.name))
                    throw new System.InvalidOperationException($"[DeskPainted] The bake has no {o.name}.");

            Transform lamps = Child(root.transform, "Lamps");
            if (bezel != null)
                LayLamps(lamps, bezel, anchors, materials);
            Renderer screen = LayScreen(parts.First(p => p.name == "PC_Crt").mesh, screenAnchor, anchors, materials);
            float pad = PadTop(parts.First(p => p.name == DeskPaintedMap.Mat).mesh);

            (DeskHome[] homes, DeskHome[] fixedArt) = Measure(root.transform, parts, pad);
            if (!root.TryGetComponent(out DeskPainted desk))
                desk = root.AddComponent<DeskPainted>();
            desk.Configure(parts.ToArray(), homes, fixedArt, pad, screen);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

            log.Insert(0, $"[DeskPainted] {PrefabPath} built: {parts.Count} objects, {lamps.childCount} lamps, worst pivot {worstPivot * 1000f:0.00} mm, the pad at {pad:0.0000} m, {homes.Length} homes, {fixedArt.Length} fixed art, {materials.Count} materials.\n");
            foreach (DeskHome h in homes.Concat(fixedArt))
                log.AppendLine($"home {h.id}: ({h.x:0.000}, {h.y:0.000}) {h.width:0.000} x {h.depth:0.000}, {h.height:0.000} high, {h.show}, slide {h.slide}");
            Debug.Log(log.ToString());
        }
        finally
        {
            if (existing)
                PrefabUtility.UnloadPrefabContents(root);
            else
                Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>The distance between <paramref name="source"/>'s pivot as imported (turned Upright) and anchors.json's pivot_unity for it (0 when anchors.json does not list it).</summary>
    private static float PivotError(ContentNode anchors, Transform source)
    {
        ContentNode entry = anchors.Get("objects")?.Get(source.name);
        ContentNode pivot = entry?.Get("pivot_unity");
        if (pivot == null || pivot.Items.Count < 3)
            return 0f;
        var want = new Vector3(Num(pivot.Items[0]), Num(pivot.Items[1]), Num(pivot.Items[2]));
        return Vector3.Distance(want, Upright * source.position);
    }

    private static float Num(ContentNode node) => float.Parse(node.Text, System.Globalization.CultureInfo.InvariantCulture);

    private static Vector3 Vec(ContentNode node) => new Vector3(Num(node.Items[0]), Num(node.Items[1]), Num(node.Items[2]));

    /// <summary>
    /// Copies the bake's <paramref name="source"/> into the prefab where it
    /// was baked (turned Upright): the shell under Shell (casting no shadow), a fixed object
    /// under Fixed, a movable one as Homes/Home_name (at its pivot) with its
    /// Slide child holding it; its renderers take its texture's material
    /// (anchors.json's texture, its children their parent's); the scanner's
    /// lid becomes PaintedLid.
    /// </summary>
    private static DeskPainted.Part Lay(Transform source, PaintedObject painted, Transform shell, Transform fixedRoot, Transform homesRoot,
                                        Dictionary<string, Material> materials, ContentNode anchors)
    {
        GameObject copy = Object.Instantiate(source.gameObject);
        copy.name = source.name;
        Transform mesh = copy.transform;
        Transform root = mesh;
        switch (painted.role)
        {
            case PaintedRole.Shell:
                mesh.SetParent(shell, false);
                break;
            case PaintedRole.Fixed:
                mesh.SetParent(fixedRoot, false);
                break;
            default:
                root = Child(homesRoot, "Home_" + source.name);
                root.localPosition = Upright * source.position;
                Transform slide = Child(root, "Slide");
                mesh.SetParent(slide, false);
                break;
        }
        mesh.SetPositionAndRotation(Upright * source.position, Upright * source.rotation);
        mesh.localScale = source.lossyScale;

        Material material = MaterialFor(source.name, anchors, materials);
        foreach (Renderer r in mesh.GetComponentsInChildren<Renderer>(true))
        {
            if (material != null)
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            r.shadowCastingMode = painted.role == PaintedRole.Shell || painted.role == PaintedRole.Fixed ? ShadowCastingMode.Off : ShadowCastingMode.On;
            r.receiveShadows = true;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
        Transform lid = mesh.Find(DeskPaintedMap.Lid);
        if (lid != null && painted.home == DeskHomeMap.Scanner)
            lid.name = DeskPaintedMap.PaintedLid;
        if (painted.role == PaintedRole.Slider)
            ClickBox(mesh);
        return new DeskPainted.Part { name = source.name, root = root, mesh = mesh };
    }

    /// <summary>How far a painted slider's click box reaches past its mesh on every side (metres; DeskHomeMap.ClickPad, as the built objects').</summary>
    private const float ClickPad = DeskHomeMap.ClickPad;

    /// <summary>
    /// A painted slider no gameplay object stands for (the glasses) gets a
    /// box on the Interactable layer around its mesh, riding its slide: the
    /// slide-in ranks the silhouettes under the cursor by those boxes (a
    /// cursor on it takes it at once, as on every machine), and its owner's
    /// click (GLASSES) lands on it later.
    /// </summary>
    private static void ClickBox(Transform mesh)
    {
        var filter = mesh.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            return;
        int layer = LayerMask.NameToLayer(OfficeLayers.Interactable);
        if (layer < 0)
            throw new System.InvalidOperationException($"[DeskPainted] The project has no {OfficeLayers.Interactable} layer (Build Office UI adds it).");
        mesh.gameObject.layer = layer;
        var box = mesh.gameObject.AddComponent<BoxCollider>();
        Bounds b = filter.sharedMesh.bounds;
        box.center = b.center;
        box.size = b.size + Vector3.one * (2f * ClickPad);
    }

    /// <summary>The material of the bake's <paramref name="name"/>: its texture's (anchors.json; DeskShell and DeskMat their own, the glass the sheen's).</summary>
    private static Material MaterialFor(string name, ContentNode anchors, Dictionary<string, Material> materials)
    {
        string texture = anchors.Get("objects")?.Get(name)?.Get("texture")?.Text;
        string key = texture != null ? Path.GetFileNameWithoutExtension(texture) : null;
        return key != null && materials.TryGetValue(key, out Material m) ? m : null;
    }

    /// <summary>
    /// One material per paint texture (keyed by the texture's name): NOPE/Desk
    /// Painted with the paint and its mask (when there is one), the locked
    /// homes' inserts fully desaturated by their mask's B; the glass's sheen
    /// and the lamps' jewel unlit and blended; the PC's screen unlit black
    /// (the desktop's clone draws over it).
    /// </summary>
    private static Dictionary<string, Material> BuildMaterials()
    {
        var made = new Dictionary<string, Material>();
        foreach (string path in Directory.GetFiles(TextureFolder, "*_Paint.png").Select(p => p.Replace('\\', '/')))
        {
            string key = Path.GetFileNameWithoutExtension(path);
            string baseName = key.Substring(0, key.Length - "_Paint".Length);
            var paint = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{baseName}_Mask.png");
            if (paint == null)
                throw new FileNotFoundException($"[DeskPainted] {path} did not import.");
            string materialName = baseName == "DeskShell" ? "DeskPainted_Shell" : baseName == "Desk_Mat" ? "DeskPainted_Mat" : baseName;
            bool locked = baseName == "Desk_BabelHome" || baseName == "Desk_RolodexHome" || baseName == "Desk_CardsHome";
            made[key] = EnsureMaterial(materialName, PaintedShader, m =>
            {
                m.SetTexture("_BaseMap", paint);
                m.SetTexture("_MaskMap", mask);
                m.SetFloat("_UseMask", mask != null ? 1f : 0f);
                if (mask != null)
                    m.EnableKeyword("_MASKMAP");
                else
                    m.DisableKeyword("_MASKMAP");
                m.SetFloat("_Locked", locked ? 1f : 0f);
                m.SetFloat("_Glow", DeskPaintedMap.Glow(baseName)); // 1, the AVAILABLE sign's 0.35 (the review of the Unity capture)
            });
        }
        made["Glass_Sheen"] = EnsureMaterial("Desk_Glass", UnlitShader, m => Blended(m, AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/Glass_Sheen.png"), Color.white));
        made["Lamp_Jewel"] = EnsureMaterial("Desk_LampJewel", UnlitShader, m => Blended(m, AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/Lamp_Jewel.png"), Color.white));
        made["Screen"] = EnsureMaterial("Desk_PC_Screen", UnlitShader, m =>
        {
            m.SetTexture("_BaseMap", null);
            m.SetColor("_BaseColor", new Color(0.02f, 0.03f, 0.03f, 1f));
        });
        return made;
    }

    /// <summary>An unlit material showing <paramref name="texture"/> tinted <paramref name="tint"/>, alpha blended over what is behind it.</summary>
    private static void Blended(Material m, Texture2D texture, Color tint)
    {
        m.SetTexture("_BaseMap", texture);
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_ZWrite", 0f);
        UnityEditor.BaseShaderGUI.SetMaterialKeywords(m);
        m.renderQueue = (int)RenderQueue.Transparent;
    }

    /// <summary>The material <paramref name="name"/> in the folder on <paramref name="shader"/> (made when missing), set up by <paramref name="setup"/> every build (the bake owns these; nobody tunes them by hand).</summary>
    private static Material EnsureMaterial(string name, string shader, System.Action<Material> setup)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader s = Shader.Find(shader);
        if (s == null)
            throw new System.InvalidOperationException($"[DeskPainted] The shader {shader} is missing.");
        if (material == null)
        {
            material = new Material(s) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != s)
        {
            material.shader = s;
        }
        setup(material);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>The six lamps (anchors.json's lamps): the bake's bezel at each base and a jewel quad at each jewel centre, as wide as the jewel, facing the office eye.</summary>
    private static void LayLamps(Transform lamps, GameObject bezel, ContentNode anchors, Dictionary<string, Material> materials)
    {
        Material bezelMaterial = MaterialFor(DeskPaintedMap.LampBezel, anchors, materials);
        Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        foreach (KeyValuePair<string, ContentNode> lamp in anchors.Get("lamps").Members)
        {
            Transform at = Child(lamps, lamp.Key);
            GameObject b = Object.Instantiate(bezel, at);
            b.name = DeskPaintedMap.LampBezel;
            b.transform.SetPositionAndRotation(Vec(lamp.Value.Get("bezel_base_unity")), Upright * bezel.transform.rotation);
            b.transform.localScale = bezel.transform.lossyScale;
            foreach (Renderer r in b.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = bezelMaterial;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }

            var jewel = new GameObject("Jewel", typeof(MeshFilter), typeof(MeshRenderer));
            jewel.transform.SetParent(at, false);
            Vector3 centre = Vec(lamp.Value.Get("jewel_centre_unity"));
            float size = Num(lamp.Value.Get("jewel_radius_m")) * 2f;
            jewel.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(centre - OfficeEye, Vector3.up));
            jewel.transform.localScale = new Vector3(size, size, 1f);
            jewel.GetComponent<MeshFilter>().sharedMesh = quad;
            var renderer = jewel.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = materials["Lamp_Jewel"];
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    /// <summary>
    /// The PC's screen (anchors.json's pc_screen): a quad of its width and
    /// height at its centre, facing the chair turned with the PC (its face
    /// toward -y of the desk's frame turned 35 degrees about the vertical),
    /// standing ScreenStandOff in front of the cut-out, under the CRT.
    /// </summary>
    private static Renderer LayScreen(Transform crt, Transform anchor, ContentNode anchors, Dictionary<string, Material> materials)
    {
        ContentNode spec = anchors.Get("pc_screen");
        Vector3 centre = anchor != null ? Upright * anchor.position : Vec(spec.Get("centre_unity"));
        float w = Num(spec.Get("width_m")), h = Num(spec.Get("height_m"));
        const float turn = 35f;
        Vector3 facing = Quaternion.AngleAxis(-turn, Vector3.up) * Vector3.back; // the face's normal: toward the chair, turned toward the mat
        var screen = new GameObject("Screen", typeof(MeshFilter), typeof(MeshRenderer));
        screen.transform.SetParent(crt, false);
        screen.transform.SetPositionAndRotation(centre + facing * ScreenStandOff, Quaternion.LookRotation(-facing, Vector3.up));
        Vector3 parent = crt.lossyScale;
        screen.transform.localScale = new Vector3(w / parent.x, h / parent.y, 1f / parent.z);
        screen.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var renderer = screen.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = materials["Screen"];
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return renderer;
    }

    /// <summary>
    /// Every painted home and fixed art (DeskPaintedMap.Homes) in the desk's
    /// frame (the bake's frame is the game's: x right, y away from the chair,
    /// metres from the art mat's centre): its rectangle desk v3's layout gives
    /// it (Data/desk_layout_v3.json, layout.py's homes: the designed homes the
    /// bake was built on), else the rectangle its objects cover from above
    /// (the PC's mouse, which the layout lists elsewhere: it was moved with
    /// its cable in the bake); its height the highest point of its objects
    /// over the painted mat's pad, where the papers lie (the shell's deal
    /// tray: DESK-PLAN's).
    /// </summary>
    private static (DeskHome[] homes, DeskHome[] fixedArt) Measure(Transform root, List<DeskPainted.Part> parts, float deskTop)
    {
        ContentNode layout = ContentJson.Parse(File.ReadAllText(LayoutPath)).Get("homes");
        DeskHome[] classic = DeskHomeMap.Approved();
        var boxes = new Dictionary<string, (float x0, float y0, float x1, float y1, float top)>();
        foreach (PaintedObject o in DeskPaintedMap.Objects.Where(o => o.home != null))
        {
            Transform mesh = parts.First(p => p.name == o.name).mesh;
            (float x0, float y0, float x1, float y1, float top) box = boxes.TryGetValue(o.home, out var had) ? had : (float.MaxValue, float.MaxValue, float.MinValue, float.MinValue, float.MinValue);
            foreach (MeshFilter filter in mesh.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                bool standsUp = filter.name == DeskPaintedMap.PaintedLid || filter.name == DeskPaintedMap.Flap;
                Bounds b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = root.InverseTransformPoint(filter.transform.TransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z)));
                    box.top = Mathf.Max(box.top, c.y - deskTop);
                    if (standsUp)
                        continue;
                    box = (Mathf.Min(box.x0, c.x), Mathf.Min(box.y0, c.z), Mathf.Max(box.x1, c.x), Mathf.Max(box.y1, c.z), box.top);
                }
            }
            boxes[o.home] = box;
        }
        DeskHome dealTray = DeskHomeMap.Find(classic, DeskHomeMap.DealTray);
        return DeskPaintedMap.Homes(
            id =>
            {
                ContentNode rect = layout.Get(DeskPaintedMap.LayoutId(id))?.Get("rect");
                if (rect != null && id != DeskPaintedMap.PcMouse)
                    return (Num(rect.Items[0]), Num(rect.Items[1]), Num(rect.Items[2]), Num(rect.Items[3]));
                return boxes.TryGetValue(id, out var box) ? (box.x0, box.y0, box.x1, box.y1) : ((float, float, float, float)?)null;
            },
            id => boxes.TryGetValue(id, out var box) ? box.top : dealTray != null ? dealTray.height : 0f,
            classic);
    }

    /// <summary>
    /// The height of the mat's pad (metres over the bake's floor) where a
    /// vertical line through the mat's centre meets its highest face there
    /// (its brass corners and the counter strip stand higher, at its edges).
    /// </summary>
    private static float PadTop(Transform mat)
    {
        Mesh mesh = mat.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float best = float.MinValue;
        var at = new Vector2(0f, 0f);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = mat.TransformPoint(vertices[triangles[i]]), b = mat.TransformPoint(vertices[triangles[i + 1]]), c = mat.TransformPoint(vertices[triangles[i + 2]]);
            Vector2 a2 = new Vector2(a.x, a.z), b2 = new Vector2(b.x, b.z), c2 = new Vector2(c.x, c.z);
            float area = Cross(b2 - a2, c2 - a2);
            if (Mathf.Abs(area) < 1e-9f)
                continue;
            float u = Cross(b2 - at, c2 - at) / area, v = Cross(c2 - at, a2 - at) / area, w = 1f - u - v;
            if (u < 0f || v < 0f || w < 0f)
                continue;
            best = Mathf.Max(best, u * a.y + v * b.y + w * c.y);
        }
        if (best == float.MinValue)
            throw new System.InvalidOperationException("[DeskPainted] The mat has no face over its centre.");
        return best;
    }

    private static float Cross(Vector2 p, Vector2 q) => p.x * q.y - p.y * q.x;

    /// <summary>The child <paramref name="name"/> of <paramref name="parent"/> (made when missing).</summary>
    private static Transform Child(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null)
            return t;
        t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
