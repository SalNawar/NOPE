using UnityEngine;

/// <summary>
/// A decoration prop's id. Decoration hook (item 7): Build Office UI writes it
/// on each prop; nothing reads it at run time yet (the decoration piece will).
/// No public members until then.
/// </summary>
public sealed class DeskItem : MonoBehaviour
{
    /// <summary>The item's id ("stamp", "mug", "plant", "poster").</summary>
    [SerializeField] private string itemId;
}
