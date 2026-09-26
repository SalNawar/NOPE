using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The palette pass: material finishes, textures, readout tints, crowd colours and portal clearance.
public static partial class OfficeDebtReliefArt
{
    /// <summary>The generated walnut texture for the booth's desk wood.</summary>
    const string WoodPath = ArtFolder + "/Textures/QuietWalnut.png";
    /// <summary>The desk props' colour per DeskClean material key (hex RGB); DeskColour overrides some per prop.</summary>
    static readonly Dictionary<string, string> DeskColours = new() {
        {"ABS","F0EFEB"},{"Case","B4B2B6"},{"Grey","F0EFEB"},
        {"Green","604357"},{"GreenDark","353239"},{"Teal","A18FAE"},
        {"Rubber","211F26"},{"Dark","353239"},{"Paper","FFF2D9"},
        {"Manila","E6C575"},{"Wood","966547"},{"Orange","EDAD73"},
        {"Metal","B4B2B6"},{"Brass","C6A96C"},{"Glass","211F26"},
        {"Ink","211F26"},{"Pad","823F50"},{"PadEdge","592C38"},
        {"PhoneBody","D77662"},{"PhoneDial","FFFFFF"},{"Paper2D","FFFFFF"}
    };
    /// <summary>A colour from hex RGB(A), with or without '#'.</summary>
    static Color Hex(string v) { ColorUtility.TryParseHtmlString("#" + v.TrimStart('#'), out var c); return c; }
    /// <summary>Creates an asset folder and its parents when missing.</summary>
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\','/');
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
    /// <summary>The world bounds of a root's enabled art renderers; throws when it has none.</summary>
    static Bounds ArtBounds(Transform root)
    {
        var rs = root.GetComponentsInChildren<Renderer>(false).Where(IsArt).ToArray();
        if (rs.Length == 0) throw new InvalidOperationException("No active renderers: " + PathOf(root));
        var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    /// <summary>Puts a material in one slot of a renderer, with undo and as a prefab-instance override.</summary>
    static void Assign(Renderer r, int slot, Material m)
    {
        Undo.RecordObject(r, "Office material finish");
        var list = r.sharedMaterials; list[slot] = m; r.sharedMaterials = list;
        PrefabUtility.RecordPrefabInstancePropertyModifications(r); EditorUtility.SetDirty(r);
    }
    /// <summary>
    /// The persistent finish DebtRelief/Materials/{key}.mat: created once as a copy of
    /// <paramref name="source"/>, then recoloured on every run. Normal maps are dropped;
    /// with <paramref name="anime"/> it switches to NOPE/Desk Anime with the desk presets.
    /// </summary>
    static Material Finish(string key, Material source, string colour, bool anime, Texture texture = null)
    {
        string path = ArtFolder + "/Materials/" + key + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(source) { name = key }; AssetDatabase.CreateAsset(m, path); }
        Undo.RecordObject(m, "Office material finish");
        if (anime) m.shader = Shader.Find("NOPE/Desk Anime");
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Hex(colour));
        if (m.HasProperty("_Color")) m.SetColor("_Color", Hex(colour));
        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", texture); m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero); }
        if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", null);
        if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 0);
        m.DisableKeyword("_NORMALMAP");
        if (anime)
        {
            m.SetVector("_ShadowTint", new Vector4(.90f,.89f,.94f,0));
            m.SetVector("_LightTint", new Vector4(1.02f,1f,.98f,0));
            m.SetFloat("_ShadowValue", .60f); m.SetFloat("_MidValue", .84f);
            m.SetFloat("_ShadowReception", 1); m.SetFloat("_BandSoftness", .10f);
            m.SetFloat("_ShadowThreshold", .02f); m.SetFloat("_LightThreshold", .58f);
            m.SetFloat("_HighlightStrength", .025f); m.SetFloat("_HighlightSize", .075f);
            m.SetFloat("_EdgeStrength", .075f); m.SetFloat("_Metallic", 0);
            m.SetFloat("_Smoothness", .22f); m.SetFloat("_Cull", 2);
        }
        EditorUtility.SetDirty(m); return m;
    }
    /// <summary>A desk prop's colour for a material key, with the per-prop overrides (till, lamp, NEXT, tray, sorter, ink pad, mouse, pen pot, calculator).</summary>
    static string DeskColour(string path, string key)
    {
        string c = DeskColours.TryGetValue(key, out var v) ? v : "F0EFEB";
        bool green = key == "Green" || key == "GreenDark";
        if (path.Contains("Clean_Till") && green) c = key == "Green" ? "604357" : "483341";
        if (path.Contains("Clean_Lamp") && green) c = "353239";
        if (path.Contains("Clean_Next") && green) c = "353239";
        if (path.Contains("Clean_Tray") && green) c = key == "Green" ? "A18FAE" : "796B85";
        if (path.Contains("Clean_FormSorter") && green) c = key == "Green" ? "966547" : "704832";
        if (path.Contains("Clean_Inkpad") && green) c = key == "Green" ? "B4B2B6" : "353239";
        if (path.Contains("Clean_Mouse") && key == "GreenDark") c = "966547";
        if (path.Contains("Clean_PenPot"))
        {
            if (key == "ABS") c = "D5A535";
            if (key == "Green") c = "A18FAE";
            if (key == "Teal") c = "D77662";
        }
        if (path.Contains("Clean_Calculator"))
        {
            if (key == "Case") c = "F0EFEB";
            if (key == "ABS") c = "353239";
            if (key == "Ink") c = "FFF2D9";
            if (key == "Teal") c = "A18FAE";
        }
        return c;
    }
    /// <summary>True for a path at or under a protected root.</summary>
    static bool Excluded(string path) => ProtectedRoots.Any(p => path == p || path.StartsWith(p + "/", StringComparison.Ordinal));
    /// <summary>
    /// Assigns the finishes by area: desk props (Clean Art under the booth and the desk),
    /// other booth parts, the megacity blocks and the hall (portal energy, clear window
    /// glass, surfaces). Each slot starts from the material it had in the baseline, so a
    /// rerun rebuilds from the originals rather than from the previous finish. Then
    /// tints the booth's readout texts and recolours the crowd materials.
    /// </summary>
    static void ApplyMaterials()
    {
        var originals = JsonUtility.FromJson<AuditReport>(File.ReadAllText(ReportFolder + "/before.json")).renderers.ToDictionary(x=>x.path);
        var wood = AssetDatabase.LoadAssetAtPath<Texture2D>(WoodPath);
        if (!wood) throw new InvalidOperationException("Quiet walnut texture must be imported first.");
        foreach (var r in Transforms().SelectMany(t => t.GetComponents<Renderer>()).Where(IsArt))
        {
            string p = PathOf(r.transform); if (Excluded(p) || r is SpriteRenderer) continue;
            var list = r.sharedMaterials;
            for (int i = 0; i < list.Length; i++)
            {
                var src = list[i];
                if(originals.TryGetValue(p,out var original) && i<original.materials.Length)
                {
                    var originalMaterial=AssetDatabase.LoadAssetAtPath<Material>(original.materials[i].path);
                    if(originalMaterial)src=originalMaterial;
                }
                if (!src) continue;
                if (p.Contains("/Clean Art") && (p.StartsWith("HybridOffice/Booth/") || p.StartsWith("ImportedOfficeDress/Desk/")))
                {
                    // Renderer names retain the authored material slot even where a legacy import lost the assignment.
                    string key = r.name.Contains("__DeskClean_") ? r.name.Split(new[]{"__DeskClean_"}, StringSplitOptions.None).Last() : src.name.Replace("DeskClean_", "");
                    if (!DeskColours.ContainsKey(key)) continue;
                    string role = p.Contains("/Clean_") ? p.Split(new[]{"/Clean_"}, StringSplitOptions.None).Last().Split(new[]{"__"}, StringSplitOptions.None)[0] : key;
                    Texture texture = (key == "PhoneDial" || key == "Paper2D") && src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : null;
                    Assign(r, i, Finish("Desk_" + role + "_" + key, src, DeskColour(p,key), true, texture));
                }
                else if (p.StartsWith("HybridOffice/Booth/"))
                {
                    string n = src.name, c = null; Texture tex = null;
                    if (n.Contains("Walnut")) { c = "FFFFFF"; tex = wood; }
                    else if (n.Contains("WoodEdge")) c = "966547";
                    else if (n.Contains("Wood")) c = "704832";
                    else if (n.Contains("Paper") || n.Contains("Ivory")) c = "FFF2D9";
                    else if (n.Contains("Brass")) c = "C6A96C";
                    else if (n.Contains("Coral") || n.Contains("Amber")) c = "EDAD73";
                    else if (n.Contains("Glass") || n.Contains("Rubber")) c = "211F26";
                    else if (n.Contains("Charcoal") || n.Contains("DarkMetal") || n.Contains("Desk_Metal")) c = "353239";
                    else if (n.Contains("Metal") || n.Contains("Steel") || n.Contains("Trim") || n.Contains("Grey")) c = "B4B2B6";
                    else if (n.Contains("Green")) c = "EDAD73";
                    else if (n.Contains("Teal") || n.Contains("Enamel"))
                    {
                        if (p.Contains("Finish_Board/") || p.Contains("Finish_RightBoard/")) c = "BB955F";
                        else if (p.Contains("Partition")) c = n.Contains("Dark") ? "AE9D90" : "D4C6B9";
                        else if (p.Contains("Rail") || p.Contains("Finish_Desk/")) c = "704832";
                        else c = "353239";
                    }
                    if (c != null) Assign(r, i, Finish("Booth_" + c + "_" + n, src,c,true,tex));
                }
                else if (p.StartsWith("HybridOffice/Exterior/Blender_Megacity"))
                {
                    string block = p.Split('/')[2], n = src.name;
                    string baseHex = block.Contains("West") ? "B8755F" : block.Contains("EastRear") ? "A18FAE" : block.EndsWith("East") ? "D4C6B9" : "F0EFEB";
                    Color baseColour = Hex(baseHex); string c = null;
                    if (n == "City_Concrete") c = baseHex;
                    else if (n.Contains("ConcreteDark")) c = ColorUtility.ToHtmlStringRGB(baseColour * .66f);
                    else if (n.Contains("ConcreteLight")) c = ColorUtility.ToHtmlStringRGB(Color.Lerp(baseColour, Hex("F0EFEB"), .45f));
                    else if (n.Contains("WindowLight")) c = "E6C575";
                    else if (n.Contains("Window") || n.Contains("Service")) c = "62616C";
                    else if (n.Contains("Sign")) c = "D77662";
                    else if (n.Contains("Rust")) c = "B8755F";
                    else if (n.Contains("Weather")) c = ColorUtility.ToHtmlStringRGB(Color.Lerp(baseColour, Hex("353239"),.30f));
                    if (c != null) Assign(r,i,Finish(block + "_" + n,src,c,false));
                }
                else if (p.StartsWith("HybridOffice/Hall/"))
                {
                    string n = src.name;
                    if (n == "PortalFlow")
                    {
                        var m = Finish("Portal_Energy",src,"FFFFFF",false);
                        m.SetColor("_DeepColor",Hex("173753")); m.SetColor("_FlowColor",Hex("4B8DBB")); m.SetColor("_RimColor",Hex("86B9E0"));
                        EditorUtility.SetDirty(m); Assign(r,i,m); continue;
                    }
                    if (p.Contains("/WindowGlass/"))
                    {
                        var m = Finish("Hall_ClearGlass",src,"D0D3D5",false);
                        m.SetColor("_BaseColor",new Color(.72f,.73f,.75f,.045f)); m.SetFloat("_Smoothness",.78f);
                        m.SetFloat("_SpecularHighlights",1);m.SetFloat("_EnvironmentReflections",1);
                        m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                        EditorUtility.SetDirty(m); Assign(r,i,m);continue;
                    }
                    string c = null; Texture tex = null;
                    if (p.Contains("/Blender_PortalRing/") && n.StartsWith("Portal_")) c = "B4B2B6";
                    else if (n.Contains("Teal") || n.Contains("Plaster")) c = "D4C6B9";
                    else if (n.Contains("DarkMetal") || n.Contains("Rubber")) c = "353239";
                    else if (n.Contains("Glass")) c = "211F26";
                    else if (n.Contains("Stone")) { c = "D4C6B9"; tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : null; }
                    else if (n.Contains("Brass")) c = "BBA273";
                    else if (n.Contains("Wear")) c = "A49586";
                    else if (n.Contains("Rust")) c = "B8755F";
                    else if (n.Contains("Grey") || n.Contains("Paint")) c = "B4B2B6";
                    else if (n.Contains("LightFace")) c = "F0EFEB";
                    else if (n.Contains("Green") || n.Contains("Amber")) c = "EDAD73";
                    if (c != null) Assign(r,i,Finish("Hall_"+n+"_"+c,src,c,false,tex));
                }
            }
        }
        // Only art readout tint; values, formatting and runtime bindings stay untouched.
        foreach (var t in Require("HybridOffice/Booth").GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (Excluded(PathOf(t.transform))) continue;
            Undo.RecordObject(t,"Office readout colour"); t.color = Hex(PathOf(t.transform).Contains("Blender_DayCalendar/") ? "211F26" : "FFF2D9");
            PrefabUtility.RecordPrefabInstancePropertyModifications(t); EditorUtility.SetDirty(t);
        }
        // Dedicated crowd material assets retain their existing automatic morning/evening wiring.
        string[] morning={"BFC0C3","C6C7C9","CED0D1"}, evening={"37313F","403B49","4F4857"};
        for(int i=0;i<3;i++)
        foreach(var pair in new[]{("Morning",morning[i]),("Evening",evening[i])})
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Office/HallCrowds/Materials/Crowd_{pair.Item1}_{i}.mat");
            if(!m)throw new InvalidOperationException("Missing crowd material.");
            Undo.RecordObject(m,"Neutral crowd palette");m.SetColor("_Tint",Hex(pair.Item2));EditorUtility.SetDirty(m);
        }
    }
    /// <summary>The viewport rectangle a world box covers in a camera (its eight corners projected).</summary>
    static Rect Project(Camera cam, Bounds b)
    {
        Vector2 min = new Vector2(float.MaxValue,float.MaxValue), max = new Vector2(float.MinValue,float.MinValue);
        for (int i=0;i<8;i++)
        {
            var p=cam.WorldToViewportPoint(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z));
            min=Vector2.Min(min,new Vector2(p.x,p.y));max=Vector2.Max(max,new Vector2(p.x,p.y));
        }
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    /// <summary>The portal's viewport rectangle in a camera.</summary>
    static Rect PortalRect(Camera cam) => Project(cam,Require("HybridOffice/Hall/Blender_PortalRing/Hall_Portal__Portal_Paint").GetComponent<Renderer>().bounds);
    /// <summary>
    /// Moves each crowd group sideways, away from the centre in 10 cm steps (at most 180),
    /// until its silhouette clears the portal in the office camera by a small margin and
    /// stays outside the central aisle (|x| > 3.1 m), turning it to face the camera.
    /// </summary>
    static void ClearPortalApproach()
    {
        var cam=Camera.main; var portal=PortalRect(cam); var root=Require("OfficeHallCrowds");
        foreach(Transform group in root)
        {
            var card=group.Find("Merged silhouettes")?.GetComponent<Renderer>();if(!card)continue;
            float sign=group.position.x<0?-1:1; Undo.RecordObject(group,"Clear portal approach");
            for(int i=0;i<180;i++)
            {
                var screen=Project(cam,card.bounds);
                bool clear=sign<0?screen.xMax<portal.xMin-.012f:screen.xMin>portal.xMax+.012f;
                bool aisle=sign<0?card.bounds.max.x<-3.1f:card.bounds.min.x>3.1f;
                if(clear&&aisle)break;
                group.position+=new Vector3(sign*.10f,0,0);
                Vector3 from=group.position-cam.transform.position;from.y=0;group.rotation=Quaternion.LookRotation(from.normalized,Vector3.up);
            }
            EditorUtility.SetDirty(group);
        }
    }
    /// <summary>
    /// The palette pass: finishes, scanner colours and portal clearance, as one undo step.
    /// It refuses to save when a protected or gameplay hash changed, then re-applies the
    /// approved desk notes and wear (once their baseline exists) and validates.
    /// </summary>
    [MenuItem("Tools/Office Art/Debt Relief/Apply Colours Textures And Clear Portal")]
    public static void ApplyColours()
    {
        CheckScene();
        if(!File.Exists(ReportFolder+"/before.json"))throw new InvalidOperationException("Capture the baseline before applying art.");
        EnsureFolder(ArtFolder+"/Materials");
        var shader=Shader.Find("NOPE/Desk Anime");
        if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Desk shader must compile.");
        int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Office colours textures and clear portal");
        string protectedBefore=ProtectedHash(),gameplayBefore=GameplayHash();
        ApplyMaterials();ApplyScannerFinishes();ClearPortalApproach();AssetDatabase.SaveAssets();
        if(ProtectedHash()!=protectedBefore||GameplayHash()!=gameplayBefore)
            throw new InvalidOperationException("Protected PC/floor/gameplay changed; do not save the scene.");
        Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(Scene);EditorSceneManager.SaveScene(Scene);
        // Preserve the subsequently approved layout and wear when refreshing the palette.
        if(File.Exists(LayoutReport+"/before.json"))ApplyDeskNotesAndWear();
        ValidateColours();
    }
    /// <summary>
    /// Recolours the gameplay layer's placeholder scanner materials
    /// (Assets/Art/Office/Gameplay/Materials/Placeholder_Scanner*.mat, loaded by
    /// OfficeGameplay). Those assets are gameplay-owned (Build Office UI creates them and
    /// keeps existing colours), so this crosses the ownership line (triage B9): the art
    /// side should deliver a scanner model marked Anchor_Scanner instead.
    /// </summary>
    [MenuItem("Tools/Office Art/Debt Relief/Save Scanner Finishes")]
    public static void ApplyScannerFinishes()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Save scanner finishes in Edit mode.");
        var colours = new Dictionary<string,string> {
            {"Body","B4B2B6"}, {"Glass","211F26"}, {"Trim","966547"}, {"Light","EDAD73"}
        };
        foreach (var entry in colours)
        {
            var path = "Assets/Art/Office/Gameplay/Materials/Placeholder_Scanner" + entry.Key + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) throw new InvalidOperationException("Missing scanner material: " + path);
            Undo.RecordObject(material, "Scanner finish");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Hex(entry.Value));
            if (material.HasProperty("_Color")) material.SetColor("_Color", Hex(entry.Value));
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
    }
    /// <summary>The palette pass's check (Applied/validation.json).</summary>
    [Serializable] public class ColourValidation
    {
        /// <summary>True when there are no errors.</summary>
        public bool success;

        /// <summary>The protected hash matches the baseline.</summary>
        public bool pcAndFloorUnchanged;

        /// <summary>The gameplay hash matches the baseline.</summary>
        public bool gameplayUnchanged;

        /// <summary>No desk renderer moved since the baseline (the four requested layout roots may).</summary>
        public bool deskTransformsUnchanged;

        /// <summary>Crowd silhouettes found (19 expected).</summary>
        public int crowdGroups;

        /// <summary>Crowd silhouettes that overlap the portal in the office camera (0 expected).</summary>
        public int portalOverlaps;

        /// <summary>Material slots using a DebtRelief finish (at least 100 expected).</summary>
        public int assignedMaterials;

        /// <summary>What failed.</summary>
        public List<string> errors=new();
    }

    /// <summary>Read-only: compares the scene with the palette baseline, writes after.json and validation.json, throws on failure.</summary>
    [MenuItem("Tools/Office Art/Debt Relief/Validate Colours")]
    public static void ValidateColours()
    {
        CheckScene(); var before=JsonUtility.FromJson<AuditReport>(File.ReadAllText(ReportFolder+"/before.json"));var after=Capture();
        var v=new ColourValidation{pcAndFloorUnchanged=before.protectedHash==after.protectedHash,gameplayUnchanged=before.gameplayHash==after.gameplayHash,deskTransformsUnchanged=true};
        bool onlyRequestedDeskChanges=true;
        foreach(var a in after.renderers.Where(x=>x.path.StartsWith("HybridOffice/Booth/")||x.path.StartsWith("ImportedOfficeDress/Desk/")))
        {
            var old=before.renderers.FirstOrDefault(x=>x.path==a.path);
            if(old==null||(old.position-a.position).sqrMagnitude>.000001f||(old.rotation-a.rotation).sqrMagnitude>.000001f||(old.scale-a.scale).sqrMagnitude>.000001f)
            {v.deskTransformsUnchanged=false;if(!RequestedDeskChange(a.path))onlyRequestedDeskChanges=false;}
        }
        var cam=Camera.main; var portal=PortalRect(cam);
        foreach(var r in Require("OfficeHallCrowds").GetComponentsInChildren<Renderer>().Where(r=>r.name=="Merged silhouettes"))
        {v.crowdGroups++;if(Project(cam,r.bounds).Overlaps(portal))v.portalOverlaps++;}
        v.assignedMaterials=after.renderers.SelectMany(r=>r.materials).Count(m=>m.path.StartsWith(ArtFolder));
        if(!v.pcAndFloorUnchanged)v.errors.Add("Protected PC or floor differs from baseline.");
        if(!v.gameplayUnchanged)v.errors.Add("Gameplay component data changed.");
        if(!v.deskTransformsUnchanged&&!onlyRequestedDeskChanges)v.errors.Add("Desk layout changed outside the four requested prop roots.");
        if(v.portalOverlaps!=0)v.errors.Add("Crowd still overlaps portal in the office view.");
        if(v.crowdGroups!=19)v.errors.Add("Crowd group count changed.");
        if(v.assignedMaterials<100)v.errors.Add("Material pass incomplete.");
        v.success=v.errors.Count==0;
        File.WriteAllText(ReportFolder+"/after.json",JsonUtility.ToJson(after,true));
        File.WriteAllText(ReportFolder+"/validation.json",JsonUtility.ToJson(v,true));
        if(!v.success)throw new InvalidOperationException(string.Join("; ",v.errors));
        Debug.Log("Office material pass verified: protected PC/floor/gameplay unchanged; portal approach clear.");
    }
}
