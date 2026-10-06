using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Temporary art preview only. Closing restores the previous lighting override.</summary>
public sealed class HallLightingPreviewWindow : EditorWindow
{
    [SerializeField] bool ownsPreview;
    [SerializeField] bool previousPreview;
    [SerializeField] float previousHour;
    [SerializeField] float hour;
    HallLightingRig rig;
    HallLightingSO settings;

    [MenuItem("Tools/Terminal Art/Lighting/Time Slider")]
    public static void Open()
    {
        var window=GetWindow<HallLightingPreviewWindow>();
        window.titleContent=new GUIContent("Hall lighting");
        window.minSize=new Vector2(520,210);
        window.maxSize=new Vector2(900,250);
        window.FindRig();
        if(window.settings!=null && !window.ownsPreview)window.hour=window.rig.Hour;
        window.ShowUtility();
        window.Focus();
    }
    void OnEnable()=>EditorApplication.update+=Refresh;
    void OnDisable()
    {
        EditorApplication.update-=Refresh;
        Restore();
    }
    void FindRig()
    {
        if(rig==null)rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        if(rig!=null)settings=rig.Settings;
    }
    void Restore()
    {
        if(ownsPreview && settings!=null)
        {
            settings.previewHourOn=previousPreview;
            settings.previewHour=previousHour;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        ownsPreview=false;
    }
    void Refresh()
    {
        FindRig();
        if(rig==null)return;
        if(!ownsPreview)hour=rig.Hour;
        if(ownsPreview)EditorApplication.QueuePlayerLoopUpdate();
        Repaint();
    }
    public void SetHour(float value)
    {
        FindRig();
        if(settings==null)return;
        if(!ownsPreview)
        {
            previousPreview=settings.previewHourOn;
            previousHour=settings.previewHour;
            ownsPreview=true;
        }
        hour=Mathf.Clamp(value,0,24);
        settings.previewHourOn=true;
        settings.previewHour=hour;
        rig.GetComponent<HallBakedLighting>()?.Apply();
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        Repaint();
    }
    public void UseGameClock()
    {
        FindRig();
        if(settings==null)return;
        settings.previewHourOn=false;
        if(ownsPreview)settings.previewHour=previousHour;
        ownsPreview=false;
        hour=rig.Hour;
        EditorApplication.QueuePlayerLoopUpdate();
    }
    public void SetCrowdOpacity(float value)
    {
        var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();
        if(crowds==null)return;
        if(!Application.isPlaying)Undo.RecordObject(crowds,"Crowd opacity");
        crowds.crowdOpacity=Mathf.Clamp01(value);
        crowds.Apply(Application.isPlaying?Time.timeSinceLevelLoad:0);
        if(!Application.isPlaying)UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(crowds.gameObject.scene);
        EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();Repaint();
    }
    void OnGUI()
    {
        FindRig();
        if(settings==null)
        {
            EditorGUILayout.HelpBox("Open the AnimeHall scene to preview its lighting.",MessageType.Info);
            return;
        }
        EditorGUILayout.Space(6);
        using(new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Lighting time",EditorStyles.boldLabel);
            int minutes=Mathf.RoundToInt(hour*60)%1440;
            GUILayout.Label($"{minutes/60:00}:{minutes%60:00}",EditorStyles.boldLabel,GUILayout.Width(55));
            GUILayout.Label(ownsPreview?"Preview":"Clock",GUILayout.Width(55));
        }
        EditorGUI.BeginChangeCheck();
        float next=EditorGUILayout.Slider(hour,0,24);
        if(EditorGUI.EndChangeCheck())SetHour(next);
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("Morning"))SetHour(8);
            if(GUILayout.Button("Noon"))SetHour(12);
            if(GUILayout.Button("Evening"))SetHour(16.5f);
            if(GUILayout.Button("Night"))SetHour(22);
            if(GUILayout.Button("Use game clock"))UseGameClock();
        }
        EditorGUILayout.Space(8);
        var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();
        if(crowds!=null)
        {
            EditorGUI.BeginChangeCheck();
            float opacity=EditorGUILayout.Slider("Crowd opacity (%)",crowds.crowdOpacity*100,0,100);
            if(EditorGUI.EndChangeCheck())SetCrowdOpacity(opacity/100);
        }
        var weights=HallBakedCycle.Weights(ownsPreview?hour:rig.Hour);
        EditorGUILayout.LabelField($"Morning {weights.x:P0}     Noon {weights.y:P0}     Evening {weights.z:P0}     Night {weights.w:P0}",EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField("Only the lighting changes. The shift clock keeps running.",EditorStyles.centeredGreyMiniLabel);
    }

    [MenuItem("Tools/Terminal Art/Lighting/Verify Time Slider")]
    public static void Verify()
    {
        Open();
        var window=GetWindow<HallLightingPreviewWindow>();
        window.FindRig();
        if(window.settings==null)throw new InvalidOperationException("No hall lighting rig.");
        float before=window.rig.Hour;
        window.SetHour(10);
        var day=HallBakedCycle.Weights(window.rig.Hour);
        if(Mathf.Abs(window.rig.Hour-10)>.001f || Mathf.Abs(day.x-.5f)>.001f || Mathf.Abs(day.y-.5f)>.001f)
            throw new InvalidOperationException("Morning/noon preview did not reach the lighting rig.");
        window.SetHour(18);
        var dusk=HallBakedCycle.Weights(window.rig.Hour);
        if(Mathf.Abs(dusk.z-.5f)>.001f || Mathf.Abs(dusk.w-.5f)>.001f)
            throw new InvalidOperationException("Evening/night preview did not reach the lighting rig.");
        window.UseGameClock();
        if(window.settings.previewHourOn)throw new InvalidOperationException("Clock override was not released.");
        window.SetHour(12);
        window.Restore();
        if(window.settings.previewHourOn)throw new InvalidOperationException("Temporary preview was not restored.");
        string folder="ArtDeliverables/TimeDesk/HallLayers/Completion/FourState";
        Directory.CreateDirectory(folder);
        File.WriteAllText(folder+"/slider-verification.txt",
            $"10:00 morning/noon 50/50 verified.\n18:00 evening/night 50/50 verified.\nGame clock resumes; temporary preview restored.\nSlider window left open. Clock before {before:0.00}, after {window.rig.Hour:0.00}.\n");
        Debug.Log("Hall lighting time slider verified and open.");
    }
    [MenuItem("Tools/Terminal Art/Lighting/Verify Crowd Slider")]
    public static void VerifyCrowds()
    {
        Open();var window=GetWindow<HallLightingPreviewWindow>();
        var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();
        if(crowds==null)throw new InvalidOperationException("No crowd controller.");
        var group=crowds.Groups[0];var position=group.silhouette.transform.position;float time=10-group.phase;
        var block=new MaterialPropertyBlock();string report="";
        foreach(float value in new[]{0f,1f,.65f})
        {
            window.SetCrowdOpacity(value);crowds.Apply(time);group.silhouette.GetPropertyBlock(block);var tint=block.GetColor("_Tint");
            if(value==0 && group.silhouette.enabled)throw new InvalidOperationException("Zero slider left crowd visible.");
            if(value==1 && Mathf.Abs(tint.a-1)>.001f)throw new InvalidOperationException("100 percent did not reach full opacity.");
            if(value==.65f && (tint.a<=group.opacity || tint.a>=1))throw new InvalidOperationException("65 percent did not increase visibility.");
            if(Mathf.Abs(tint.r-tint.g)>.001f || Mathf.Abs(tint.g-tint.b)>.001f)throw new InvalidOperationException("Crowd is not neutral white/grey.");
            if(group.silhouette.transform.position!=position)throw new InvalidOperationException("Slider moved a crowd.");
            report+=$"Slider {value:P0}, alpha {tint.a:F3}, neutral RGB {tint.r:F3}, visible {group.silhouette.enabled}\n";
        }
        crowds.Apply(Application.isPlaying?Time.timeSinceLevelLoad:0);
        Directory.CreateDirectory("ArtDeliverables/TimeDesk/WarmStone");File.WriteAllText("ArtDeliverables/TimeDesk/WarmStone/crowd-slider.txt",report);
    }
}
