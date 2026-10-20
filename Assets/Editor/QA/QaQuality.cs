using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Editor and player quality parity (BUG-53: "the editor renders at quality
/// 0 (frozen SSAO, half-res) while the player ships at Ultra"). Reads
/// ProjectSettings/QualitySettings.asset as text (pure, unit-tested): the
/// editor's current level, the levels' names and the Standalone default the
/// Windows player boots with. QA plays at the shipping level, and the gate
/// fails when the editor's level is not the player's.
/// </summary>
public static class QaQuality
{
    /// <summary>The quality settings file, relative to the project.</summary>
    public const string AssetPath = "ProjectSettings/QualitySettings.asset";

    /// <summary>What the file says.</summary>
    public sealed class Levels
    {
        /// <summary>The editor's current level (m_CurrentQuality).</summary>
        public int Current = -1;

        /// <summary>The Standalone player's default level (m_PerPlatformDefaultQuality: Standalone), -1 when absent.</summary>
        public int StandaloneDefault = -1;

        /// <summary>The levels' names, in index order.</summary>
        public readonly List<string> Names = new List<string>();

        /// <summary>The name of level <paramref name="i"/>, or "#i" when unnamed.</summary>
        public string Name(int i) => i >= 0 && i < Names.Count ? Names[i] : "#" + i;

        /// <summary>True when the editor plays at the Standalone player's level.</summary>
        public bool Match => Current >= 0 && Current == StandaloneDefault;
    }

    /// <summary>Parses QualitySettings.asset's text.</summary>
    public static Levels Parse(string yaml)
    {
        var levels = new Levels();
        Match current = Regex.Match(yaml, @"^\s*m_CurrentQuality:\s*(\d+)", RegexOptions.Multiline);
        if (current.Success)
            levels.Current = int.Parse(current.Groups[1].Value);
        Match list = Regex.Match(yaml, @"m_QualitySettings:\s*\n(?<body>(?:[ \t-].*\n)*)");
        if (list.Success)
            foreach (Match name in Regex.Matches(list.Groups["body"].Value, @"^[ ]{4}name:[ ]*(.+?)[ ]*$", RegexOptions.Multiline))
                levels.Names.Add(name.Groups[1].Value);
        Match defaults = Regex.Match(yaml, @"m_PerPlatformDefaultQuality:\s*\n(?<body>(?:[ \t]+.*\n?)*)");
        if (defaults.Success)
        {
            Match standalone = Regex.Match(defaults.Groups["body"].Value, @"^[ ]+Standalone:[ ]*(\d+)", RegexOptions.Multiline);
            if (standalone.Success)
                levels.StandaloneDefault = int.Parse(standalone.Groups[1].Value);
        }
        return levels;
    }

    /// <summary>Reads the project's file (under <paramref name="projectRoot"/>).</summary>
    public static Levels Read(string projectRoot) => Parse(File.ReadAllText(Path.Combine(projectRoot, AssetPath)).Replace("\r\n", "\n"));

    /// <summary>One line: the editor's level against the player's.</summary>
    public static string Describe(Levels l) =>
        $"editor {l.Name(l.Current)} ({l.Current}), Standalone player {l.Name(l.StandaloneDefault)} ({l.StandaloneDefault})";
}
