using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A source's entry in the Investigation app's navigator as the player moves
/// it (the PC redesign AP4; the PC UX redesign IA11): dragged up or down the
/// list it moves past each neighbour whose middle the pointer passes
/// (AppNav.DragEntry: the app reorders the sources and saves the order), and
/// a right-click opens its menu (Move up, Move down, Reset order). A plain
/// click still shows the source (its Button). On each entry, wired by the
/// builder.
/// </summary>
public sealed class AppTabHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    /// <summary>The navigator whose list holds the entry.</summary>
    [SerializeField] private AppNav nav;

    /// <summary>The source.</summary>
    [SerializeField] private AppTab tab;

    /// <summary>The drag begins (past the EventSystem's threshold).</summary>
    public void OnBeginDrag(PointerEventData eventData) => Drag(eventData);

    /// <summary>The entry follows the pointer along the list.</summary>
    public void OnDrag(PointerEventData eventData) => Drag(eventData);

    /// <summary>The drag ends where the entry now is.</summary>
    public void OnEndDrag(PointerEventData eventData) => Drag(eventData);

    /// <summary>A right-click opens the source's menu.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && nav != null)
            nav.RequestMenu(tab, eventData);
    }

    private void Drag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && nav != null)
            nav.DragEntry(tab, eventData);
    }
}
