using System;
using UnityEngine;

// Art-only controls. Gameplay supplies time and pan; this component owns no clock or input.
[ExecuteAlways]
public sealed class AnimeHallPresentation : MonoBehaviour
{
    [Serializable] public sealed class Layer
    {
        public string id;
        public SpriteRenderer renderer;
    }
    public Layer[] layers = Array.Empty<Layer>();
    [Range(0,1)] public float lightingAmount = 1;
    [Range(0,1)] public float evening;
    [Range(0,1)] public float lookLeft;
    public Light daylight;
    public Light localLight;
    public Vector3 forwardLocalPosition;
    public Vector3 leftLocalPosition;
    MaterialPropertyBlock block;

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }
    void Update() { Apply(); }
    public void SetTime(float normalizedEvening) { evening = Mathf.Clamp01(normalizedEvening); Apply(); }
    public void SetPan(float normalizedPan) { lookLeft = Mathf.Clamp01(normalizedPan); Apply(); }
    public SpriteRenderer FindLayer(string id)
    {
        foreach (var layer in layers) if (layer.id == id) return layer.renderer;
        return null;
    }
    public void SetLayerTint(string id, Color color)
    {
        var target = FindLayer(id);
        if (target != null) target.color = color;
    }
    public void Apply()
    {
        if (layers == null || layers.Length == 0) return;
        block ??= new MaterialPropertyBlock();
        var ambient = Color.Lerp(new Color(.55f,.55f,.57f),new Color(.10f,.13f,.24f),evening);
        foreach (var layer in layers)
        {
            if (layer.renderer == null) continue;
            layer.renderer.GetPropertyBlock(block);
            block.SetFloat("_LightingAmount",lightingAmount);
            block.SetColor("_AmbientColor",ambient);
            layer.renderer.SetPropertyBlock(block);
        }
        transform.localPosition = Vector3.Lerp(forwardLocalPosition,leftLocalPosition,lookLeft);
        if (daylight != null)
        {
            daylight.intensity = Mathf.Lerp(.65f,.14f,evening);
            daylight.color = Color.Lerp(new Color(1f,.95f,.88f),new Color(.52f,.63f,1f),evening);
        }
    }
}
