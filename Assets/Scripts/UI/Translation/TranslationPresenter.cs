using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Today's speech translation for the transcript and the wheel (piece 9;
/// papers are always English), one per office day: for each traveller,
/// whether their claimed place's tongue is
/// foreign (TranslationDay, read from the day-start snapshot) and how it
/// shows. Each tongue's look is built once: its parsed table and direction,
/// and its script's OS font through the session's RuntimeFonts (piece 6),
/// resolved once per script with every cell of every tongue in that script.
/// When the table does not parse, no font draws the script, or the culture
/// service is not running, the tongue shows in the fallback cipher in the
/// text's own font, and one warning names the script.
/// </summary>
public sealed class TranslationPresenter
{
    private readonly TranslationDay _day;
    private readonly TranslationSettings _settings;
    private readonly Dictionary<string, (ForeignText text, TMP_FontAsset font)> _looks = new Dictionary<string, (ForeignText, TMP_FontAsset)>();
    private readonly HashSet<string> _warned = new HashSet<string>();

    /// <summary>A presenter for one day's translation and the library's settings.</summary>
    public TranslationPresenter(TranslationDay day, TranslationSettings settings)
    {
        _day = day;
        _settings = settings ?? new TranslationSettings();
    }

    /// <summary>A traveller's translation: None unless their claimed place's tongue is foreign today.</summary>
    public CaseTranslation ForCase(CaseInstance inst)
    {
        string id = inst != null ? inst.tongueId : null;
        if (_day == null || !_day.Foreign(id))
            return CaseTranslation.None;

        Tongue tongue = _day.TongueOf(id);
        TranslatorPack pack = _day.PackOf(tongue);
        (ForeignText text, TMP_FontAsset font) = Look(tongue);
        string placeholder = UiText.Format("compare.untranslated", tongue.displayName, pack != null ? pack.displayName : tongue.pack);
        return new CaseTranslation(new SpeechTranslation(text, _day.Translated(id), _settings.flip, MotionPreference.Reduced, placeholder), font,
                                   tongue.id, tongue.displayName);
    }

    /// <summary>
    /// What a script's font must draw: every cell of every tongue written in
    /// it, upper-cased where the case rule shows it so, plus a..z and A..Z
    /// (the flipped English letters).
    /// </summary>
    private static string ScriptSample(TranslationSettings settings, string scriptId)
    {
        var sb = new StringBuilder("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        foreach (Tongue t in settings?.rules?.tongues ?? new List<Tongue>())
        {
            if (t == null || t.Native || t.script != scriptId)
                continue;
            IReadOnlyList<string> table = Pseudoscript.ParseTable(t.glyphs, out _);
            if (table == null)
                continue;
            foreach (string cell in table)
            {
                sb.Append(cell);
                if (cell.Length == 1)
                    sb.Append(char.ToUpperInvariant(cell[0]));
            }
        }
        return sb.ToString();
    }

    /// <summary>A tongue's look, built once: its table and script font, or the fallback cipher in the text's own font (warned once per script).</summary>
    private (ForeignText text, TMP_FontAsset font) Look(Tongue tongue)
    {
        if (_looks.TryGetValue(tongue.id, out var look))
            return look;

        TranslationScript script = _settings.GetScript(tongue.script);
        IReadOnlyList<string> table = Pseudoscript.ParseTable(tongue.glyphs, out string problem);
        RuntimeFonts fonts = CultureThemeService.Instance != null ? CultureThemeService.Instance.Fonts : null;
        string why;
        if (table == null || script == null)
        {
            why = table == null ? $"its table does not parse ({problem})" : $"its script '{tongue.script}' is not in the library";
        }
        else if (fonts == null)
        {
            why = "the culture theme service that loads the OS fonts is not running";
        }
        else
        {
            RuntimeFonts.Result font = fonts.Resolve("script:" + script.id, script.fonts, ScriptSample(_settings, script.id));
            if (font.Asset != null && font.Covers)
            {
                look = (new ForeignText(table, script.rightToLeft), font.Asset);
                _looks[tongue.id] = look;
                return look;
            }
            why = $"no font draws the {script.id} script (tried: {font.Tried}; missing {font.Missing})";
        }

        if (_warned.Add(tongue.script ?? string.Empty))
            Debug.LogWarning($"[Translation] {why}: {tongue.displayName} shows in the fallback cipher. Install one of the fonts or add a candidate in world_source.json translation.scripts, then run Tools > TimeDesk > Generate World.");
        IReadOnlyList<string> fallback = Pseudoscript.ParseTable(_settings.fallbackGlyphs, out _);
        look = (new ForeignText(fallback, false), null);
        _looks[tongue.id] = look;
        return look;
    }
}
