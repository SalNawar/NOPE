using System;
using TMPro;
using UnityEngine;

/// <summary>
/// A physical paper on the desk: a root on the desk plane carrying its
/// Clickable and DeskDraggable, and a Sheet child lying flat, lifted by the
/// paper's place in the stack (so the top paper is nearest the camera and wins
/// the raycast), with the lit paper quad, the collider, the document's title,
/// the holder's name and, on a photo document, the traveller's photo. Every
/// field is read on the scanned PC copy. The texts stay in their source script
/// (piece 9 translates the scanned copy, not the paper). Slides are linear
/// moves in Update, only while sliding.
/// </summary>
public sealed class DeskDocument : MonoBehaviour
{
    /// <summary>The lying sheet: lifted by the stack, holding the paper, its collider, texts and photo.</summary>
    [SerializeField] private Transform sheet;

    /// <summary>The document's title ("Travel Passport").</summary>
    [SerializeField] private TextMeshPro title;

    /// <summary>The holder's name (the traveller's registered given name).</summary>
    [SerializeField] private TextMeshPro holder;

    /// <summary>The photo's frame, shown only on a photo document.</summary>
    [SerializeField] private GameObject photoSlot;

    /// <summary>The traveller's photo inside the frame (crop sprites).</summary>
    [SerializeField] private LookSpriteStack photo;

    /// <summary>The paper's click (brings it to the front).</summary>
    [SerializeField] private Clickable click;

    /// <summary>The paper's drag.</summary>
    [SerializeField] private DeskDraggable drag;

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

    /// <summary>Shows the traveller's photo in the frame (tinted into the room's light); a null look hides the frame (a document without a photo).</summary>
    public void ShowPhoto(TravellerLook look, CharacterArt art, Color tint)
    {
        if (photoSlot != null)
            photoSlot.SetActive(look != null);
        if (photo != null)
        {
            photo.Show(look, art);
            photo.SetTint(tint);
        }
    }

    /// <summary>Lifts the sheet off the desk plane (its place in the stack, or a held paper's lift), in metres.</summary>
    public void SetLift(float height)
    {
        if (sheet != null)
            sheet.localPosition = new Vector3(0f, height, 0f);
    }

    /// <summary>
    /// Lets the paper be dragged and clicked, or not. <paramref name="raycastable"/>
    /// false also takes it out of the raycast: a paper the office has put away
    /// (the frame open, the wheel open, a newsletter up) must let clicks through
    /// to what lies under it. A paper inert only for itself (sliding,
    /// scanning) stays raycastable and still covers what lies under it.
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
