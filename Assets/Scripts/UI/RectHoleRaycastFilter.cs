using UnityEngine;

/// <summary>
/// Lets clicks through a hole in an overlay graphic: a point inside the hole's
/// rectangle is not a hit on this graphic. The PC frame's bezel and its
/// click-outside-to-close catcher both leave the glass open, so clicks there
/// reach the desktop behind (through the frame camera), and a click on a dark
/// screen closes nothing.
/// </summary>
public sealed class RectHoleRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    /// <summary>The hole (on the same overlay canvas).</summary>
    [SerializeField] private RectTransform hole;

    /// <summary>A hit anywhere but inside the hole.</summary>
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) =>
        hole == null || !RectTransformUtility.RectangleContainsScreenPoint(hole, screenPoint, eventCamera);
}
