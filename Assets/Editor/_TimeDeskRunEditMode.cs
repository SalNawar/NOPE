#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Local job only (never committed): runs the EditMode suite and writes Library/ClaudeJobs/editmode_result.txt.</summary>
internal static class _TimeDeskRunEditMode
{
    private static readonly string OutPath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "ClaudeJobs", "editmode_result.txt");

    public static void Run()
    {
        if (File.Exists(OutPath))
            File.Delete(OutPath);
        // An open, modified scene makes the test runner ask to save it (a modal dialog): start from an empty scene.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
    }

    private sealed class Callbacks : ICallbacks
    {
        private readonly StringBuilder _failed = new StringBuilder();
        private int _pass, _fail, _skip, _inc;

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            File.WriteAllText(OutPath, $"passed {_pass} failed {_fail} skipped {_skip} inconclusive {_inc} duration {result.Duration:0.0}s\n{_failed}");
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.IsSuite)
                return;
            switch (result.TestStatus)
            {
                case TestStatus.Passed: _pass++; break;
                case TestStatus.Failed:
                    _fail++;
                    _failed.AppendLine($"FAILED {result.Test.FullName}: {result.Message}");
                    break;
                case TestStatus.Skipped: _skip++; break;
                default: _inc++; break;
            }
        }
    }
}
#endif
