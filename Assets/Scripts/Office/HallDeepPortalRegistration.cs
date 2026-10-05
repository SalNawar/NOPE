using UnityEngine;

/// <summary>Registers gameplay portal glow to the approved painted openings.</summary>
[DefaultExecutionOrder(230)]
public sealed class HallDeepPortalRegistration : MonoBehaviour
{
    [SerializeField] SpriteRenderer architecture;
    [SerializeField] Material clippedGlow;
    PortalEffect[] effects;
    MaterialPropertyBlock block;
    public void Configure(SpriteRenderer drawing,Material glow){architecture=drawing;clippedGlow=glow;}
    void LateUpdate()
    {
        if(architecture==null || architecture.sprite==null || clippedGlow==null)return;
        if(effects==null || effects.Length==0)effects=FindObjectsByType<PortalEffect>(FindObjectsSortMode.None);
        block??=new MaterialPropertyBlock();
        foreach(var effect in effects)
        {
            if(effect==null)continue;
            var renderer=effect.GetComponentInChildren<SpriteRenderer>(true);
            if(renderer==null)continue;
            if(renderer.sharedMaterial!=clippedGlow)renderer.sharedMaterial=clippedGlow;
            renderer.GetPropertyBlock(block);
            var sprite=architecture.sprite;
            block.SetMatrix("_ArtToLocal",architecture.transform.worldToLocalMatrix);
            block.SetVector("_CanvasMetrics",new Vector4(sprite.pixelsPerUnit,sprite.pivot.x,sprite.pivot.y,0));
            block.SetVector("_CanvasSize",new Vector4(sprite.rect.width,sprite.rect.height,0,0));
            renderer.SetPropertyBlock(block);
        }
    }
}
