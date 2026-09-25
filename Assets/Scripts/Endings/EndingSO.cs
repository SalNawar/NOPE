// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// A possible run outcome ("fired", "bankrupt", "you survived to retirement",
/// an attribute epilogue). EndingService.Evaluate checks every EndingSO in the
/// content library each time an ending check runs; EndingRules.Select picks:
/// failures at any check, and at the day boundary the milestone (Retirement),
/// which an attribute ending whose total is reached replaces as its epilogue.
/// </summary>
[CreateAssetMenu(fileName = "Ending_", menuName = "TimeDesk/Endings/Ending", order = 30)]
public sealed class EndingSO : ScriptableObject
{
    /// <summary>Stable ID stored in WorldState.endingId.</summary>
    public string id;

    /// <summary>Designer-facing title shown on the title/ending screen.</summary>
    public string displayName;

    /// <summary>Flavor text shown on the ending screen.</summary>
    [TextArea]
    public string bodyText;

    /// <summary>Which condition this ending checks for.</summary>
    public EndingConditionType conditionType;

    /// <summary>Target attribute (AttrTotalAtLeast only).</summary>
    public AttributeSO attribute;

    /// <summary>Numeric threshold (AttrTotalAtLeast / DayAtLeast).</summary>
    public float threshold;

    /// <summary>
    /// When multiple endings match at once, the highest priority wins
    /// (ties resolved by content library order).
    /// </summary>
    public int priority;
}
