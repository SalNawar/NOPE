#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS
using UnityEngine;

/// <summary>
/// The desk machine's cheat in the cheat menu's "More (other tracks)"
/// (DevCheats.Register; the desk machine spec §2, Decision: a scripted
/// law-breaker exists early enough to try DETAIN): "Law-breaker next" makes
/// the next generated traveller carry someone else's photo (DevToolsState.ForcedLie,
/// consumed by CaseFactory) and starts today's shift over, so the first
/// traveller who can carry it is a law-breaker.
/// </summary>
public static class LawBreakerCheats
{
    /// <summary>Registers the cheat once, at startup.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => DevCheats.Register("Law-breaker next", ForceLawBreaker);

    /// <summary>Forces someone else's photo (a false identity: Law.Breaks) on the next generated traveller and starts the shift over (from the next shift outside the office).</summary>
    private static void ForceLawBreaker(RunManager run)
    {
        DevToolsState.ForcedLie = LieKind.SwappedPhoto;
        bool inShift = Object.FindFirstObjectByType<GameManager>() != null;
        Debug.Log($"[LawBreakerCheats] The next generated traveller carries someone else's photo{(inShift ? ": today's shift starts over." : ", from the next shift.")}");
        if (inShift && run != null)
            run.RestartShift();
    }
}
#endif
