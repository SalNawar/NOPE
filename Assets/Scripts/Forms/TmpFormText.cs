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

    /// <summary>The least share of its size a printed text shrinks to so that its widest word fits its box (wave 5 A3: "TRANSPON / DER CLASS" never breaks mid-word).</summary>
    public const float WordFitFloor = 0.6f;

    /// <summary>Where a printed text's words part (a space, a line break).</summary>
    private static readonly char[] WordBreaks = { ' ', '\n' };

    /// <summary>
    /// The share of its size <paramref name="text"/> in <paramref name="role"/>'s
    /// style at <paramref name="size"/> is printed at so that its widest word
    /// fits <paramref name="width"/> (1 when it fits; never under
    /// WordFitFloor): a word wraps whole and never breaks in the middle, and
    /// the box keeps the layout's place and size (the document design spec,
    /// D2). Measured on <paramref name="measure"/> (styled by this call; in the
    /// units of <paramref name="size"/> and <paramref name="width"/>).
    /// </summary>
    public static float WordFit(TMP_Text measure, string text, FormTextRole role, float size, float width)
    {
        if (measure == null || string.IsNullOrEmpty(text) || width <= 0f)
            return 1f;
        Style(measure, role, size);
        float widest = 0f;
        foreach (string word in text.Split(WordBreaks, StringSplitOptions.RemoveEmptyEntries))
            widest = Mathf.Max(widest, measure.GetPreferredValues(word, float.PositiveInfinity, float.PositiveInfinity).x);
        return widest > width ? Mathf.Max(WordFitFloor, width / widest * 0.98f) : 1f;
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
        text.fontSize *= ArtFit(measure, item, font);
        text.enableAutoSizing = false;
        text.alignment = item.Align == FormTextAlign.Centre ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
        text.color = ArtInk(art, item, style);
    }

    /// <summary>True for a role printed in the art's label face (a label, a caption, an art caption's title or paragraph); a value or a hand takes the value face.</summary>
    private static bool IsArtLabel(FormTextRole role) => role != FormTextRole.Value && role != FormTextRole.Hand;

    /// <summary>The steps ArtFit tries between the floor and the full size.</summary>
    private const int FitSteps = 8;

    /// <summary>The largest share of <paramref name="item"/>'s size, from 1 down to ArtLayout.FitFloor (the floor when none fits), at which its words in <paramref name="font"/> (null: the measure's own), wrapped at its place's width, are no taller than its place, measured on <paramref name="measure"/> in the item's units (its font put back after; its style is the last tried).</summary>
    public static float ArtFit(TMP_Text measure, FormItem item, TMP_FontAsset font)
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
        float fit = ArtLayout.FitFloor;
        for (int i = 0; i <= FitSteps; i++)
        {
            float share = 1f - (1f - ArtLayout.FitFloor) * i / FitSteps;
            Style(measure, item.Role, item.Size * share);
            Vector2 need = measure.GetPreferredValues(item.Text, item.Rect.Width, float.PositiveInfinity);
            if (need.y <= item.Rect.Height * 1.02f && need.x <= item.Rect.Width * 1.02f)
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
