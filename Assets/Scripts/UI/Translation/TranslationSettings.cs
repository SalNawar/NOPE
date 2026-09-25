using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A script tongues are drawn in: its direction and its OS font chain (piece 6's FontCandidate; empty = the runtime LiberationSans).</summary>
[Serializable]
public sealed class TranslationScript
{
    /// <summary>Stable id ("hieroglyphs"); tongues name it.</summary>
    public string id;

    /// <summary>True for a right-to-left script (shaped by ArabicShaper; its cells must be shapeable).</summary>
    public bool rightToLeft;

    /// <summary>OS fonts tried in order (RuntimeFonts); the runtime LiberationSans is always the last resort.</summary>
    public List<FontCandidate> fonts = new List<FontCandidate>();
}

/// <summary>
/// Everything translation needs at runtime (piece 9), written by Tools &gt;
/// TimeDesk &gt; Generate World from world_source.json "translation" into the
/// content library: the rules the day reads, the scripts, the flip's knobs and
/// the fallback cipher for a tongue no installed font can draw.
/// </summary>
[Serializable]
public sealed class TranslationSettings
{
    /// <summary>fromDay, the tongues and the packs (TranslationDay reads them).</summary>
    public TranslationRules rules = new TranslationRules();

    /// <summary>Every script a tongue may name.</summary>
    public List<TranslationScript> scripts = new List<TranslationScript>();

    /// <summary>The letter flip's knobs.</summary>
    public FlipTiming flip = new FlipTiming();

    /// <summary>26 distinct lower-case ASCII letters: the table of a tongue whose script no installed font draws.</summary>
    public string fallbackGlyphs;

    /// <summary>True when Generate World has written tongues.</summary>
    public bool HasData => rules?.tongues != null && rules.tongues.Count > 0;

    /// <summary>The script with this id, or null.</summary>
    public TranslationScript GetScript(string id) =>
        scripts?.FirstOrDefault(s => s != null && !string.IsNullOrEmpty(id) && s.id == id);

    /// <summary>
    /// The content problems the generator and the validator share: the rules
    /// (Translation.Problems, with <paramref name="placeTongues"/> as place id
    /// and tongue id), blank or duplicate script ids, a font candidate naming
    /// neither a file nor a family, every foreign tongue's table
    /// (Pseudoscript.ParseTable; shapeable cells for a right-to-left script),
    /// the fallback cipher (Pseudoscript.IsAsciiLetters) and negative flip knobs.
    /// </summary>
    public List<string> Problems(IEnumerable<KeyValuePair<string, string>> placeTongues)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>();
        foreach (TranslationScript s in scripts ?? new List<TranslationScript>())
        {
            if (s == null || string.IsNullOrWhiteSpace(s.id) || !ids.Add(s.id))
            {
                problems.Add($"translation.scripts: script '{s?.id}' has a blank or repeated id.");
                continue;
            }
            if ((s.fonts ?? new List<FontCandidate>()).Any(f => f == null || string.IsNullOrWhiteSpace(f.file) && string.IsNullOrWhiteSpace(f.family)))
                problems.Add($"translation.scripts: script '{s.id}' has a font candidate that names neither a file nor a family.");
        }

        problems.AddRange(Translation.Problems(rules, ids, placeTongues));

        foreach (Tongue t in rules?.tongues ?? new List<Tongue>())
        {
            if (t == null || t.Native || string.IsNullOrWhiteSpace(t.glyphs))
                continue;
            if (Pseudoscript.ParseTable(t.glyphs, out string problem) == null)
                problems.Add($"translation.tongues: tongue '{t.id}': {problem}.");
            else if (GetScript(t.script)?.rightToLeft == true && !ArabicShaper.CanShape(t.glyphs, out string unsupported))
                problems.Add($"translation.tongues: tongue '{t.id}' is right to left but holds characters the Arabic shaper cannot shape ({unsupported}).");
        }

        IReadOnlyList<string> fallback = Pseudoscript.ParseTable(fallbackGlyphs, out string fallbackProblem);
        if (!Pseudoscript.IsAsciiLetters(fallback))
            problems.Add($"translation.fallbackGlyphs must be 26 distinct lower-case ASCII letters ({fallbackProblem ?? "it holds other characters"}).");

        FlipTiming f = flip ?? new FlipTiming();
        if (f.startDelay < 0f || f.letterInterval < 0f || f.letterSeconds < 0f || f.scrambleSteps < 0 || f.rowStagger < 0f)
            problems.Add("translation.flip: every knob must be at least 0.");
        return problems;
    }
}
