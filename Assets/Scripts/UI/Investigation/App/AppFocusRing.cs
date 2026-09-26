using UnityEngine;

/// <summary>
/// The Investigation app's keyboard focus ring (the PC redesign KB4; the
/// FocusRing role, the 3:1 outline class): a 3-unit outline the builder
/// draws as four edges, laid over the focused target (a row, a tab, a chip,
/// a pin, a button, the search field) and following it each frame, zoomed
/// or scrolled, cut to the viewport it is shown in (a row scrolled out of
/// the pane shows no ring); it hides when its target goes (a page turned, a
/// view hidden). It takes no raycasts.
/// </summary>
public sealed class AppFocusRing : MonoBehaviour
{
    /// <summary>The ring's edges' group (faded out while the target is outside its viewport).</summary>
    [SerializeField] private CanvasGroup group;

    private readonly Vector3[] _corners = new Vector3[4];
    private RectTransform _target;
    private RectTransform _clip;

    /// <summary>The target the ring is on, or null.</summary>
    public RectTransform Target => _target;

    /// <summary>Lays the ring over <paramref name="target"/> (null hides it), cut to <paramref name="clip"/> (null: not cut).</summary>
    public void Show(RectTransform target, RectTransform clip = null)
    {
        _target = target;
        _clip = clip;
        if (gameObject.activeSelf != (target != null))
            gameObject.SetActive(target != null);
        Follow();
    }

    /// <summary>Hides the ring.</summary>
    public void Hide() => Show(null);

    private void LateUpdate()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
            Hide();
        else
            Follow();
    }

    /// <summary>The ring's rect becomes the target's (cut to the clip), in the ring's parent space.</summary>
    private void Follow()
    {
        if (_target == null || !(transform.parent is RectTransform parent))
            return;
        _target.GetWorldCorners(_corners);
        Vector2 min = parent.InverseTransformPoint(_corners[0]);
        Vector2 max = parent.InverseTransformPoint(_corners[2]);
        if (_clip != null)
        {
            _clip.GetWorldCorners(_corners);
            min = Vector2.Max(min, parent.InverseTransformPoint(_corners[0]));
            max = Vector2.Min(max, parent.InverseTransformPoint(_corners[2]));
        }
        bool visible = max.x > min.x && max.y > min.y;
        if (group != null)
            group.alpha = visible ? 1f : 0f;
        if (!visible)
            return;
        var ring = (RectTransform)transform;
        ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
        ring.pivot = new Vector2(0.5f, 0.5f);
        Vector2 centre = parent.rect.center;
        ring.anchoredPosition = (min + max) / 2f - centre;
        ring.sizeDelta = max - min;
    }
}
