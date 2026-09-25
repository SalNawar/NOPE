using UnityEngine;

/// <summary>
/// The PC frame (ReStory-style): clicking the PC opens a flat, front-facing
/// monitor over the left of the office, with the room still visible on the
/// right. The frame (bezel, close X, power button, exits) is overlay UI; the
/// desktop itself shows inside its glass through the frame camera, which
/// renders only the desktop's layer into the glass's screen rectangle and is
/// the desktop canvas's event camera, so clicks, dragging and text fields
/// work as on any canvas. OfficeViewController opens and closes it.
/// </summary>
public sealed class PcFrame : MonoBehaviour
{
    /// <summary>The frame's overlay objects (bezel, glass, close X, power button, exit catcher), shown while open.</summary>
    [SerializeField] private GameObject root;

    /// <summary>The glass rectangle on the overlay canvas: the frame camera draws the desktop exactly there (4:3, like the desktop).</summary>
    [SerializeField] private RectTransform glass;

    /// <summary>The camera that draws the desktop's layer into the glass (enabled only while open).</summary>
    [SerializeField] private Camera frameCamera;

    /// <summary>The glass's corners in screen pixels (reused; an overlay canvas's world space is the screen).</summary>
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>True while the frame is open.</summary>
    public bool IsOpen => root != null && root.activeSelf;

    /// <summary>The frame starts closed.</summary>
    private void Awake() => SetOpen(false);

    /// <summary>Opens the frame (fitting the frame camera to the glass) or closes it.</summary>
    public void SetOpen(bool open)
    {
        if (root != null)
            root.SetActive(open);
        if (frameCamera != null)
            frameCamera.enabled = open;
        if (open)
            Fit();
    }

    /// <summary>Only while open: follows the glass when the screen's size changes (no allocation).</summary>
    private void LateUpdate()
    {
        if (IsOpen)
            Fit();
    }

    /// <summary>Sets the frame camera's viewport to the glass's rectangle on the screen.</summary>
    private void Fit()
    {
        if (glass == null || frameCamera == null)
            return;
        glass.GetWorldCorners(_corners);
        (float x, float y, float w, float h) = ScreenMapping.Viewport(_corners[0].x, _corners[0].y,
            _corners[2].x - _corners[0].x, _corners[2].y - _corners[0].y, Screen.width, Screen.height);
        frameCamera.rect = new Rect(x, y, w, h);
    }
}
