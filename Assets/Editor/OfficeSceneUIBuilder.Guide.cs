using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Build Office UI's guide part (the FTUE and the daily guide, Saleh
/// 2026-10-06): the prompt on the office overlay (GuidePrompt: a plate at the
/// top left with its header, line, Skip and Got it, and the arrow) and
/// the guide director (GuideDirector, Office/Guide), wired to the desk, inspect
/// mode, the stamp bar, the workbench, the rulebook, the office view and the
/// things its arrows point at, to GameManager and the office binder; and the
/// "Replay the desk tutorial" buttons of Settings and the F1 card. Rebuilt
/// fresh each run.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The name of the "Replay the desk tutorial" buttons (Settings, the F1 card) BuildGuide wires to the director.</summary>
    private const string GuideReplayButton = "ReplayTutorialButton";

    /// <summary>The prompt's plate (reference px) and its place from the overlay's top left corner (Saleh's follow-up: it never covers the papers or the rulebook in the reading view: the counter's papers lie round the centre line, the rulebook and the papers lower down, the stamp hint mirrors it at the top right); the arrow's size.</summary>
    private static readonly Vector2 GuidePlateSize = new Vector2(640f, 124f);
    private static readonly Vector2 GuidePlateAt = new Vector2(16f, -16f);
    private static readonly Vector2 GuideArrowSize = new Vector2(56f, 56f);

    /// <summary>
    /// The guide (the class summary): <paramref name="overlay"/> gets the
    /// prompt (last, over the PC frame), the Office root its director; every
    /// part is wired. The overlay controls the arrows point at are read from
    /// their owners (the inspect button, the stamp tab, the PC tab).
    /// </summary>
    private static void BuildGuide(Transform overlay, OfficeViewController view, DeskView deskView, DeskController desk, DeskInspect inspect, DeskStampTray stamps,
                                   DeskRulebook rulebook, OfficeControls controls, Clickable readySign, Transform calendar, DepartureBoardView board,
                                   DeskScanner scanner, DeskCounter counter, Clickable travellerZone, OfficeSceneBinder binder, GameManager game)
    {
        GuidePrompt prompt = BuildGuidePrompt(overlay);

        DestroyChildIfPresent(view.transform, "Guide");
        GuideDirector director = EnsureChild(view.transform, "Guide").gameObject.AddComponent<GuideDirector>();
        var so = new SerializedObject(director);
        SetRef(so, "prompt", prompt);
        SetRef(so, "rulebook", rulebook);
        SetRef(so, "desk", desk);
        SetRef(so, "inspect", inspect);
        SetRef(so, "stamps", stamps);
        SetRef(so, "board", Object.FindFirstObjectByType<MatchBoard>(FindObjectsInactive.Include));
        SetRef(so, "view", view);
        SetRef(so, "sign", readySign.transform);
        SetRef(so, "calendar", calendar);
        SetRef(so, "deskView", deskView);
        SetRef(so, "city", new SerializedObject(controls).FindProperty("cityView").objectReferenceValue);
        SetRef(so, "departureBoard", board.transform.Find("ClickBox"));
        SetRef(so, "scanner", scanner.transform);
        SetRef(so, "counter", counter.transform);
        SetRef(so, "traveller", travellerZone.GetComponent<Collider>());
        SetRef(so, "inspectButton", OwnedRect(inspect, "inspectButton"));
        SetRef(so, "stampTab", OwnedRect(stamps, "tab"));
        SetRef(so, "pcTab", OwnedRect(controls, "pcTab"));
        so.ApplyModifiedProperties();
        foreach (string missing in new[] { "board", "inspectButton", "stampTab", "pcTab" }.Where(p => so.FindProperty(p).objectReferenceValue == null))
            Debug.LogError($"[TimeDesk] The guide's '{missing}' is unwired: its arrow cannot point there.", director);

        var soGame = new SerializedObject(game);
        SetRef(soGame, "guide", director);
        soGame.ApplyModifiedProperties();
        var soBinder = new SerializedObject(binder);
        SetRef(soBinder, "guide", director);
        soBinder.ApplyModifiedProperties();

        Button[] replays = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b.name == GuideReplayButton).ToArray();
        foreach (Button replay in replays)
            WirePersistentVoid(replay, "m_OnClick", director, nameof(GuideDirector.Replay));
        if (replays.Length != 2)
            Debug.LogError($"[TimeDesk] {replays.Length} '{GuideReplayButton}' buttons (Settings' and the F1 card's expected): the tutorial's replay is not where it should be.");
    }

    /// <summary>The rect of the overlay control <paramref name="owner"/> keeps in its serialized field <paramref name="field"/> (a Button or a RectTransform), or null.</summary>
    private static RectTransform OwnedRect(Object owner, string field)
    {
        Object value = owner != null ? new SerializedObject(owner).FindProperty(field)?.objectReferenceValue : null;
        return value is Component c ? c.transform as RectTransform : null;
    }

    /// <summary>The prompt: a full-screen host (no graphic) with the Plate (the tooltip's yellow, top left: Header, Line, Skip, Ok; only the buttons take clicks) and the Arrow (a tooltip plate with "▼"), both inactive until shown; last on the overlay.</summary>
    private static GuidePrompt BuildGuidePrompt(Transform overlay)
    {
        DestroyChildIfPresent(overlay, "GuidePrompt");
        Transform host = Panel(overlay, "GuidePrompt", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        var plate = (RectTransform)Panel(host, "Plate", new Vector2(0f, 1f), new Vector2(0f, 1f), GuidePlateAt, GuidePlateSize, Tooltip, ThemeRoleId.Tooltip);
        plate.pivot = new Vector2(0f, 1f);
        plate.GetComponent<Image>().raycastTarget = false;
        TMP_Text header = Text(plate, "Header", "", 18, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.72f), new Vector2(0.72f, 0.95f), Ink, ThemeRoleId.Tooltip,
                               null, FontStyles.Bold, ThemeTextKind.Heading);
        header.raycastTarget = false;
        TMP_Text line = Text(plate, "Line", "", 22, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.05f), new Vector2(0.72f, 0.74f), Ink, ThemeRoleId.Tooltip);
        line.raycastTarget = false;
        line.textWrappingMode = TextWrappingModes.Normal;
        line.enableAutoSizing = true;
        line.fontSizeMin = 14f;
        line.fontSizeMax = 22f;
        Color buttonFill = new Color(0.2f, 0.3f, 0.5f, 0.95f);
        Button skip = MakeButton(plate, "Skip", null, new Vector2(0.74f, 0.3f), new Vector2(0.98f, 0.7f), buttonFill, ThemeRoleId.DeskButton, "guide.skip");
        Button ok = MakeButton(plate, "Ok", null, new Vector2(0.74f, 0.3f), new Vector2(0.98f, 0.7f), buttonFill, ThemeRoleId.DeskButton, "guide.gotIt");

        var arrow = (RectTransform)Panel(host, "Arrow", Center, Center, Vector2.zero, GuideArrowSize, Tooltip, ThemeRoleId.Tooltip);
        arrow.pivot = Center;
        arrow.GetComponent<Image>().raycastTarget = false;
        TMP_Text glyph = Text(arrow, "Glyph", "▼", 34, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Ink, ThemeRoleId.Tooltip, null, FontStyles.Bold);
        glyph.raycastTarget = false;

        plate.gameObject.SetActive(false);
        arrow.gameObject.SetActive(false);
        host.SetAsLastSibling();

        GuidePrompt prompt = GetOrAdd<GuidePrompt>(host.gameObject);
        var so = new SerializedObject(prompt);
        SetRef(so, "plate", plate);
        SetRef(so, "header", header);
        SetRef(so, "line", line);
        SetRef(so, "skipButton", skip);
        SetRef(so, "okButton", ok);
        SetRef(so, "arrow", arrow);
        so.ApplyModifiedProperties();
        return prompt;
    }
}
