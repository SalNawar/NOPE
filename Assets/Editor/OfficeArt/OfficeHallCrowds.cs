using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Authors complete painted crowd groups. Only the OfficeHallCrowds hierarchy is replaced.</summary>
public static class OfficeHallCrowds
{
    const string Root = "OfficeHallCrowds";
    const string Folder = "Assets/Art/Office/HallCrowds";
    const string ReportFolder = "ArtDeliverables/TimeDesk/HallCrowds";
    const string Atlas = Folder + "/Textures/CrowdGroups_Atlas.png";
    // Art-directed against the oversized hall: the initial 1.7–1.9 heights read too small.
    const float HallScale = 1.30f;
    // x, z, tallest height, authored group, depth palette, mirror. Each entry is a whole crowd mass.
    static readonly (float x, float z, float height, int group, int palette, bool mirror)[] Layout =
    {
        (-5.1f, 7.8f, 1.78f, 3, 0, false), (5.0f, 8.6f, 1.82f, 0, 0, true),
        (-3.9f, 10.4f, 1.75f, 1, 1, false), (3.5f, 11.0f, 1.78f, 4, 1, true),
        (-7.2f, 11.9f, 1.86f, 5, 1, true), (7.1f, 12.3f, 1.76f, 2, 1, false),
        (-2.9f, 13.5f, 1.72f, 2, 1, true), (2.7f, 14.7f, 1.82f, 3, 1, false),
        (-9.1f, 15.6f, 1.80f, 0, 1, false), (9.3f, 16.0f, 1.85f, 1, 1, true),
        (-5.5f, 17.3f, 1.74f, 4, 2, true), (5.8f, 18.0f, 1.82f, 5, 2, false),
        (-10.8f, 20.6f, 1.76f, 3, 2, false), (10.7f, 21.4f, 1.73f, 2, 2, true),
        (-7.7f, 23.4f, 1.83f, 1, 2, false), (7.6f, 24.1f, 1.78f, 0, 2, true),
        (-4.3f, 24.4f, 1.76f, 5, 2, true), (4.8f, 25.1f, 1.72f, 4, 2, false),
        (-.8f, 16.6f, 1.73f, 4, 2, false)
    };

    [MenuItem("Tools/Office Art/Build Hall Crowd Groups")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Build crowds in Edit mode.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/OfficeScene.unity") throw new InvalidOperationException("Open OfficeScene first.");
        var shader = Shader.Find("NOPE/Hall Crowd Silhouette");
        var contactShader = Shader.Find("NOPE/Hall Crowd Contact");
        if (!shader || !contactShader || ShaderUtil.ShaderHasError(shader) || ShaderUtil.ShaderHasError(contactShader))
            throw new InvalidOperationException("Crowd shaders must compile before installation.");
        var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        var floor = GameObject.Find("HybridOffice/Hall/Blender_HallFloor/Hall_Floor__Hall_Floor").GetComponent<Renderer>();
        float floorY = floor.bounds.max.y;
        var orchestrator = UnityEngine.Object.FindFirstObjectByType<DayOrchestrator>();
        if (!orchestrator) throw new InvalidOperationException("DayOrchestrator is required for shift colour progression.");
        EnsureFolder(Folder + "/Meshes"); EnsureFolder(Folder + "/Materials");
        AssetDatabase.ImportAsset(Atlas, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Atlas);
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true; importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = true;
        importer.mipMapsPreserveCoverage = true; importer.alphaTestReferenceValue = .5f;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Trilinear;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
        var pixels = texture.GetPixels32(); var meshes = new Mesh[6];
        for (int i = 0; i < meshes.Length; i++) meshes[i] = CreateGroupMesh(texture, pixels, i);
        var contactMesh = SaveMesh("GroupContact", new[] {
            new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f)},
            new[] {Vector2.zero,Vector2.right,Vector2.up,Vector2.one});
        // Low chroma, opaque masses: keep the crowd subordinate to the portal and desk.
        string[] morningColours = {"#899C9F", "#8E9C9E", "#939D9D"};
        string[] eveningColours = {"#39414F", "#444C59", "#505764"};
        var morning = new Material[3]; var evening = new Material[3];
        for (int i = 0; i < 3; i++)
        {
            morning[i] = MakeMaterial($"Crowd_Morning_{i}", shader, Colour(morningColours[i]), texture);
            evening[i] = MakeMaterial($"Crowd_Evening_{i}", shader, Colour(eveningColours[i]), texture);
        }
        var morningContact = MakeMaterial("Contact_Morning", contactShader, new Color(.11f,.15f,.16f,.11f));
        var eveningContact = MakeMaterial("Contact_Evening", contactShader, new Color(.09f,.10f,.14f,.15f));
        var existing = GameObject.Find(Root);
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Author hall crowd groups");
        if (existing) Undo.DestroyObjectImmediate(existing);
        var root = new GameObject(Root); Undo.RegisterCreatedObjectUndo(root, "Author hall crowd groups");
        var entries = new List<OfficeHallCrowdPalette.Group>();
        for (int i = 0; i < Layout.Length; i++)
        {
            var p = Layout[i];
            var group = new GameObject($"Crowd {i+1:00} - composition {p.group+1}"); group.layer = 2;
            group.transform.SetParent(root.transform, false);
            group.transform.position = new Vector3(p.x, floorY + .006f, p.z);
            Vector3 towardCamera = camera.transform.position - group.transform.position; towardCamera.y = 0;
            group.transform.rotation = Quaternion.LookRotation(-towardCamera.normalized, Vector3.up);
            group.transform.localScale = Vector3.one * (p.height * HallScale);
            var card = AddRenderer("Merged silhouettes", group.transform, meshes[p.group], morning[p.palette]);
            card.transform.localScale = new Vector3(p.mirror ? -1 : 1, 1, 1);
            entries.Add(new OfficeHallCrowdPalette.Group {renderer = card, morning = morning[p.palette], evening = evening[p.palette]});
            var contact = AddRenderer("Soft group contact", group.transform, contactMesh, morningContact);
            contact.transform.localPosition = new Vector3(0, -.002f, .02f);
            contact.transform.localScale = new Vector3(meshes[p.group].bounds.size.x * .9f, 1, .20f);
            entries.Add(new OfficeHallCrowdPalette.Group {renderer = contact, morning = morningContact, evening = eveningContact});
        }
        var palette = root.AddComponent<OfficeHallCrowdPalette>();
        var serialized = new SerializedObject(palette);
        serialized.FindProperty("orchestrator").objectReferenceValue = orchestrator;
        var groups = serialized.FindProperty("groups"); groups.arraySize = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = groups.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("renderer").objectReferenceValue = entries[i].renderer;
            entry.FindPropertyRelative("morning").objectReferenceValue = entries[i].morning;
            entry.FindPropertyRelative("evening").objectReferenceValue = entries[i].evening;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo(); palette.SetPreview(OfficeHallCrowdPalette.PreviewMode.Automatic);
        Undo.CollapseUndoOperations(undo);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Validate();
    }

    static Mesh CreateGroupMesh(Texture2D texture, Color32[] pixels, int index)
    {
        int cw = texture.width / 3, ch = texture.height / 2, x0 = index % 3 * cw, y0 = (1-index/3) * ch;
        int xmin = x0+cw, xmax = x0, ymin = y0+ch, ymax = y0;
        for (int y=y0; y<y0+ch; y++) for (int x=x0; x<x0+cw; x++)
        {
            if (pixels[y*texture.width+x].a < 128) continue;
            xmin=Mathf.Min(xmin,x); xmax=Mathf.Max(xmax,x); ymin=Mathf.Min(ymin,y); ymax=Mathf.Max(ymax,y);
        }
        if (xmax <= xmin || ymax <= ymin) throw new InvalidOperationException($"Atlas group {index} has no opaque shape.");
        // Crop with UVs only. The source image and original alpha remain untouched.
        xmin=Mathf.Max(x0,xmin-2); xmax=Mathf.Min(x0+cw-1,xmax+2);
        ymax=Mathf.Min(y0+ch-1,ymax+2);
        float aspect=(xmax-xmin+1f)/(ymax-ymin+1f);
        float u0=xmin/(float)texture.width,u1=(xmax+1f)/texture.width;
        float v0=ymin/(float)texture.height,v1=(ymax+1f)/texture.height;
        return SaveMesh($"CrowdGroup_{index+1:00}", new[] {
            new Vector3(-aspect*.5f,0,0),new Vector3(aspect*.5f,0,0),new Vector3(-aspect*.5f,1,0),new Vector3(aspect*.5f,1,0)},
            new[] {new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u0,v1),new Vector2(u1,v1)});
    }
    static Mesh SaveMesh(string name, Vector3[] vertices, Vector2[] uv)
    {
        string path=Folder+"/Meshes/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh) {mesh=new Mesh{name=name}; AssetDatabase.CreateAsset(mesh,path);} else mesh.Clear();
        mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=new[]{0,2,1,2,3,1};mesh.RecalculateNormals();mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);return mesh;
    }
    static Material MakeMaterial(string name, Shader shader, Color tint, Texture2D texture=null)
    {
        string path=Folder+"/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) {mat=new Material(shader){name=name};AssetDatabase.CreateAsset(mat,path);} else mat.shader=shader;
        mat.SetColor("_Tint",tint);if(texture){mat.SetTexture("_BaseMap",texture);mat.SetFloat("_Cutoff",.5f);}
        EditorUtility.SetDirty(mat);return mat;
    }
    static MeshRenderer AddRenderer(string name, Transform parent, Mesh mesh, Material material)
    {
        var go=new GameObject(name);go.layer=2;go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;
        r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;
        r.reflectionProbeUsage=ReflectionProbeUsage.Off;r.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
        return r;
    }
    static Color Colour(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }

    [MenuItem("Tools/Office Art/Hall Crowds/Preview Morning")]
    public static void Morning()=>Preview(OfficeHallCrowdPalette.PreviewMode.Morning);
    [MenuItem("Tools/Office Art/Hall Crowds/Preview Evening")]
    public static void Evening()=>Preview(OfficeHallCrowdPalette.PreviewMode.Evening);
    [MenuItem("Tools/Office Art/Hall Crowds/Follow Shift")]
    public static void Automatic()=>Preview(OfficeHallCrowdPalette.PreviewMode.Automatic);
    static void Preview(OfficeHallCrowdPalette.PreviewMode mode)
    {
        var palette=GameObject.Find(Root).GetComponent<OfficeHallCrowdPalette>();
        if(!EditorApplication.isPlaying)Undo.RecordObject(palette,"Crowd palette preview");
        palette.SetPreview(mode);EditorUtility.SetDirty(palette);SceneView.RepaintAll();
    }
    [Serializable] sealed class Check
    {
        public bool success;public int groups,uniqueCompositions,visibleGroups,colliders,renderers;
        public float floorTop,eveningBlend;public string mode;public List<string> errors=new();
    }
    [MenuItem("Tools/Office Art/Hall Crowds/Validate")]
    public static void Validate()
    {
        var report=new Check();var root=GameObject.Find(Root);
        if(!root)throw new InvalidOperationException("Crowds are not installed.");
        var camera=GameObject.Find("Main Camera").GetComponent<Camera>();
        var planes=GeometryUtility.CalculateFrustumPlanes(camera);
        var cards=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Merged silhouettes").ToArray();
        report.groups=root.transform.childCount;report.renderers=root.GetComponentsInChildren<Renderer>().Length;
        report.uniqueCompositions=cards.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct().Count();
        report.visibleGroups=cards.Count(r=>GeometryUtility.TestPlanesAABB(planes,r.bounds));
        report.colliders=root.GetComponentsInChildren<Collider>(true).Length;
        report.floorTop=GameObject.Find("HybridOffice/Hall/Blender_HallFloor/Hall_Floor__Hall_Floor").GetComponent<Renderer>().bounds.max.y;
        var palette=root.GetComponent<OfficeHallCrowdPalette>();report.eveningBlend=palette.EveningBlend;report.mode=palette.Preview.ToString();
        if(report.groups!=Layout.Length || report.uniqueCompositions!=6)report.errors.Add("Expected all placements and six authored compositions.");
        if(report.colliders!=0)report.errors.Add("Background crowds must not intercept gameplay input.");
        foreach(var r in cards)
        {
            if(!r.sharedMaterial || ShaderUtil.ShaderHasError(r.sharedMaterial.shader))report.errors.Add("Invalid silhouette material.");
            if(Mathf.Abs(r.bounds.min.y-report.floorTop)>.015f)report.errors.Add(r.transform.parent.name+" feet miss floor.");
            if(r.shadowCastingMode!=ShadowCastingMode.Off)report.errors.Add("Unexpected 3D crowd shadow.");
            if(r.transform.position.z<7)report.errors.Add("Crowd entered the desk foreground.");
        }
        var so=new SerializedObject(palette);
        if(!so.FindProperty("orchestrator").objectReferenceValue)report.errors.Add("Missing shift binding.");
        var entries=so.FindProperty("groups");
        var block=new MaterialPropertyBlock();
        for(int i=0;i<entries.arraySize;i++)
        {
            var p=entries.GetArrayElementAtIndex(i);
            var r=(Renderer)p.FindPropertyRelative("renderer").objectReferenceValue;
            var morning=(Material)p.FindPropertyRelative("morning").objectReferenceValue;
            var evening=(Material)p.FindPropertyRelative("evening").objectReferenceValue;
            if(!r||!morning||!evening){report.errors.Add("Incomplete palette entry.");continue;}
            r.GetPropertyBlock(block);
            Color expected=Color.Lerp(morning.GetColor("_Tint"),evening.GetColor("_Tint"),palette.EveningBlend);
            if(((Vector4)block.GetColor("_Tint")-(Vector4)expected).sqrMagnitude>.00001f)report.errors.Add("Renderer palette does not match shift.");
        }
        report.success=report.errors.Count==0;Directory.CreateDirectory(ReportFolder);
        File.WriteAllText(ReportFolder+"/validation_"+report.mode.ToLowerInvariant()+".json",JsonUtility.ToJson(report,true));
        if(!report.success)Debug.LogError(string.Join("; ",report.errors));
    }
}
