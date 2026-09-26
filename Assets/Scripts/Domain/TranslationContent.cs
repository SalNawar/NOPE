using System;
using System.Collections.Generic;

/// <summary>
/// A place's tongue (world_source.json translation.tongues[], piece 9): the
/// language a traveller claiming the place speaks, drawn in its script until
/// the player owns its pack's Speech translator (their papers are always
/// filled in English).
/// </summary>
[Serializable]
public sealed class Tongue
{
    /// <summary>Stable id ("arabic"); places name it (NationEraProfileSO.tongue).</summary>
    public string id;

    /// <summary>The name the compare bar's placeholder shows ("Arabic").</summary>
    public string displayName;

    /// <summary>The script it is drawn in: a translation.scripts[] id (direction and fonts).</summary>
    public string script;

    /// <summary>The translator pack that reads it: a translation.packs[] id; blank = native (the clerk reads it).</summary>
    public string pack;

    /// <summary>The 26-cell substitution table for a..z (Pseudoscript.ParseTable); blank for a native tongue.</summary>
    public string glyphs;

    /// <summary>True when the clerk reads it without a translator (no pack).</summary>
    public bool Native => string.IsNullOrWhiteSpace(pack);
}

/// <summary>A translator pack: one region, sold at Home as its Speech translator.</summary>
[Serializable]
public sealed class TranslatorPack
{
    /// <summary>Stable id ("near_east"); the upgrade ids are built from it (Translation.UpgradeId).</summary>
    public string id;

    /// <summary>The region's name ("Near East"), in the upgrade names and the compare bar's placeholder.</summary>
    public string displayName;
}

/// <summary>The translation rules the day reads (the content library's copy of world_source.json translation).</summary>
[Serializable]
public sealed class TranslationRules
{
    /// <summary>The first office day on which a foreign tongue's speech shows untranslated (at least 1).</summary>
    public int fromDay;

    /// <summary>Every tongue a place may name.</summary>
    public List<Tongue> tongues = new List<Tongue>();

    /// <summary>The translator packs, in shop order.</summary>
    public List<TranslatorPack> packs = new List<TranslatorPack>();
}
