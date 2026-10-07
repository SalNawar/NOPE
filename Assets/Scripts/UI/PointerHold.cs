using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The left button going down on an office object and coming back up (the
/// desk machine spec §1: a dater "holding the button holds the stamp down";
/// the gate lever): Down and Up, through the office camera's
/// PhysicsRaycaster like Clickable's click. Up comes to the object the press
/// went down on, wherever the pointer is then. Only the left button
/// (Papers, Please's one button; a right-click backs out).
/// </summary>
public sealed class PointerHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    /// <summary>Raised when the left button goes down on the object, with the pointer's screen position.</summary>
    public event Action<Vector2> Down;

    /// <summary>Raised when that press comes back up, with the pointer's screen position.</summary>
    public event Action<Vector2> Up;

    /// <summary>True while the left button is held after going down on the object.</summary>
    public bool Held { get; private set; }

    /// <summary>The left button went down on the object.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        Held = true;
        Down?.Invoke(eventData.position);
    }

    /// <summary>That press came back up.</summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !Held)
            return;
        Held = false;
        Up?.Invoke(eventData.position);
    }

    /// <summary>A disabled object gets no release: the hold ends here (and says so).</summary>
    private void OnDisable()
    {
        if (!Held)
            return;
        Held = false;
        Up?.Invoke(Vector2.zero);
    }
}
