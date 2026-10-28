using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The painting's red book in its pocket as the rulebook folder's handle
/// (Saleh's playtest of 2026-10-28, point 5: "isnt the big red binder for the
/// folder?"; DeskRulebook.Pocket.cs): a left drag that begins on the book
/// takes the folder out under the pointer (DeskRulebook.TakeOutUnder) and
/// hands the drag to the folder's own (DeskDraggable, made the event's drag
/// target), so the folder follows the pointer, drops and rests as it does on
/// the desk, one hold. A click is the book's Clickable's (DeskRulebook.
/// TakeOut). The office binder puts it on the book's click box.
/// </summary>
public sealed class PocketBook : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The folder the book stands for.</summary>
    private DeskRulebook _rulebook;

    /// <summary>The book's handle on <paramref name="host"/> (its click box) for <paramref name="rulebook"/>, added once.</summary>
    public static PocketBook Bind(GameObject host, DeskRulebook rulebook)
    {
        if (host == null)
            return null;
        if (!host.TryGetComponent(out PocketBook book))
            book = host.AddComponent<PocketBook>();
        book._rulebook = rulebook;
        return book;
    }

    /// <summary>A left drag from the book: the folder comes out under the pointer and the drag becomes the folder's (its moves and its release go to it from here on).</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _rulebook == null || _rulebook.Drag == null || !_rulebook.TakeOutUnder())
            return;
        GameObject folder = _rulebook.Drag.gameObject;
        eventData.pointerDrag = folder;
        ExecuteEvents.Execute(folder, eventData, ExecuteEvents.beginDragHandler);
    }

    /// <summary>Nothing: the folder's drag takes the moves once it began (the book must take drags to be the press's drag target).</summary>
    public void OnDrag(PointerEventData eventData) { }

    /// <summary>Nothing: the folder's drag takes the release.</summary>
    public void OnEndDrag(PointerEventData eventData) { }
}
