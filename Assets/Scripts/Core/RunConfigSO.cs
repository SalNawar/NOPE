// ReSharper disable InconsistentNaming
using System.Collections.Generic;
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

    /// <summary>Game cursor + hover outline look, used in every scene (see InteractionFeedbackBootstrap).</summary>
    public InteractionFeedbackSO interactionFeedback;

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
    /// <summary>The art office (owned by the art side); loading it loads the gameplay layer on top (OfficeScenes).</summary>
    public string officeSceneName = "OfficeScene";

    /// <summary>The office's gameplay layer (the shift loop, the PC, the desk, the traveller), loaded additively on the art office.</summary>
    public string officeGameplaySceneName = "OfficeGameplay";

    /// <summary>Where the gameplay layer finds its places in the art office, and the art office's leftover gameplay objects it switches off.</summary>
    public OfficeSceneContractSO officeContract;

    /// <summary>Scene containing the home phase (expenses/shop/slot).</summary>
    public string homeSceneName = "HomeScene";

    /// <summary>Scene containing the title screen (Continue/New Run + ending display).</summary>
    public string titleSceneName = "TitleScene";

    [Header("Determinism")]
    /// <summary>If non-zero, every new run uses this seed (useful for testing). 0 = random.</summary>
    public int fixedRunSeed = 0;

    [Header("Home / Family")]
    /// <summary>Family member names seeded into a new run (condition starts at 0).</summary>
    public List<string> startingFamilyMembers = new() { "Partner", "Kid" };
}
