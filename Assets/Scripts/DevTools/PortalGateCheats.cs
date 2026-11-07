#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS
using UnityEngine;

/// <summary>
/// The hall gates' cheats in the cheat menu's "More (other tracks)"
/// (DevCheats.Register; the night of 2026-11-07, DECISIONS D6): "Hall: portals
/// open" opens every ring's event horizon whatever the board or the day says,
/// or gives the rings back to them (AnimeHallPortalLink.AllOpen); "Hall:
/// kawoosh" plays the kawoosh on every ring whose event horizon shows
/// (PortalEffect.Send), for review captures of the hall's portals.
/// </summary>
public static class PortalGateCheats
{
    /// <summary>Registers the cheats once, at startup.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        DevCheats.Register("Hall: portals open", run =>
        {
            AnimeHallPortalLink.AllOpen = !AnimeHallPortalLink.AllOpen;
            Debug.Log("[PortalGateCheats] " + (AnimeHallPortalLink.AllOpen ? "Every hall ring shows its event horizon." : "The hall rings follow the board and the day again."));
        });
        DevCheats.Register("Hall: kawoosh", run =>
        {
            int sent = 0;
            foreach (PortalEffect effect in Object.FindObjectsByType<PortalEffect>())
                if (effect.Showing)
                {
                    effect.Send();
                    sent++;
                }
            Debug.Log($"[PortalGateCheats] A kawoosh on {sent} ring(s) (only a ring whose event horizon shows plays one).");
        });
    }
}
#endif
