using UnityEngine;

/// <summary>
/// One portal ring's effect in the anime hall (the portals spec v3 VX1,
/// VX3-VX6): an unlit additive sprite (TimeDesk/PortalGlow) inside its ring:
/// the glow of an open departure portal (tinted, slowly turning, still with
/// reduced motion), the Return Gate's spiral, or nothing (CLOSED, under
/// maintenance); a departure flares it (<see cref="Pulse"/>: up and down over
/// the config's seconds, one step with reduced motion). It draws on the art's
/// sorting layer (Default) at its ring's secure-bay order less one, so the
/// bay's front fence and panels, the metal ring and the painted glass draw
/// over it, and it follows its ring's opaque rect each frame if the art pans.
/// AnimeHallPortalLink places and shows it; the gameplay layer builds one per
/// portal (Build Office UI), hidden until then.
/// </summary>
public sealed class PortalEffect : MonoBehaviour
{
    /// <summary>The effect's sprite (its material is TimeDesk/PortalGlow).</summary>
    [SerializeField] private SpriteRenderer glow;

    private Transform _ring;
    private Rect _ringRect;
    private float _share = 1f;
    private float _spinDegrees;
    private float _pulseScale = 1f;
    private float _pulseSeconds = 1f;
    private float _pulseAge = -1f;
    private float _angle;

    /// <summary>
    /// Puts the effect inside a ring: <paramref name="ringRect"/> (the ring's
    /// opaque rect in <paramref name="ring"/>'s space) placed each frame, its
    /// diameter <paramref name="share"/> of the ring's width; drawn on the
    /// sorting layer <paramref name="sortingLayerId"/> at <paramref name="sortingOrder"/>.
    /// </summary>
    public void Place(Transform ring, Rect ringRect, float share, int sortingLayerId, int sortingOrder)
    {
        _ring = ring;
        _ringRect = ringRect;
        _share = share;
        if (glow != null)
        {
            glow.sortingLayerID = sortingLayerId;
            glow.sortingOrder = sortingOrder;
        }
        Follow();
    }

    /// <summary>
    /// Shows <paramref name="sprite"/> tinted <paramref name="tint"/>, turning
    /// <paramref name="spinDegrees"/> a second, flaring to
    /// <paramref name="pulseScale"/> over <paramref name="pulseSeconds"/> on a
    /// departure; a null sprite shows nothing (a CLOSED ring or one under maintenance).
    /// </summary>
    public void Show(Sprite sprite, Color tint, float spinDegrees, float pulseScale, float pulseSeconds)
    {
        _spinDegrees = spinDegrees;
        _pulseScale = pulseScale;
        _pulseSeconds = Mathf.Max(0.05f, pulseSeconds);
        _pulseAge = -1f;
        if (glow == null)
            return;
        glow.sprite = sprite;
        glow.color = tint;
        glow.enabled = sprite != null;
    }

    /// <summary>True while it shows an effect.</summary>
    public bool Showing => glow != null && glow.enabled;

    /// <summary>A traveller left through this ring: the effect flares (nothing when it shows nothing).</summary>
    public void Pulse()
    {
        if (Showing)
            _pulseAge = 0f;
    }

    private void LateUpdate()
    {
        if (!Showing)
            return;

        if (!MotionPreference.Reduced)
            _angle = Mathf.Repeat(_angle + _spinDegrees * Time.deltaTime, 360f);

        float flare = 0f;
        if (_pulseAge >= 0f)
        {
            _pulseAge += Time.deltaTime;
            float progress = _pulseAge / _pulseSeconds;
            flare = PortalGlowPlaceholder.Pulse(progress, MotionPreference.Reduced);
            if (progress >= 1f)
                _pulseAge = -1f;
        }
        Follow(1f + (_pulseScale - 1f) * flare);
    }

    /// <summary>Centres the effect on its ring's opaque rect, facing as the ring does, its diameter the share of the ring's width times <paramref name="flare"/>.</summary>
    private void Follow(float flare = 1f)
    {
        if (_ring == null || glow == null)
            return;

        Bounds ring = OfficeAnchors.WorldBounds(_ring, _ringRect);
        Transform t = transform;
        t.SetPositionAndRotation(ring.center, _ring.rotation * Quaternion.Euler(0f, 0f, _angle));
        Vector2 spriteSize = glow.sprite != null ? (Vector2)glow.sprite.bounds.size : Vector2.one;
        float diameter = ring.size.x * _share * flare;
        t.localScale = new Vector3(diameter / Mathf.Max(1e-4f, spriteSize.x), diameter / Mathf.Max(1e-4f, spriteSize.y), 1f);
    }
}
