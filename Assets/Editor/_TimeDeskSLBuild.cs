// TEMPORARY automation for SL (never committed): Import Sound List, Build Home UI from disk twice (saved) compared by semantic dump, Title rebuilt unsaved against its committed scene.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class _TimeDeskSLBuild
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\d63602d5-a5a2-4440-a4b8-42c6496b21ab\scratchpad\run7\SL\build";
    private static string _report;
    private static int _fails;
    private static void Report(string line) => File.AppendAllText(_report, line + "\n");
    private static void Check(bool ok, string what)
    {
        Report((ok ? "PASS " : "FAIL ") + what);
        if (!ok)
            _fails++;
    }

    public static void Build() => Run("build_report.txt", true);

    /// <summary>The player scripts compile and the EditMode suite (TimeDeskEditMode) through TestRunnerApi.</summary>
    public static void Pass()
    {
        Directory.CreateDirectory(Scratch);
        string report = Path.Combine(Scratch, "pass.txt");
        File.WriteAllText(report, "start " + DateTime.Now.ToString("s") + Environment.NewLine);
        SessionState.SetString("SLPass.Report", report);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var settings = new UnityEditor.Build.Player.ScriptCompilationSettings { target = BuildTarget.StandaloneWindows64, group = BuildTargetGroup.Standalone };
        var result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, Path.Combine(Path.GetTempPath(), "TimeDeskSLPlayerScripts"));
        var built = result.assemblies != null ? result.assemblies.ToList() : new List<string>();
        File.AppendAllText(report, (built.Contains("Assembly-CSharp.dll") ? "PASS" : "FAIL") + $" player scripts compile: {built.Count} assemblies" + Environment.NewLine);
        var api = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
        api.RegisterCallbacks(new PassCallbacks());
        api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter { testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode, assemblyNames = new[] { "TimeDeskEditMode" } }));
    }

    private sealed class PassCallbacks : UnityEditor.TestTools.TestRunner.Api.ICallbacks
    {
        private static void Line(string l) => File.AppendAllText(SessionState.GetString("SLPass.Report", Path.Combine(Scratch, "pass.txt")), l + Environment.NewLine);
        public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor testsToRun) { }
        public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            Line($"tests passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} state={result.ResultState}");
            Line("done " + DateTime.Now.ToString("s"));
        }
        public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }
        public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            if (!result.HasChildren && result.TestStatus == UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed)
                Line($"FAIL {result.FullName}: {result.Message}");
        }
    }

    public static void Verify() => Run("verify_report.txt", false);

    private static void Run(string file, bool save)
    {
        Directory.CreateDirectory(Scratch);
        _report = Path.Combine(Scratch, file);
        _fails = 0;
        File.WriteAllText(_report, "start " + DateTime.Now.ToString("s") + "\n");
        try
        {
            string art = FileHash("Assets/Scenes/OfficeScene.unity"), gameplay = FileHash("Assets/Scenes/OfficeGameplay.unity");
            if (save)
                RunMenu("Tools/Audio/Import Sound List");
            One("title", "Assets/Scenes/TitleScene.unity", "Tools/TimeDesk/Build Title UI (Panels + Wiring)", false);
            One("home", "Assets/Scenes/HomeScene.unity", "Tools/TimeDesk/Build Home UI (Panels + Wiring)", save);
            AssetDatabase.SaveAssets();
            Check(FileHash("Assets/Scenes/OfficeScene.unity") == art && FileHash("Assets/Scenes/OfficeGameplay.unity") == gameplay, "the office scenes are byte-unchanged");
        }
        catch (Exception e)
        {
            Check(false, "EXCEPTION " + e);
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Report($"fails={_fails}");
            Report("done " + DateTime.Now.ToString("s"));
        }
    }

    private static void One(string tag, string scene, string menu, bool save)
    {
        EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        List<string> committed = DumpScene();
        File.WriteAllLines(Path.Combine(Scratch, tag + "_committed.txt"), committed);
        List<string> l1 = RunMenu(menu);
        if (!save)
        {
            List<string> once = DumpScene();
            Check(!l1.Any(IsError), tag + ": the builder logs no error: [" + string.Join(" | ", l1.Where(IsError).Take(30)) + "]");
            Compare(tag + ": the committed scene against its rebuild (identical)", committed, once, 40);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return;
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        List<string> r1 = DumpScene();
        List<string> l2 = RunMenu(menu);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        List<string> r2 = DumpScene();
        File.WriteAllLines(Path.Combine(Scratch, tag + "_rebuilt1.txt"), r1);
        File.WriteAllLines(Path.Combine(Scratch, tag + "_rebuilt2.txt"), r2);
        Check(!l1.Concat(l2).Any(IsError), tag + ": the builder logs no error: [" + string.Join(" | ", l1.Concat(l2).Where(IsError).Take(30)) + "]");
        Compare(tag + ": a rebuild against a second rebuild (idempotent, from disk)", r1, r2, 40);
        Report($"{tag}: committed {committed.Count} lines, rebuilt {r1.Count}; {r1.Except(committed).Count()} added, {committed.Except(r1).Count()} removed");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void Compare(string what, List<string> a, List<string> b, int show)
    {
        List<string> sa = a.OrderBy(l => l, StringComparer.Ordinal).ToList(), sb = b.OrderBy(l => l, StringComparer.Ordinal).ToList();
        List<string> onlyA = sa.Except(sb).ToList(), onlyB = sb.Except(sa).ToList();
        Check(onlyA.Count == 0 && onlyB.Count == 0 && a.SequenceEqual(b), $"{what}: {a.Count} vs {b.Count} lines, the same set, same order {a.SequenceEqual(b)}");
        foreach (string l in onlyA.Take(show))
            Report("   - " + l);
        foreach (string l in onlyB.Take(show))
            Report("   + " + l);
    }

    private static List<string> DumpScene()
    {
        var lines = new List<string>();
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            Dump(root.transform, lines);
        return lines;
    }

    private static void Dump(Transform t, List<string> lines)
    {
        string path = PathOf(t);
        lines.Add($"{path} active={t.gameObject.activeSelf} layer={t.gameObject.layer} tag={t.tag} sibling={t.GetSiblingIndex()}");
        if (t is RectTransform rt)
            lines.Add($"{path} rect a=({rt.anchorMin.x:0.###},{rt.anchorMin.y:0.###})-({rt.anchorMax.x:0.###},{rt.anchorMax.y:0.###}) p={rt.anchoredPosition} s={rt.sizeDelta} pv={rt.pivot}");
        foreach (Component c in t.GetComponents<Component>())
        {
            if (c == null)
            {
                lines.Add($"{path} MISSING SCRIPT");
                continue;
            }
            string type = c.GetType().Name;
            lines.Add($"{path} :{type}{(c is Behaviour b ? " enabled=" + b.enabled : "")}");
            if (c is TMP_Text text)
                lines.Add($"{path} :{type} text='{text.text}' size={text.fontSize:0.##}");
            string asm = c.GetType().Assembly.GetName().Name;
            if (c is MonoBehaviour && (asm == "Assembly-CSharp" || asm.StartsWith("TimeDesk")))
            {
                var so = new SerializedObject(c);
                SerializedProperty p = so.GetIterator();
                bool enter = true;
                while (p.NextVisible(enter))
                {
                    enter = true;
                    if (p.propertyPath == "m_Script")
                        continue;
                    string value = p.propertyType switch
                    {
                        SerializedPropertyType.ObjectReference => Ref(p.objectReferenceValue),
                        SerializedPropertyType.Integer => p.intValue.ToString(),
                        SerializedPropertyType.Boolean => p.boolValue.ToString(),
                        SerializedPropertyType.Float => p.floatValue.ToString("0.###"),
                        SerializedPropertyType.String => "'" + p.stringValue + "'",
                        SerializedPropertyType.Enum => p.enumValueIndex.ToString(),
                        _ => null
                    };
                    if (value != null)
                        lines.Add($"{path} :{type}.{p.propertyPath}={value}");
                }
            }
        }
        foreach (Transform child in t)
            Dump(child, lines);
    }

    private static string Ref(Object o)
    {
        if (o == null)
            return "null";
        if (o is Component c)
            return PathOf(c.transform) + ":" + c.GetType().Name;
        if (o is GameObject g)
            return g.scene.IsValid() ? PathOf(g.transform) : AssetDatabase.GetAssetPath(g) + ":" + g.name;
        string asset = AssetDatabase.GetAssetPath(o);
        return (string.IsNullOrEmpty(asset) ? "(runtime)" : asset) + ":" + o.name;
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    private static bool IsError(string l) => l.StartsWith("[Error]") || l.StartsWith("[Exception]") || l.StartsWith("[Assert]");

    private static List<string> RunMenu(string menu)
    {
        var lines = new List<string>();
        Application.LogCallback capture = (message, stack, type) => lines.Add($"[{type}] {message}");
        Application.logMessageReceived += capture;
        bool ran;
        try
        {
            ran = EditorApplication.ExecuteMenuItem(menu);
        }
        finally
        {
            Application.logMessageReceived -= capture;
        }
        Report($"menu '{menu}' ran={ran} ({lines.Count} log lines)");
        foreach (string l in lines.Where(l => !l.StartsWith("[Log]") || l.Contains("World generated") || l.Contains("no issues")))
            Report("  " + (l.Length > 600 ? l.Substring(0, 600) + "..." : l));
        if (!ran)
            lines.Add("[Error] the menu did not run: " + menu);
        return lines;
    }

    private static string FileHash(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using var sha = SHA1.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes));
    }
}

