using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Takes the drag that starts on a document of a side (the Lens screen). It sits
/// on each side and on every scroll rect inside it: the EventSystem calls every
/// drag handler of the object it picked, so on a scroll rect this runs beside
/// the scroll rect's own. When the app says the drag moves the document
/// (inspect off, the side full) the scroll rect is switched off for the drag, so
/// the page does not scroll under it; otherwise nothing changes. The mouse wheel
/// is not a drag and still scrolls.
/// </summary>
public sealed class LensDocDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InvestigationApp _app;
    private ScrollRect _scroll;
    private bool _mine;

    /// <inheritdoc />
    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left)
            return;
        _app = GetComponentInParent<InvestigationApp>();
        _mine = _app != null && _app.BeginMove(GetComponentInParent<AppPane>(), e);
        if (_mine && TryGetComponent(out _scroll))
            _scroll.enabled = false;
    }

    private void OnDisable()
    {
        _mine = false;
        if (_scroll != null)
            _scroll.enabled = true;
    }

    /// <inheritdoc />
    public void OnDrag(PointerEventData e)
    {
        if (_mine)
            _app.MoveDrag(e);
    }

    /// <inheritdoc />
    public void OnEndDrag(PointerEventData e)
    {
        if (!_mine)
            return;
        _mine = false;
        if (_scroll != null)
            _scroll.enabled = true;
        _app.EndMove(e);
    }
}
