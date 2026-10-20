using UnityEngine;

/// <summary>
/// Departures: an accepted traveller's portal announced for the hall.
/// Carved out of GameManager.cs by FLOW (pure move; FIX_PLAN_v2 FLOW delta,
/// R7 PHASEB LEASES.md). Phase B holder: PORTALS.
/// </summary>
public sealed partial class GameManager
{
    /// <summary>Raised when an accepted traveller leaves, with the portal they leave through (PortalDay.DepartureFor; the hall's rings pulse it, VX4).</summary>
    public event System.Action<int> Departed;

    /// <summary>An accepted traveller leaves through today's portal for them (the displaced by the Return Gate once repaired, else 01; a citizen by the open route to their destination, else 01): the portal is announced for the hall's pulse; none when the hall has no portals.</summary>
    private void AnnounceDeparture(CaseInstance inst)
    {
        if (_today == null || inst.claimedNation == null || inst.claimedEra == null)
            return;
        int portal = _today.Portals.DepartureFor(new PlaceRef(inst.claimedNation.id, inst.claimedEra.id), inst.kind == TravellerKind.Displaced);
        if (portal <= 0)
            return;
        Debug.Log($"[GameManager] Case {_activeCaseIndex1Based} leaves through portal {PortalText.Number(portal)}.");
        Departed?.Invoke(portal);
    }
}
