// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Boot configuration for a run. Must live at Assets/Resources/RunConfig.asset
/// so RunManager can load it with Resources.Load("RunConfig").
/// </summary>
[CreateAssetMenu(fileName = "RunConfig", menuName = "TimeDesk/Run Config", order = 1)]
public sealed class RunConfigSO : ScriptableObject
{
    /// <summary>Content library used by all runtime systems.</summary>
    public ContentLibrarySO contentLibrary;

    /// <summary>Gameplay tuning (pay, citations, stability).</summary>
    public GameConfigSO gameConfig;

    [Header("Starting values")]
    /// <summary>Money the player starts a new run with.</summary>
    public int startingMoney = 50;

    /// <summary>Stability the player starts a new run with (0..100).</summary>
    [Range(0f, 100f)]
    public float startingStability = 100f;

    /// <summary>First day of a new run (1-based).</summary>
    [Min(1)]
    public int startingDay = 1;

    [Header("Scenes")]
    /// <summary>Scene containing the office/shift loop.</summary>
    public string officeSceneName = "OfficeScene";

    /// <summary>Scene containing the home phase (expenses/shop/slot).</summary>
    public string homeSceneName = "HomeScene";

    [Header("Determinism")]
    /// <summary>If non-zero,