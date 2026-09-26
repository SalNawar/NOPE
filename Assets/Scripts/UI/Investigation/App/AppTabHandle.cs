using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A tab of a pane's strip as the player moves it (the PC redesign AP4):
/// dragged along the strip it moves past each neighbour whose middle the
/// pointer passes (AppPane.DragTab: the app reorders both strips and saves
/// the order), and a right-click opens its menu (Move left, Move right, Reset
/// tab order). A plain click still shows the tab (its Button). On each tab
/// button, wired by the builder.
/// </summary>
public sealed class AppTabHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    /// <summary>The pane whose strip holds the tab.</summary>
    [SerializeField] private AppPane pane;

    /// <summary>The tab.</summary>
    [SerializeField] private AppTab tab;

    /// <summary>The drag begins (past the EventSystem's threshold).</summary>
    public void OnBeginDrag(PointerEventData eventData) => Drag(eventData);

    /// <summary>The tab follows the pointer along the strip.</summary>
    public void OnDrag(PointerEventData eventData) => Drag(eventData);

    /// <summary>The drag ends where the tab now is.</summary>
    public void OnEndDrag(PointerEventData eventData) => Drag(eventData);

    /// <summary>A right-click opens the tab's menu.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && pane != null)
            pane.RequestTabMenu(tab, eventData);
    }

    private void Drag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && pane != null)
            pane.DragTab(tab, eventData);
    }
}
