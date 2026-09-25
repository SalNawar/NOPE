using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// A transparent click area (the PC frame's surround: a click on the room
/// beside the frame closes it): a left click raises <see cref="onClick"/>. Not
/// a Selectable, so the hover highlighter neither outlines it (an outline of a
/// transparent full-screen graphic would fill the screen) nor shows the hand
/// over the whole screen.
/// </summary>
public sealed class ClickCatcher : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Raised on a left click.</summary>
    public UnityEvent onClick = new UnityEvent();

    /// <inheritdoc />
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            onClick.Invoke();
    }
}
