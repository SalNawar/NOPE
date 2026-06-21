using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A scene prop whose sprite changes with the current timeline. Subclasses the
/// generic <see cref="TimelineCueReceiver"/> on the Visuals channel: when the
/// active timeline effects broadcast cue ids, the first cue that matches one of
/// the inspector mappings swaps this prop's sprite (otherwise the default shows).
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

    /// <summary>Sprite shown when no mapped cue is active.</summary>
    [SerializeField] private Sprite defaultSprite;

    /// <summary>Cue-id → sprite mappings; first active match wins.</summary>
    [SerializeField] private List<CueSprite> mappings = new List<CueSprite>();

    private void Reset()
    {
        target = GetComponent<SpriteRenderer>();
    }

    /// <summary>Swaps to the first mapped cue present in the active set, else default.</summary>
    protected override void OnCuesChanged(IReadOnlyList<string> cues)
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();

        if (target == null)
            return;

        Sprite chosen = defaultSprite;

        for (int m = 0; m < mappings.Count && chosen == defaultSprite; m++)
        {
            string cueId = mappings[m].cueId;
            if (string.IsNullOrEmpty(cueId))
                continue;

            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i] == cueId)
                {
                    chosen = mappings[m].sprite;
                    break;
                }
            }
        }

        target.sprite = chosen;
    }
}
