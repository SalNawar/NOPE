// ReSharper disable InconsistentNaming
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An era (Ancient, Medieval, Early modern, Industrial, Modern, Future).
/// Each country's moment within an era is a NationEraProfileSO.
/// </summary>
[CreateAssetMenu(fileName = "Era_", menuName = "TimeDesk/Era", order = 10)]
public sealed class EraSO : ScriptableObject
{
    /// <summary>Stable ID used for save/load and lookups (e.g., "ancient").</summary>
    public string id;

    /// <summary>Display name shown in UI (e.g., "Ancient").</summary>
    public string displayName;

    /// <summary>Chronological position (0 = oldest); orders places in the books.</summary>
    public int order;

    /// <summary>
    /// The office's own time: at most one era; its places come and go with
    /// history (one a day: the timeline leader's). Written by Generate World
    /// from eras[].future.
    /// </summary>
    public bool isFuture;

    /// <summary>The main era this one is a second moment of (eras[].group; null: a main era): a nation's second place of that era (EraGroups; Track E2).</summary>
    public EraSO group;

    /// <summary>The era's group id: its group's, or its own (EraGroups.GroupOf).</summary>
    public string GroupId => EraGroups.GroupOf(id, group != null ? group.id : null);

    /// <summary>Small-talk lines of travellers claiming this era (used when their place has none).</summary>
    public List<LineText> smallTalk = new();
}
