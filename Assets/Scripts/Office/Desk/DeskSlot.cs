using UnityEngine;

/// <summary>What a desk slot may hold.</summary>
public enum DeskSlotKind
{
    /// <summary>A decoration's home spot (the plant, the mug, the photo).</summary>
    Decoration,

    /// <summary>A free spot for anything the player places.</summary>
    Free
}

/// <summary>
/// A named spot on the desk. Decoration hook (item 7): read by the builder now
/// (props stand at their slot), by the decoration piece later. No public
/// members until then.
/// </summary>
public sealed class DeskSlot : MonoBehaviour
{
    /// <summary>The slot's id ("plant", "mug", "photo", "free_1"...).</summary>
    [SerializeField] private string slotId;

    /// <summary>What the slot may hold.</summary>
    [SerializeField] private DeskSlotKind kind;
}
