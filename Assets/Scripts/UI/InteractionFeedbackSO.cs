using UnityEngine;

/// <summary>
/// Look of the interaction feedback: the game cursor (arrow, and a hand over
/// anything clickable), the hover outline on office props and desktop UI, and
/// a paper's hover lift (Saleh's D15: papers lift Papers, Please-style, props
/// get a thin outline on the office camera).
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
    /// <summary>Outline colour for office props (the rim round their silhouette).</summary>
    public Color outlineColor = Color.white;

    /// <summary>
    /// The office props' outline width in screen pixels at 1080p (scaled by
    /// the screen's height): the same rim on every prop, near or far, big or
    /// small (BUG-10a; the review of the desk v3 capture: 3-4 px on BABEL
    /// against 1 px on the intercom).
    /// </summary>
    [Min(0.5f)]
    public float outlinePixels = 2f;

    /// <summary>The outline's material (TimeDesk/HoverHull: a stencil mask of the prop's silhouette, then its meshes pushed out by outlinePixels in screen space where the mask is not, unlit, never depth-tested against the desk: HoverHighlighter makes its mask and hull from it).</summary>
    public Material outlineMaterial;

    /// <summary>How far a hovered paper lifts, as a share of a held paper's shadow lift (D15; DeskPaperDepth.HoverLift): a paper gets this and the hand cursor, never an outline.</summary>
    [Range(0f, 1f)]
    public float paperHoverLift = 0.3f;

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
