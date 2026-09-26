using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Art-side editor tool (Tools > Office Art): restores the NOPE/Desk Anime shader
/// and its presets (ArtDeliverables/TimeDesk/ImportedOffice/DeskClean/ANIME_SHADER.md)
/// on the Rebuilt CRT's CRT2_* materials. Materials only: no scene, lighting or
/// gameplay change. The desk props' finishes belong to the Debt Relief pass.
/// </summary>
public static class OfficeDeskAnime
{
    /// <summary>
    /// Sets NOPE/Desk Anime with the desk presets on every CRT2_* material in
    /// ImportedOffice/Materials: cool shadows, warm light, three tone bands, and small
    /// highlights by material kind (almost none on glass and ink; the bezel receives a
    /// quarter of the cast shadow, so its vents draw no second stripe).
    /// </summary>
    [MenuItem("Tools/Office Art/Apply Desk Anime Shading")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Apply materials in Edit mode.");
        var shader=Shader.Find("NOPE/Desk Anime");
        if(!shader || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Desk anime shader must compile first.");
        var paths=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Office/ImportedOffice/Materials"})
            .Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileName(p).StartsWith("CRT2_"));
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
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
    }
}
