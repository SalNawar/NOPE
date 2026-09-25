using TMPro;
using UnityEngine;

/// <summary>
/// Drives one TMP text through DisplayText (piece 9 T8, R6, R8): writes its
/// plain, untranslated or flipping form; while any foreign or scramble cell
/// shows, the text uses the script's runtime font (whose fallback is the
/// runtime LiberationSans, so flipped English letters draw too), and once it
/// settles its own font and material come back, so the tracked TMP assets
/// never receive a foreign glyph. A text that shows foreign cells may shrink
/// to fit (UiText.FitLabel, keeping its wrapping): a script's glyphs can be
/// far wider than the English. A flipping text is rebuilt only when
/// DisplayText.Progress changes, never per frame.
/// </summary>
public sealed class TextFlip
{
    private TMP_Text _text;
    private TMP_FontAsset _ownFont;
    private Material _ownMaterial;
    private string _canonical;
    private Reveal _reveal;
    private CaseTranslation _tr = CaseTranslation.None;
    private int _progress;

    /// <summary>True while a flip is still changing the text.</summary>
    public bool Running { get; private set; }

    /// <summary>
    /// Shows <paramref name="canonical"/> in <paramref name="text"/> as
    /// <paramref name="reveal"/> says; the text's own font and material are
    /// remembered the first time a text is shown.
    /// </summary>
    public void Show(TMP_Text text, string canonical, Reveal reveal, CaseTranslation tr)
    {
        if (text == null)
            return;
        if (text != _text)
        {
            _text = text;
            _ownFont = text.font;
            _ownMaterial = text.fontSharedMaterial;
        }

        _canonical = canonical;
        _reveal = reveal;
        _tr = tr ?? CaseTranslation.None;
        _progress = DisplayText.Progress(_canonical, reveal, _tr.Timing, _tr.ReducedMotion);
        Apply(reveal);
        Running = reveal.Kind == RevealKind.Flipping && DisplayText.Remaining(_canonical, reveal, _tr.Timing, _tr.ReducedMotion) > 0f;
    }

    /// <summary>A running flip <paramref name="elapsed"/> seconds after its reveal: recomposes only when a letter changes; false once it has settled (its own font back).</summary>
    public bool Tick(float elapsed)
    {
        if (!Running || _text == null)
            return false;

        Reveal now = Reveal.Flipping(_reveal.Foreign, elapsed, _reveal.Row);
        int progress = DisplayText.Progress(_canonical, now, _tr.Timing, _tr.ReducedMotion);
        bool settled = DisplayText.Remaining(_canonical, now, _tr.Timing, _tr.ReducedMotion) <= 0f;
        if (progress != _progress || settled)
        {
            _progress = progress;
            Apply(now);
        }
        Running = !settled;
        return Running;
    }

    /// <summary>The skip: a flipping text shows its English at once, in its own font; an untranslated or plain text is left as it is.</summary>
    public void Complete()
    {
        if (_text == null || _reveal.Kind != RevealKind.Flipping)
            return;
        Running = false;
        _reveal = Reveal.Plain;
        Apply(_reveal);
    }

    /// <summary>The text is being hidden: stops any flip and gives it back its own font.</summary>
    public void Release()
    {
        Running = false;
        _reveal = Reveal.Plain;
        if (_text != null)
            SetFont(_text, _ownFont, _ownMaterial, false, null);
    }

    /// <summary>
    /// Writes a text that never animates (a transcript row, a clone of its
    /// template): its DisplayText form, in the script's font while it shows
    /// foreign cells.
    /// </summary>
    public static void Write(TMP_Text text, string canonical, Reveal reveal, CaseTranslation tr)
    {
        if (text == null)
            return;
        tr = tr ?? CaseTranslation.None;
        text.text = DisplayText.For(canonical, reveal, tr.Timing, tr.ReducedMotion);
        if (!DisplayText.ShowsForeign(canonical, reveal, tr.Timing, tr.ReducedMotion))
            return;
        if (tr.Font != null)
            text.font = tr.Font;
        Fit(text);
    }

    /// <summary>Writes the reveal's form and picks the font for it.</summary>
    private void Apply(Reveal reveal)
    {
        bool foreign = DisplayText.ShowsForeign(_canonical, reveal, _tr.Timing, _tr.ReducedMotion);
        SetFont(_text, _ownFont, _ownMaterial, foreign, _tr.Font);
        if (foreign)
            Fit(_text);
        _text.text = DisplayText.For(_canonical, reveal, _tr.Timing, _tr.ReducedMotion);
    }

    /// <summary>A text that does not size itself shrinks to fit (it keeps its wrapping); one that already auto-sizes keeps its own range. Its English then shows at its size, since that fits.</summary>
    private static void Fit(TMP_Text text)
    {
        if (!text.enableAutoSizing)
            UiText.FitLabel(text, true);
    }

    /// <summary>The script's font while foreign cells show (when there is one), else the text's own font and material.</summary>
    private static void SetFont(TMP_Text text, TMP_FontAsset own, Material ownMaterial, bool foreign, TMP_FontAsset script)
    {
        TMP_FontAsset font = foreign && script != null ? script : own;
        if (font == null || text.font == font)
            return;
        text.font = font;
        if (font == own && ownMaterial != null)
            text.fontSharedMaterial = ownMaterial;
    }
}
