using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Day 1 built through the real CaseFactory from ContentLibrary_Main (first-case-clean follow-up). It lives in
/// Assembly-CSharp-Editor because the EditMode assembly cannot see CaseFactory.
/// </summary>
public class FirstCaseSeedsTests
{
    [Test]
    public void DayOne_Case1_ShouldAccept_For200RunSeeds()
    {
        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>("Assets/Data/Content Library/ContentLibrary_Main.asset");
        var run = Resources.Load<RunConfigSO>("RunConfig");
        Assert.IsNotNull(lib, "ContentLibrary_Main");
        Assert.IsNotNull(run, "RunConfig");
        for (int seed = 1; seed <= 200; seed++)
        {
            WorldState world = DayCycle.NewWorld(run, lib, seed);
            Assert.AreEqual(1, world.day, $"seed {seed}: a new run starts on day 1");
            DayPlanSO plan = lib.GetDayPlan(world.day);
            InterviewDay interview = TimelineService.BuildInterviewDay(lib, world, new ShiftLedger());
            TodaysWorld today = lib.BuildToday(plan, world);
            List<CaseInstance> cases = new CaseFactory(lib, today, true).GenerateDayCases(plan, world, Seeds.Day(world.runSeed, world.day), interview, true);
            Assert.IsNotEmpty(cases, $"seed {seed}");
            Assert.IsTrue(cases[0].ShouldAccept, $"seed {seed}: Case 1 must be an accept ({cases[0].FaultReason})");
        }
    }
}
