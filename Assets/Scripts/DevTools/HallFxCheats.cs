#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS
using UnityEngine;

/// <summary>
/// HALL-FX's cheat in the cheat menu's "More (other tracks)" (DevCheats.Register):
/// "Hall FX: dust" hides or shows the hall's dust motes (the particle renderers
/// under every HallLightingRig; the rig still runs them, nothing it drives is
/// changed). HALL-FX's flicker sequences (tools/qa/scenarios/hall_fx_*.json) run
/// with the dust hidden: the dust's shimmer (BUG-13's "dust shimmer", VIS-9) is
/// LIGHT's, which measures it at its own merge with the dust shown, so neither
/// gate waves the other's pixels through.
/// </summary>
public static class HallFxCheats
{
    /// <summary>True while the cheat hides the dust.</summary>
    public static bool DustHidden { get; private set; }

    /// <summary>Registers the cheat once, at startup.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => DevCheats.Register("Hall FX: dust", run => ToggleDust());

    /// <summary>Hides the dust if shown, shows it if hidden.</summary>
    private static void ToggleDust()
    {
        DustHidden = !DustHidden;
        int count = 0;
        foreach (HallLightingRig rig in Object.FindObjectsByType<HallLightingRig>())
            foreach (ParticleSystemRenderer dust in rig.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                dust.enabled = !DustHidden;
                count++;
            }
        Debug.Log($"[HallFxCheats] The hall's dust {(DustHidden ? "hidden" : "shown")} ({count} emitter(s)).");
    }
}
#endif
