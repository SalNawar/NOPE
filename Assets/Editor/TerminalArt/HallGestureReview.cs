using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class HallGestureReview
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion";
    [MenuItem("Tools/Terminal Art/Review/Apply Placement And Shadow Corrections")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop preview before saving.");
        if(EditorSceneManager.GetActiveScene().path!="Assets/Art/Office/AnimeHallLayers/AnimeHall.unity") throw new InvalidOperationException("Open AnimeHall.");
        GameObject.Find("Anchor_Traveller").transform.position=new Vector3(0,0,1.6f);
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var floor=art.FindLayer("06 Concourse terracotta floor");
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        foreach(var caster in UnityEngine.Object.FindObjectsByType<ShadowCaster2D>(FindObjectsSortMode.None))
        {
            var so=new SerializedObject(caster);var layers=so.FindProperty("m_ApplyToSortingLayers");
            layers.arraySize=1;layers.GetArrayElementAtIndex(0).intValue=floor.sortingLayerID;so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var renderer in art.GetComponentsInChildren<MeshRenderer>())
        {
            if(renderer.sharedMaterial==null || renderer.sharedMaterial.shader.name!="NOPE/Hall Contact Shadow") continue;
            renderer.sortingLayerID=floor.sortingLayerID;
            renderer.sharedMaterial.SetFloat("_Strength",.45f);
            EditorUtility.SetDirty(renderer.sharedMaterial);
        }
        var root=art.transform.Find("Ground cast shadows");
        if(root==null) {var g=new GameObject("Ground cast shadows");g.transform.SetParent(art.transform,false);root=g.transform;}
        root.gameObject.layer=floor.gameObject.layer;
        root.localPosition=floor.transform.localPosition;root.localRotation=floor.transform.localRotation;root.localScale=floor.transform.localScale;
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/GroundShadows.mat");
        if(material==null) {material=new Material(Shader.Find("NOPE/Hall Ground Shadow"));AssetDatabase.CreateAsset(material,Folder+"/GroundShadows.mat");}
        foreach(var pair in new[]{("_Ground","06 Concourse terracotta floor"),("_Platform","12 Foreground platform"),("_Gallery","07 Upper gallery floor")}) material.SetTexture(pair.Item1,art.FindLayer(pair.Item2).sprite.texture);
        EditorUtility.SetDirty(material);
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var receivers=new List<Vector2>();var colors=new List<Color>();var indices=new List<int>();
        void Cast(float x,float y,float width,float dx,float dy,float share=1)
        {
            int start=vertices.Count;
            var points=new[]{new Vector2(x-width*.5f,y),new Vector2(x+width*.5f,y),new Vector2(x+width*.5f+dx,y+dy),new Vector2(x-width*.5f+dx,y+dy)};
            var corners=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            for(int i=0;i<4;i++)
            {
                var p=points[i];vertices.Add(new Vector3((p.x-floor.sprite.pivot.x)/floor.sprite.pixelsPerUnit,(724-p.y-floor.sprite.pivot.y)/floor.sprite.pixelsPerUnit,-.004f));
                uv.Add(corners[i]);receivers.Add(new Vector2(p.x/2172f,1-p.y/724f));colors.Add(new Color(1,1,1,share));
            }
            indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
        }
        // Sun enters through the left windows. Shadows continue rightwards on
        // their own painted receiver plane, never over the objects themselves.
        Cast(625,479,116,145,65,.85f);Cast(1422,520,116,165,60,.85f);
        Cast(1033,626,300,145,57);Cast(866,428,135,72,29);Cast(1117,432,128,74,29);
        Cast(1855,653,180,115,34,.8f);Cast(1988,648,62,95,28,.8f);
        Cast(1670,604,65,70,31,.8f);Cast(1259,379,70,47,20,.7f);Cast(999,361,104,48,18,.7f);
        float[] feet={30,141,264,379,511,649,774,906,1035,1156,1285,1414,1532};
        foreach(float x in feet) Cast(x,690,8,83,32,.9f);
        for(float x=1520;x<2100;x+=108) Cast(x,280,4,33,9,.7f);
        for(float x=490;x<1350;x+=103) Cast(x,276,4,28,9,.65f);
        // Horizontal rails join their post shadows on the front platform.
        Cast(785,711,1480,20,5,.4f);
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/GroundShadowMesh.asset");
        if(mesh==null) {mesh=new Mesh{name="Registered hall cast shadows"};AssetDatabase.CreateAsset(mesh,Folder+"/GroundShadowMesh.asset");}
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,receivers);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var filter=root.GetComponent<MeshFilter>();if(filter==null) filter=root.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
        var drawing=root.GetComponent<MeshRenderer>();if(drawing==null) drawing=root.gameObject.AddComponent<MeshRenderer>();drawing.sharedMaterial=material;drawing.sortingLayerID=floor.sortingLayerID;drawing.sortingOrder=90;
        var controller=root.GetComponent<HallGroundShadows>();if(controller==null) controller=root.gameObject.AddComponent<HallGroundShadows>();controller.Configure(rig,drawing);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(root.gameObject.scene);
        Debug.Log($"Gesture review: original 1.6m distance, undeformed character art; {vertices.Count/4} visible receiver-masked cast shadows and corrected shadow sorting.");
    }
}
