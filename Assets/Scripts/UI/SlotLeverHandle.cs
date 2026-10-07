using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The Night Slots lever's grip (an invisible hit area over the ball, the
/// rod and the hub): passes the pointer's press, drag and release to the
/// machine (SlotMachineView), which pulls the lever. The drag starts at once
/// (no drag threshold), so the lever follows the hand from the first pixel.
/// </summary>
[DisallowMultipleComponent]
public sealed class SlotLeverHandle : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
{
    /// <summary>The pointer pressed on the lever.</summary>
    public event Action<PointerEventData> Pressed;

    /// <summary>The pointer moved while held on the lever.</summary>
    public event Action<PointerEventData> Dragged;

    /// <summary>The pointer let go of the lever.</summary>
    public event Action<PointerEventData> Released;

    /// <summary>Raises <see cref="Pressed"/>.</summary>
    public void OnPointerDown(PointerEventData eventData) => Pressed?.Invoke(eventData);

    /// <summary>No drag threshold: the lever moves with the first pixel of the drag.</summary>
    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

    /// <summary>Raises <see cref="Dragged"/>.</summary>
    public void OnDrag(PointerEventData eventData) => Dragged?.Invoke(eventData);

    /// <summary>Raises <see cref="Released"/>.</summary>
    public void OnPointerUp(PointerEventData eventData) => Released?.Invoke(eventData);
}
