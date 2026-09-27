using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// A live planar reflection for the concourse only; no baked reflection image.
[ExecuteAlways]
public sealed class TerminalFloorReflection : MonoBehaviour
{
    public Camera source;
    public float floorHeight=-5.2f;
    public LayerMask reflectedLayers=1<<30;
    Camera reflection;
    RenderTexture texture;
    bool rendering;
    void OnEnable(){RenderPipelineManager.beginCameraRendering+=Render;}
    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering-=Render;
        Shader.SetGlobalTexture("_TerminalFloorReflection",Texture2D.blackTexture);
        if(reflection)DestroyImmediate(reflection.gameObject);
        if(texture){texture.Release();DestroyImmediate(texture);}
    }
    void Render(ScriptableRenderContext context,Camera camera)
    {
        if(rendering||camera!=source||!source)return;
        rendering=true;bool inverted=GL.invertCulling;
        try{
            if(!reflection){var go=new GameObject("Terminal floor reflection camera"){hideFlags=HideFlags.HideAndDontSave};reflection=go.AddComponent<Camera>();reflection.enabled=false;}
            if(!texture){texture=new RenderTexture(1024,576,24,RenderTextureFormat.DefaultHDR){name="Live concourse reflection",hideFlags=HideFlags.HideAndDontSave,useMipMap=true,autoGenerateMips=true};texture.Create();}
            reflection.CopyFrom(source);reflection.enabled=false;reflection.cullingMask=reflectedLayers;
            reflection.targetTexture=texture;// URP ignores AdditionalCameraData for Reflection cameras and otherwise selects this project's 2D default renderer.
            reflection.cameraType=CameraType.Game;
            var data=reflection.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.renderPostProcessing=false;data.renderShadows=false;
            var mirror=Matrix4x4.identity;mirror.m11=-1;mirror.m13=2*floorHeight;
            reflection.worldToCameraMatrix=source.worldToCameraMatrix*mirror;
            reflection.transform.position=mirror.MultiplyPoint(source.transform.position);
            var point=reflection.worldToCameraMatrix.MultiplyPoint(new Vector3(0,floorHeight+.025f,0));
            var normal=reflection.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            reflection.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(point,normal)));
            GL.invertCulling=!inverted;
            #pragma warning disable CS0618
            UniversalRenderPipeline.RenderSingleCamera(context,reflection);
            #pragma warning restore CS0618
            Shader.SetGlobalTexture("_TerminalFloorReflection",texture);
            Shader.SetGlobalMatrix("_TerminalFloorVP",GL.GetGPUProjectionMatrix(reflection.projectionMatrix,true)*reflection.worldToCameraMatrix);
        }finally{GL.invertCulling=inverted;rendering=false;}
    }
}
