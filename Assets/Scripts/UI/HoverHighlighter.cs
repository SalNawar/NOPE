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
/// no renderer features, the art's own objects untouched. A Clickable with no
/// outline renderers (a hit zone) gets the hand cursor only.
/// </summary>
public sealed class HoverHighlighter : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int WidthId = Shader.PropertyToID("_Width");

    /// <summary>Cursor textures and outline look.</summary>
    [SerializeField] private InteractionFeedbackSO settings;

    /// <summary>Reused raycast results (fallback path only).</summary>
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <summary>The hovered object's meshes and their renderers (rebuilt when the target changes).</summary>
    private readonly List<(Mesh mesh, Renderer renderer)> _hull = new List<(Mesh, Renderer)>();

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
            _hull.Clear();
            if (!on || clickable == null)
                return;

            foreach (Renderer r in clickable.Outline)
                if (r != null && r.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                    _hull.Add((filter.sharedMesh, r));
        }
        else if (target is Selectable selectable && selectable != null && selectable.targetGraphic != null
                 && (selectable.targetGraphic.color.a > 0f || !on))
        {
            // A transparent graphic (a click area) gets no outline: the outline copies its quad opaque.
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

    /// <summary>Draws the hovered office object's outline hull this frame (every submesh of every visible outline renderer).</summary>
    private void DrawHull()
    {
        if (_hullMaterial == null || _hull.Count == 0)
            return;

        var parameters = new RenderParams(_hullMaterial);
        foreach ((Mesh mesh, Renderer renderer) in _hull)
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
        _lastHitObject = null;
        _lastCandidate = null;
        _hovered = null;
    }
}
