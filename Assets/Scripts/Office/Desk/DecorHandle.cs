using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>A decoration's drag handle (DeskDecor): the pointer's drag lifts it, carries it over the desk and lets it settle; a click without a drag stays the prop's own (its reaction).</summary>
public sealed class DecorHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private DeskDecor _decor;
    private int _index;

    /// <summary>Hands the handle its decoration (index <paramref name="index"/> of <paramref name="decor"/>).</summary>
    public void Init(DeskDecor decor, int index)
    {
        _decor = decor;
        _index = index;
    }

    /// <summary>A left drag began: the decoration lifts.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_decor != null && eventData.button == PointerEventData.InputButton.Left)
            _decor.Lift(_index);
    }

    /// <summary>The decoration follows the pointer over the desk.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        Camera cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        if (_decor != null && cam != null && eventData.button == PointerEventData.InputButton.Left)
            _decor.Follow(_index, cam.ScreenPointToRay(eventData.position));
    }

    /// <summary>Let go: the decoration settles in a zone and the places are saved.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_decor != null && eventData.button == PointerEventData.InputButton.Left)
            _decor.Drop(_index);
    }
}
