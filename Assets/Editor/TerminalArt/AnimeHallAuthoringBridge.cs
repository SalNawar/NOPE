using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit, one-shot local authoring requests. Does not change UnitySkills permissions.
[InitializeOnLoad]
public static class AnimeHallAuthoringBridge
{
    const string Request = "Temp/AnimeHallAuthoring.request";
    static AnimeHallAuthoringBridge() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        var action = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try
        {
            if (action == "connect")
            {
                var server = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("UnitySkills.SkillsHttpServer"))
                    .FirstOrDefault(t => t != null);
                if (server == null) throw new InvalidOperationException("UnitySkills server assembly is unavailable.");
                server.GetMethod("Start").Invoke(null, new object[] { 0, false });
            }
            else if (action == "status")
            {
                var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                File.WriteAllText("Temp/AnimeHallAuthoring.status",
                    s.path + "\nDirty: " + s.isDirty + "\n" +
                    string.Join("\n", s.GetRootGameObjects().Select(g => g.name)));
            }
            else return;
            File.WriteAllText("Temp/AnimeHallAuthoring.result", action + " completed " + DateTime.UtcNow.ToString("o"));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            File.WriteAllText("Temp/AnimeHallAuthoring.result", e.ToString());
        }
    }
}
