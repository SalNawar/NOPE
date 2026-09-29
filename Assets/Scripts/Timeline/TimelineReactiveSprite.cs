using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A scene prop whose sprite changes with the current timeline. Subclasses the
/// generic <see cref="TimelineCueReceiver"/> on the Visuals channel: when the
/// active timeline effects broadcast cue ids, the first mapping (in the
/// inspector's order) whose cue is active swaps this prop's sprite (CueMatch),
/// otherwise the default shows: the default sprite, or, when none is set, the
/// sprite the renderer was authored with (audit R3-004: a mapping whose sprite
/// equalled the default did not stop the search, and an unset default blanked
/// the prop).
/// Drop on booth posters, queue silhouettes, decor, etc. so the office visibly
/// reacts to dominant cultures / active effects.
/// </summary>
public sealed class TimelineReactiveSprite : TimelineCueReceiver
{
    /// <summary>One cue id mapped to the sprite it should show.</summary>
    [Serializable]
    public struct CueSprite
    {
        /// <summary>Cue id broadcast by an EffectSO Cue op on the Visuals channel.</summary>
        public string cueId;

        /// <summary>Sprite to show while that cue is active.</summary>
        public Sprite sprite;
    }

    /// <summary>The renderer whose sprite is swapped (defaults to this object's).</summary>
    [SerializeField] private SpriteRenderer target;

    /// <summary>Sprite shown when no mapped cue is active (unset: the renderer's own sprite).</summary>
    [SerializeField] private Sprite defaultSprite;

    /// <summary>Cue-id → sprite mappings; first active match wins.</summary>
    [SerializeField] private List<CueSprite> mappings = new List<CueSprite>();

    /// <summary>The renderer's sprite before any cue (Awake): the default when defaultSprite is unset.</summary>
    private Sprite _authoredSprite;

    private void Reset()
    {
        target = GetComponent<SpriteRenderer>();
    }

    /// <summary>Remembers the renderer's own sprite before the first refresh (Start) changes it.</summary>
    private void Awake()
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();
        if (target != null)
            _authoredSprite = target.sprite;
    }

    /// <summary>Swaps to the first mapping whose cue is active (CueMatch), else the default.</summary>
    protected override void OnCuesChanged(IReadOnlyList<string> cues)
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();

        if (target == null)
            return;

        int match = CueMatch.First(mappings.Count, m => mappings[m].cueId, cues);
        target.sprite = match >= 0 ? mappings[match].sprite : defaultSprite != null ? defaultSprite : _authoredSprite;
    }
}
