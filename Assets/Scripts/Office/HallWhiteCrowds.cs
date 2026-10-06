using System;
using UnityEngine;

/// <summary>Stationary background silhouettes. Visibility and palette vary; transforms never animate.</summary>
[ExecuteAlways, DefaultExecutionOrder(250)]
public sealed class HallWhiteCrowds : MonoBehaviour
{
    [Serializable] public sealed class Group
    {
        public MeshRenderer silhouette;
        public MeshRenderer contact;
        public bool balcony;
        public float sourceFootY;
        public float cycle=80, phase, hold=38;
        [Range(0,1)] public float opacity=.52f;
    }
    [SerializeField] HallLightingRig lighting;
    [SerializeField] SpriteRenderer architecture;
    [SerializeField] Group[] groups=Array.Empty<Group>();
    MaterialPropertyBlock properties;
    public Group[] Groups=>groups;
    public Color Palette
    {
        get
        {
            var w=HallBakedCycle.Weights(lighting!=null?lighting.Hour:12);
            return new Color(1,.92f,.82f)*w.x+new Color(.96f,.98f,1)*w.y+new Color(.82f,.66f,.55f)*w.z+new Color(.35f,.43f,.60f)*w.w;
        }
    }
    public void Configure(HallLightingRig rig,SpriteRenderer hall,Group[] entries)
    {lighting=rig;architecture=hall;groups=entries;Apply(0);}
    public static float Fade(Group group,float seconds)
    {
        float t=Mathf.Repeat(seconds+group.phase,group.cycle);
        const float duration=4;
        if(t<duration)return Mathf.SmoothStep(0,1,t/duration);
        if(t<duration+group.hold)return 1;
        if(t<duration*2+group.hold)return 1-Mathf.SmoothStep(0,1,(t-duration-group.hold)/duration);
        return 0;
    }
    void LateUpdate()=>Apply(Application.isPlaying?Time.timeSinceLevelLoad:0);
    public void Apply(float seconds)
    {
        properties??=new MaterialPropertyBlock();
        var color=Palette;
        foreach(var group in groups)
        {
            if(group.silhouette==null)continue;
            float fade=MotionPreference.Reduced?1:Fade(group,seconds);
            float alpha=fade*group.opacity;
            group.silhouette.enabled=alpha>.001f;
            group.silhouette.GetPropertyBlock(properties);
            var tint=color;tint.a=alpha;properties.SetColor("_Tint",tint);
            if(architecture!=null && architecture.sprite!=null)
            {
                var sprite=architecture.sprite;
                properties.SetMatrix("_ArtToLocal",architecture.transform.worldToLocalMatrix);
                properties.SetVector("_Canvas",new Vector4(sprite.pixelsPerUnit,sprite.pivot.x,sprite.pivot.y,group.balcony?1:0));
                properties.SetFloat("_FootPixelY",group.sourceFootY);
            }
            group.silhouette.SetPropertyBlock(properties);
            if(group.contact!=null)
            {
                group.contact.enabled=alpha>.001f;
                group.contact.GetPropertyBlock(properties);
                properties.SetColor("_Tint",new Color(.03f,.045f,.06f,alpha*.18f));
                group.contact.SetPropertyBlock(properties);
            }
        }
    }
}
