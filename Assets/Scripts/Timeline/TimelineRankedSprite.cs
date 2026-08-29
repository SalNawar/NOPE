using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// An office layer whose sprite follows whichever nation / attribute is currently
/// winning. Unlike <see cref="TimelineReactiveSprite"/> (which reacts to authored
/// EffectSO cues), this reads the raw timeline scores and ranks them, so a layer
/// responds to the running totals with no content authoring required.
///
/// Three of these drive the booth: background = top nation, mid decor = top
/// attribute, desk decor = top nation-at-an-era + attribute pairing.
/// </summary>
public sealed class TimelineRankedSprite : MonoBehaviour
{
    /// <summary>One winning id mapped to the sprite it should show.</summary>
    [Serializable]
    public struct RankedSprite
    {
        /// <summary>
        /// Id to match: a nation id ("latia"), attribute id ("robotics"), or
        /// "profileId:attributeId" ("latia_rome:aristocracy") depending on category.
        /// </summary>
        public string id;

        /// <summary>Sprite to show while that id is winning.</summary>
        public Sprite sprite;
    }

    /// <summary>Which slice of the scores this layer follows.</summary>
    [SerializeField] private RankCategory category = RankCategory.TopNation;

    /// <summary>The renderer whose sprite is swapped (defaults to this object's).</summary>
    [SerializeField] private SpriteRenderer target;

    /// <summary>Sprite shown when nothing is winning yet or the winner has no mapping.</summary>
    [SerializeField] private Sprite defaultSprite;

    /// <summary>Winning-id → sprite mappings; authored per layer.</summary>
    [SerializeField] private List<RankedSprite> mappings = new List<RankedSprite>();

    /// <summary>
    /// Shows the winning id and value as world-space text on this layer. On while
    /// wiring the layers up; turn off per layer once its mappings are verified.
    /// </summary>
    [SerializeField] private bool showDebugLabel = true;

    /// <summary>Child object name for the debug label.</summary>
    private const string LabelName = "RankDebugLabel";

    /// <summary>Spawned lazily while <see cref="showDebugLabel"/> is on.</summary>
    private TextMeshPro _label;

    /// <summary>
    /// Editor and development builds only — mirrors DebugPanelController's guard so
    /// developer text can never reach a player build, whatever the serialized flag
    /// says. The scene builder ships <see cref="showDebugLabel"/> on by default, so
    /// without this a release build would render the labels over the booth.
    /// </summary>
    private static bool DebugLabelsAllowed => Application.isEditor || Debug.isDebugBuild;

    private void Reset()
    {
        target = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        Refresh();
    }

    /// <summary>
    /// Re-reads the current scores and applies the winning sprite (and debug label).
    /// Called on Start; the office scene reloads each day, so no event wiring is needed.
    /// </summary>
    public void Refresh()
    {
        RunManager run = RunManager.HasInstance ? RunManager.Instance : null;
        WorldState world = run != null ? run.World : null;
        ContentLibrarySO lib = run != null ? run.Library : null;

        if (world == null || world.timeline == null)
        {
            Apply(defaultSprite, "(no run)");
            return;
        }

        float minScore = run != null && run.Config != null && run.Config.gameConfig != null
            ? run.Config.gameConfig.rankedLayerMinScore
            : 0f;

        if (!ScoreRanking.TryGetTop(ToPairs(world.timeline.scores), category, minScore, out RankedScore top))
        {
            // Separate the two neutral states so the label says WHY this layer is
            // blank: no data for the category at all, versus data that never got
            // above the floor (e.g. every attribute still negative).
            bool anyEntries = ScoreRanking.TryGetTop(
                ToPairs(world.timeline.scores), category, float.NegativeInfinity, out _);

            Apply(defaultSprite, anyEntries
                ? $"{category}: (no clear leader)"
                : $"{category}: (no scores yet)");
            return;
        }

        Sprite chosen = defaultSprite;
        bool mapped = false;

        foreach (RankedSprite mapping in mappings)
        {
            if (string.IsNullOrEmpty(mapping.id) || mapping.id != top.id)
                continue;

            chosen = mapping.sprite;
            mapped = true;
            break;
        }

        if (!mapped && mappings.Count > 0)
        {
            Debug.LogWarning(
                $"[TimelineRankedSprite] '{name}' ({category}): winning id '{top.id}' has no sprite mapping — " +
                $"showing the default. Add it to this component's Mappings list to fix.");
        }

        Apply(chosen, $"{category}: {TimelineScoreDisplay.DescribeScoreKey(lib, top.key)} ({top.value:0.##})");
    }

    /// <summary>Assigns the sprite and updates (or removes) the debug label.</summary>
    private void Apply(Sprite sprite, string labelText)
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();

        if (target != null)
            target.sprite = sprite;

        if (!showDebugLabel || !DebugLabelsAllowed)
        {
            // Also catches a label left behind in the scene from a previous run.
            Transform stale = _label != null ? _label.transform : transform.Find(LabelName);

            if (stale != null)
                stale.gameObject.SetActive(false);

            return;
        }

        if (_label == null)
            _label = BuildLabel();

        if (_label == null)
            return;

        _label.gameObject.SetActive(true);
        _label.text = labelText;
    }

    /// <summary>Creates the world-space debug label as a child of this object.</summary>
    private TextMeshPro BuildLabel()
    {
        Transform existing = transform.Find(LabelName);

        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(LabelName, typeof(TextMeshPro));

        if (existing == null)
            go.transform.SetParent(transform, false);

        var tmp = go.GetComponent<TextMeshPro>();

        if (tmp == null)
            return null;

        tmp.fontSize = 3f;
        tmp.color = new Color(1f, 0.95f, 0.4f);
        tmp.alignment = TextAlignmentOptions.Center;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sortingOrder = 500;

        return tmp;
    }

    /// <summary>Adapts serialized score entries to the pure (key, value) pairs the ranker takes.</summary>
    private static IEnumerable<KeyValuePair<string, float>> ToPairs(List<ScoreEntry> scores)
    {
        foreach (ScoreEntry entry in scores)
        {
            if (entry != null)
                yield return new KeyValuePair<string, float>(entry.key, entry.value);
        }
    }
}
