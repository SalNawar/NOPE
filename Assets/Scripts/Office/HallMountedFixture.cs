using UnityEngine;
[ExecuteAlways,DefaultExecutionOrder(210)]
public sealed class HallMountedFixture : MonoBehaviour
{
    public Vector4 floorPool;
    public HallLightingRig rig;
    public SpriteRenderer drawing;
    MaterialPropertyBlock properties;
    static readonly int FixtureLevelId=Shader.PropertyToID("_FixtureLevel");
    void LateUpdate()
    {
        if(drawing==null)return;
        var fixture=GetComponent<HallLight>();
        float level=EmissionLevel(rig,fixture);
        // The housing stays visible while off; only the diffuser emits.
        drawing.color=Color.white;
        properties??=new MaterialPropertyBlock();
        drawing.GetPropertyBlock(properties);
        properties.SetFloat(FixtureLevelId,level);
        drawing.SetPropertyBlock(properties);
    }
    internal static float EmissionLevel(HallLightingRig rig,HallLight fixture)
    {
        var settings=rig!=null?rig.Settings:null;
        float level=0;
        if(settings!=null && settings.lightingOn && fixture!=null)
        {
            float scheduled=HallDayCycle.FixtureLevel(rig.Hour,fixture.order,settings.Cycle);
            level=Mathf.Lerp(settings.fixtureOffShare,1,scheduled)
                *HallDayCycle.Flicker(fixture.SinceOn,MotionPreference.Reduced || !settings.fixtureFlicker);
        }
        return level;
    }
}
