using NUnit.Framework;

/// <summary>
/// Quality parity (BUG-53: the editor rendered at Very Low while the player
/// ships at Ultra): the settings file is read right, and the project names
/// the Standalone player's level, which every QA run plays at (QaPlayHarness;
/// the run's quality assertion). The editor's own level is not gated. Runs
/// outside Unity (QA_PROJECT for the project file).
/// </summary>
public class QaQualityTests
{
    private const string Sample =
        "QualitySettings:\n" +
        "  m_ObjectHideFlags: 0\n" +
        "  serializedVersion: 5\n" +
        "  m_CurrentQuality: 2\n" +
        "  m_QualitySettings:\n" +
        "  - serializedVersion: 5\n" +
        "    name: Very Low\n" +
        "    pixelLightCount: 0\n" +
        "  - serializedVersion: 5\n" +
        "    name: Medium\n" +
        "    pixelLightCount: 1\n" +
        "  - serializedVersion: 5\n" +
        "    name: Ultra\n" +
        "    customRenderPipeline: {fileID: 0}\n" +
        "  m_TextureMipmapLimitGroupNames: []\n" +
        "  m_PerPlatformDefaultQuality:\n" +
        "    Android: 1\n" +
        "    Standalone: 2\n" +
        "    WebGL: 1\n";

    [Test]
    public void Parse_ReadsCurrentNamesAndTheStandaloneDefault()
    {
        QaQuality.Levels l = QaQuality.Parse(Sample);
        Assert.AreEqual(2, l.Current);
        Assert.AreEqual(2, l.StandaloneDefault);
        CollectionAssert.AreEqual(new[] { "Very Low", "Medium", "Ultra" }, l.Names);
        Assert.IsTrue(l.HasStandaloneLevel);
        Assert.AreEqual("editor Ultra (2), Standalone player Ultra (2)", QaQuality.Describe(l));
    }

    [Test]
    public void Parse_NoOrAMissingStandaloneLevel_IsNotPlayable()
    {
        Assert.IsFalse(QaQuality.Parse(Sample.Replace("    Standalone: 2\n", string.Empty)).HasStandaloneLevel);
        Assert.IsFalse(QaQuality.Parse(Sample.Replace("Standalone: 2", "Standalone: 7")).HasStandaloneLevel);
        Assert.IsTrue(QaQuality.Parse(Sample.Replace("m_CurrentQuality: 2", "m_CurrentQuality: 0")).HasStandaloneLevel, "the editor's own level does not matter");
    }

    [Test]
    public void Project_NamesThePlayersLevel()
    {
        QaQuality.Levels l = QaQuality.Read(SoFieldReadersTests.ProjectRoot());
        Assert.IsTrue(l.HasStandaloneLevel, QaQuality.Describe(l));
        Assert.AreEqual("Ultra", l.Name(l.StandaloneDefault), "the player ships at Ultra");
        Assert.AreEqual(6, l.Names.Count, "the six quality levels");
    }
}
