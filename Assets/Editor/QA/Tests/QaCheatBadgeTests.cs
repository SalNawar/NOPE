using System.IO;
using NUnit.Framework;

/// <summary>
/// No dev label in a review capture (desk v3 round 9: the day-12 still carried
/// "CHEATS ON" after the scenario's GiveAllItems): while the rig drives the
/// game the cheated run's badge is not drawn (DevToolsState.HideCheatBadge,
/// read by DebugPanelController), a scenario's New Run does not clear that
/// (ResetAll keeps it), and the rig puts it back when the run ends. Runs
/// outside Unity (QA_PROJECT for the sources).
/// </summary>
public class QaCheatBadgeTests
{
    [Test]
    public void ResetAll_KeepsTheRigsBadgeSwitch()
    {
        bool before = DevToolsState.HideCheatBadge;
        try
        {
            DevToolsState.HideCheatBadge = true;
            DevToolsState.ResetAll();
            Assert.IsTrue(DevToolsState.HideCheatBadge, "a scenario's newRun (RunManager.NewRun calls ResetAll) must not bring the badge back into its captures");
        }
        finally
        {
            DevToolsState.HideCheatBadge = before;
        }
    }

    [Test]
    public void TheRig_HidesTheBadgeWhileItPlays_AndPutsItBack_AndThePanelObeys()
    {
        string root = SoFieldReadersTests.ProjectRoot();
        string runner = File.ReadAllText(Path.Combine(root, "Assets/Tests/PlayMode/Rig/QaRunner.cs"));
        StringAssert.Contains("DevToolsState.HideCheatBadge = true;", runner, "QaRunner.Run hides it before the first step");
        StringAssert.Contains("DevToolsState.HideCheatBadge = _badgeWasHidden;", runner, "QaRunner.CleanUp puts it back");
        string panel = File.ReadAllText(Path.Combine(root, "Assets/Scripts/DevTools/DebugPanelController.cs"));
        StringAssert.Contains("if (Cheated && !DevToolsState.HideCheatBadge)", panel, "the badge is drawn only when the rig does not hide it");
    }
}
