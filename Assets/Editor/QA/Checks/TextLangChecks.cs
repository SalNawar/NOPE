using System.Linq;
using UnityEngine;

/// <summary>
/// TEXT-LANG's gate checks (BUG-3a, BUG-38a): the shipped OfficeGameplay
/// scene's fonts, the kit atlases, and a play's takeover grep. Written at
/// INTEGRATION from docs/pending/TEXT-LANG.md once QA-RIG was on main; the
/// takeover check reads the textlang_takeover scenarios' takeover.*
/// assertions (TextLangRig's textLangAudit: no culture text on the chrome or
/// the PC over days 7-9, and the papers in the language each day calls for).
/// </summary>
public static class TextLangChecks
{
    /// <summary>OfficeGameplay sets no text in LiberationSans (BUG-38a).</summary>
    [GateCheck("textlang.officeGameplay.liberationSans", "TEXT-LANG")]
    public static GateResult OfficeGameplayFonts()
    {
        var files = TextLangAudit.LiberationSansInFiles(GateResults.ProjectRoot).Where(f => f.StartsWith("Assets/Scenes/OfficeGameplay.unity")).ToList();
        return GateResult.Of("textlang.officeGameplay.liberationSans", "TEXT-LANG", files.Count == 0, $"{files.Count} file(s)", string.Join(", ", files));
    }

    /// <summary>Every kit face draws today's English, German and Italian letters, digits and punctuation itself (BUG-38a, CODE-83). Symbols are not read here (orchestrator ruling, 2026-10-21; KitFontAtlases.IsChecked): see textlang.kitSymbols.</summary>
    [GateCheck("textlang.kitAtlases", "TEXT-LANG")]
    public static GateResult KitAtlases()
    {
        var gaps = TextLangAudit.KitAtlasGaps(Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath).contentLibrary);
        return GateResult.Of("textlang.kitAtlases", "TEXT-LANG", gaps.Count == 0, $"{gaps.Count} face(s) with gaps", string.Join(" | ", gaps));
    }

    /// <summary>The symbols today's strings print that a kit face's font has no glyph for (U+25BA, U+25CF, U+25B2, U+2212 at TEXT-LANG's merge): the fallback draws them. Known red until CONTENT swaps the glyphs or a symbol face joins the kit (ruling, 2026-10-21).</summary>
    [GateCheck("textlang.kitSymbols", "TEXT-LANG")]
    public static GateResult KitSymbols()
    {
        var gaps = TextLangAudit.KitSymbolGaps(Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath).contentLibrary);
        return GateResult.Of("textlang.kitSymbols", "TEXT-LANG", gaps.Count == 0, $"{gaps.Count} face(s) lack symbols", string.Join(" | ", gaps));
    }

    /// <summary>The takeover grep: 0 culture text on the chrome or the PC over days 7-9, with no stored choice and with Follow history (BUG-3a).</summary>
    [GateCheck("textlang.play.takeover", "TEXT-LANG", GateKind.Play)]
    public static GateResult Takeover() => GateResults.Require("textlang.play.takeover", "TEXT-LANG", "textlang_takeover*.takeover.*");
}
