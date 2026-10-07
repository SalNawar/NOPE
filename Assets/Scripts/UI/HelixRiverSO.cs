using UnityEngine;

/// <summary>
/// The Helix River's tuning (the river that shows timeline stability without a
/// number): how fast it follows a change, how wild each kind of damage gets as
/// stability falls to the firing line, the citation pulse's length and the
/// Reduced Motion speed. One asset (Assets/Data/Config/HelixRiver_Default.asset,
/// made by Build Office UI when missing) that every HelixRiverMonitor reads; an
/// Inspector knob, never written by Generate World.
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/Helix River", fileName = "HelixRiver_Default")]
public sealed class HelixRiverSO : ScriptableObject
{
    /// <summary>The river's knobs (HelixRiverKnobs; lengths in the reference glass's pixels).</summary>
    public HelixRiverKnobs knobs = new HelixRiverKnobs();
}
