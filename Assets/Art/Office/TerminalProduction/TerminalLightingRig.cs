using UnityEngine;
using UnityEngine.Rendering;

// Art preview only. Gameplay can drive SetTime after agreeing the integration hook.
[ExecuteAlways]
public sealed class TerminalLightingRig : MonoBehaviour
{
    public RenderPipelineAsset previewPipeline;
    public Light daylight;
    public Light[] practicals;
    public Camera previewCamera;
    [Range(0,1)] public float evening;
    RenderPipelineAsset previousPipeline;
    bool ownsOverride;
    void OnEnable()
    {
        if(previewPipeline && QualitySettings.renderPipeline!=previewPipeline)
        { previousPipeline=QualitySettings.renderPipeline;QualitySettings.renderPipeline=previewPipeline;ownsOverride=true; }
        Apply();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.quitting+=ReleasePipeline;
        #endif
    }
    void OnDisable()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.quitting-=ReleasePipeline;
        #endif
        ReleasePipeline();
    }
    void ReleasePipeline()
    {
        if(ownsOverride && QualitySettings.renderPipeline==previewPipeline)QualitySettings.renderPipeline=previousPipeline;
        ownsOverride=false;
    }
    void OnValidate(){Apply();}
    public void SetTime(float value){evening=Mathf.Clamp01(value);Apply();}
    [ContextMenu("Preview morning")] public void Morning(){SetTime(0);}
    [ContextMenu("Preview evening")] public void Evening(){SetTime(1);}
    public void Apply()
    {
        if(!daylight)return;
        daylight.transform.rotation=Quaternion.Euler(Mathf.Lerp(38,15,evening),Mathf.Lerp(55,110,evening),0);
        daylight.color=Color.Lerp(new Color(1,.91f,.77f),new Color(.72f,.73f,1),evening);
        daylight.intensity=Mathf.Lerp(1.45f,.23f,evening);
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=Color.Lerp(new Color(.48f,.53f,.61f),new Color(.18f,.21f,.31f),evening);
        RenderSettings.ambientEquatorColor=Color.Lerp(new Color(.39f,.35f,.30f),new Color(.16f,.14f,.19f),evening);
        RenderSettings.ambientGroundColor=Color.Lerp(new Color(.14f,.12f,.11f),new Color(.045f,.04f,.07f),evening);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.006f;
        RenderSettings.fogColor=Color.Lerp(new Color(.76f,.73f,.67f),new Color(.15f,.16f,.25f),evening);
        if(previewCamera)previewCamera.backgroundColor=RenderSettings.fogColor;
        if(practicals!=null)foreach(var l in practicals)if(l)l.intensity=Mathf.Lerp(5,14,evening);
    }
}
