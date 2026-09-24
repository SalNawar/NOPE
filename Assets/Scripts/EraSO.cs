// ReSharper disable InconsistentNaming
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
}
