using UnityEngine;

/// <summary>Registered cast shadows for the painted hall's floor receivers.</summary>
[ExecuteAlways]
public sealed class HallGroundShadows : MonoBehaviour
{
    [SerializeField] HallLightingRig lighting;
    [SerializeField] MeshRenderer drawing;
    MaterialPropertyBlock _properties;
    public void Configure(HallLightingRig rig,MeshRenderer renderer) {lighting=rig;drawing=renderer;Apply();}
    void LateUpdate()=>Apply();
    public void Apply()
    {
        if(drawing==null) return;
        _properties??=new MaterialPropertyBlock();
        drawing.GetPropertyBlock(_properties);
        _properties.SetFloat("_Strength",Mathf.Lerp(.42f,.25f,lighting!=null?lighting.Evening:0));
        drawing.SetPropertyBlock(_properties);
    }
}
