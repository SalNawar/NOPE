using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>A decoration's drag handle (DeskDecor): a left drag takes it by the point pressed and carries it over the desk, let go it rests where it was let go; a click without a drag stays the prop's own (its reaction), and a drag never fires it (Clickable).</summary>
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

    /// <summary>A left drag began: the decoration is taken by the point under the pointer and lifts.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_decor != null && eventData.button == PointerEventData.InputButton.Left && TryRay(eventData, out Ray ray))
            _decor.Lift(_index, ray);
    }

    /// <summary>The decoration follows the pointer over the desk.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_decor != null && eventData.button == PointerEventData.InputButton.Left && TryRay(eventData, out Ray ray))
            _decor.Follow(_index, ray);
    }

    /// <summary>Let go: the decoration rests where it was let go and the places are saved.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_decor != null && eventData.button == PointerEventData.InputButton.Left)
            _decor.Drop(_index);
    }

    private static bool TryRay(PointerEventData eventData, out Ray ray)
    {
        Camera cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        ray = cam != null ? cam.ScreenPointToRay(eventData.position) : default;
        return cam != null;
    }
}
