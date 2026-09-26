using TMPro;

/// <summary>
/// A form's words in TextMeshPro (redesign phase 4): the one place a text role
/// becomes a type style (bold, small capitals, a fixed size, wrapping) and the
/// measure FormLayout lays out with (its ITextMeasure: the text's preferred
/// height). Sizes are in the text's own units: metres on a world-space
/// TextMeshPro (an em is 0.1 x fontSize there), canvas units on a UGUI one.
/// The desk paper prints and measures with it, and Build Office UI checks the
/// forms with it, so a check measures what the paper prints.
/// </summary>
public sealed class TmpFormText : ITextMeasure
{
    /// <summary>A world-space TextMeshPro's em per font-size unit, in metres.</summary>
    public const float WorldEmPerFontSize = 0.1f;

    /// <summary>The room a measured text may take downward (in the text's units; more than any page).</summary>
    private const float MeasureRoom = 100000f;

    private readonly TMP_Text _text;

    /// <summary>A measure over <paramref name="text"/> (its style is changed by each measure; keep it hidden).</summary>
    public TmpFormText(TMP_Text text) => _text = text;

    /// <summary>The height <paramref name="text"/> takes in <paramref name="role"/>'s style at <paramref name="size"/>, wrapped at <paramref name="width"/>.</summary>
    public float Height(string text, FormTextRole role, float size, float width)
    {
        Style(_text, role, size);
        return _text.GetPreferredValues(text, width, MeasureRoom).y;
    }

    /// <summary>Sets <paramref name="text"/> to a role's type style at a size in its units: bold and small capitals by role, a fixed size, words wrapping, nothing cut.</summary>
    public static void Style(TMP_Text text, FormTextRole role, float size)
    {
        text.enableAutoSizing = false;
        text.fontSize = text is TextMeshPro ? size / WorldEmPerFontSize : size;
        FontStyles styles = FontStyles.Normal;
        if (FormTextStyles.IsBold(role))
            styles |= FontStyles.Bold;
        if (FormTextStyles.IsSmallCaps(role))
            styles |= FontStyles.SmallCaps;
        text.fontStyle = styles;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
    }
}
