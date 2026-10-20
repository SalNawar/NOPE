using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a day-changing cheat's route (RunRoutes: a skip, a jump, a restart)
/// on a world with play's own day steps, up to the morning it lands on
/// (RUN-SAFE, BUG-28a: "cheats route through the same close and night code
/// as play"): today's shift closes as played (the live shift on its own
/// ledger, else an empty one; DayCycle.EndShift) with the ending check play
/// runs after it, a day jumped over is played with its whole queue decided
/// correctly (DayCycle.PlayShiftCorrectly), Home's night on the default care
/// (DayCycle.HomeNight), Sleep (DayCycle.Sleep: the endings before and after
/// the night), a jump back's new world (the run's seed, its pet adopted
/// afresh), a restart's morning snapshot. RunManager plays it, then saves and
/// opens the morning; the ledger tests play it headless and compare it with
/// the same steps played by hand. Stops at the first ending.
/// </summary>
public static class CheatRoute
{
    /// <summary>
    /// Plays <paramref name="route"/> on <paramref name="world"/> (replaced by
    /// a new world or a morning snapshot on the way) with
    /// <paramref name="run"/>'s library and tuning, up to its OpenMorning step
    /// (not played: RunManager's landing). <paramref name="closeLive"/> closes
    /// the live shift on its ledger (GameManager.CloseShiftForCheat) at the
    /// route's first close of its starting day (null: none is running);
    /// <paramref name="morning"/> gives today's morning snapshot (a restart);
    /// <paramref name="endOfShift"/> gives the end-of-shift save a route
    /// starting at Home begins its night from (SaveSystem.Load), so a Home
    /// arrival already charged in memory is never charged twice. Returns the
    /// ending reached on the way, or null when the route landed at a morning.
    /// </summary>
    public static EndingSO Play(ref WorldState world, IReadOnlyList<RunStep> route, RunConfigSO run,
                                Func<bool> closeLive = null, Func<WorldState> morning = null, Func<WorldState> endOfShift = null)
    {
        ContentLibrarySO lib = run != null ? run.contentLibrary : null;
        GameConfigSO config = run != null ? run.gameConfig : null;
        int startDay = world.day;
        bool fromHome = world.phase == RunPhase.Home;

        foreach (RunStep step in route)
        {
            world.cheated = true;
            EndingSO ending = null;
            switch (step)
            {
                case RunStep.CloseShift:
                    bool moved = closeLive != null && world.day == startDay
                        ? closeLive()
                        : DayCycle.EndShift(world, new ShiftLedger(), Array.Empty<CaseInstance>(), null, lib, config);
                    closeLive = null;
                    ending = moved && config != null ? EndingService.Evaluate(world, lib, config, EndingMoment.Immediate) : null;
                    break;
                case RunStep.PlayShift:
                    ending = DayCycle.PlayShiftCorrectly(world, lib, config);
                    break;
                case RunStep.HomeNight:
                    if (fromHome)
                        world = EndOfShift(world, endOfShift);
                    fromHome = false;
                    DayCycle.HomeNight(world, lib, config);
                    break;
                case RunStep.Sleep:
                    ending = DayCycle.Sleep(world, lib, config);
                    break;
                case RunStep.NewWorld:
                    world = Replay(world, run, lib);
                    closeLive = null;
                    break;
                case RunStep.RestoreMorning:
                    world = (morning != null ? morning() : null) ?? MorningFallback(world);
                    closeLive = null;
                    break;
                case RunStep.OpenMorning:
                    world.cheated = true;
                    return null;
            }

            if (ending != null)
            {
                world.cheated = true;
                Debug.Log($"[CheatRoute] The route ended the run at day {world.day}: '{ending.id}' ({ending.displayName}).");
                return ending;
            }
        }
        world.cheated = true;
        return null;
    }

    /// <summary>The end-of-shift save when it is <paramref name="world"/>'s day at Home with no ending, else <paramref name="world"/> as it stands (logged).</summary>
    private static WorldState EndOfShift(WorldState world, Func<WorldState> endOfShift)
    {
        WorldState saved = endOfShift != null ? endOfShift() : null;
        if (saved != null && saved.day == world.day && saved.phase == RunPhase.Home && string.IsNullOrEmpty(saved.endingId))
        {
            saved.cheated = true;
            return saved;
        }
        Debug.LogWarning($"[CheatRoute] No end-of-shift save for day {world.day}; Home's night runs on the world in memory.");
        return world;
    }

    /// <summary>A jump back's new world (DayCycle.NewWorld on the run's seed), with the pet's kind, name and coat adopted afresh on day 1: never a later day's pet (BUG-27).</summary>
    private static WorldState Replay(WorldState world, RunConfigSO run, ContentLibrarySO lib)
    {
        PetState pet = world.pet;
        WorldState fresh = DayCycle.NewWorld(run, lib, world.runSeed);
        if (pet != null && pet.Adopted)
            DayCycle.Adopt(fresh, lib, pet.kind, pet.name, pet.coat);
        fresh.cheated = true;
        return fresh;
    }

    /// <summary>A restart with no morning snapshot of today (a run continued from before the snapshots): the shift from the world as it stands, its ending lifted (the old restart), logged.</summary>
    private static WorldState MorningFallback(WorldState world)
    {
        Debug.LogWarning($"[CheatRoute] No morning snapshot of day {world.day}; the shift restarts from the world as it stands.");
        world.phase = RunPhase.Office;
        world.endingId = string.Empty;
        return world;
    }
}
