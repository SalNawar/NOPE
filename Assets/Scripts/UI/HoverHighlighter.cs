using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One persistent instance (created by InteractionFeedbackBootstrap). Each frame
/// it reads what the pointer is over from the EventSystem (reusing the Input
/// System UI module's own raycast) and, when that changes, moves the hover
/// highlight and swaps the cursor: an outline and hand cursor on any
/// interactable Clickable (booth sprite) or Selectable (UI), the arrow
/// elsewhere. A Clickable whose sprite is hidden (a hit zone over other art)
/// gets the hand cursor but no outline. Objects need no setup of their own.
/// </summary>
public sealed class HoverHighlighter : MonoBehaviour
{
    /// <summary>Upper bound on the generated ring width, keeping outline generation cheap.</summary>
    private const int MaxRingWidthPx = 64;

    /// <summary>Cursor textures and outline look.</summary>
    [SerializeField] private InteractionFeedbackSO settings;

    /// <summary>Reused raycast results (fallback path only).</summary>
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <summary>Generated outlines per booth sprite renderer.</summary>
    private readonly Dictionary<SpriteRenderer, WorldOutline> _worldOutlines = new Dictionary<SpriteRenderer, WorldOutline>();

    /// <summary>Reused pointer data for the fallback raycast.</summary>
    private PointerEventData _pointerData;

    /// <summary>EventSystem the fallback pointer data belongs to.</summary>
    private EventSystem _pointerDataOwner;

    /// <summary>Object under the pointer last frame.</summary>
    private GameObject _lastHitObject;

    /// <summary>Nearest Clickable/Selectable of <see cref="_lastHitObject"/> (interactable or not).</summary>
    private Component _lastCandidate;

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

    /// <summary>Assigns the look (call before the component first enables).</summary>
    public void Configure(InteractionFeedbackSO feedback)
    {
        settings = feedback;
    }

    private void OnEnable()
    {
        if (settings == null)
            Debug.LogWarning("HoverHighlighter: no InteractionFeedbackSO assigned, so there is no custom cursor or hover outline. Run Tools > TimeDesk > Build Office UI.", this);

        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    /// <summary>LateUpdate so the UI module has already raycast this frame.</summary>
    private void LateUpdate()
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
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
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

    /// <summary>The interactable under the pointer, or null (hierarchy walk only when the hit object changes).</summary>
    private Component FindHoverTarget()
    {
        GameObject hit = PointerHitObject();
        if (hit != _lastHitObject)
        {
            _lastHitObject = hit;
            _lastCandidate = hit != null ? NearestCandidate(hit) : null;
        }

        // Re-checked every frame: READY and buttons toggle interactable while hovered.
        return IsInteractable(_lastCandidate) ? _lastCandidate : null;
    }

    /// <summary>
    /// Topmost object under the pointer: the Input System UI module's own raycast
    /// when that module is active (no second raycast), else a direct EventSystem raycast.
    /// </summary>
    private GameObject PointerHitObject()
    {
        EventSystem eventSystem = EventSystem.current;
        Pointer pointer = Pointer.current;
        if (eventSystem == null || pointer == null)
            return null;

        if (eventSystem.currentInputModule is InputSystemUIInputModule module)
            return module.GetLastRaycastResult(pointer.deviceId).gameObject;

        if (_pointerData == null || _pointerDataOwner != eventSystem)
        {
            _pointerData = new PointerEventData(eventSystem);
            _pointerDataOwner = eventSystem;
        }
        _pointerData.position = pointer.position.ReadValue();

        _hits.Clear();
        eventSystem.RaycastAll(_pointerData, _hits);
        return _hits.Count > 0 ? _hits[0].gameObject : null;
    }

    /// <summary>The nearest Clickable or Selectable on this object or its parents, or null.</summary>
    private static Component NearestCandidate(GameObject go)
    {
        for (Transform t = go.transform; t != null; t = t.parent)
        {
            if (t.TryGetComponent(out Clickable clickable))
                return clickable;
            if (t.TryGetComponent(out Selectable selectable))
                return selectable;
        }
        return null;
    }

    /// <summary>True when a candidate is enabled and currently accepts clicks.</summary>
    private static bool IsInteractable(Component candidate)
    {
        if (candidate is Clickable clickable)
            return clickable != null && clickable.isActiveAndEnabled && clickable.Interactable;
        if (candidate is Selectable selectable)
            return selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable();
        return false;
    }

    /// <summary>Turns the highlight on a target on or off.</summary>
    private void SetHighlighted(Component target, bool on)
    {
        if (target == null)
            return;

        if (target is Clickable clickable)
        {
            if (!clickable.TryGetComponent(out SpriteRenderer sr))
                return;

            // A hidden sprite is a hit zone over other art: no outline of its shape.
            bool show = on && sr.enabled;
            WorldOutline outline = show ? EnsureWorldOutline(sr) : Lookup(sr);
            if (outline != null && outline.renderer != null)
                outline.renderer.enabled = show && outline.outline != null;
        }
        else if (target is Selectable selectable && selectable.targetGraphic != null)
        {
            GameObject host = selectable.targetGraphic.gameObject;
            if (!host.TryGetComponent(out HoverUIOutline uiOutline))
            {
                if (!on)
                    return;
                uiOutline = host.AddComponent<HoverUIOutline>();
            }

            uiOutline.effectColor = settings.uiOutlineColor;
            uiOutline.effectDistance = settings.uiOutlineDistance;
            uiOutline.useGraphicAlpha = false; // translucent rows would otherwise fade the outline
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
            PurgeDeadOutlines();
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

    /// <summary>Drops outlines whose sprites went away with an unloaded scene.</summary>
    private void HandleSceneUnloaded(Scene scene)
    {
        PurgeDeadOutlines();

        _lastHitObject = null;
        _lastCandidate = null;
        _hovered = null;
    }

    /// <summary>
    /// Drops the outlines of destroyed renderers (an unloaded scene's sprites,
    /// the papers of a finished case) with their generated sprites and textures.
    /// Runs when a scene unloads and before a new outline is created, so the
    /// cache holds at most the last case's dead papers.
    /// </summary>
    private void PurgeDeadOutlines()
    {
        var dead = new List<SpriteRenderer>();
        foreach (KeyValuePair<SpriteRenderer, WorldOutline> pair in _worldOutlines)
        {
            if (pair.Key == null)
                dead.Add(pair.Key);
        }

        foreach (SpriteRenderer key in dead)
        {
            DestroyOutlineSprite(_worldOutlines[key]);
            _worldOutlines.Remove(key);
        }
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
