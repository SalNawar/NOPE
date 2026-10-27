using UnityEngine;

/// <summary>
/// Look of the interaction feedback: the game cursor (arrow, and a hand over
/// anything clickable) and the hover outline on office objects and desktop UI.
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
    /// <summary>Outline colour for office objects (the 3D outline).</summary>
    public Color outlineColor = Color.white;

    /// <summary>The office objects' outline thickness in metres (how far the outline hull stands out of the mesh).</summary>
    [Min(0.0005f)]
    public float worldOutlineWidth = 0.006f;

    /// <summary>The outline hull's material (TimeDesk/HoverHull: the mesh's silhouette into the stencil, then the mesh again pushed out along its normals where it is not, unlit, never depth-tested against the desk: HoverHighlighter makes its mask and hull from it).</summary>
    public Material outlineMaterial;

    /// <summary>
    /// The office objects' outline as screen px at 1080p (scaled by the
    /// screen's height; 0 uses worldOutlineWidth): one rim for every object,
    /// near or far, big or small (the review of the desk v3 capture: 3-4 px
    /// on BABEL against 1 px on the intercom).
    /// </summary>
    [Min(0f)]
    public float outlinePx = 2f;

    /// <summary>How long the outline takes to fade in and to fade out (seconds; a cut under Reduced Motion): it never pops on or off.</summary>
    [Min(0f)]
    public float outlineFadeSeconds = 0.07f;

    /// <summary>
    /// Outline colour for desktop UI that carries no theme tag (the Title and
    /// Home scenes); themed UI uses its theme's two rings (piece 6 R7).
    /// </summary>
    public Color uiOutlineColor = new Color(0.98f, 0.72f, 0.2f, 1f);

    /// <summary>UI outline offset in pixels (uGUI Outline effect distance).</summary>
    public Vector2 uiOutlineDistance = new Vector2(2f, -2f);
}
