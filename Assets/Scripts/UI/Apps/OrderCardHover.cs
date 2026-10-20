using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The pointer over an Orders card (one-click ordering, UI-25: "order straight
/// from the card (one click), with the detail on hover"): tells the Orders
/// window, which shows the card in its detail card. Added by OrdersWindow to
/// each card it lays out.
/// </summary>
public sealed class OrderCardHover : MonoBehaviour, IPointerEnterHandler
{
    /// <summary>Raised when the pointer comes over the card.</summary>
    public Action Entered;

    /// <inheritdoc />
    public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();
}
