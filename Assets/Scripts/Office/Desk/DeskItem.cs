using UnityEngine;

/// <summary>
/// A decoration prop's id. Decoration hook (item 7): read by the builder now,
/// by the decoration piece later. No public members until then.
/// </summary>
public sealed class DeskItem : MonoBehaviour
{
    /// <summary>The item's id ("stamp", "mug", "plant", "poster").</summary>
    [SerializeField] private string itemId;
}
