using UnityEngine;
/// <summary>A gallery lamp drawn into the hall: its unlit housing and mount always show; its diffuser glass glows by its fixture light's lit share (HallLight.Lit, set by the rig from the existing fixture schedule, off with the rig's lighting).</summary>
[ExecuteAlways,DefaultExecutionOrder(210)]
public sealed class HallMountedFixture : MonoBehaviour
{
    /// <summary>The lamp's light pool on the floor (the four-state baker's).</summary>
    public Vector4 floorPool;
    /// <summary>The hall's lighting rig (the baker reads it).</summary>
    public HallLightingRig rig;
    /// <summary>The housing drawing (NOPE/Hall Mounted Fixture), whose diffuser takes the light's level.</summary>
    public SpriteRenderer drawing;
    MaterialPropertyBlock properties;
    HallLight fixture;
    static readonly int FixtureLevelId=Shader.PropertyToID("_FixtureLevel");
    void LateUpdate()
    {
        if(drawing==null)return;
        if(fixture==null)fixture=GetComponent<HallLight>();
        // The housing stays visible while off; only the diffuser emits.
        drawing.color=Color.white;
        properties??=new MaterialPropertyBlock();
        drawing.GetPropertyBlock(properties);
        properties.SetFloat(FixtureLevelId,fixture!=null?fixture.Lit:0);
        drawing.SetPropertyBlock(properties);
    }
}
