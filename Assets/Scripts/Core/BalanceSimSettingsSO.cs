using UnityEngine;

/// <summary>
/// The balance simulation's own knobs (days 7-15 X3, Saleh's Q11; Tools >
/// TimeDesk > Balance). An asset Generate World never writes
/// (Assets/Data/Config/BalanceSimSettings.asset): Saleh edits it in the
/// Inspector. Read by BalanceSimulation only; the game never reads it.
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/Balance Sim Settings", fileName = "BalanceSimSettings")]
public sealed class BalanceSimSettingsSO : ScriptableObject
{
    /// <summary>The asset's path.</summary>
    public const string AssetPath = "Assets/Data/Config/BalanceSimSettings.asset";

    /// <summary>
    /// The travellers a careful clerk gets through in a shift before the clock
    /// closes (8 real minutes at about 45 s a traveller: 10). The simulation
    /// plays every style twice, on the whole queue and at this pace (the rest
    /// of the queue goes home, PlayPolicy.Reaches), and reports the world
    /// each style leaves at day 15 at this pace. 0: the whole queue only.
    /// </summary>
    [Min(0)] public int travellersPerShift = 10;

    /// <summary>
    /// The House buyer's reserve (the Home upgrades spec HU10, §9): the buyer
    /// runs treat and buy at Home only while the wallet keeps this many credits
    /// after paying (60: about a night's household), so upkeep never walks it
    /// into bankruptcy on purpose.
    /// </summary>
    [Min(0)] public int houseReserve = 60;

    /// <summary>The condition at which the House buyer treats a family member (one point a treatment, the sickest first, while the reserve holds).</summary>
    [Min(1)] public int careThreshold = 3;
}
