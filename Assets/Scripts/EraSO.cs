// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// An era/destination (e.g., Rome, Future, Medieval).
/// </summary>
[CreateAssetMenu(fileName = "Era_", menuName = "TimeDesk/Era", order = 10)]
public sealed class EraSO : ScriptableObject
{
    /// <summary>Stable ID used for save/load and lookups (e.g., "rome").</summary>
    public string id;

    /// <summary>Display name shown in UI (e.g., "Ancient Rome").</summary>
    public string displayName;
}
