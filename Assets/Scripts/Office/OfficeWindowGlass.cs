using UnityEngine;

/// <summary>
/// Art side (on the Hall_WindowGlass prefab, the hall's three window walls): one
/// Inspector control for every child pane, without material copies. The glass
/// material (Hall_ClearGlass, set by the Debt Relief palette pass) owns the panes'
/// alpha. The transparency slider the user asked for applies only while
/// <see cref="overrideMaterialAlpha"/> is on: then each pane's _BaseColor alpha
/// comes from the slider through a property block, and 100% hides the panes.
/// Turning the override off (the default), or disabling the component, hands the
/// alpha back to the material.
/// </summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class OfficeWindowGlass : MonoBehaviour
{
    /// <summary>On: the slider sets every pane's alpha. Off (the default): the material's own alpha shows.</summary>
    [SerializeField]
    [Tooltip("Off: the glass material's own alpha shows (the palette's clear glass). On: the slider below sets every pane's alpha.")]
    private bool overrideMaterialAlpha;

    /// <summary>0 = opaque, 100 = hidden; used only while <see cref="overrideMaterialAlpha"/> is on.</summary>
    [SerializeField, Range(0f, 100f)]
    [Tooltip("0 = opaque, 100 = fully transparent. Applies to all child window panes while Override Material Alpha is on.")]
    private float glassTransparency = 25f;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    /// <summary>The child pane renderers (gathered again when the children change).</summary>
    private Renderer[] _panes;

    private MaterialPropertyBlock _properties;

    /// <summary>The override state last applied; null forces the next Update to apply.</summary>
    private bool? _appliedOverride;

    /// <summary>The transparency last applied while overriding.</summary>
    private float _appliedTransparency = -1f;

    /// <summary>True when the panes must be gathered again.</summary>
    private bool _refreshPanes = true;

    private void OnEnable()
    {
        _refreshPanes = true;
        _appliedOverride = null;
    }

    // Validation can run during loading; rendering work waits for the next Update.
    private void OnValidate()
    {
        glassTransparency = Mathf.Clamp(glassTransparency, 0f, 100f);
        _appliedOverride = null;
    }

    private void OnTransformChildrenChanged() => _refreshPanes = true;

    private void Update()
    {
        if (_refreshPanes || _appliedOverride != overrideMaterialAlpha ||
            (overrideMaterialAlpha && !Mathf.Approximately(_appliedTransparency, glassTransparency)))
            Apply();
    }

    /// <summary>Hands the alpha back to the material when the component is switched off.</summary>
    private void OnDisable()
    {
        Release();
        _appliedOverride = null;
    }

    /// <summary>Applies the slider to every pane while overriding; otherwise releases them to the material.</summary>
    private void Apply()
    {
        if (_refreshPanes || _panes == null)
        {
            _panes = GetComponentsInChildren<Renderer>(true);
            _refreshPanes = false;
        }

        if (!overrideMaterialAlpha)
        {
            Release();
        }
        else
        {
            _properties ??= new MaterialPropertyBlock();
            float opacity = 1f - glassTransparency / 100f;
            foreach (Renderer pane in _panes)
            {
                if (pane == null || pane.sharedMaterial == null)
                    continue;

                Color tint = pane.sharedMaterial.GetColor(BaseColor);
                tint.a = opacity;
                pane.GetPropertyBlock(_properties);
                _properties.SetColor(BaseColor, tint);
                pane.SetPropertyBlock(_properties);
                // Fully clear means no residual specular silhouette at the endpoint.
                pane.enabled = opacity > 0f;
            }
        }

        _appliedOverride = overrideMaterialAlpha;
        _appliedTransparency = glassTransparency;
    }

    /// <summary>Clears the panes' property blocks and shows them: the material's own alpha applies.</summary>
    private void Release()
    {
        if (_panes == null)
            return;

        foreach (Renderer pane in _panes)
        {
            if (pane == null)
                continue;
            pane.SetPropertyBlock(null);
            pane.enabled = true;
        }
    }
}
