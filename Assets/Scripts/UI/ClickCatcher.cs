using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// A transparent click area (the PC frame's surround: a click on the room
/// beside the frame closes it): a left click raises <see cref="onClick"/>. Not
/// a Selectable, so the hover highlighter neither outlines it (an outline of a
/// transparent full-screen graphic would fill the screen) nor shows the hand
/// over the whole screen. With <see cref="passThrough"/> the click then goes
/// on to whatever lies under the pointer once the catcher has done its job
/// (the PC frame's: a click on the traveller, the intercom or the desk beside
/// the frame closes it and reaches them at once; Saleh 2026-09-30: "smoothly
/// transition to dialogue wheel or pc without having to first back out").
/// </summary>
public sealed class ClickCatcher : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Raised on a left click.</summary>
    public UnityEvent onClick = new UnityEvent();

    /// <summary>After <see cref="onClick"/>, the click reaches the first thing under the pointer other than this catcher (what the click would have hit without it), as the EventSystem would deliver it.</summary>
    [SerializeField] private bool passThrough;

    /// <summary>The raycast's hits under the pointer (reused).</summary>
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <inheritdoc />
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        onClick.Invoke();
        if (passThrough)
            PassOn(eventData);
    }

    /// <summary>Delivers the click to the click handler of the first hit under the pointer that is not this catcher (or inside it); nothing when that hit takes no clicks.</summary>
    private void PassOn(PointerEventData eventData)
    {
        EventSystem events = EventSystem.current;
        if (events == null)
            return;

        _hits.Clear();
        events.RaycastAll(eventData, _hits);
        foreach (RaycastResult hit in _hits)
        {
            if (hit.gameObject == null || hit.gameObject.transform.IsChildOf(transform))
                continue;
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject);
            if (handler != null)
                ExecuteEvents.Execute(handler, eventData, ExecuteEvents.pointerClickHandler);
            return;
        }
    }
}
