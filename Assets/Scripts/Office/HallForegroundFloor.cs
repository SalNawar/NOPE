using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Extends the painted platform below its canvas with a perspective-correct floor.</summary>
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class HallForegroundFloor : MonoBehaviour
{
    [SerializeField] AnimeHallPresentation presentation;
    [SerializeField] HallLightingRig lighting;
    [SerializeField] SpriteRenderer registeredLayer;
    [SerializeField] HallBackdrop backdrop;
    [SerializeField] bool matchPaintedPerspective=true;
    [SerializeField] Vector2 paintedVanishingPoint=new Vector2(1000,280);
    [SerializeField] Vector3 referenceCameraPosition=new Vector3(0,2.16f,-2.62f);
    static readonly int Edge=Shader.PropertyToID("_HallFloorEdge");
    static readonly int Shade=Shader.PropertyToID("_HallFloorShade");
    public void Configure(AnimeHallPresentation art, HallLightingRig rig, SpriteRenderer layer)
    { presentation=art; lighting=rig; registeredLayer=layer; backdrop=rig!=null?rig.GetComponent<HallBackdrop>():null; Apply(); }
    void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeforeCamera;Apply();}
    void OnDisable(){RenderPipelineManager.beginCameraRendering-=BeforeCamera;Shader.SetGlobalFloat("_HallFloorHasBackdrop",0);}
    void BeforeCamera(ScriptableRenderContext context,Camera camera)
    {
        // Sync has already resized the hall texture before cameras render. Bind it
        // here so manual captures and resolution changes cannot leave a stale RT.
        var texture=backdrop!=null && backdrop.Active?backdrop.RenderedTexture:null;
        Shader.SetGlobalFloat("_HallFloorHasBackdrop",texture!=null && texture.IsCreated()?1:0);
        if(texture!=null)
        {
            Shader.SetGlobalTexture("_HallFloorBackdrop",texture);
            Shader.SetGlobalVector("_HallFloorBackdropSize",new Vector4(1f/texture.width,1f/texture.height,texture.width,texture.height));
        }
    }
    void LateUpdate()=>Apply();
    public void Apply()
    {
        if(registeredLayer==null || registeredLayer.sprite==null) return;
        var bounds=registeredLayer.sprite.bounds;
        if(matchPaintedPerspective)
        {
            // The painted hall uses its own perspective. Align this visual-only
            // ground proxy to its vanishing direction rather than the desk's grid.
            // The hall is an illustration, not a physical ground plane. Register
            // the continuation in that same plane: a second perspective grid
            // can match its vanishing point but cannot match its painted joints.
            transform.SetPositionAndRotation(registeredLayer.transform.position,registeredLayer.transform.rotation);
            transform.localScale=registeredLayer.transform.lossyScale;
        }
        var edge=registeredLayer.transform.TransformPoint(new Vector3(bounds.center.x,bounds.min.y,0));
        Shader.SetGlobalVector(Edge,new Vector4(edge.x,edge.y,edge.z,1));
        Shader.SetGlobalVector("_HallFloorCanvas",new Vector4(registeredLayer.sprite.rect.width,registeredLayer.sprite.rect.height,paintedVanishingPoint.x,paintedVanishingPoint.y));
        Shader.SetGlobalMatrix("_HallFloorCanvasToWorld",registeredLayer.transform.localToWorldMatrix);
        Shader.SetGlobalVector("_HallFloorRegistration",new Vector4(registeredLayer.sprite.pixelsPerUnit,registeredLayer.sprite.pivot.x,registeredLayer.sprite.pivot.y,0));
        var colour=Color.white;
        if(lighting!=null && lighting.Settings!=null && lighting.Settings.lightingOn)
        {
            var settings=lighting.Settings;
            float solar=HallDayCycle.SolarPosition(lighting.Hour,settings.Cycle);
            colour=settings.globalColour.Evaluate(solar)*settings.globalIntensity.Evaluate(solar);
        }
        else if(lighting==null && presentation!=null) colour=Color.Lerp(Color.white,new Color(.4f,.46f,.62f),presentation.evening);
        Shader.SetGlobalColor(Shade,colour);
    }
}
