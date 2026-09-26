/// <summary>
/// A drag on the desktop that Escape can cancel (the PC redesign section
/// 3.5, CancelDrag): a window's title bar (WindowDrag) or an icon
/// (DesktopIconView). The window manager holds the one under way.
/// </summary>
public interface IDesktopDrag
{
    /// <summary>Ends the drag with the dragged thing back where it started.</summary>
    void CancelDrag();
}
