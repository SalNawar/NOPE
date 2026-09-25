using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// One-object art study. Room, lights, camera and other desktop props are untouched.
public static class OfficeCrtStudy
{
    const string Folder="Assets/Art/Office/ImportedOffice";

    [MenuItem("Tools/Office Art/Apply Blender CRT Study")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path!="Assets/Scenes/OfficeScene.unity")
            throw new InvalidOperationException("Open OfficeScene in Edit mode.");
        var pivot=GameObject.Find("ImportedOfficeDress/Desk/Retro CRT").transform;
        var old=pivot.Find("Computer");
        if(!old)throw new InvalidOperationException("Original pack computer missing.");
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
