using UnityEngine;

/// <summary>
/// The living city behind the hall's windows (art side: on the hall's window
/// layer in the AnimeHall scene) and its flying traffic, registered to the
/// painted window aperture. Each frame it hands the city shaders their clock
/// (the globals _CitySeconds and _CityMotion: still under Reduced Motion or
/// with <see cref="animateCity"/> off) and the windows' fade band
/// (_CityWindowFade, HallCity.NextFadeWidth: each window fades in once over
/// about 0.4 s at the shift clock's pace, and holds still while the clock
/// does; HALL-FX, BUG-13), moves each lane's car along its lane, faded in and
/// out at the lane's ends and lit by the hour's light (HallCity.HourLight of
/// the hall's lights' hour, as the city painting is: no daylight-bright car
/// over the night city, CODE-69). The pan and depth reach the window, its
/// panels and the hall layers that show the city only when they change
/// (BUG-65l: no per-frame property writes on every layer).
/// </summary>
[ExecuteAlways,DefaultExecutionOrder(210)]
public sealed class HallCityExterior : MonoBehaviour
{
    /// <summary>One flying car's lane across the window's canvas (px, top-left origin).</summary>
    [System.Serializable] public sealed class Lane
    {
        /// <summary>The car.</summary>
        public SpriteRenderer vehicle;

        /// <summary>The lane's start and end across the canvas, its row, the car's drawn width (px), its speed (px a second) and where along the lane it starts (0 to 1).</summary>
        public float xStart,xEnd,yPixels,widthPixels,speed,phase;
    }

    /// <summary>The hall's window layer (the living city's material, its sprite the canvas the lanes are measured on).</summary>
    public SpriteRenderer window;

    /// <summary>Retired: the city is always dry and no shader reads rain any more (HALL-FX, BUG-73d). The field stays only for its readers outside HALL-FX (CityView, three Terminal Art tools), which drop it at their cleanup; then it goes.</summary>
    [Range(0,1)] public float rain;

    /// <summary>How far the city's depth map shifts it with the hall's pan (0: flat).</summary>
    [Range(0,1)] public float depthStrength=.7f;

    /// <summary>False holds the city's clouds, smoke, flyers and cars still (as Reduced Motion does).</summary>
    public bool animateCity=true;

    /// <summary>The flying cars and their lanes.</summary>
    public Lane[] lanes;

    /// <summary>The window panels' sprites (art authoring).</summary>
    public Sprite[] connectedPanels;

    /// <summary>The window panels that draw the living city too (they take the pan and depth with the window).</summary>
    public SpriteRenderer[] panelRenderers;

    /// <summary>The share of a lane over which its car fades in at the start and out at the end.</summary>
    const float LaneFade=.06f;

    static readonly int PanId=Shader.PropertyToID("_CityPan");
    static readonly int DepthId=Shader.PropertyToID("_CityDepthStrength");
    static readonly int SecondsId=Shader.PropertyToID("_CitySeconds");
    static readonly int MotionId=Shader.PropertyToID("_CityMotion");
    static readonly int WindowFadeId=Shader.PropertyToID("_CityWindowFade");
    static readonly int CityLightsId=Shader.PropertyToID("_HallCityLights");
    static readonly int WindowToLocalId=Shader.PropertyToID("_WindowToLocal");
    static readonly int CanvasMetricsId=Shader.PropertyToID("_CanvasMetrics");
    static readonly int CanvasSizeId=Shader.PropertyToID("_CanvasSize");

    MaterialPropertyBlock properties;
    AnimeHallPresentation presentation;
    HallLightingRig lights;
    bool lightsSought;
    // The hall layers that take the city's pan, by shader (found once: Shader.name allocates every read).
    static Shader waitingBay,deepLayout;
    // What was last written (NaN: nothing yet), so a frame writes only what changed.
    float writtenPan=float.NaN,writtenDepth=float.NaN;
    Matrix4x4 writtenWindowToLocal;
    bool vehiclesWritten;
    float fadeWidth,lastLit=-1f;

    void OnEnable()
    {
        writtenPan=float.NaN;
        vehiclesWritten=false;
    }

    void LateUpdate()=>Apply(Application.isPlaying?Time.time:0);

    /// <summary>Draws the city at <paramref name="seconds"/> of its clock (the windows' fade band follows the shift clock's pace over this frame's unscaled time; in edit mode it holds).</summary>
    public void Apply(float seconds)
    {
        float dt=Application.isPlaying?Time.unscaledDeltaTime:0;
        if(window==null || window.sprite==null || lanes==null)return;
        properties??=new MaterialPropertyBlock();
        var sprite=window.sprite;
        float ppu=sprite.pixelsPerUnit;
        bool reduced=MotionPreference.Reduced;
        bool moving=!reduced && animateCity;
        if(presentation==null)presentation=GetComponentInParent<AnimeHallPresentation>();
        if(lights==null && !lightsSought)
        {
            lights=FindFirstObjectByType<HallLightingRig>();
            lightsSought=Application.isPlaying;
        }

        // The clock and the windows' fade band: globals, one write each a frame (the city view's own material keeps its own clock).
        Shader.SetGlobalFloat(SecondsId,seconds);
        Shader.SetGlobalFloat(MotionId,moving?1:0);
        float lit=Shader.GetGlobalVector(CityLightsId).x;
        fadeWidth=HallCity.NextFadeWidth(fadeWidth,lastLit<0?lit:lastLit,lit,dt);
        lastLit=lit;
        Shader.SetGlobalFloat(WindowFadeId,fadeWidth);

        // Exterior movement relative to its fixed window frames, written only when the pan or the depth changes.
        float pan=presentation!=null?(presentation.lookLeft-.5f)*(.05f/3f):0;
        if(pan!=writtenPan || depthStrength!=writtenDepth)
        {
            writtenPan=pan;
            writtenDepth=depthStrength;
            WritePan(window,pan);
            if(panelRenderers!=null)foreach(var panel in panelRenderers)
                if(panel!=null)WritePan(panel,pan);
            if(waitingBay==null)waitingBay=Shader.Find("NOPE/Hall Waiting Bay Repair");
            if(deepLayout==null)deepLayout=Shader.Find("NOPE/Hall Deep Layout");
            if(presentation!=null)foreach(var layer in presentation.layers)
            {
                var renderer=layer.renderer;
                if(renderer==null || renderer.sharedMaterial==null ||
                    (renderer.sharedMaterial.shader!=waitingBay && renderer.sharedMaterial.shader!=deepLayout))continue;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat(PanId,pan);
                renderer.SetPropertyBlock(properties);
            }
        }

        // The cars: each along its lane, faded at the lane's ends, lit by the hour; their aperture registration only when the window moved.
        var (r,g,b)=HallCity.HourLight(HallBakedCycle.Weights(lights!=null?lights.Hour:12f));
        Matrix4x4 windowToLocal=window.transform.worldToLocalMatrix;
        bool registration=!vehiclesWritten || windowToLocal!=writtenWindowToLocal;
        writtenWindowToLocal=windowToLocal;
        vehiclesWritten=true;
        foreach(var lane in lanes)
        {
            if(lane.vehicle==null || lane.vehicle.sprite==null)continue;
            float travel=Mathf.Repeat(lane.phase+(moving?seconds*lane.speed/Mathf.Max(1e-3f,Mathf.Abs(lane.xEnd-lane.xStart)):0),1);
            float x=Mathf.Lerp(lane.xStart,lane.xEnd,travel);
            lane.vehicle.transform.localPosition=new Vector3((x-sprite.pivot.x)/ppu,
                (sprite.rect.height-lane.yPixels-sprite.pivot.y)/ppu,-.001f);
            float scale=lane.widthPixels/lane.vehicle.sprite.rect.width;
            lane.vehicle.transform.localScale=new Vector3(lane.xEnd<lane.xStart?-scale:scale,scale,1);
            lane.vehicle.color=new Color(r,g,b,HallCity.LaneEndFade(travel,LaneFade));
            if(!registration)continue;
            lane.vehicle.GetPropertyBlock(properties);
            properties.SetMatrix(WindowToLocalId,windowToLocal);
            properties.SetVector(CanvasMetricsId,new Vector4(ppu,sprite.pivot.x,sprite.pivot.y,0));
            properties.SetVector(CanvasSizeId,new Vector4(sprite.rect.width,sprite.rect.height,0,0));
            lane.vehicle.SetPropertyBlock(properties);
        }
    }

    /// <summary>The pan and depth on one living-city renderer (its property block, read first so other writers' values stay).</summary>
    void WritePan(SpriteRenderer renderer,float pan)
    {
        renderer.GetPropertyBlock(properties);
        properties.SetFloat(PanId,pan);
        properties.SetFloat(DepthId,depthStrength);
        renderer.SetPropertyBlock(properties);
    }
}
