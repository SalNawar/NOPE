using UnityEngine;

/// <summary>Feeds baked art maps and the existing desk/character light hook from the same clock.</summary>
[ExecuteAlways,DefaultExecutionOrder(200)]
public sealed class HallBakedLighting : MonoBehaviour
{
    /// <summary>The hall's lighting rig: its hour drives the baked states.</summary>
    [SerializeField] HallLightingRig rig;
    /// <summary>The painted hall whose layers get the state weights.</summary>
    [SerializeField] AnimeHallPresentation art;
    /// <summary>The four-state material (NOPE/Hall Four State); nothing is applied without it.</summary>
    [SerializeField] Material material;
    MaterialPropertyBlock properties;
    HallLight[] fixtures;
    readonly float[] fixtureLevels=new float[16];
    readonly Vector4[] fixtureCenters=new Vector4[16];
    static readonly int FixtureLevelsId=Shader.PropertyToID("_FixtureLevels");
    static readonly int FixtureCountId=Shader.PropertyToID("_ScheduledFixtureCount");
    static readonly int FixtureCentersId=Shader.PropertyToID("_FixtureCenters");
    static readonly int StateWeightsId=Shader.PropertyToID("_StateWeights");
    static readonly int ShadowRayId=Shader.PropertyToID("_HallShadowRay");
    static readonly int LightingAmountId=Shader.PropertyToID("_LightingAmount");
    static readonly int CloudMotionId=Shader.PropertyToID("_CloudMotion");
    static readonly int PaletteAmountId=Shader.PropertyToID("_PaletteAmount");
    /// <summary>The traveller's tint at the rig's hour: the four states' shades blended (white without a rig).</summary>
    public Color TravellerShade => rig == null ? Color.white : Blend(HallBakedCycle.Weights(rig.Hour),
        new Color(1,.91f,.82f),Color.white,new Color(1,.77f,.63f),new Color(.67f,.73f,.9f));
    /// <summary>Wires the rig, the hall and the material, then applies (the art's authoring).</summary>
    public void Configure(HallLightingRig lights,AnimeHallPresentation presentation,Material drawing)
    {rig=lights;art=presentation;material=drawing;Apply();}
    static Color Blend(HallBakedCycle.Weights4 w,Color a,Color b,Color c,Color d)=>a*w.x+b*w.y+c*w.z+d*w.w;
    void LateUpdate()=>Apply();
    /// <summary>The daylight's direction for the states' shares (also the floor shadows' ray).</summary>
    public static Vector3 DaylightDirection(HallBakedCycle.Weights4 weights) =>
        new Vector3(1,-.55f,.65f)*weights.x+new Vector3(1,-1.5f,.08f)*weights.y+new Vector3(1,-.45f,-.65f)*weights.z+new Vector3(1,-.8f,.15f)*weights.w;
    /// <summary>Writes the state weights, shadow ray and motion to every painted layer and poses the daylight.</summary>
    public void Apply()
    {
        if(rig==null || art==null || material==null) return;
        var weights=HallBakedCycle.Weights(rig.Hour);
        var direction=DaylightDirection(weights);
        if(fixtures==null || !Application.isPlaying)
        {
            var found=rig.GetComponentsInChildren<HallLight>(true);
            fixtures=System.Array.FindAll(found,l=>l.kind==HallLightKind.Fixture);
            System.Array.Sort(fixtures,(a,b)=>string.CompareOrdinal(a.name,b.name));
        }
        int fixtureCount=System.Math.Min(fixtures.Length,16);
        for(int i=0;i<fixtureCount;i++)fixtureLevels[i]=fixtures[i]!=null && fixtures[i].gameObject.activeInHierarchy
            ?HallMountedFixture.EmissionLevel(rig,fixtures[i]):0;
        properties??=new MaterialPropertyBlock();
        foreach(var layer in art.layers)
        {
            if(layer.renderer==null) continue;
            layer.renderer.GetPropertyBlock(properties);
            properties.SetVector(StateWeightsId,new Vector4(weights.x,weights.y,weights.z,weights.w));
            properties.SetVector(ShadowRayId,new Vector4(direction.x/-direction.y,direction.z/-direction.y,0,0));
            properties.SetFloat(LightingAmountId,rig.Settings!=null && rig.Settings.lightingOn?art.lightingAmount:0);
            properties.SetFloat(CloudMotionId,MotionPreference.Reduced?0:1);
            properties.SetFloat(PaletteAmountId,1);
            properties.SetFloatArray(FixtureLevelsId,fixtureLevels);
            properties.SetInt(FixtureCountId,fixtureCount);
            var sprite=layer.renderer.sprite;
            if(sprite!=null)
                for(int i=0;i<fixtureCount;i++)
                {
                    var local=layer.renderer.transform.InverseTransformPoint(fixtures[i].transform.position);
                    fixtureCenters[i]=new Vector4((local.x*sprite.pixelsPerUnit+sprite.pivot.x)/sprite.rect.width,
                        (local.y*sprite.pixelsPerUnit+sprite.pivot.y)/sprite.rect.height,0,0);
                }
            properties.SetVectorArray(FixtureCentersId,fixtureCenters);
            layer.renderer.SetPropertyBlock(properties);
        }
        if(art.daylight!=null)
        {
            art.daylight.color=Blend(weights,new Color(1,.88f,.7f),new Color(1,.97f,.91f),new Color(1,.66f,.4f),new Color(.47f,.58f,.87f));
            art.daylight.intensity=weights.x*.58f+weights.y*.7f+weights.z*.38f+weights.w*.12f;
            art.daylight.transform.rotation=Quaternion.LookRotation(direction.normalized,Vector3.up);
            art.daylight.shadows=LightShadows.Soft;
        }
    }
}



