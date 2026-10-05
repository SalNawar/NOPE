using UnityEngine;

/// <summary>Exterior-only flying traffic registered to the hall's painted window aperture.</summary>
[ExecuteAlways,DefaultExecutionOrder(210)]
public sealed class HallCityExterior : MonoBehaviour
{
    [System.Serializable] public sealed class Lane
    {
        public SpriteRenderer vehicle;
        public float xStart,xEnd,yPixels,widthPixels,speed,phase;
    }
    public SpriteRenderer window;
    public Lane[] lanes;
    MaterialPropertyBlock properties;
    void LateUpdate()=>Apply(Application.isPlaying?Time.time:0);
    public void Apply(float seconds)
    {
        if(window==null || window.sprite==null || lanes==null)return;
        properties??=new MaterialPropertyBlock();
        var sprite=window.sprite;
        float ppu=sprite.pixelsPerUnit;
        bool reduced=MotionPreference.Reduced;
        foreach(var lane in lanes)
        {
            if(lane.vehicle==null || lane.vehicle.sprite==null)continue;
            float travel=reduced?0:seconds*lane.speed/Mathf.Abs(lane.xEnd-lane.xStart);
            float x=Mathf.Lerp(lane.xStart,lane.xEnd,Mathf.Repeat(lane.phase+travel,1));
            lane.vehicle.transform.localPosition=new Vector3((x-sprite.pivot.x)/ppu,
                (sprite.rect.height-lane.yPixels-sprite.pivot.y)/ppu,-.001f);
            float scale=lane.widthPixels/lane.vehicle.sprite.rect.width;
            lane.vehicle.transform.localScale=new Vector3(lane.xEnd<lane.xStart?-scale:scale,scale,1);
            lane.vehicle.GetPropertyBlock(properties);
            properties.SetMatrix("_WindowToLocal",window.transform.worldToLocalMatrix);
            properties.SetVector("_CanvasMetrics",new Vector4(ppu,sprite.pivot.x,sprite.pivot.y,0));
            properties.SetVector("_CanvasSize",new Vector4(sprite.rect.width,sprite.rect.height,0,0));
            lane.vehicle.SetPropertyBlock(properties);
        }
    }
}

