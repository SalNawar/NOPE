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

    /// <summary>
    /// The share of its size <paramref name="text"/> in <paramref name="role"/>'s
    /// style at <paramref name="size"/> is printed at so that its widest word
    /// fits <paramref name="width"/> (TextFit.WordShare: 1 when it fits; never
    /// under TextFit.Floor, BUG-9): a word wraps whole and never breaks in the
    /// middle (Unbreak), and the box keeps the layout's place and size (the
    /// document design spec, D2). Measured on <paramref name="measure"/>
    /// (styled by this call; in the units of <paramref name="size"/> and
    /// <paramref name="width"/>).
    /// </summary>
    public static float WordFit(TMP_Text measure, string text, FormTextRole role, float size, float width)
    {
        if (measure == null || string.IsNullOrEmpty(text) || width <= 0f)
            return 1f;
        Style(measure, role, size);
        float widest = 0f;
        foreach (string word in TextFit.Words(text))
            widest = Mathf.Max(widest, Width(measure, word));
        return TextFit.WordShare(widest, width);
    }

    /// <summary>
    /// Lays a printed text out so that no word breaks across lines (BUG-9):
    /// its widest word wider than <paramref name="width"/> (the box's, in the
    /// text's units) shrinks the text just enough (TextFit.WordShare, never
    /// under TextFit.WordLeast of its size now; only with
    /// <paramref name="shrink"/>: a text whose size its owner sets again
    /// after, like the rulebook's rows, is only cut), and a word still too
    /// wide is cut with a full stop (TextFit.Unbroken; the validator keeps
    /// generated values clear of it); a logical Arabic text is broken into
    /// lines on its logical text, each line then put into visual order by the
    /// per-line API (ArabicShaper.ToVisualLines, BUG-37c), and set from the
    /// right.
    /// </summary>
    public static void Unbreak(TMP_Text text, float width, bool shrink = true)
    {
        if (text == null || string.IsNullOrEmpty(text.text) || width <= 0f)
            return;
        Func<string, float> measure = s => Width(text, s);
        float widest = 0f;
        foreach (string word in TextFit.Words(text.text))
            widest = Mathf.Max(widest, measure(word));
        float share = shrink ? TextFit.WordShare(widest, width) : 1f;
        if (share < 1f)
            text.fontSize *= share;
        string laid = TextFit.Unbroken(text.text, width, 1f, measure);
        if (TextFit.IsLogicalArabic(laid))
        {
            laid = string.Join("\n", ArabicShaper.ToVisualLines(TextFit.WrapWords(laid, width, measure)));
            text.horizontalAlignment = HorizontalAlignmentOptions.Right;
        }
        if (laid != text.text)
            text.text = laid;
    }

    /// <summary>A single line's width in <paramref name="text"/>'s style now.</summary>
    private static float Width(TMP_Text text, string line) => text.GetPreferredValues(line, float.PositiveInfinity, float.PositiveInfinity).x;

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

    /// <summary>
    /// Puts <paramref name="text"/> (styled for <paramref name="item"/>'s role
    /// by Style) in the look of a document drawn on its art
    /// (<paramref name="art"/>; the Canva documents): a value or a hand in
    /// <paramref name="style"/>'s art value font, a printed label or caption in
    /// its art label font (each in the font's own material), in the art's inks
    /// (ArtInk), centred down its place (at its left, or across it for a centred
    /// item), and at the largest share of the size it was styled at, down
    /// to ArtLayout.FitFloor, at which it fits its place, words wrapping onto
    /// a second line before it shrinks further (ArtFit, measured on
    /// <paramref name="measure"/> at the layout's units: a world-space text a
    /// few millimetres tall measures taller than it draws). The desk paper and
    /// the PC's copy print every text on the art through it.
    /// </summary>
    public static void OnArt(TMP_Text text, FormItem item, FormStyleSO style, FormArt art, TMP_Text measure)
    {
        bool label = IsArtLabel(item.Role);
        TMP_FontAsset culture = label && CultureThemeService.Instance != null ? CultureThemeService.Instance.CultureFont : null;
        TMP_FontAsset font = culture != null ? culture : style == null ? null : label ? style.artLabelFont : style.artValueFont;
        if (font != null && text.font != font)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }
        text.fontSize *= ArtFit(measure, item, font, ArtLayout.FitFloorOf(art, item.Role));
        text.enableAutoSizing = false;
        text.alignment = item.Align == FormTextAlign.Centre ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
        text.color = ArtInk(art, item, style);
    }

    /// <summary>True for a role printed in the art's label face (a label, a caption, an art caption's title or paragraph); a value or a hand takes the value face.</summary>
    private static bool IsArtLabel(FormTextRole role) => role != FormTextRole.Value && role != FormTextRole.Hand;

    /// <summary>The steps ArtFit tries between the floor and the full size.</summary>
    private const int FitSteps = 8;

    /// <summary>The largest share of <paramref name="item"/>'s size, from 1 down to <paramref name="least"/> (ArtLayout.FitFloorOf; the least when none fits), at which its words in <paramref name="font"/> (null: the measure's own), wrapped at its place's width, are no taller than its place and none of them is wider than it (a word TextMeshPro would break mid-word fits nothing: Unbreak would cut it; ArtLayout.CutWords keeps the content's words clear of that at the least size), measured on <paramref name="measure"/> in the item's units (its font put back after; its style is the last tried).</summary>
    public static float ArtFit(TMP_Text measure, FormItem item, TMP_FontAsset font, float least)
    {
        if (measure == null || string.IsNullOrEmpty(item.Text))
            return 1f;
        TMP_FontAsset own = measure.font;
        Material ownMaterial = measure.fontSharedMaterial;
        if (font != null && own != font)
        {
            measure.font = font;
            measure.fontSharedMaterial = font.material;
        }
        Style(measure, item.Role, item.Size);
        float widest = 0f;
        foreach (string word in TextFit.Words(item.Text))
            widest = Mathf.Max(widest, Width(measure, word));
        float fit = least;
        for (int i = 0; i <= FitSteps; i++)
        {
            float share = 1f - (1f - least) * i / FitSteps;
            Style(measure, item.Role, item.Size * share);
            Vector2 need = measure.GetPreferredValues(item.Text, item.Rect.Width, float.PositiveInfinity);
            if (need.y <= item.Rect.Height * 1.02f && need.x <= item.Rect.Width * 1.02f && widest * share <= item.Rect.Width * TextFit.WidthSlack)
            {
                fit = share;
                break;
            }
        }
        if (measure.font != own)
        {
            measure.font = own;
            measure.fontSharedMaterial = ownMaterial;
        }
        return fit;
    }

    /// <summary>The ink <paramref name="item"/> prints in on <paramref name="art"/>: its own (an art caption's), else a caption in its stamp ink (the visa box's), a label or a caption without one in its label ink, any other in its value ink, each the style's (FormStyleSO.Ink) when the art names none.</summary>
    public static Color ArtInk(FormArt art, FormItem item, FormStyleSO style)
    {
        FormTextRole role = item.Role;
        bool label = IsArtLabel(role);
        string hex = !string.IsNullOrEmpty(item.Ink) ? item.Ink : art == null ? null : role == FormTextRole.Caption && !string.IsNullOrEmpty(art.stampInk) ? art.stampInk : label ? art.labelInk : art.ink;
        if (Rgba.TryParseHex(hex, out Rgba ink))
            return new Color(ink.R, ink.G, ink.B, 1f);
        return style != null ? style.Ink(role) : Color.black;
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
        if (FormTextStyles.IsItalic(role))
            styles |= FontStyles.Italic;
        text.fontStyle = styles;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
    }
}
