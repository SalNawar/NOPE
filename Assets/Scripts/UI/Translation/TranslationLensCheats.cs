#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Translation Lens's cheats in the cheat menu's "More (other tracks)"
/// (DevCheats.Register; Saleh 2026-10-06: test the lens at once): grant the
/// lens up to Word, Sentence or Object (TranslationLens.Grant into the owned
/// upgrades), take it away, and lock or unlock the office's language now
/// (CultureThemeService.LockOverride; "follow the ramp" clears it). The
/// labels are re-applied at once; the lens reads its level on the next
/// pointer move.
/// </summary>
public static class TranslationLensCheats
{
    /// <summary>Registers the cheats once, at startup.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        DevCheats.Register("Lens: Word", run => Grant(run, LensReach.Word));
        DevCheats.Register("Lens: Sentence", run => Grant(run, LensReach.Sentence));
        DevCheats.Register("Lens: Object", run => Grant(run, LensReach.Object));
        DevCheats.Register("Lens: remove", Remove);
        DevCheats.Register("Language: lock now", run => Lock(true));
        DevCheats.Register("Language: unlock", run => Lock(false));
        DevCheats.Register("Language: follow the ramp", run => Lock(null));
    }

    /// <summary>Owns the lens's levels up to <paramref name="level"/>.</summary>
    private static void Grant(RunManager run, LensReach level)
    {
        if (run == null || run.World == null || run.Library == null)
            return;
        int added = TranslationLens.Grant(level, run.World.unlockedUpgradeIds, run.Library.Translation.lens.rules.levelIds);
        Debug.Log($"[TranslationLensCheats] Lens up to {level}: {added} level(s) granted; the lens now reads {TranslationLensPresenter.CurrentReach}.");
    }

    /// <summary>Takes every owned lens level away (from the lens's day the Bureau's Word lens still reads).</summary>
    private static void Remove(RunManager run)
    {
        if (run == null || run.World == null || run.Library == null)
            return;
        List<string> ids = run.Library.Translation.lens.rules.levelIds;
        run.World.unlockedUpgradeIds.RemoveAll(ids.Contains);
        Debug.Log($"[TranslationLensCheats] Lens levels removed; the lens now reads {TranslationLensPresenter.CurrentReach}.");
    }

    /// <summary>Locks (true) or unlocks (false) the language now, or returns it to the ramp (null), and re-applies the labels.</summary>
    private static void Lock(bool? locked)
    {
        CultureThemeService.LockOverride = locked;
        CultureThemeService.RefreshActive();
        Debug.Log($"[TranslationLensCheats] Language {(locked == null ? "follows the ramp" : locked.Value ? "locked" : "unlocked")}: locked now {CultureThemeService.LanguageLocked}.");
    }
}
#endif
