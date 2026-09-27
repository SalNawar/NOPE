/// <summary>
/// One copied value (the PC redesign CP1, CP2): the text as shown, where it
/// came from (its entry key and label) and whose case it belongs to; for an
/// untranslated transcript line, its tongue and its canonical text. The
/// canonical text is for search's glyph-to-glyph match only (SE5): it is
/// never shown or pasted.
/// </summary>
public sealed class Clip
{
    private Clip(string text, string sourceKey, string sourceLabel, string traveller, bool foreign, string tongueId, string tongueName, string canonical)
    {
        Text = text ?? string.Empty;
        SourceKey = sourceKey ?? string.Empty;
        SourceLabel = sourceLabel ?? string.Empty;
        Traveller = traveller ?? string.Empty;
        Foreign = foreign;
        TongueId = tongueId;
        TongueName = tongueName;
        Canonical = canonical;
    }

    /// <summary>A plain clip: a value as shown (papers, records, books and translated lines are all plain).</summary>
    public static Clip Plain(string text, string sourceKey, string sourceLabel, string traveller) =>
        new Clip(text, sourceKey, sourceLabel, traveller, false, null, null, null);

    /// <summary>An untranslated transcript line as shown (its glyphs and English key words), with its tongue and its hidden canonical text.</summary>
    public static Clip Untranslated(string shown, string sourceKey, string sourceLabel, string traveller, string tongueId, string tongueName, string canonical) =>
        new Clip(shown, sourceKey, sourceLabel, traveller, true, tongueId, tongueName, canonical);

    /// <summary>The text as shown where it was copied (what a paste writes).</summary>
    public string Text { get; }

    /// <summary>The source's entry key (PickKeys or EntryKeys), or empty.</summary>
    public string SourceKey { get; }

    /// <summary>Where it came from ("Visa · Visa Class", "Transcript · line 7").</summary>
    public string SourceLabel { get; }

    /// <summary>The traveller whose case it was copied in (empty between travellers).</summary>
    public string Traveller { get; }

    /// <summary>True for an untranslated line: pasted into the search field it becomes a chip.</summary>
    public bool Foreign { get; }

    /// <summary>The untranslated line's tongue id (null for a plain clip).</summary>
    public string TongueId { get; }

    /// <summary>The untranslated line's tongue as named ("Greek"; null for a plain clip).</summary>
    public string TongueName { get; }

    /// <summary>The untranslated line's canonical text, for search's match only (null for a plain clip); never shown or pasted.</summary>
    public string Canonical { get; }

    /// <summary>The clip as a Notes clipping (its text, source and traveller; never the canonical text).</summary>
    public Clipping ToClipping() =>
        new Clipping { text = Text, label = SourceLabel, sourceKey = SourceKey, traveller = Traveller, foreign = Foreign };
}

/// <summary>
/// The desktop's one clipboard (the PC redesign CP2): it holds the last clip
/// copied (a blank one is not kept). The clip's text also goes to the system
/// clipboard (the engine side does that), so text fields paste it the normal
/// way; IsCurrent tells a paste target that the text it got is this clip, so
/// it can keep the clip's source (a Notes clipping) or show a foreign clip as
/// a chip (the search field). Pure.
/// </summary>
public sealed class AppClipboard
{
    /// <summary>The last clip copied, or null.</summary>
    public Clip Current { get; private set; }

    /// <summary>Keeps <paramref name="clip"/> as the current clip (a null or blank clip changes nothing).</summary>
    public void Copy(Clip clip)
    {
        if (clip != null && !string.IsNullOrWhiteSpace(clip.Text))
            Current = clip;
    }

    /// <summary>True when <paramref name="pasted"/> is exactly the current clip's text (the paste came from this clip).</summary>
    public bool IsCurrent(string pasted) => Current != null && pasted != null && pasted == Current.Text;
}
