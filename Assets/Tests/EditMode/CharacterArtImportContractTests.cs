using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// BLD-05 (D11): the committed import settings of the character art match
/// CharacterArtImporter. A change of the importer's code alone reimports
/// nothing, so a checkout reimports the art to BC7 without mipmaps only when
/// its .meta files change; the meta writer of tools/characters must write the
/// same settings for new art. Reads the files as text (the importer is
/// editor code, outside this assembly).
/// </summary>
public sealed class CharacterArtImportContractTests
{
    private const string ImporterPath = "Assets/Editor/CharacterArtImporter.cs";
    private const string ArtFolder = "Assets/Art/Characters";
    private const string MetaWriter = "tools/characters/charkit/unitymeta.py";

    private static string Root([CallerFilePath] string here = "") =>
        File.Exists(ImporterPath) ? "." : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..");

    private static string Read(string relative) => File.ReadAllText(Path.Combine(Root(), relative)).Replace("\r\n", "\n");

    /// <summary>The importer's two D11 values, read from its source.</summary>
    private static (bool mipmaps, int maxSize) Importer()
    {
        string code = Read(ImporterPath);
        Match mips = Regex.Match(code, @"public const bool Mipmaps = (true|false);");
        Match max = Regex.Match(code, @"public const int MaxSize = (\d+);");
        Assert.IsTrue(mips.Success && max.Success, "CharacterArtImporter names Mipmaps and MaxSize as constants");
        return (mips.Groups[1].Value == "true", int.Parse(max.Groups[1].Value));
    }

    /// <summary>The settings a texture .meta must carry for the importer's values; empty when it carries them.</summary>
    private static string Off(string meta, bool mipmaps, int maxSize)
    {
        string text = meta.Replace("\r\n", "\n");
        Match platform = Regex.Match(text, @"buildTarget: DefaultTexturePlatform\n\s+maxTextureSize: (\d+)");
        string off = string.Empty;
        if (!text.Contains($"enableMipMap: {(mipmaps ? 1 : 0)}\n"))
            off += " mipmaps";
        if (!text.Contains($"filterMode: {(mipmaps ? 2 : 1)}\n"))
            off += " filter";
        if (!platform.Success || platform.Groups[1].Value != maxSize.ToString())
            off += $" max size {(platform.Success ? platform.Groups[1].Value : "none")}";
        return off.Trim();
    }

    [Test]
    public void TheMaxSize_ImportsThe2x3CanvasAtSidesBC7CanTake()
    {
        (_, int maxSize) = Importer();
        int width = (int)System.Math.Round(LookCanvas.Width * (double)maxSize / LookCanvas.Height, System.MidpointRounding.AwayFromZero);
        Assert.AreEqual(0, maxSize % 4, $"the canvas's long side at {maxSize}");
        Assert.AreEqual(0, width % 4, $"the canvas's short side at {width}");
    }

    [Test]
    public void EveryCharacterMeta_CarriesTheImportersSettings()
    {
        (bool mipmaps, int maxSize) = Importer();
        string[] metas = Directory.GetFiles(Path.Combine(Root(), ArtFolder), "*.png.meta", SearchOption.AllDirectories);
        Assert.Greater(metas.Length, 0, "character art .meta files");
        var off = metas.Select(m => (path: m, off: Off(File.ReadAllText(m), mipmaps, maxSize))).Where(m => m.off.Length > 0).ToList();
        Assert.IsEmpty(off, $"{off.Count} of {metas.Length} character .meta files differ from CharacterArtImporter " +
                            $"(mipmaps {mipmaps}, max size {maxSize}); first: " +
                            string.Join("; ", off.Take(3).Select(m => Path.GetFileName(m.path) + ": " + m.off)) +
                            ". Run Tools > TimeDesk > Reimport Character Art and commit the .meta files.");
    }

    [Test]
    public void TheImporter_HasAVersion_SoALiveEditorImportsTheArtAgain()
    {
        // VERIFY0 P1 (fix round 1): a live editor imports the changed .meta files with the old
        // importer before the new code loads, and that importer writes its own settings back.
        // Only a new GetVersion makes Unity import the art again once the new code has loaded.
        Match version = Regex.Match(Read(ImporterPath), @"public override uint GetVersion\(\) => (\d+);");
        Assert.IsTrue(version.Success, "CharacterArtImporter overrides GetVersion");
        Assert.GreaterOrEqual(int.Parse(version.Groups[1].Value), 1, "above the importer's version before D11 (0, no override)");
    }

    [Test]
    public void TheMetaWriter_WritesTheImportersSettings()
    {
        (bool mipmaps, int maxSize) = Importer();
        string template = Read(MetaWriter);
        Assert.IsTrue(template.Contains($"enableMipMap: {(mipmaps ? 1 : 0)}\n"), "unitymeta.py's mipmaps");
        Assert.IsTrue(template.Contains($"filterMode: {(mipmaps ? 2 : 1)}\n"), "unitymeta.py's filter");
        Match platform = Regex.Match(template, @"buildTarget: DefaultTexturePlatform\n\s+maxTextureSize: (\d+)");
        Assert.IsTrue(platform.Success, "unitymeta.py writes the default platform block");
        Assert.AreEqual(maxSize.ToString(), platform.Groups[1].Value, "unitymeta.py's max size");
    }
}
