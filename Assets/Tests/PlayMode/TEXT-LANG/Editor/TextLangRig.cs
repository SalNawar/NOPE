using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TEXT-LANG's scenario ops on QA-RIG's rig (FIX_PLAN TEXT-LANG verification,
/// "language takeover"; FIX_PLAN_v2: "0 culture-language strings outside
/// Paper surfaces and signs, and 0 on any PC surface"). Registered in the
/// editor, because textLangAudit reads the screen through TextLangAudit
/// (Assembly-CSharp-Editor), so the scenarios that use them are editor plays
/// and no built-player check reads them. textLangChoice stores the player's
/// language choice (none, Follow history, Always English) and applies it at
/// once; textLangAudit asserts the takeover grep at that moment (and, when
/// asked, which language the papers read); textLangWrongCall makes a wrong
/// call so a citation slip, Mail's copy and the visit record exist;
/// textLangApp opens a PC app; textLangWheel opens or closes the wheel.
/// </summary>
public static class TextLangRig
{
    /// <summary>The owner every assertion names.</summary>
    private const string Owner = "TEXT-LANG";

    /// <summary>SessionState keys (they outlive play mode's domain reload): whether the choice before the first textLangChoice was kept, and that choice.</summary>
    private const string KeptKey = "TextLangRig.kept", BeforeKey = "TextLangRig.before";

    /// <summary>A stored value that means "no choice" in <see cref="BeforeKey"/>.</summary>
    private const string NoChoice = "<none>";

    /// <summary>Registers the ops in the editor (the scenarios' edit-mode test reads them, and play mode's domain reload registers them again).</summary>
    [InitializeOnLoadMethod]
    private static void Register()
    {
        QaRunner.RegisterOp("textLangChoice", Choice);
        QaRunner.RegisterOp("textLangAudit", Audit);
        QaRunner.RegisterOp("textLangWrongCall", WrongCall);
        QaRunner.RegisterOp("textLangApp", App);
        QaRunner.RegisterOp("textLangWheel", Wheel);
    }

    /// <summary>The PlayerPrefs name of the player's language choice (UiLanguagePreference's own key, through PlayerPrefKeys.For).</summary>
    private static string PrefName()
    {
        FieldInfo key = typeof(UiLanguagePreference).GetField("Key", BindingFlags.NonPublic | BindingFlags.Static)
                        ?? throw new MissingFieldException(nameof(UiLanguagePreference), "Key");
        return PlayerPrefKeys.For((string)key.GetRawConstantValue());
    }

    /// <summary>
    /// text "none" (no stored choice), "history" (Follow history), "english"
    /// (Always English) or "restore" (the choice before this session's first
    /// textLangChoice); the papers follow at once (CultureThemeService.RefreshActive).
    /// </summary>
    private static IEnumerator Choice(QaRunner runner, QaStep step)
    {
        string name = PrefName();
        if (!SessionState.GetBool(KeptKey, false))
        {
            SessionState.SetString(BeforeKey, PlayerPrefs.HasKey(name) ? PlayerPrefs.GetString(name) : NoChoice);
            SessionState.SetBool(KeptKey, true);
        }

        switch ((step.text ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "none":
                PlayerPrefs.DeleteKey(name);
                break;
            case "history":
                UiLanguagePreference.Choose(false);
                break;
            case "english":
                UiLanguagePreference.Choose(true);
                break;
            case "restore":
                string before = SessionState.GetString(BeforeKey, NoChoice);
                if (before == NoChoice)
                    PlayerPrefs.DeleteKey(name);
                else
                    PlayerPrefs.SetString(name, before);
                SessionState.EraseBool(KeptKey);
                SessionState.EraseString(BeforeKey);
                break;
            default:
                throw new ArgumentException($"textLangChoice: text '{step.text}' is not none, history, english or restore");
        }
        PlayerPrefs.Save();
        CultureThemeService.RefreshActive();
        Debug.Log($"[TextLangRig] Language choice '{step.text}': stored '{UiLanguagePreference.Stored ?? "none"}'.");
        yield return null;
    }

    /// <summary>The moment's language state, for the assertions and the log.</summary>
    private static string State()
    {
        CultureThemeService s = CultureThemeService.Instance;
        return $"day {QaGame.Day()}, culture '{(s != null ? s.ActiveCultureId ?? "none" : "no service")}', papers {(s != null ? s.Language.ToString() : "-")}, " +
               $"culture day {CultureThemeService.LanguageLocked}, choice '{UiLanguagePreference.Stored ?? "none"}', culture labels shown {(s != null && s.Strings != null ? s.Strings.Phrases.Count : 0)}";
    }

    /// <summary>
    /// Asserts takeover.&lt;name&gt;: no text on the chrome or the PC holds a
    /// culture label (TextLangAudit.CultureOnPlayerSurfaces). With text
    /// "culture" or "english" it also asserts takeover.papers.&lt;name&gt;:
    /// the papers read the world's language (LabelLanguage.Culture) or not.
    /// </summary>
    private static IEnumerator Audit(QaRunner runner, QaStep step)
    {
        yield return null;
        List<string> hits = TextLangAudit.CultureOnPlayerSurfaces();
        string state = State();
        runner.Assert($"takeover.{step.name}", Owner, hits.Count == 0, $"{hits.Count} culture text(s) on the chrome or the PC; {state}",
            hits.Count == 0 ? "the takeover grep (BUG-3a): no culture label on a player-language surface" : string.Join(" | ", hits.Take(12)));

        string want = (step.text ?? string.Empty).Trim().ToLowerInvariant();
        if (want == "culture" || want == "english")
        {
            CultureThemeService s = CultureThemeService.Instance;
            bool culture = s != null && s.Language == LabelLanguage.Culture;
            runner.Assert($"takeover.papers.{step.name}", Owner, culture == (want == "culture"), $"papers {(s != null ? s.Language.ToString() : "no service")}; {state}",
                want == "culture" ? "the papers and the newspaper read the world's language (the culture day; Follow history or no choice: D7, R2)"
                                  : "the papers and the newspaper read English (before the culture day, or Always English)");
        }
        Debug.Log($"[TextLangRig] {step.name}: {state}; {hits.Count} culture text(s) on the chrome or the PC{(hits.Count > 0 ? ": " + string.Join(" | ", hits) : string.Empty)}");
    }

    /// <summary>
    /// A wrong call, so the citation slip opens and Mail's copy and the visit
    /// record are written: each traveller who should be let in is decided
    /// correctly and the next one waited for (f: the timeout per traveller,
    /// 60 s), until one who should be turned away is let in (n: at most that
    /// many travellers, 6). The stamp is the desk's own verdict path
    /// (InvestigationUIController.Decide), not a drag.
    /// </summary>
    private static IEnumerator WrongCall(QaRunner runner, QaStep step)
    {
        float timeout = step.f > 0f ? step.f : 60f;
        int most = step.n > 0 ? step.n : 6;
        for (int i = 0; i < most; i++)
        {
            yield return QaGame.Until(QaGame.TravellerAtDesk, timeout, "a traveller at the desk");
            GameManager game = GameManager.Current ?? throw new InvalidOperationException("no GameManager");
            CaseInstance inst = game.ActiveCase ?? throw new InvalidOperationException("no active case");
            var ui = (InvestigationUIController)QaGame.Field(game, "investigationUI");
            if (inst.ShouldAccept)
            {
                Debug.Log($"[TextLangRig] '{inst.visitorDisplayName}' should be let in: decided correctly, next traveller.");
                ui.Decide(DeskStamp.Approved);
                yield return QaGame.Until(() => !QaGame.TravellerAtDesk() || game.ActiveCase != inst, timeout, "the traveller to leave");
                continue;
            }
            Debug.Log($"[TextLangRig] '{inst.visitorDisplayName}' should be turned away: let in (a wrong call).");
            ui.Decide(DeskStamp.Approved);
            yield return QaGame.Frames(2);
            yield break;
        }
        throw new InvalidOperationException($"textLangWrongCall: no traveller to turn away among the next {most}");
    }

    /// <summary>Opens the PC app named by text (a DesktopAppIds id) on the open PC.</summary>
    private static IEnumerator App(QaRunner runner, QaStep step)
    {
        DesktopApps apps = QaGame.Find<DesktopApps>() ?? throw new InvalidOperationException("no DesktopApps");
        apps.OpenApp(step.text);
        yield return QaGame.Frames(10);
    }

    /// <summary>Opens (n 1) or closes (n 0) the traveller's wheel.</summary>
    private static IEnumerator Wheel(QaRunner runner, QaStep step)
    {
        TravellerWheel wheel = QaGame.Find<TravellerWheel>() ?? throw new InvalidOperationException("no TravellerWheel");
        if (step.n == 1)
        {
            yield return QaGame.Until(() => wheel.CanOpen, step.f > 0f ? step.f : 20f, "the wheel to be allowed");
            wheel.Open();
        }
        else
        {
            wheel.Close();
        }
        yield return QaGame.Frames(10);
    }
}
