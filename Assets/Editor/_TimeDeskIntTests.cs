#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Local integration automation, never committed: runs the EditMode suite (optionally filtered) and writes Library/QA/int_tests.txt.</summary>
internal static class _TimeDeskIntTests
{
    private static string Out => Path.Combine(Directory.GetCurrentDirectory(), "Library", "QA", "int_tests.txt");
    private static string FilterFile => Path.Combine(Directory.GetCurrentDirectory(), "Library", "QA", "int_tests_filter.txt");

    /// <summary>Runs every EditMode test (or the groups named one per line in int_tests_filter.txt).</summary>
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Out));
        File.WriteAllText(Out, "running\n");
        var filter = new Filter { testMode = TestMode.EditMode };
        if (File.Exists(FilterFile))
        {
            string[] groups = File.ReadAllLines(FilterFile);
            if (groups.Length > 0)
                filter.groupNames = groups;
        }
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
        api.Execute(new ExecutionSettings(filter));
    }

    private sealed class Callbacks : ICallbacks
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private int _pass, _fail, _skip, _inc;

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            _sb.AppendLine($"DONE passed={_pass} failed={_fail} skipped={_skip} inconclusive={_inc} state={result.ResultState}");
            File.WriteAllText(Out, _sb.ToString());
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren)
                return;
            switch (result.TestStatus)
            {
                case TestStatus.Passed: _pass++; break;
                case TestStatus.Failed:
                    _fail++;
                    _sb.AppendLine($"FAIL {result.FullName}: {result.Message}\n{result.StackTrace}");
                    break;
                case TestStatus.Skipped: _skip++; break;
                default: _inc++; break;
            }
        }
    }
}
#endif
