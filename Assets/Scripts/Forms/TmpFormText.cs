using System;
using TMPro;
using UnityEngine;

/// <summary>
/// A form's words in TextMeshPro (redesign phase 4): the one place a text role
/// becomes a type style (bold, small capitals, a fixed size, wrapping) and the
/// measure FormLayout lays out with (its ITextMeasure: the text's preferred
/// height). Sizes are in the text's own units: metres on a world-space
/// TextMeshPro (an em is 0.1 x fontSize there), canvas units on a UGUI one.
/// The desk paper prints and measures with it, and Build Office UI checks the
/// forms with it, so a check measures what the paper prints. A text its
/// ScriptOf gives a font (a transcript line in its tongue's script, on the
/// PC) is measured and printed in that font (SetFont), every other in the
/// measure's own: a foreign line is never measured in a font without its
/// glyphs, so its box is as tall as it draws.
/// </summary>
public sealed class TmpFormText : ITextMeasure
{
    /// <summary>A world-space TextMeshPro's em per font-size unit, in metres.</summary>
    public const float WorldEmPerFontSize = 0.1f;

    /// <summary>The room a measured text may take downward (in the text's units; more than any page).</summary>
    private const float MeasureRoom = 100000f;

    private readonly TMP_Text _text;

    /// <summary>The measure's own font and its material (the text's when the measure was made): every text ScriptOf gives no font is in them.</summary>
    private readonly TMP_FontAsset _font;
    private readonly Material _material;

    /// <summary>A measure over <paramref name="text"/> (its style and font are changed by each measure; keep it hidden), in its font and material.</summary>
    public TmpFormText(TMP_Text text)
    {
        _text = text;
        _font = text.font;
        _material = text.fontSharedMaterial;
    }

    /// <summary>The font a text is written in when not the measure's own (a transcript line's script, from the page's owner); null gives none, and every text is in the own font.</summary>
    public Func<string, TMP_FontAsset> ScriptOf { get; set; }

    /// <summary>The height <paramref name="text"/> takes in <paramref name="role"/>'s style at <paramref name="size"/>, wrapped at <paramref name="width"/>, in the font it is printed in.</summary>
    public float Height(string text, FormTextRole role, float size, float width)
    {
        SetFont(_text, text);
        Style(_text, role, size);
        return _text.GetPreferredValues(text, width, MeasureRoom).y;
    }

    /// <summary>Puts <paramref name="target"/> in the font <paramref name="text"/> is printed in: ScriptOf's for it, else the measure's own font with its own material back (a pooled text that printed a foreign line gets the paper's ink again).</summary>
    public void SetFont(TMP_Text target, string text)
    {
        TMP_FontAsset script = ScriptOf?.Invoke(text);
        TMP_FontAsset font = script != null ? script : _font;
        if (target.font == font)
            return;
        target.font = font;
        if (script == null && _material != null)
            target.fontSharedMaterial = _material;
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
