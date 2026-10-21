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
/// interactable Clickable (an office object) or Selectable (UI), the arrow
/// elsewhere. An office object's outline is its meshes drawn again as a hull
/// (the outline material: pushed out along the normals, back faces only)
/// through Graphics.RenderMesh, every frame while hovered: no scene objects,
/// no renderer features, the art's own objects untouched. The rim is the
/// same screen px for every object (InteractionFeedbackSO.outlinePx), drawn
/// a little toward the camera so the surface under an object never cuts it,
/// and it fades in on the new object and out on the old one
/// (outlineFadeSeconds; a cut under Reduced Motion): it never pops (the
/// review of the desk v3 capture). A Clickable with no outline renderers (a
/// hit zone) gets the hand cursor only.
/// </summary>
public sealed class HoverHighlighter : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int WidthId = Shader.PropertyToID("_Width");
    private static readonly int WidthPxId = Shader.PropertyToID("_WidthPx");
    private static readonly int PullId = Shader.PropertyToID("_Pull");

    /// <summary>Cursor textures and outline look.</summary>
    [SerializeField] private InteractionFeedbackSO settings;

    /// <summary>Reused raycast results (fallback path only).</summary>
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <summary>The hovered object's meshes and their renderers (rebuilt when the target changes).</summary>
    private readonly List<(Mesh mesh, Renderer renderer)> _hull = new List<(Mesh, Renderer)>();

    /// <summary>The object left last, its outline fading out.</summary>
    private readonly List<(Mesh mesh, Renderer renderer)> _fading = new List<(Mesh, Renderer)>();

    /// <summary>How far the hovered object's outline and the left one's have faded in (0 to 1).</summary>
    private float _shown, _fadingShown;

    /// <summary>Each outline's colour with its fade (made once; one block each, so neither overwrites the other's draw).</summary>
    private MaterialPropertyBlock _shownBlock, _fadingBlock;

    /// <summary>The outline material with the settings' colour and width (made once).</summary>
    private Material _hullMaterial;

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

    /// <summary>Assigns the look (call before the component first enables).</summary>
    public void Configure(InteractionFeedbackSO feedback)
    {
        settings = feedback;
    }

    private void OnEnable()
    {
        if (settings == null)
            Debug.LogWarning("HoverHighlighter: no InteractionFeedbackSO assigned, so there is no custom cursor or hover outline. Run Tools > TimeDesk > Build Office UI.", this);
        else if (settings.outlineMaterial != null && _hullMaterial == null)
        {
            _hullMaterial = new Material(settings.outlineMaterial) { name = settings.outlineMaterial.name + " (hover)" };
            _hullMaterial.SetColor(ColorId, settings.outlineColor);
            _hullMaterial.SetFloat(WidthId, settings.worldOutlineWidth);
            _hullMaterial.SetFloat(WidthPxId, settings.outlinePx);
            _hullMaterial.SetFloat(PullId, settings.outlinePull);
        }

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

        DrawHull();
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
        if (_hullMaterial != null)
            Destroy(_hullMaterial);
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

    /// <summary>Turns the highlight on a target on or off: an office object's outline meshes, or a UI element's outline effect.</summary>
    private void SetHighlighted(Component target, bool on)
    {
        if (target is Clickable clickable)
        {
            if (!on)
            {
                // The object left: its outline fades out from where it was while the next one's fades in.
                _fading.Clear();
                _fading.AddRange(_hull);
                _fadingShown = _shown;
                _hull.Clear();
                _shown = 0f;
                return;
            }
            _hull.Clear();
            _shown = 0f;
            if (clickable == null)
                return;

            foreach (Renderer r in clickable.Outline)
                if (r != null && r.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                    _hull.Add((filter.sharedMesh, r));
        }
        else if (target is Selectable selectable && selectable != null && selectable.targetGraphic != null
                 && (selectable.targetGraphic.color.a > 0f || !on) && (selectable.transition != Selectable.Transition.SpriteSwap || !on))
        {
            // A transparent graphic (a click area) gets no outline: the outline copies its quad opaque. A UI kit control
            // (sprite swap) gets none either: its hover face is its highlight (the halo and the star glint).
            GameObject host = selectable.targetGraphic.gameObject;
            if (!host.TryGetComponent(out HoverUIOutline uiOutline))
            {
                if (!on)
                    return;
                uiOutline = host.AddComponent<HoverUIOutline>();
            }

            // Themed UI takes its theme's two rings; untagged UI (Title, Home) the settings' colour.
            CultureThemeService theme = CultureThemeService.Instance;
            bool rings = theme != null && theme.ActiveTheme != null && host.TryGetComponent(out ThemeTag _);
            uiOutline.twoRings = rings;
            uiOutline.effectColor = rings ? theme.ActiveTheme.ringDark : settings.uiOutlineColor;
            uiOutline.outerColor = rings ? theme.ActiveTheme.ringLight : Color.clear;
            uiOutline.effectDistance = settings.uiOutlineDistance;
            uiOutline.useGraphicAlpha = false; // translucent rows would otherwise fade the outline
            uiOutline.enabled = on;
        }
    }

    /// <summary>Draws this frame's outline hulls (every submesh of every visible outline renderer): the hovered object's fading in, the one left fading out.</summary>
    private void DrawHull()
    {
        if (_hullMaterial == null)
            return;
        float step = settings.outlineFadeSeconds > 0f && !UiMotion.Amount.Still ? Time.unscaledDeltaTime / settings.outlineFadeSeconds : 1f;
        _shown = _hull.Count > 0 ? Mathf.Min(1f, _shown + step) : 0f;
        _fadingShown = Mathf.Max(0f, _fadingShown - step);
        if (_fadingShown <= 0f)
            _fading.Clear();
        _shownBlock ??= new MaterialPropertyBlock();
        _fadingBlock ??= new MaterialPropertyBlock();
        Draw(_fading, _fadingShown, _fadingBlock);
        Draw(_hull, _shown, _shownBlock);
    }

    /// <summary>Draws <paramref name="hull"/> at <paramref name="shown"/> of the outline's colour (its alpha).</summary>
    private void Draw(List<(Mesh mesh, Renderer renderer)> hull, float shown, MaterialPropertyBlock block)
    {
        if (hull.Count == 0 || shown <= 0f)
            return;
        Color colour = settings.outlineColor;
        colour.a *= shown;
        block.SetColor(ColorId, colour);
        var parameters = new RenderParams(_hullMaterial) { matProps = block };
        foreach ((Mesh mesh, Renderer renderer) in hull)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;
            Matrix4x4 matrix = renderer.localToWorldMatrix;
            for (int s = 0; s < mesh.subMeshCount; s++)
                Graphics.RenderMesh(parameters, mesh, s, matrix);
        }
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

    /// <summary>Forgets the hover when a scene unloads (its objects are gone).</summary>
    private void HandleSceneUnloaded(Scene scene)
    {
        _hull.Clear();
        _fading.Clear();
        _lastHitObject = null;
        _lastCandidate = null;
        _hovered = null;
    }
}
