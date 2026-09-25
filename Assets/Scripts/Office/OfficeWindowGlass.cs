using UnityEngine;

/// <summary>One Inspector control for the three window walls; no material copies.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class OfficeWindowGlass : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    [Tooltip("0 = opaque, 100 = fully transparent. Applies to all child window panes.")]
    private float glassTransparency = 25f;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private Renderer[] panes;
    private MaterialPropertyBlock properties;
    private float appliedTransparency = -1f;
    private bool refreshPanes = true;

    public float GlassTransparency
    {
        get => glassTransparency;
        set
        {
            glassTransparency = Mathf.Clamp(value, 0f, 100f);
            Apply();
        }
    }

    private void OnEnable() => refreshPanes = true;

    // Validation can run during loading; rendering work waits for the next update.
    private void OnValidate()
    {
        glassTransparency = Mathf.Clamp(glassTransparency, 0f, 100f);
        appliedTransparency = -1f;
    }

    private void OnTransformChildrenChanged() => refreshPanes = true;

    private void Update()
    {
        if (refreshPanes || !Mathf.Approximately(appliedTransparency, glassTransparency))
            Apply();
    }

    private void Apply()
    {
        if (refreshPanes || panes == null)
        {
            panes = GetComponentsInChildren<Renderer>(true);
            refreshPanes = false;
        }

        properties ??= new MaterialPropertyBlock();
        float opacity = 1f - glassTransparency / 100f;
        foreach (Renderer pane in panes)
        {
            if (pane == null || pane.sharedMaterial == null)
                continue;

            Color tint = pane.sharedMaterial.GetColor(BaseColor);
            tint.a = opacity;
            pane.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, tint);
            pane.SetPropertyBlock(properties);
            // Fully clear means no residual specular silhouette at the endpoint.
            pane.enabled = opacity > 0f;
        }

        appliedTransparency = glassTransparency;
    }
}
