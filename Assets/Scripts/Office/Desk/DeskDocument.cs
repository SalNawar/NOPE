using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A physical paper on the desk: a SortingGroup root carrying its sprite, its
/// collider, a Clickable and a DeskDraggable (so the hover outline follows the
/// paper and a raycast reports the group's order). It shows the document's
/// title, the holder's name and a reserved photo slot; every field is read on
/// the scanned PC copy. The texts stay in their source script (piece 9
/// translates the scanned copy, not the paper). Slides are linear moves in
/// Update, only while sliding.
/// </summary>
public sealed class DeskDocument : MonoBehaviour
{
    /// <summary>The document's title ("Travel Passport").</summary>
    [SerializeField] private TextMeshPro title;

    /// <summary>The holder's name (the traveller's registered given name).</summary>
    [SerializeField] private TextMeshPro holder;

    /// <summary>Reserved for piece 4's passport photo (inactive until then).</summary>
    [SerializeField] private GameObject photoSlot;

    /// <summary>The paper's click (brings it to the front).</summary>
    [SerializeField] private Clickable click;

    /// <summary>The paper's drag.</summary>
    [SerializeField] private DeskDraggable drag;

    /// <summary>The paper's sorting group (its order is the paper's place in the stack).</summary>
    [SerializeField] private SortingGroup group;

    private Vector3 _slideFrom;
    private Vector3 _slideTo;
    private float _slideSeconds;
    private float _slideElapsed;
    private Action _slideDone;

    /// <summary>The paper's index in the case (DeskController maps a drag or a click to it).</summary>
    public int Index { get; private set; }

    /// <summary>True while the paper slides (it takes no input then).</summary>
    public bool IsSliding { get; private set; }

    /// <summary>Only while sliding: moves along the slide and calls its done callback on landing.</summary>
    private void Update()
    {
        if (!IsSliding)
            return;

        _slideElapsed += Time.deltaTime;
        float t = _slideSeconds > 0f ? Mathf.Clamp01(_slideElapsed / _slideSeconds) : 1f;
        transform.position = Vector3.Lerp(_slideFrom, _slideTo, t);
        if (t < 1f)
            return;

        IsSliding = false;
        Action done = _slideDone;
        _slideDone = null;
        done?.Invoke();
    }

    /// <summary>Shows a document: its index, canonical title and holder (the paper keeps its source text).</summary>
    public void Bind(int index, CaseDocument doc)
    {
        Index = index;
        if (title != null)
            title.text = doc != null ? doc.name : string.Empty;
        if (holder != null)
            holder.text = doc != null ? doc.holder : string.Empty;
    }

    /// <summary>Sets the paper's sorting order (its place in the stack, or the held order).</summary>
    public void SetOrder(int order)
    {
        if (group != null)
            group.sortingOrder = order;
    }

    /// <summary>
    /// Lets the paper be dragged and clicked, or not. <paramref name="raycastable"/>
    /// false also takes it out of the raycast (spec R38): papers sort above the
    /// focus exit zone, so a paper the booth has put away, showing at the edge
    /// of the focused view on a wide screen, must let the click that leaves
    /// focus through. A paper inert only for itself (sliding, scanning) stays
    /// raycastable and still covers what lies under it.
    /// </summary>
    public void SetLive(bool live, bool raycastable)
    {
        if (drag != null)
        {
            drag.enabled = live;
            drag.SetRaycastable(raycastable);
        }
        if (click != null)
            click.Interactable = live;
    }

    /// <summary>Slides the paper to a world point in <paramref name="seconds"/> (a linear move), then calls <paramref name="done"/>.</summary>
    public void SlideTo(Vector3 target, float seconds, Action done)
    {
        _slideFrom = transform.position;
        _slideTo = target;
        _slideSeconds = seconds;
        _slideElapsed = 0f;
        _slideDone = done;
        IsSliding = true;
    }
}
