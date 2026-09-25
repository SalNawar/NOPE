using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// One-object art study. Only the monitor focus camera follows the revised screen.
public static class OfficeCrtStudy
{
    const string Folder="Assets/Art/Office/ImportedOffice";

    [MenuItem("Tools/Office Art/Apply Rebuilt CRT Study")]
    public static void ApplyRebuilt()
    {
        if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path!="Assets/Scenes/OfficeScene.unity")
            throw new InvalidOperationException("Open OfficeScene in Edit mode.");
        const string path=Folder+"/Models/CRT_Rebuilt.fbx";
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!asset)throw new InvalidOperationException("Run rebuild_crt.py in Blender first.");
        var pivot=GameObject.Find("ImportedOfficeDress/Desk/Retro CRT").transform;
        var original=pivot.Find("Computer");
        var target=original.GetComponentInChildren<Renderer>(true).bounds;
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.importNormals=ModelImporterNormals.Import;
        importer.importTangents=ModelImporterTangents.CalculateMikk;
        importer.SaveAndReimport();
        var previous=pivot.Find("Rebuilt CRT");
        if(previous)Undo.DestroyObjectImmediate(previous.gameObject);
        var variant=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),pivot);
        variant.name="Rebuilt CRT";Undo.RegisterCreatedObjectUndo(variant,"Rebuilt CRT study");
        variant.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.Euler(0,180,0));
        variant.transform.localScale=Vector3.one;
        var colors=new System.Collections.Generic.Dictionary<string,(string color,float roughness)> {
            {"Shell",("ADA18A",.76f)},{"Bezel",("C5B89E",.73f)},{"Case",("A39883",.78f)},
            {"Trim",("655F53",.81f)},{"Dark",("343C39",.84f)},{"Glass",("273333",.47f)},
            {"Orange",("B87D45",.74f)},{"Teal",("557D73",.76f)},{"Ink",("686C61",.85f)}
        };
        var renderers=variant.GetComponentsInChildren<MeshRenderer>();
        foreach(var renderer in renderers)
        {
            string key=renderer.name.Replace("CRT2_","");
            if(!colors.TryGetValue(key,out var settings))throw new InvalidOperationException("Unmapped CRT mesh: "+renderer.name);
            ColorUtility.TryParseHtmlString("#"+settings.color,out var color);
            var mat=Material("CRT2_"+key,color,1-settings.roughness);
            mat.SetTexture("_BaseMap",null);
            mat.SetFloat("_WorkflowMode",0);mat.EnableKeyword("_SPECULAR_SETUP");
            float spec=key=="Glass"?.012f:.024f;
            mat.SetColor("_SpecColor",new Color(spec,spec,spec));
            renderer.sharedMaterials=Enumerable.Repeat(mat,renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount).ToArray();
        }
        Bounds bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        variant.transform.position+=new Vector3(target.center.x,target.min.y,target.center.z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        foreach(string name in new[]{"Computer","Blender CRT"})
        {
            var old=pivot.Find(name);if(!old)continue;
            Undo.RecordObject(old.gameObject,"Retain earlier CRT for comparison");old.gameObject.SetActive(false);
        }
        var screen=renderers.Single(r=>r.name=="CRT2_Glass");
        Vector3 center=screen.bounds.center;
        var proxy=GameObject.Find("OfficeRoot/CRTMonitor");
        Undo.RecordObject(proxy.transform,"Align rebuilt CRT click target");proxy.transform.position=center;
        var collider=proxy.GetComponent<BoxCollider2D>();Undo.RecordObject(collider,"Fit rebuilt CRT click target");
        collider.size=new Vector2(screen.bounds.size.x,screen.bounds.size.y);
        var zoom=GameObject.Find("Cameras/MonitorVCam").transform;
        Undo.RecordObject(zoom,"Align rebuilt CRT focus");
        zoom.position=center+(pivot.rotation*Vector3.back)*.87f+Vector3.up*.035f;zoom.LookAt(center);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        File.WriteAllText("ArtDeliverables/TimeDesk/ImportedOffice/BlenderCRT/revision2_unity_alignment.txt",
            $"Original bounds: {target}\nRebuilt bounds: {bounds}\nScreen center: {center}\nClick target: {proxy.transform.position}\nCollider size: {collider.size}\nFocus camera: {zoom.position}\n");
    }

    [MenuItem("Tools/Office Art/Apply Blender CRT Study")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path!="Assets/Scenes/OfficeScene.unity")
            throw new InvalidOperationException("Open OfficeScene in Edit mode.");
        var pivot=GameObject.Find("ImportedOfficeDress/Desk/Retro CRT").transform;
        var old=pivot.Find("Computer");
        if(!old)throw new InvalidOperationException("Original pack computer missing.");
        if(pivot.Find("Rebuilt CRT"))
            throw new InvalidOperationException("Use Apply Rebuilt CRT Study for the current scene; the old study is retained for comparison.");
        var previous=pivot.Find("Blender CRT");if(previous)Undo.DestroyObjectImmediate(previous.gameObject);
        var sourceRenderer=old.GetComponentInChildren<Renderer>(true);
        var target=sourceRenderer.bounds;
        string path=Folder+"/Models/CRT_Painted.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.importNormals=ModelImporterNormals.Import;
        importer.importTangents=ModelImporterTangents.CalculateMikk;
        importer.SaveAndReimport();
        var variant=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),pivot);
        variant.name="Blender CRT";Undo.RegisterCreatedObjectUndo(variant,"Apply Blender CRT study");
        // Blender's FBX forward axis is opposite this pack's Unity-facing pivot.
        variant.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.Euler(0,180,0));variant.transform.localScale=Vector3.one;
        var plastic=Material("CRT_Study_Ivory",new Color(1,1,1),.32f);
        plastic.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/computer_ivory_painted.png"));
        var glass=Material("CRT_Study_Glass",new Color(.095f,.15f,.16f),.56f);
        glass.SetFloat("_WorkflowMode",0);glass.EnableKeyword("_SPECULAR_SETUP");
        glass.SetColor("_SpecColor",new Color(.018f,.018f,.018f));
        var renderers=variant.GetComponentsInChildren<MeshRenderer>();
        foreach(var renderer in renderers)
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            if(mesh.subMeshCount!=2)throw new InvalidOperationException("Expected chassis and screen material slots.");
            renderer.sharedMaterials=new[]{plastic,glass};
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
        }
        var bounds=renderers[0].bounds;
        foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        variant.transform.position+=new Vector3(target.center.x,target.min.y,target.center.z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        Undo.RecordObject(old.gameObject,"Keep original CRT for comparison");old.gameObject.SetActive(false);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        File.WriteAllText("ArtDeliverables/TimeDesk/ImportedOffice/BlenderCRT/unity_alignment.txt",
            $"Original bounds: {target}\nRevised bounds: {renderers[0].bounds}\nOriginal screen: {ScreenCenter(old)}\nRevised screen: {ScreenCenter(variant.transform)}\n");
    }

    static Vector3 ScreenCenter(Transform root)
    {
        var filter=root.GetComponentInChildren<MeshFilter>(true);
        return filter.transform.TransformPoint(filter.sharedMesh.GetSubMesh(1).bounds.center);
    }

    static Material Material(string name,Color color,float smoothness)
    {
        string path=$"{Folder}/Materials/{name}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        Undo.RecordObject(m,"CRT material study");m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",smoothness);
        m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
}
