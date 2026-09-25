using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Where a compare pick lights up (piece 10 X20): a PC row's background, a
/// desk paper's row, the speech bubble. CompareController shows it in the
/// theme's highlight colour while its side is picked and hides it when the
/// comparison clears. Each implementation restores its own colour and is
/// null-safe for a graphic destroyed meanwhile (a page flipped, a paper gone
/// at the decision).
/// </summary>
public interface ICompareHighlight
{
    /// <summary>Lights the pick in <paramref name="colour"/> (<paramref name="picked"/>), or restores it.</summary>
    void Show(bool picked, Color colour);
}

/// <summary>A uGUI row's background as a compare highlight (the PC's rows): tinted while picked, its own colour restored after.</summary>
public sealed class ImageHighlight : ICompareHighlight
{
    private readonly Image _image;
    private Color _original;
    private bool _shown;

    /// <summary>A highlight on <paramref name="image"/> (null: shows nothing).</summary>
    public ImageHighlight(Image image) => _image = image;

    /// <inheritdoc />
    public void Show(bool picked, Color colour)
    {
        if (_image == null)
            return;

        if (picked)
        {
            if (!_shown)
                _original = _image.color;
            _shown = true;
            _image.color = colour;
        }
        else if (_shown)
        {
            _shown = false;
            _image.color = _original;
        }
    }
}
