using System;
using System.Collections.Generic;

/// <summary>How far one hover of the Translation Lens reads (never serialized): nothing, the word under the pointer, its sentence, or the whole object.</summary>
public enum LensReach
{
    /// <summary>No lens: foreign words stay as they are.</summary>
    None,

    /// <summary>Level 1: the hovered word.</summary>
    Word,

    /// <summary>Level 2: the sentence (the label) the hovered word is in.</summary>
    Sentence,

    /// <summary>Level 3: every foreign word of the hovered object (a window, a page, the paper).</summary>
    Object
}

/// <summary>One word of a hovered object: its phrase (in the object's order) and the word's rank in that phrase.</summary>
public readonly struct LensWordRef : IEquatable<LensWordRef>
{
    /// <summary>A word of phrase <paramref name="phrase"/>.</summary>
    public LensWordRef(int phrase, int word)
    {
        Phrase = phrase;
        Word = word;
    }

    /// <summary>The phrase's index among the object's phrases.</summary>
    public int Phrase { get; }

    /// <summary>The word's rank among its phrase's words (0 first, in reading order).</summary>
    public int Word { get; }

    /// <inheritdoc />
    public bool Equals(LensWordRef other) => Phrase == other.Phrase && Word == other.Word;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is LensWordRef other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Phrase * 397 ^ Word;
}

/// <summary>
/// The lens's levels as the content library holds them (world_source.json
/// translation.lens.levels, through Generate World): the upgrade id of each
/// level, Word first. The first is issued by the Bureau on the lens's day
/// (Feature.Lens; never sold); the others are Orders upgrades.
/// </summary>
[Serializable]
public sealed class LensRules
{
    /// <summary>The upgrade ids of the levels in order: Word, Sentence, Object.</summary>
    public List<string> levelIds = new List<string>();
}

/// <summary>
/// The Translation Lens's rules (Saleh 2026-10-06: "lock changing language
/// after week 1 and only after the player unlocks a new ability which
/// translates any word the player hovers on ... The player can upgrade to
/// translate sentences and then entire objects"). The lock and the first
/// level come with the lens's introduction (Introductions, Feature.Lens: day
/// 8 of the ramp), so the ramp stays the one source of what arrives when;
/// the further levels are owned upgrades, each counting only on top of the
/// one before. Pure; tested headless (TranslationLensTests).
/// </summary>
public static class TranslationLens
{
    /// <summary>The number of levels a lens has (Word, Sentence, Object).</summary>
    public const int LevelCount = 3;

    /// <summary>True from the lens's first day on: the office's language follows the world's leading culture and Settings no longer switches it.</summary>
    public static bool LanguageLocked(int day, Introductions intro) => intro != null && intro.Has(day, Feature.Lens);

    /// <summary>The first day the language is locked (the lens's introduction), or 0 when no day introduces the lens (never locked).</summary>
    public static int LockDay(Introductions intro) => intro != null ? intro.FirstDay(Feature.Lens) : 0;

    /// <summary>
    /// How far a hover reads on <paramref name="day"/>: Word once the first
    /// level is owned or issued (the lens is introduced by that day), then
    /// one level more for each further level owned in order (a level owned
    /// without the one before it counts for nothing yet).
    /// </summary>
    public static LensReach Reach(int day, Introductions intro, IReadOnlyList<string> levelIds, Func<string, bool> owns)
    {
        if (levelIds == null || levelIds.Count == 0)
            return LensReach.None;

        bool issued = LanguageLocked(day, intro) || Owns(owns, levelIds[0]);
        if (!issued)
            return LensReach.None;

        int level = 1;
        while (level < levelIds.Count && level < LevelCount && Owns(owns, levelIds[level]))
            level++;
        return (LensReach)level;
    }

    /// <summary>
    /// Grants the lens up to <paramref name="level"/> (the debug cheats'
    /// hook): adds each level's id up to it to <paramref name="owned"/>
    /// (WorldState.unlockedUpgradeIds) unless already there. Returns how many
    /// ids it added. Granting None adds nothing; nothing is ever taken away.
    /// </summary>
    public static int Grant(LensReach level, ICollection<string> owned, IReadOnlyList<string> levelIds)
    {
        if (owned == null || levelIds == null)
            return 0;

        int added = 0;
        for (int i = 0; i < (int)level && i < levelIds.Count; i++)
        {
            string id = levelIds[i];
            if (string.IsNullOrWhiteSpace(id) || owned.Contains(id))
                continue;
            owned.Add(id);
            added++;
        }
        return added;
    }

    /// <summary>
    /// What a hover covers, in the order the words flip, into
    /// <paramref name="covered"/> (cleared first; no allocation): with Word
    /// the hovered word (nothing when the pointer is on a phrase but not on a
    /// word: a space, a digit); with Sentence every word of the hovered
    /// phrase; with Object every word of every phrase of the object, phrase by
    /// phrase, whenever the pointer is over the object at all.
    /// <paramref name="wordsPerPhrase"/> lists each phrase's word count;
    /// <paramref name="phrase"/> and <paramref name="word"/> are -1 for none.
    /// </summary>
    public static void Covered(LensReach reach, bool overObject, int phrase, int word, IReadOnlyList<int> wordsPerPhrase, List<LensWordRef> covered)
    {
        covered.Clear();
        int phrases = wordsPerPhrase?.Count ?? 0;
        switch (reach)
        {
            case LensReach.Word:
                if (phrase >= 0 && phrase < phrases && word >= 0 && word < wordsPerPhrase[phrase])
                    covered.Add(new LensWordRef(phrase, word));
                return;
            case LensReach.Sentence:
                if (phrase >= 0 && phrase < phrases)
                    AddPhrase(phrase, wordsPerPhrase[phrase], covered);
                return;
            case LensReach.Object:
                if (!overObject && (phrase < 0 || phrase >= phrases))
                    return;
                for (int p = 0; p < phrases; p++)
                    AddPhrase(p, wordsPerPhrase[p], covered);
                return;
        }
    }

    /// <summary>
    /// Content problems (Generate World and the validator): exactly
    /// <see cref="LevelCount"/> levels, each with a non-blank id listed once.
    /// </summary>
    public static List<string> Problems(LensRules rules)
    {
        var problems = new List<string>();
        List<string> ids = rules?.levelIds ?? new List<string>();
        if (ids.Count != LevelCount)
            problems.Add($"translation.lens.levels lists {ids.Count} levels; the lens has {LevelCount} (Word, Sentence, Object).");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
                problems.Add("translation.lens.levels: a level has a blank id.");
            else if (!seen.Add(id))
                problems.Add($"translation.lens.levels: the id '{id}' is listed twice.");
        }
        return problems;
    }

    /// <summary>True when <paramref name="owns"/> says the id is owned.</summary>
    private static bool Owns(Func<string, bool> owns, string id) => owns != null && !string.IsNullOrWhiteSpace(id) && owns(id);

    /// <summary>Every word of one phrase, in order.</summary>
    private static void AddPhrase(int phrase, int words, List<LensWordRef> covered)
    {
        for (int w = 0; w < words; w++)
            covered.Add(new LensWordRef(phrase, w));
    }
}
