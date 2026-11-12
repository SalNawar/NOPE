using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>A dock item's drag (the Lens screen): the ghost follows the pointer, and the drop goes to the dock's Dropped. A click that does not drag is the button's own.</summary>
public sealed class LensDockItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private LensDock _dock;
    private ShelfItem _item;

    /// <summary>Binds the item to its dock.</summary>
    public void Setup(LensDock dock, ShelfItem item)
    {
        _dock = dock;
        _item = item;
    }

    /// <inheritdoc />
    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left)
            return;
        e.eligibleForClick = false; // a drag that ends on this item is not a click
        _dock.ShowGhost(_item);
        _dock.ItemDragging(e.position, e.pressEventCamera);
    }

    /// <inheritdoc />
    public void OnDrag(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
            _dock.ItemDragging(e.position, e.pressEventCamera);
    }

    /// <inheritdoc />
    public void OnEndDrag(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
            _dock.ItemDropped(_item, e.position, e.pressEventCamera);
    }
}
