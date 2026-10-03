using UnityEngine;

/// <summary>Extends the painted platform below its canvas with a perspective-correct floor.</summary>
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class HallForegroundFloor : MonoBehaviour
{
    [SerializeField] AnimeHallPresentation presentation;
    [SerializeField] HallLightingRig lighting;
    [SerializeField] SpriteRenderer registeredLayer;
    static readonly int Edge=Shader.PropertyToID("_HallFloorEdge");
    static readonly int Shade=Shader.PropertyToID("_HallFloorShade");
    public void Configure(AnimeHallPresentation art, HallLightingRig rig, SpriteRenderer layer)
    { presentation=art; lighting=rig; registeredLayer=layer; Apply(); }
    void OnEnable()=>Apply();
    void LateUpdate()=>Apply();
    public void Apply()
    {
        if(registeredLayer==null || registeredLayer.sprite==null) return;
        var bounds=registeredLayer.sprite.bounds;
        var edge=registeredLayer.transform.TransformPoint(new Vector3(bounds.center.x,bounds.min.y,0));
        Shader.SetGlobalVector(Edge,new Vector4(edge.x,edge.y,edge.z,1));
        var colour=Color.white;
        if(lighting!=null && lighting.Settings!=null && lighting.Settings.lightingOn)
        {
            var settings=lighting.Settings;
            float solar=HallDayCycle.SolarPosition(lighting.Hour,settings.Cycle);
            colour=settings.globalColour.Evaluate(solar)*settings.globalIntensity.Evaluate(solar);
        }
        else if(presentation!=null) colour=Color.Lerp(Color.white,new Color(.4f,.46f,.62f),presentation.evening);
        Shader.SetGlobalColor(Shade,colour);
    }
}
