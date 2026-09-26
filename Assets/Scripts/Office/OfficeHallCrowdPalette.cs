using System;
using UnityEngine;

/// <summary>
/// Art side (OfficeScene, on the OfficeHallCrowds root): tints the hall's painted
/// crowd groups from their morning to their evening colours as the shift runs.
/// Presentation only: it never spawns people or touches gameplay. Each group's
/// renderer gets a property-block _Tint lerped between its morning and evening
/// materials' _Tint, so the materials themselves never change. In Automatic mode
/// the blend follows the gameplay layer's shift clock through its read-only hook
/// (<see cref="ShiftClockDriver.Live"/>) along <see cref="CrowdPaletteBlend"/>;
/// without a gameplay layer (the art office on its own, edit mode) the crowds keep
/// their morning colours. Morning and Evening force one end for previews
/// (Tools > Office Art > Hall Crowds).
/// </summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class OfficeHallCrowdPalette : MonoBehaviour
{
    /// <summary>Which palette the crowds show.</summary>
    public enum PreviewMode
    {
        /// <summary>Follow the shift clock (normal play).</summary>
        Automatic,

        /// <summary>Always the morning colours (editor preview).</summary>
        Morning,

        /// <summary>Always the evening colours (editor preview).</summary>
        Evening
    }

    /// <summary>One tinted renderer and the two materials whose _Tint it blends between.</summary>
    [Serializable]
    public sealed class Group
    {
        /// <summary>A group's merged silhouette card or its ground contact.</summary>
        public MeshRenderer renderer;

        /// <summary>The material holding the morning _Tint (the renderer's own material).</summary>
        public Material morning;

        /// <summary>The material holding the evening _Tint.</summary>
        public Material evening;
    }

    /// <summary>Every tinted renderer: each group's silhouette and its ground contact.</summary>
    [SerializeField] private Group[] groups = Array.Empty<Group>();

    /// <summary>Automatic for play; Morning or Evening to preview one palette.</summary>
    [SerializeField] private PreviewMode preview;

    /// <summary>Shift progress (0 opening, 1 closing) at which the crowds start to darken.</summary>
    [SerializeField, Range(0, 1)] private float eveningStartsAt = .50f;

    /// <summary>Shift progress at which the crowds wear their full evening colours.</summary>
    [SerializeField, Range(0, 1)] private float eveningFullAt = .90f;

    private static readonly int Tint = Shader.PropertyToID("_Tint");

    private MaterialPropertyBlock _properties;

    /// <summary>The blend last written to the renderers (-1 before the first write).</summary>
    private float _appliedBlend = -1f;

    /// <summary>The blend last applied: 0 morning, 1 evening (-1 before the first refresh). Read by Hall Crowds > Validate.</summary>
    public float EveningBlend => _appliedBlend;

    /// <summary>The current mode. Read by Hall Crowds > Validate.</summary>
    public PreviewMode Preview => preview;

    /// <summary>Switches the mode and repaints at once (the Hall Crowds preview menus).</summary>
    public void SetPreview(PreviewMode mode)
    {
        preview = mode;
        _appliedBlend = -1f;
        Refresh();
    }

    private void OnEnable()
    {
        _appliedBlend = -1f;
        Refresh();
    }

    private void Update() => Refresh();

    /// <summary>Hands the renderers back to their materials' own colours.</summary>
    private void OnDisable()
    {
        foreach (Group group in groups)
            if (group != null && group.renderer != null)
                group.renderer.SetPropertyBlock(null);
        _appliedBlend = -1f;
    }

    /// <summary>The blend for the current mode: forced by a preview, else the live shift's, else morning.</summary>
    private float TargetBlend()
    {
        if (preview == PreviewMode.Morning)
            return 0f;
        if (preview == PreviewMode.Evening)
            return 1f;

        // The gameplay layer publishes its shift clock while it is loaded; the art scene
        // on its own (or edit mode) has none, and the crowds stay in the morning.
        IShiftProgress shift = ShiftClockDriver.Live;
        return shift != null ? CrowdPaletteBlend.Evening(shift.Progress01, eveningStartsAt, eveningFullAt) : 0f;
    }

    /// <summary>Writes the blended tint to every group, only when the blend changed.</summary>
    private void Refresh()
    {
        float blend = TargetBlend();
        if (Mathf.Approximately(_appliedBlend, blend))
            return;

        _appliedBlend = blend;
        _properties ??= new MaterialPropertyBlock();
        foreach (Group group in groups)
        {
            if (group == null || group.renderer == null || group.morning == null || group.evening == null)
                continue;

            // Only this renderer's colour changes; the shared material assets stay as authored.
            _properties.Clear();
            _properties.SetColor(Tint, Color.Lerp(group.morning.GetColor(Tint), group.evening.GetColor(Tint), blend));
            group.renderer.SetPropertyBlock(_properties);
        }
    }
}
