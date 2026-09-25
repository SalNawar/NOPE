using UnityEngine;

/// <summary>
/// One desk prop's click reaction (DeskReaction): its animation, an optional
/// sound and an optional tooltip with its placement and duration. The builder
/// creates the booth's reactions under Assets/Data/Config/DeskReactions and
/// keeps a designer's edits.
/// </summary>
[CreateAssetMenu(fileName = "Reaction_", menuName = "TimeDesk/Office/Desk Reaction")]
public sealed class DeskReactionSO : ScriptableObject
{
    /// <summary>The animation (None: tooltip only).</summary>
    public ReactionKind kind;

    /// <summary>Seconds the animation takes.</summary>
    [Min(0.05f)] public float seconds = 0.35f;

    /// <summary>How strong the animation is (scale fraction, offset in local units, 30 degrees per unit of wobble).</summary>
    public float amplitude = 0.12f;

    /// <summary>Optional: played through the prop's AudioSource (the project has no clips yet).</summary>
    public AudioClip clip;

    /// <summary>The tooltip: a UI string key whose {0} is the prop's readout text and {1} the wallet's word (world_source.json ui.strings; empty = no tooltip).</summary>
    public string tooltipKey;

    /// <summary>Seconds the tooltip stays up.</summary>
    [Min(0.1f)] public float tooltipSeconds = 2.5f;

    /// <summary>Where the tooltip sits from the prop (overlay reference px; up is positive).</summary>
    public Vector2 tooltipOffset = new Vector2(0f, 60f);
}
