using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Editor-only asset review. It renders isolated previews without changing source
// assets or the open scene, and records actual bounds, meshes and material slots.
public static class PackAssetCatalog
{
    const string Output = "ArtDeliverables/TimeDesk/ImportedOffice/Catalog";
    [Serializable] public class Input { public string[] paths; }
    [Serializable] public class Entry
    {
        public string path, image, error;
        public Vector3 center, size;
        public int triangles, vertices, meshes;
        public bool hasUV;
        public string[] materials;
    }
    [Serializable] public class Report { public List<Entry> assets = new(); public bool complete; }
    static Queue<string> pending;
    static Report report;
    static PreviewRenderUtility preview;

    [MenuItem("Tools/Office Art/Review Imported Packs")]
    public static void Start()
    {
        if (pending != null) throw new InvalidOperationException("Asset review already running");
        Directory.CreateDirectory(Output);
        pending = new Queue<string>(JsonUtility.FromJson<Input>(File.ReadAllText(Output + "/input.json")).paths);
        report = new Report();
        preview = new PreviewRenderUtility();
        preview.camera.fieldOfView = 32;
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.camera.backgroundColor = new Color(.19f, .22f, .24f);
        preview.camera.nearClipPlane = .001f;
        preview.camera.farClipPlane = 2000;
        preview.ambientColor = new Color(.24f,.25f,.27f);
        preview.lights[0].intensity = 1.5f;
        preview.lights[0].transform.rotation = Quaternion.Euler(35, 150, 0);
        preview.lights[1].intensity = .65f;
        preview.lights[1].transform.rotation = Quaternion.Euler(20, -35, 0);
        EditorApplication.update += Next;
    }

    static void Next()
    {
        if (pending.Count == 0)
        {
            EditorApplication.update -= Next;
            preview.Cleanup(); preview = null; pending = null;
            report.complete = true;
            File.WriteAllText(Output + "/catalog.json", JsonUtility.ToJson(report, true));
            Debug.Log("Office asset review complete: " + report.assets.Count);
            return;
        }
        string path = pending.Dequeue();
        var entry = new Entry { path=path, image=$"{report.assets.Count:D3}_{Path.GetFileNameWithoutExtension(path)}.png" };
        GameObject instance = null;
        var temporaryMaterials = new List<Material>();
        try
        {
            instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            preview.AddSingleGO(instance);
            foreach (var light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new Exception("No renderers");
            var bounds = renderers[0].bounds;
            var materialPaths = new HashSet<string>();
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                Material[] replacement = renderer.sharedMaterials;
                for (int i=0;i<replacement.Length;i++)
                {
                    var src = replacement[i]; if (!src) continue;
                    materialPaths.Add(AssetDatabase.GetAssetPath(src));
                    var converted = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    converted.hideFlags = HideFlags.HideAndDontSave;
                    converted.SetColor("_BaseColor",src.HasProperty("_BaseColor")?src.GetColor("_BaseColor"):src.HasProperty("_Color")?src.GetColor("_Color"):Color.white);
                    string map=src.HasProperty("_Albedo")?"_Albedo":src.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
                    if(src.HasProperty(map)) converted.SetTexture("_BaseMap",src.GetTexture(map));
                    converted.SetFloat("_Smoothness",.2f);
                    replacement[i]=converted;temporaryMaterials.Add(converted);
                }
                renderer.sharedMaterials = replacement;
            }
            entry.center=bounds.center;entry.size=bounds.size;entry.materials=materialPaths.OrderBy(x=>x).ToArray();
            var meshes = instance.GetComponentsInChildren<MeshFilter>().Select(x=>x.sharedMesh).Where(x=>x).ToArray();
            entry.meshes=meshes.Length;entry.vertices=meshes.Sum(x=>x.vertexCount);
            entry.triangles=(int)meshes.Sum(x=>Enumerable.Range(0,x.subMeshCount).Sum(i=>(long)x.GetIndexCount(i)/3));
            entry.hasUV=meshes.All(x=>x.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0));
            var radius=Mathf.Max(bounds.extents.magnitude,.01f);
            preview.camera.transform.position=bounds.center+new Vector3(.8f,.55f,1).normalized*radius*4.3f;
            preview.camera.transform.LookAt(bounds.center);
            preview.BeginPreview(new Rect(0,0,384,384),GUIStyle.none);
            preview.Render(true);
            var result=(RenderTexture)preview.EndPreview();
            var old=RenderTexture.active;RenderTexture.active=result;
            var readable=new Texture2D(384,384,TextureFormat.RGB24,false);
            readable.ReadPixels(new Rect(0,0,384,384),0,0);readable.Apply();
            File.WriteAllBytes(Output+"/"+entry.image,readable.EncodeToPNG());
            RenderTexture.active=old;Object.DestroyImmediate(readable);
        }
        catch(Exception e) { entry.error=e.ToString(); }
        finally
        {
            if(instance)Object.DestroyImmediate(instance);
            foreach(var mat in temporaryMaterials)Object.DestroyImmediate(mat);
        }
        report.assets.Add(entry);
        File.WriteAllText(Output+"/catalog.json",JsonUtility.ToJson(report,true));
    }
}
