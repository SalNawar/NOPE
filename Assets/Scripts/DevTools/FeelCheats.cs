#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS
using UnityEngine;

/// <summary>
/// The game feel's cheats in the cheat menu's "More (other tracks)"
/// (DevCheats.Register): "Feel: intensity" steps the Motion intensity
/// through 100, 50 and 0 % (the player's setting until Settings shows a
/// slider: MotionPreference.Intensity); "Feel: stamp", "Feel: citation",
/// "Feel: famous" and "Feel: breach" fire that moment's hit-stop, camera
/// impulse and sound (FeelDirector.Punch) without touching the run.
/// </summary>
public static class FeelCheats
{
    /// <summary>Registers the cheats once, at startup.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        DevCheats.Register("Feel: intensity", run =>
        {
            float now = MotionPreference.Intensity;
            MotionPreference.Intensity = now > 0.75f ? 0.5f : now > 0.25f ? 0f : 1f;
            Debug.Log($"[FeelCheats] Motion intensity {MotionPreference.Intensity * 100f:0} %.");
        });
        DevCheats.Register("Feel: stamp", run => FeelDirector.Punch(FeelHit.Stamp));
        DevCheats.Register("Feel: citation", run => FeelDirector.Punch(FeelHit.Citation));
        DevCheats.Register("Feel: famous", run => FeelDirector.Punch(FeelHit.Famous));
        DevCheats.Register("Feel: breach", run => FeelDirector.Punch(FeelHit.Breach));
    }
}
#endif
