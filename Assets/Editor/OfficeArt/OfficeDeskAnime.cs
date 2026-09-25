using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Material-only art tool. Does not change the scene lighting, room, or gameplay.
public static class OfficeDeskAnime
{
    const string Report="ArtDeliverables/TimeDesk/ImportedOffice/DeskClean";
    [MenuItem("Tools/Office Art/Apply Desk Anime Shading")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Apply materials in Edit mode.");
        var shader=Shader.Find("NOPE/Desk Anime");
        if(!shader || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Desk anime shader must compile first.");
        var paths=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Office/DeskClean/Materials","Assets/Art/Office/ImportedOffice/Materials"})
            .Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileName(p).StartsWith("DeskClean_")||Path.GetFileName(p).StartsWith("CRT2_"));
        foreach(string path in paths)
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);Undo.RecordObject(mat,"Anime desk shading");
            mat.shader=shader;
            bool paper=mat.name.Contains("Paper")||mat.name.Contains("Manila");
            bool dark=mat.name.Contains("Rubber")||mat.name.Contains("Glass")||mat.name.Contains("Ink")||mat.name.Contains("Pad");
            bool metal=mat.name.Contains("Metal")||mat.name.Contains("Brass");
            mat.SetVector("_ShadowTint",new Vector4(.72f,.81f,.94f,0));
            mat.SetVector("_LightTint",new Vector4(1.06f,1.015f,.92f,0));
            mat.SetFloat("_ShadowValue",paper?.60f:.48f);mat.SetFloat("_MidValue",.76f);
            // Tiny raised vents must not draw a second striped graphic over the fascia.
            // The fascia still receives shadows; the complete CRT still casts normally.
            mat.SetFloat("_ShadowReception",mat.name=="CRT2_Bezel"?.25f:1);
            mat.SetFloat("_Cull",2);
            mat.SetFloat("_BandSoftness",.065f);mat.SetFloat("_ShadowThreshold",.02f);mat.SetFloat("_LightThreshold",.58f);
            mat.SetFloat("_HighlightStrength",paper?0:dark?.012f:metal?.13f:.055f);
            mat.SetFloat("_HighlightSize",metal?.13f:.075f);mat.SetFloat("_EdgeStrength",paper?0:.10f);
            mat.DisableKeyword("_SPECULAR_SETUP");mat.DisableKeyword("_NORMALMAP");
            if(mat.name=="DeskClean_Paper2D")
            {mat.SetFloat("_AlphaClip",1);mat.EnableKeyword("_ALPHATEST_ON");mat.SetOverrideTag("RenderType","TransparentCutout");mat.renderQueue=2450;}
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
    }
    [Serializable] sealed class Check
    {
        public bool success;public int animeMaterials;public float phoneTillGap;
        public bool disksRemoved,paperIsTwoTriangles,mouseCableEndInsideComputer;
        public float paperUpNormal;public List<string> errors=new();
    }
    [MenuItem("Tools/Office Art/Validate Desk Anime Revision")]
    public static void Validate()
    {
        var report=new Check();
        var shader=Shader.Find("NOPE/Desk Anime");
        if(!shader || ShaderUtil.ShaderHasError(shader))report.errors.Add("Anime shader compile error.");
        foreach(var mat in Resources.FindObjectsOfTypeAll<Material>().Where(m=>m.shader==shader))report.animeMaterials++;
        Bounds ArtBounds(string path)
        {
            var rs=GameObject.Find(path).GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);return bounds;
        }
        var phone=ArtBounds("ImportedOfficeDress/Desk/Clerk hotline/Clean Art");
        var till=ArtBounds("HybridOffice/Booth/Finish_Till/Clean Art");
        report.phoneTillGap=phone.min.x-till.max.x;
        if(report.phoneTillGap<.025f)report.errors.Add("Phone and its cord need at least 2.5cm clearance from till.");
        report.disksRemoved=!GameObject.Find("HybridOffice/Booth/Finish_ComputerMedia");
        if(!report.disksRemoved)report.errors.Add("Floppy disks remain on desktop.");
        var paper=GameObject.Find("ImportedOfficeDress/Desk/Spare forms/Clean Art");
        report.paperIsTwoTriangles=paper.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length)==6;
        if(!report.paperIsTwoTriangles)report.errors.Add("2D paper trial is not a single quad.");
        var pf=paper.GetComponentInChildren<MeshFilter>();
        report.paperUpNormal=pf.transform.TransformDirection(pf.sharedMesh.normals[0]).y;
        if(report.paperUpNormal<.99f)report.errors.Add("Paper normal does not face up.");
        var pc=ArtBounds("ImportedOfficeDress/Desk/Retro CRT/Rebuilt CRT");
        report.mouseCableEndInsideComputer=pc.Contains(new Vector3(-1.38f,1.12f,-.14f));
        if(!report.mouseCableEndInsideComputer)report.errors.Add("Mouse cable endpoint misses the computer.");
        if(report.animeMaterials<20)report.errors.Add("Anime shading missing from desk materials.");
        report.success=report.errors.Count==0;
        File.WriteAllText(Report+"/anime_validation.json",JsonUtility.ToJson(report,true));
        if(!report.success)Debug.LogError(string.Join("; ",report.errors));
    }
}
