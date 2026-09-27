using UnityEngine;
using System.Linq;

[ExecuteAlways]
public sealed class LayeredHallRig : MonoBehaviour
{
    [Range(0,1)] public float evening;
    [Range(-1,1)] public float sunDirection;
    [Range(0,27)] public float lookLeft;
    public bool animatePortal=true;
    public Camera view;
    public Light deskSun;
    public Transform cityLayer;
    public Transform hallLayer;
    Transform[] panLayers;
    Vector3[] originalPositions;
    Quaternion[] originalRotations;
    void OnEnable(){panLayers=null;Apply();}
    void OnValidate(){Apply();}
    void Update(){Apply();}
    public void SetTime(float value){evening=Mathf.Clamp01(value);Apply();}
    [ContextMenu("Morning")] public void Morning(){SetTime(0);}
    [ContextMenu("Evening")] public void Evening(){SetTime(1);}
    [ContextMenu("Look left")] public void Left(){lookLeft=27;Apply();}
    [ContextMenu("Look forward")] public void Forward(){lookLeft=0;Apply();}
    public void Apply()
    {
        Shader.SetGlobalFloat("_HallEvening",evening);
        Shader.SetGlobalFloat("_HallSunShift",sunDirection);
        Shader.SetGlobalVector("_HallLightDirection",new Vector4(Mathf.Lerp(-.8f,.8f,(sunDirection+1)*.5f),.65f,.7f,0));
        Shader.SetGlobalFloat("_HallClock",animatePortal?Time.realtimeSinceStartup:0);
        if(view){
            if(panLayers==null){
                panLayers=GetComponentsInChildren<Renderer>().Where(r=>r is SpriteRenderer||r.name.StartsWith("03 Anonymous")||r.name.StartsWith("LH_")).Select(r=>r.transform).Distinct().ToArray();
                originalPositions=panLayers.Select(t=>t.position).ToArray();originalRotations=panLayers.Select(t=>t.rotation).ToArray();
            }
            view.transform.rotation=Quaternion.Euler(4,-lookLeft,0);
            var turn=Quaternion.Euler(0,-lookLeft,0);var referenceForward=Quaternion.Euler(4,0,0)*Vector3.forward;
            for(int i=0;i<panLayers.Length;i++){
                var relative=originalPositions[i]-view.transform.position;
                float depth=Vector3.Dot(relative,referenceForward);
                float shift=2*depth*Mathf.Tan(55*Mathf.Deg2Rad*.5f)*(16f/9)*.42f*(lookLeft/27f);
                panLayers[i].SetPositionAndRotation(view.transform.position+turn*relative+view.transform.right*shift,turn*originalRotations[i]);
            }
        }
        if(deskSun){deskSun.intensity=Mathf.Lerp(1.15f,.32f,evening);deskSun.color=Color.Lerp(new Color(1,.9f,.76f),new Color(.62f,.69f,1),evening);}
    }
}
