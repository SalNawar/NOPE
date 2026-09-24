// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// A nation tracked across time periods (England, Japan, Greece, ...).
/// Holds a global score; per-time-period state lives in NationEraProfileSO.
/// </summary>
[CreateAssetMenu(fileName = "Nation_", menuName = "TimeDesk/Timeline/Nation", order = 21)]
public sealed class NationSO : ScriptableObject
{
    /// <summary>Stable ID used in score keys and saves (e.g., "japan").</summary>
    public string id;

    /// <summary>Display name shown in UI/news.</summary>
    public string displayName;
}
