using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One language's UI strings (piece 6): the reading language's table holds
/// every key with its gloss and tier; a culture language's table holds only
/// the flavour keys it translates. Written by Tools &gt; TimeDesk &gt; Generate
/// World from world_source.json ui.strings and ui.languages; never edited by hand.
/// </summary>
[CreateAssetMenu(fileName = "Strings_", menuName = "TimeDesk/UI/String Table", order = 21)]
public sealed class UiStringTableSO : ScriptableObject
{
    /// <summary>The language code ("en", "ar", "zh-Hans").</summary>
    public string language;

    /// <summary>Right-to-left text, shaped by UiStrings when shown.</summary>
    public bool rightToLeft;

    /// <summary>The entries, in authored order.</summary>
    public List<UiStringEntry> entries = new List<UiStringEntry>();
}
