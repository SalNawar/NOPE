using UnityEngine;

/// <summary>
/// The narrative workbook's reference run (Time Sorter > Narrative Workbook > Export):
/// the run seeds whose travellers fill its Cases sheet, and how the clerk plays them.
/// An asset Generate World never writes (Assets/Data/Config/NarrativeWorkbookSettings.asset):
/// Saleh edits it in the Inspector. Read by the export only; the game never reads it.
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/Narrative Workbook Settings", fileName = "NarrativeWorkbookSettings")]
public sealed class NarrativeWorkbookSettingsSO : ScriptableObject
{
    /// <summary>The asset's path.</summary>
    public const string AssetPath = "Assets/Data/Config/NarrativeWorkbookSettings.asset";

    /// <summary>
    /// The run seeds played, each a whole run of the whole queue: 12345 (the smoke
    /// play's and the balance simulation's example seed), then the simulation's first
    /// two run seeds (7919, 15838). More seeds show more of the generated variety.
    /// </summary>
    public int[] seeds = { 12345, 7919, 15838 };

    /// <summary>How the clerk decides (the balance simulation's styles): Perfect makes every correct call, so the story follows the path a careful player sees.</summary>
    public PlayStyle style = PlayStyle.Perfect;
}
