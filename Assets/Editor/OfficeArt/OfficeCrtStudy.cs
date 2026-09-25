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
    const string PcRoot="ImportedOfficeDress/Desk/Retro CRT";

    [MenuItem("Tools/Office Art/Apply Rebuilt CRT Study")]
    public static void ApplyRebuilt()
    {
        if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path!="Assets/Scenes/OfficeScene.unity")
            throw new InvalidOperationException("Open OfficeScene in Edit mode.");
        const string path=Folder+"/Models/CRT_Rebuilt.fbx";
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!asset)throw new InvalidOperationException("Run rebuild_crt.py in Blender first.");
        // The gameplay layer finds the PC's glass by name (scene contract PCScreen): check before touching the scene.
        if(!asset.GetComponentsInChildren<Renderer>(true).Any(r=>OfficeContract.IsScreenName(r.name)))
            throw new InvalidOperationException("CRT_Rebuilt.fbx has no object named Glass or Screen, so the gameplay layer would find no PC glass (docs/SCENE_CONTRACT_GAMEPLAY.md, PCScreen). Keep CRT2_Glass in rebuild_crt.py. Nothing was changed.");
        var pivotObject=GameObject.Find(PcRoot);
        if(!pivotObject)throw new InvalidOperationException("Missing "+PcRoot+" (the scene contract's PCScreen fallback). Nothing was changed.");
        var pivot=pivotObject.transform;
        var original=pivot.Find("Computer");
        var originalRenderer=original?original.GetComponentInChildren<Renderer>(true):null;
        if(!originalRenderer)throw new InvalidOperationException("Missing "+PcRoot+"/Computer, the pack computer the study is fitted to. Nothing was changed.");
        var target=originalRenderer.bounds;
        if(target.size==Vector3.zero)throw new InvalidOperationException("The pack computer's bounds are empty (it is inactive), so the study cannot be fitted to it. Nothing was changed.");
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
        // No click proxy or focus camera to align: at load the gameplay binder derives the PC's click box
        // and the PC frame from this glass (scene contract PCScreen). Report the glass it will use.
        var (glass,_)=OfficeAnchors.FindGlass(pivot);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        File.WriteAllText("ArtDeliverables/TimeDesk/ImportedOffice/BlenderCRT/revision2_unity_alignment.txt",
            $"Original bounds: {target}\nRebuilt bounds: {bounds}\nScreen center: {center}\nGameplay PC glass (scene contract PCScreen): {(glass?glass.name:"none")}\n");
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
