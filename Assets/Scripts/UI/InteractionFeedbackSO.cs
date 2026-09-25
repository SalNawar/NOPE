using UnityEngine;

/// <summary>
/// Look of the interaction feedback: the game cursor (arrow, and a hand over
/// anything clickable) and the hover outline on booth objects and desktop UI.
/// </summary>
[CreateAssetMenu(fileName = "InteractionFeedback_", menuName = "TimeDesk/UI/Interaction Feedback", order = 10)]
public sealed class InteractionFeedbackSO : ScriptableObject
{
    [Header("Cursor")]
    /// <summary>Default cursor (Texture Type: Cursor). Null keeps the OS cursor.</summary>
    public Texture2D arrowCursor;

    /// <summary>Arrow click point, in pixels from the texture's top-left.</summary>
    public Vector2 arrowHotspot = Vector2.zero;

    /// <summary>Cursor over anything clickable (Texture Type: Cursor).</summary>
    public Texture2D handCursor;

    /// <summary>Hand click point (the fingertip), in pixels from the top-left.</summary>
    public Vector2 handHotspot = new Vector2(12f, 1f);

    [Header("Hover outline")]
    /// <summary>Outline colour for booth objects (world sprites).</summary>
    public Color outlineColor = Color.white;

    /// <summary>Booth outline thickness in world units.</summary>
    [Min(0.005f)]
    public float worldOutlineWidth = 0.05f;

    /// <summary>
    /// Outline colour for desktop UI that carries no theme tag (the Title and
    /// Home scenes); themed UI uses its theme's two rings (piece 6 R7).
    /// </summary>
    public Color uiOutlineColor = new Color(0.98f, 0.72f, 0.2f, 1f);

    /// <summary>UI outline offset in pixels (uGUI Outline effect distance).</summary>
    public Vector2 uiOutlineDistance = new Vector2(2f, -2f);

    /// <summary>Unlit sprite material for booth outlines, so lighting never dims the highlight.</summary>
    public Material outlineMaterial;
}
