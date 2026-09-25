using System;
using UnityEngine;

/// <summary>
/// The culture UI knobs the content library holds (piece 6), written by
/// Generate World from world_source.json ui: the reading language, the gloss
/// size, the shrink-to-fit floor, the contrast minimums and the Latin
/// fallback font every runtime font chain ends in.
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
}
