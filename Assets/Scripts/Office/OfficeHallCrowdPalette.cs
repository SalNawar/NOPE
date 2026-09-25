using System;
using UnityEngine;

/// <summary>Colours the authored crowd groups from shift progress. Never spawns people or changes gameplay.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class OfficeHallCrowdPalette : MonoBehaviour
{
    public enum PreviewMode { Automatic, Morning, Evening }
    [Serializable] public sealed class Group
    {
        public MeshRenderer renderer;
        public Material morning;
        public Material evening;
    }
    [SerializeField] private DayOrchestrator orchestrator;
    [SerializeField] private Group[] groups=Array.Empty<Group>();
    [SerializeField] private PreviewMode preview;
    [SerializeField, Range(0,1)] private float eveningStartsAt=.50f;
    [SerializeField, Range(0,1)] private float eveningFullAt=.90f;
    private MaterialPropertyBlock properties;
    private float appliedBlend=-1;
    private static readonly int Tint=Shader.PropertyToID("_Tint");
    public float EveningBlend => appliedBlend;
    public PreviewMode Preview => preview;

    private void OnEnable(){appliedBlend=-1;Refresh();}
    private void Update()=>Refresh();
    public void SetPreview(PreviewMode mode){preview=mode;appliedBlend=-1;Refresh();}
    public static float BlendAt(float progress,float start=.50f,float end=.90f)
        =>Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,Mathf.Max(start+.001f,end),progress));

    private void Refresh()
    {
        // Main loads gameplay additively and disables the art scene's legacy day driver.
        // Bind the active gameplay driver once it is available, retaining the editor reference.
        if (Application.isPlaying && (!orchestrator || !orchestrator.isActiveAndEnabled))
            orchestrator = UnityEngine.Object.FindAnyObjectByType<DayOrchestrator>();
        float blend=preview==PreviewMode.Morning?0:preview==PreviewMode.Evening?1:
            BlendAt(orchestrator?orchestrator.ShiftProgress:0,eveningStartsAt,eveningFullAt);
        if(Mathf.Approximately(appliedBlend,blend))return;
        appliedBlend=blend;properties??=new MaterialPropertyBlock();
        foreach(var group in groups)
        {
            if(group==null || !group.renderer || !group.morning || !group.evening)continue;
            // The source assets remain unchanged; only this renderer's colour changes.
            properties.Clear();properties.SetColor(Tint,Color.Lerp(group.morning.GetColor(Tint),group.evening.GetColor(Tint),blend));
            group.renderer.SetPropertyBlock(properties);
        }
    }
    private void OnDisable()
    {
        foreach(var group in groups)if(group?.renderer)group.renderer.SetPropertyBlock(null);
        appliedBlend=-1;
    }
}
