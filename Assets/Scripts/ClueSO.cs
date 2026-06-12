// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Category used to route clues into the “right kind” of document.
/// </summary>
public enum ClueCategory
{
    Language,
    Material,
    Politics,
    Technology,
    Currency,
    Geography,
    Culture
}

/// <summary>
/// A single clue line that can be injected into documents.
/// Supports era logic (supports/contradicts) + optional upgrade gating.
/// </summary>
[CreateAssetMenu(fileName = "Clue_", menuName = "TimeDesk/Clue", order = 2)]
public sealed class ClueSO : ScriptableObject
{
    /// <summary>Text shown to the player.</summary>
    [Header("Text shown to player")]
    [TextArea]
    public string text;

    /// <summary>Document routing category.</summary>
    [Header("Classification")]
    public ClueCategory category;

    /// <summary>How strong this clue is (1 minor, 3 major).</summary>
    [Tooltip("How strong this clue is (1 minor, 3 major)")]
    [Range(1, 3)]
    public int strength = 1;

    /// <summary>Eras this clue supports (i.e., consistent with that era).</summary>
    [Header("Era logic")]
    public EraSO[] supports;

    /// <summary>Eras this clue contradicts (i.e., inconsistent with that era).</summary>
    public EraSO[] contradicts;

    /// <summary>Optional: requires an upgrade to reveal this clue.</summary>
    [Header("Optional gating")]
    public UpgradeSO requiresUpgradeToReveal;
}
