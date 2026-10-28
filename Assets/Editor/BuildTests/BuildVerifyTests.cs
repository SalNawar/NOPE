using System.IO;
using NUnit.Framework;

/// <summary>
/// BLD-02 (audit 2026-10-28): a player Unity calls "Succeeded" is still
/// refused when its data has a hole, when the build's log has write-failure
/// asserts, when it does not boot, or when a drive is too full to build on.
/// </summary>
public class BuildVerifyTests
{
    private const long Min = 1024;

    [Test]
    public void ZeroRuns_FindsAHoleWithItsStartAndLength()
    {
        byte[] data = Filled(10000);
        System.Array.Clear(data, 3000, 2500);
        var runs = BuildVerify.ZeroRuns(new MemoryStream(data), Min);
        Assert.AreEqual(1, runs.Count);
        Assert.AreEqual(3000, runs[0].Start);
        Assert.AreEqual(2500, runs[0].Length);
    }

    [Test]
    public void ZeroRuns_IgnoresShortRuns()
    {
        byte[] data = Filled(10000);
        System.Array.Clear(data, 3000, (int)Min - 1);
        Assert.IsEmpty(BuildVerify.ZeroRuns(new MemoryStream(data), Min));
    }

    [Test]
    public void ZeroRuns_FindsAHoleAtTheEndOfTheFile()
    {
        byte[] data = Filled(10000);
        System.Array.Clear(data, 8000, 2000);
        var runs = BuildVerify.ZeroRuns(new MemoryStream(data), Min);
        Assert.AreEqual(1, runs.Count);
        Assert.AreEqual(8000, runs[0].Start);
        Assert.AreEqual(2000, runs[0].Length);
    }

    [Test]
    public void ZeroRuns_FindsAHoleAcrossTheReadBuffer()
    {
        byte[] data = Filled((4 << 20) + 10000);
        System.Array.Clear(data, (4 << 20) - 1500, 3000);
        var runs = BuildVerify.ZeroRuns(new MemoryStream(data), Min);
        Assert.AreEqual(1, runs.Count);
        Assert.AreEqual((4 << 20) - 1500, runs[0].Start);
        Assert.AreEqual(3000, runs[0].Length);
    }

    [Test]
    public void ZeroRuns_WholeDataHasNone()
    {
        Assert.IsEmpty(BuildVerify.ZeroRuns(new MemoryStream(Filled(50000)), Min));
    }

    [Test]
    public void WriteFailures_CountsEachAssert()
    {
        string log = "Building...\nAssertion failed on expression: 'success && actual == size'\nok\nAssertion failed on expression: 'success && actual == size'\n";
        Assert.AreEqual(2, BuildVerify.WriteFailures(log));
        Assert.AreEqual(0, BuildVerify.WriteFailures("Build succeeded"));
        Assert.AreEqual(0, BuildVerify.WriteFailures(null));
    }

    [Test]
    public void SpaceProblem_RefusesANearFullDrive()
    {
        Assert.IsNotNull(BuildVerify.SpaceProblem("E:", 6L << 30, BuildVerify.MinFreeBytes));
        Assert.IsNull(BuildVerify.SpaceProblem("E:", 600L << 30, BuildVerify.MinFreeBytes));
    }

    [Test]
    public void BootProblem_PassesOnlyATitleThatStaysUp()
    {
        string good = "Initialize engine\n" + TitleSceneController.ReadyLine + "\n";
        Assert.IsNull(BuildVerify.BootProblem(good, true, true));
        Assert.IsNotNull(BuildVerify.BootProblem(good, true, false));
        Assert.IsNotNull(BuildVerify.BootProblem("Initialize engine\n", false, false));
    }

    [TestCase("The referenced script (Unknown) on this Behaviour is missing!")]
    [TestCase("Crash!!!")]
    [TestCase("Mismatched serialization in the builtin class 'Sprite'. (Read 224 bytes but expected 468 bytes)")]
    public void BootProblem_RefusesABrokenPlayerEvenAtTheTitle(string line)
    {
        string log = line + "\n" + TitleSceneController.ReadyLine + "\n";
        Assert.IsNotNull(BuildVerify.BootProblem(log, true, true));
    }

    private static byte[] Filled(int length)
    {
        var data = new byte[length];
        for (int i = 0; i < length; i++)
            data[i] = (byte)(i % 251 + 1);
        return data;
    }
}
