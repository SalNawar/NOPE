using UnityEngine;
[ExecuteAlways,DefaultExecutionOrder(210)]
public sealed class HallMountedFixture : MonoBehaviour
{
    public Vector4 floorPool;
    public HallLightingRig rig;
    public SpriteRenderer drawing;
    void LateUpdate()
    {
        if(rig==null || drawing==null)return;
        var w=HallBakedCycle.Weights(rig.Hour);
        float level=w.x*.18f+w.y*.12f+w.z*.65f+w.w;
        var c=new Color(1,.79f,.54f)*(.45f+.55f*level);c.a=1;drawing.color=c;
    }
}
