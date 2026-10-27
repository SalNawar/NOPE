#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Local job only (never committed): desk v3 R9's shader compile check. Writes reports\desk_v3_unity\r13\shaders.txt.</summary>
internal static class _DeskV3R13Checks
{
    private const string Out = @"E:\unity\NOPE-tools\reports\desk_v3_unity\r13\shaders.txt";

    public static void Shaders()
    {
        var sb = new StringBuilder();
        bool all = true;
        foreach (string name in new[] { "TimeDesk/HallBackdrop", "NOPE/Hall Foreground Floor", "NOPE/Desk CRT Glass", "TimeDesk/PlanarScreen", "NOPE/Desk Painted" })
        {
            Shader shader = Shader.Find(name);
            if (shader == null)
            {
                sb.AppendLine($"{name}: NOT FOUND");
                all = false;
                continue;
            }
            // Compile every variant the pass has for this platform (ShaderUtil.CompilePass is internal; the import result is what ShaderHasError reads).
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(shader), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            shader = Shader.Find(name);
            bool error = ShaderUtil.ShaderHasError(shader);
            all &= !error;
            sb.AppendLine($"{name}: path {AssetDatabase.GetAssetPath(shader)} ShaderHasError {error} supported {shader.isSupported} passes {shader.passCount}");
            foreach (ShaderMessage m in ShaderUtil.GetShaderMessages(shader))
                sb.AppendLine($"  {m.severity} {m.platform} line {m.line}: {m.message} {m.messageDetails}");
        }
        sb.AppendLine(all ? "PASS" : "FAIL");
        File.WriteAllText(Out, sb.ToString());
        Debug.Log("[DeskV3R13] " + sb);
    }
}
#endif
