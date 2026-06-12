// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// A key attribute of the timeline: a culture, technology, or societal trait
/// (Aristocracy, Industry, Anime, Nuclear, Robots, Hellenic Warfare, ...).
/// Attributes hold scores per nation-era profile; dominance tiers drive effects.
/// </summary>
[CreateAssetMenu(fileName = "Attr_", menuName = "TimeDesk/Timeline/Attribute", order = 20)]
public sealed class AttributeSO : ScriptableObject
{
    /// <summary>Stable ID used in score keys and saves (e.g., "robots").</summary>
    public string id;

    /// <summary>Display name shown in UI/news (e.g., "Robotics").</summary>
    public string displayName;

    /// <summary>Designer notes / flavor; can feed newsletter copy.</summary>
    [TextArea]
    public string description;
}
