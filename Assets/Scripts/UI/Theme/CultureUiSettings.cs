using System;
using UnityEngine;

/// <summary>
/// The culture UI knobs the content library holds (piece 6), written by
/// Generate World from world_source.json ui: the reading language, the gloss
/// size, the shrink-to-fit floor, the contrast minimums and the Latin
/// fallback font every runtime font chain ends in; and how fast a culture's
/// font is prewarmed.
/// </summary>
[Serializable]
public sealed class CultureUiSettings
{
    /// <summary>The language every key has (the English table).</summary>
    public string readingLanguage = "en";

    /// <summary>Size of a gloss line, in percent of the label's.</summary>
    [Range(30, 100)] public int glossPercent = 60;

    /// <summary>A shrink-to-fit text never goes below this fraction of its size.</summary>
    [Range(0.3f, 1f)] public float labelMinScale = 0.55f;

    /// <summary>The contrast minimums every theme passes.</summary>
    public ContrastRules contrast = new ContrastRules();

    /// <summary>The project's LiberationSans.ttf (its import includes the font data): the runtime Latin fallback and Greece's font.</summary>
    public Font latinFallbackFont;

    /// <summary>How many of a culture's glyphs the papers' font bakes a frame while it is prewarmed (RuntimeFonts.Prewarm, CODE-81: the first CJK culture of a session no longer bakes its ~700 glyphs in one frame). Not in world_source.json: its default holds until a designer sets it on the content library.</summary>
    [Range(1, 128)] public int prewarmGlyphsPerFrame = 12;
}
