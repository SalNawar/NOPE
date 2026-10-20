using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The dead-config check (BUG-73a: "a test that every SO field has a
/// reader"): the scanner finds serialized ScriptableObject fields and their
/// readers on synthetic sources, and on the project every unread field is on
/// the cleanup allowlist (tools/qa/so_reader_allowlist.txt), so a new dead
/// knob fails here. Runs outside Unity (set QA_PROJECT to the project folder
/// for the project scan).
/// </summary>
public class SoFieldReadersTests
{
    private const string Config = @"
using UnityEngine;
public class BaseConfigSO : ScriptableObject
{
    public float inherited = 1f;
}
public sealed class DemoConfigSO : BaseConfigSO
{
    [Header(""Speed"")] [Range(0f, 1f)]
    public float readSpeed = 0.5f;          // read below
    public float deadKnob = 2f;             // nothing reads it
    [SerializeField] private int privateRead = 3;
    [SerializeField] private int privateDead = 4;
    private int notSerialized = 5;
    public static float staticOne = 6f;
    public const float ConstOne = 7f;
    [System.NonSerialized] public float nonSerialized;
    public readonly int readOnly = 8;
    public float WrittenOnly;
    public System.Collections.Generic.List<string> items = new System.Collections.Generic.List<string> { ""a"", ""b"" };
    public float Property { get; set; }
    public float Expression => readSpeed;
    public int Total() { return privateRead + notSerialized; }
#if UNITY_EDITOR
    public float editorOnlyRead;
#endif
    [System.Serializable] public class Nested { public float nestedField; }
    [System.Serializable] public class Knobs { public float usedKnob = 1f, deadSubKnob = 2f; }
    public Knobs knobs = new Knobs();
}
public class NotAnSO : MonoBehaviour { public float deadKnobOnBehaviour; }
";

    private const string User = @"
public static class User
{
    public static void Use(DemoConfigSO c)
    {
        // c.deadKnob is mentioned only in this comment
        string s = ""deadKnob"";
        string t = $""{c.items.Count} {c.editorOnlyRead}"";
        c.WrittenOnly = 3f;
        float x = c.inherited;
        float k = c.knobs.usedKnob;
    }
}
";

    private static SoFieldReaders.Scan RunSynthetic() =>
        SoFieldReaders.Run(new Dictionary<string, string> { ["Assets/Scripts/Demo.cs"] = Config, ["Assets/Scripts/User.cs"] = User });

    [Test]
    public void FindsTheSOClasses_ThroughTheirBases()
    {
        SoFieldReaders.Scan scan = RunSynthetic();
        CollectionAssert.AreEquivalent(new[] { "BaseConfigSO", "DemoConfigSO" }, scan.Classes);
    }

    [Test]
    public void FollowsTheSerializableTypesAnSOHolds_OnlyThose()
    {
        CollectionAssert.AreEquivalent(new[] { "Knobs" }, RunSynthetic().HeldTypes);
    }

    [Test]
    public void FindsOnlyTheSerializedFields()
    {
        var names = RunSynthetic().Fields.Select(f => f.Name).ToList();
        CollectionAssert.AreEquivalent(new[] { "inherited", "readSpeed", "deadKnob", "privateRead", "privateDead", "WrittenOnly", "items", "editorOnlyRead", "knobs", "usedKnob", "deadSubKnob" }, names);
    }

    [Test]
    public void FlagsTheFieldsNothingReads_IgnoringCommentsStringsAndPlainWrites()
    {
        var unread = RunSynthetic().Unread.Select(f => f.Key).ToList();
        CollectionAssert.AreEquivalent(new[] { "DemoConfigSO.deadKnob", "DemoConfigSO.privateDead", "DemoConfigSO.WrittenOnly", "Knobs.deadSubKnob" }, unread);
    }

    [Test]
    public void Clean_BlanksCommentsAndStrings_ButKeepsInterpolationHoles()
    {
        string c = SoFieldReaders.Clean("a // b\nx = \"q\" + $\"{y} z\" + @\"w\"\"v\";");
        Assert.AreEqual("a     \nx = " + new string(' ', 3) + " + " + "  {y}   " + " + " + new string(' ', 7) + ";", c);
    }

    [Test]
    public void Allowlist_ParsesKeysAndComments()
    {
        CollectionAssert.AreEquivalent(new[] { "A.b", "C.d" }, SoFieldReaders.ParseAllowlist("# head\nA.b   # owner\n\nC.d\n"));
    }

    /// <summary>The project folder: QA_PROJECT outside Unity, else the editor's project.</summary>
    public static string ProjectRoot()
    {
        string env = Environment.GetEnvironmentVariable("QA_PROJECT");
        if (!string.IsNullOrEmpty(env))
            return env;
        try
        {
            return GateResults.ProjectRoot;
        }
        catch (Exception)
        {
            Assert.Ignore("Outside Unity: set QA_PROJECT to the project folder to scan it.");
            return null;
        }
    }

    [Test]
    public void Project_EveryUnreadSOField_IsOnTheCleanupAllowlist()
    {
        string root = ProjectRoot();
        SoFieldReaders.Scan scan = SoFieldReaders.Run(SoFieldReaders.Sources(Path.Combine(root, "Assets")));
        Assert.Greater(scan.Classes.Count, 10, "the scan finds the project's SO classes");
        string allowPath = Path.Combine(root, "tools", "qa", "so_reader_allowlist.txt");
        HashSet<string> allow = SoFieldReaders.ParseAllowlist(File.Exists(allowPath) ? File.ReadAllText(allowPath) : string.Empty);
        var fresh = scan.Unread.Where(f => !allow.Contains(f.Key)).Select(f => f.ToString()).ToList();
        Assert.IsEmpty(fresh, "SO fields nothing reads (delete them, read them, or put them on INTEGRATION's cleanup allowlist): " + string.Join(", ", fresh));
    }
}
