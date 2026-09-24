using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// One per scene. Each frame it finds what the pointer is over, through the
/// EventSystem's raycasters (the same ones clicks use), and, when that changes,
/// moves the hover highlight and swaps the cursor: a white outline and hand
/// cursor on any interactable Clickable (booth sprite) or Selectable (UI), the
/// arrow elsewhere. Objects need no setup of their own.
/// </summary>
public sealed class HoverHighlighter : MonoBehaviour
{
    /// <summary>Upper bound on the generated ring width, keeping outline generation cheap.</summary>
    private const int MaxRingWidthPx = 64;

    /// <summary>Cursor textures and outline look.</summary>
    [SerializeField] private InteractionFeedbackSO settings;

    /// <summary>Reused raycast results.</summary>
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <summary>Generated outlines per booth sprite renderer.</summary>
    private readonly Dictionary<SpriteRenderer, WorldOutline> _worldOutlines = new Dictionary<SpriteRenderer, WorldOutline>();

    /// <summary>Reused pointer data for raycasts.</summary>
    private PointerEventData _pointerData;

    /// <summary>Currently highlighted Clickable or Selectable (null = nothing).</summary>
    private Component _hovered;

    /// <summary>True when the hand cursor is showing.</summary>
    private bool _handCursor;

    /// <summary>True once a cursor was set by this component.</summary>
    private bool _cursorApplied;

    /// <summary>Generated outline for one booth sprite (rebuilt when its sprite changes).</summary>
    private sealed class WorldOutline
    {
        /// <summary>Sprite the outline was built from.</summary>
        public Sprite source;

        /// <summary>Generated outline sprite (null if generation failed).</summary>
        public Sprite outline;

        /// <summary>Child renderer that shows the outline.</summary>
        public SpriteRenderer renderer;
    }

    private void OnEnable()
    {
        if (settings == null)
            Debug.LogWarning("HoverHighlighter: no InteractionFeedbackSO assigned, so there is no custom cursor or hover outline. Run Tools > TimeDesk > Build Office UI.", this);
    }

    private void Update()
    {
        if (settings == null)
            return;

        Component target = FindHoverTarget();
        if (target != _hovered)
        {
            SetHighlighted(_hovered, false);
            _hovered = target;
            SetHighlighted(_hovered, true);
        }

        ApplyCursor(_hovered != null);
    }

    private void OnDisable()
    {
        SetHighlighted(_hovered, false);
        _hovered = null;
        if (_cursorApplied)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        _cursorApplied = false;
    }

    private void OnDestroy()
    {
        foreach (WorldOutline o in _worldOutlines.Values)
        {
            DestroyOutlineSprite(o);
            if (o.renderer != null)
                Destroy(o.renderer.gameObject);
        }
        _worldOutlines.Clear();
    }

    /// <summary>Topmost interactable under the pointer, or null.</summary>
    private Component FindHoverTarget()
    {
        EventSystem eventSystem = EventSystem.current;
        Pointer pointer = Pointer.current;
        if (eventSystem == null || pointer == null)
            return null;

        if (_pointerData == null)
            _pointerData = new PointerEventData(eventSystem);
        _pointerData.position = pointer.position.ReadValue();

        _hits.Clear();
        eventSystem.RaycastAll(_pointerData, _hits);
        return _hits.Count > 0 ? InteractiveOn(_hits[0].gameObject) : null;
    }

    /// <summary>
    /// The interactable Clickable or Selectable on this object or its nearest
    /// parent that has one; null if that one is not interactable (or there is none).
    /// </summary>
    private static Component InteractiveOn(GameObject go)
    {
        for (Transform t = go.transform; t != null; t = t.parent)
        {
            Clickable clickable = t.GetComponent<Clickable>();
            if (clickable != null && clickable.isActiveAndEnabled)
                return clickable.Interactable ? clickable : null;

            Selectable selectable = t.GetComponent<Selectable>();
            if (selectable != null && selectable.isActiveAndEnabled)
                return selectable.IsInteractable() ? selectable : null;
        }
        return null;
    }

    /// <summary>Turns the highlight on a target on or off.</summary>
    private void SetHighlighted(Component target, bool on)
    {
        if (target == null)
            return;

        if (target is Clickable clickable)
        {
            SpriteRenderer sr = clickable.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;

            WorldOutline outline = on ? EnsureWorldOutline(sr) : Lookup(sr);
            if (outline != null && outline.renderer != null)
                outline.renderer.enabled = on && outline.outline != null;
        }
        else if (target is Selectable selectable && selectable.targetGraphic != null)
        {
            GameObject host = selectable.targetGraphic.gameObject;
            HoverUIOutline uiOutline = host.GetComponent<HoverUIOutline>();
            if (uiOutline == null)
            {
                if (!on)
                    return;
                uiOutline = host.AddComponent<HoverUIOutline>();
            }

            uiOutline.effectColor = settings.outlineColor;
            uiOutline.effectDistance = settings.uiOutlineDistance;
            uiOutline.enabled = on;
        }
    }

    /// <summary>Existing outline for a renderer, or null.</summary>
    private WorldOutline Lookup(SpriteRenderer sr) =>
        _worldOutlines.TryGetValue(sr, out WorldOutline o) ? o : null;

    /// <summary>Creates or refreshes the outline child for a booth sprite.</summary>
    private WorldOutline EnsureWorldOutline(SpriteRenderer sr)
    {
        if (sr.sprite == null)
            return null;

        if (!_worldOutlines.TryGetValue(sr, out WorldOutline o))
        {
            o = new WorldOutline();
            _worldOutlines[sr] = o;
        }

        if (o.renderer == null)
        {
            var child = new GameObject("HoverOutline");
            child.transform.SetParent(sr.transform, false);
            o.renderer = child.AddComponent<SpriteRenderer>();
        }

        // Rebuild only when the sprite changed (e.g. the timeline poster swapped art).
        if (o.source != sr.sprite)
        {
            DestroyOutlineSprite(o);
            o.source = sr.sprite;
            try
            {
                o.outline = SpriteOutlineBuilder.Build(sr.sprite, RingWidthPx(sr));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"HoverHighlighter: could not build an outline for '{sr.sprite.name}' ({e.Message}).", sr);
            }
        }

        SpriteRenderer r = o.renderer;
        r.sprite = o.outline;
        r.sortingLayerID = sr.sortingLayerID;
        r.sortingOrder = sr.sortingOrder + 1;
        r.flipX = sr.flipX;
        r.flipY = sr.flipY;
        r.color = settings.outlineColor;
        if (settings.outlineMaterial != null)
            r.sharedMaterial = settings.outlineMaterial;
        return o;
    }

    /// <summary>Ring width in source pixels for the configured world-space width.</summary>
    private int RingWidthPx(SpriteRenderer sr)
    {
        Vector3 s = sr.transform.lossyScale;
        float scale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        if (scale <= 0f)
            return 1;

        return Mathf.Clamp(Mathf.RoundToInt(settings.worldOutlineWidth * sr.sprite.pixelsPerUnit / scale), 1, MaxRingWidthPx);
    }

    /// <summary>Sets the arrow or the hand cursor (only on change).</summary>
    private void ApplyCursor(bool hand)
    {
        if (_cursorApplied && hand == _handCursor)
            return;

        bool useHand = hand && settings.handCursor != null;
        Texture2D texture = useHand ? settings.handCursor : settings.arrowCursor;
        Vector2 hotspot = useHand ? settings.handHotspot : settings.arrowHotspot;
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        _handCursor = hand;
        _cursorApplied = true;
    }

    /// <summary>Destroys a generated outline sprite and its texture.</summary>
    private static void DestroyOutlineSprite(WorldOutline o)
    {
        if (o.outline == null)
            return;

        Texture2D texture = o.outline.texture;
        Destroy(o.outline);
        if (texture != null)
            Destroy(texture);
        o.outline = null;
    }
}
