using UnityEngine;

/// <summary>Feeds baked art maps and the existing desk/character light hook from the same clock.</summary>
[ExecuteAlways,DefaultExecutionOrder(200)]
public sealed class HallBakedLighting : MonoBehaviour
{
    [SerializeField] HallLightingRig rig;
    [SerializeField] AnimeHallPresentation art;
    [SerializeField] Material material;
    MaterialPropertyBlock properties;
    public Color TravellerShade => Blend(HallBakedCycle.Weights(rig.Hour),
        new Color(1,.91f,.82f),Color.white,new Color(1,.77f,.63f),new Color(.67f,.73f,.9f));
    public void Configure(HallLightingRig lights,AnimeHallPresentation presentation,Material drawing)
    {rig=lights;art=presentation;material=drawing;Apply();}
    static Color Blend(HallBakedCycle.Weights4 w,Color a,Color b,Color c,Color d)=>a*w.x+b*w.y+c*w.z+d*w.w;
    void LateUpdate()=>Apply();
    public void Apply()
    {
        if(rig==null || art==null || material==null) return;
        var weights=HallBakedCycle.Weights(rig.Hour);
        properties??=new MaterialPropertyBlock();
        foreach(var layer in art.layers)
        {
            if(layer.renderer==null) continue;
            layer.renderer.GetPropertyBlock(properties);
            properties.SetVector("_StateWeights",new Vector4(weights.x,weights.y,weights.z,weights.w));
            properties.SetFloat("_LightingAmount",rig.Settings!=null && rig.Settings.lightingOn?art.lightingAmount:0);
            properties.SetFloat("_CloudMotion",MotionPreference.Reduced?0:1);
            layer.renderer.SetPropertyBlock(properties);
        }
        if(art.daylight!=null)
        {
            art.daylight.color=Blend(weights,new Color(1,.88f,.7f),new Color(1,.97f,.91f),new Color(1,.66f,.4f),new Color(.47f,.58f,.87f));
            art.daylight.intensity=weights.x*.58f+weights.y*.7f+weights.z*.38f+weights.w*.12f;
            var direction=new Vector3(1,-.55f,.65f)*weights.x+new Vector3(1,-1.5f,.08f)*weights.y+new Vector3(1,-.45f,-.65f)*weights.z+new Vector3(1,-.8f,.15f)*weights.w;
            art.daylight.transform.rotation=Quaternion.LookRotation(direction.normalized,Vector3.up);
            art.daylight.shadows=LightShadows.Soft;
        }
    }
}



