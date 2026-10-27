using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The pointer over an Orders card (one-click ordering, UI-25: "order straight
/// from the card (one click), with the detail on hover"): tells the Orders
/// window, which shows the card in its detail card while the pointer is over
/// it (OrderPick). Added by OrdersWindow to each card it lays out.
/// </summary>
public sealed class OrderCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>Raised when the pointer comes over the card.</summary>
    public Action Entered;

    /// <summary>Raised when the pointer leaves the card.</summary>
    public Action Exited;

    /// <inheritdoc />
    public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();

    /// <inheritdoc />
    public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();
}
