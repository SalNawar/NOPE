using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

/// <summary>
/// QA-RIG's own play tests (P0-B "the harness self-tests"): the virtual
/// mouse's P-* drags reach the event system within 1 px on an empty scene,
/// and the smoke day plays clean. Explicit (each enters play mode for
/// minutes): run them by name, or by the job runner's QaJobs.SelfTest and
/// QaJobs.Smoke, which play the same scenarios.
/// </summary>
public class QaRigPlayTests : QaPlayTest
{
    [UnityTest, Explicit, Category("QaPlay")]
    public IEnumerator SelfTest_EveryPathLandsWithin1Px()
    {
        Assert.IsTrue(Prepare("selftest.json", string.Empty, 5f));
        yield return new EnterPlayMode();
        yield return Play();
        yield return new ExitPlayMode();
        AssertRun(Finish());
    }

    [UnityTest, Explicit, Category("QaPlay")]
    public IEnumerator SmokeDay_PlaysClean()
    {
        Assert.IsTrue(Prepare("smoke_day1.json"));
        yield return new EnterPlayMode();
        yield return Play();
        yield return new ExitPlayMode();
        AssertRun(Finish());
    }
}
