using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// LIGHT's P0-A shader seams: DeskAnime.shader's _PaintedHighlight and
/// _InkRim (STAMP sets them; declared in Properties and in the
/// UnityPerMaterial buffer, default 0, read by nothing yet) and
/// Shaders/Include/DeskLight.hlsl (the global tint and level, and DeskLight()
/// returning 1). The text tests read the source; the "NeedsEditor" tests read
/// the imported shader and include, and run only inside Unity.
/// </summary>
public class LightSeamTests
{
    private const string DeskAnime = "Assets/Art/Office/DeskClean/DeskAnime.shader";
    private const string DeskLight = "Assets/Shaders/Include/DeskLight.hlsl";
    private static readonly string[] SeamProperties = { "_PaintedHighlight", "_InkRim" };

    [Test]
    public void DeskAnime_DeclaresTheSeamProperties_InPropertiesAndTheMaterialBuffer()
    {
        string source = File.ReadAllText(FromRoot(DeskAnime));
        string properties = Regex.Match(source, @"Properties\s*\{(.*?)\n\s*\}", RegexOptions.Singleline).Groups[1].Value;
        string buffer = Regex.Match(source, @"CBUFFER_START\(UnityPerMaterial\)(.*?)CBUFFER_END", RegexOptions.Singleline).Groups[1].Value;
        Assert.IsNotEmpty(properties, "the Properties block");
        Assert.IsNotEmpty(buffer, "the UnityPerMaterial buffer");
        foreach (string name in SeamProperties)
        {
            StringAssert.IsMatch(name + @"\(""[^""]*"",\s*Range\(0,\s*1\)\)\s*=\s*0\b", properties, $"{name} in Properties, default 0");
            StringAssert.IsMatch(@"\b" + name + @"\b", buffer, $"{name} in UnityPerMaterial");
        }
    }

    [Test]
    public void DeskLight_DeclaresTheGlobals_AndTheStubReturnsOne()
    {
        string source = File.ReadAllText(FromRoot(DeskLight));
        StringAssert.Contains("float4 _DeskLightTint;", source);
        StringAssert.Contains("float _DeskLightLevel;", source);
        StringAssert.IsMatch(@"half3 DeskLight\(\)\s*\{\s*return half3\(1, 1, 1\);\s*\}", source);
    }

    [Test, Category("NeedsEditor")]
    public void DeskAnime_ImportsWithTheSeamProperties()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(DeskAnime);
        Assert.IsNotNull(shader, DeskAnime);
        Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "DeskAnime compiles");
        foreach (string name in SeamProperties)
        {
            int index = shader.FindPropertyIndex(name);
            Assert.GreaterOrEqual(index, 0, name);
            Assert.AreEqual(0f, shader.GetPropertyDefaultFloatValue(index), name);
        }
    }

    [Test, Category("NeedsEditor")]
    public void DeskLight_ImportsAsAShaderInclude()
    {
        Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(DeskLight), DeskLight);
    }

    /// <summary>A project file, found from this file's folder (so it is this checkout's under Unity and outside it alike).</summary>
    private static string FromRoot(string projectPath, [CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", projectPath);
}
