using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Time Sorter > Narrative Workbook: Export writes ContentSheets/NarrativeWorkbook.xlsx
/// (<see cref="NarrativeWorkbook"/>: the days, a reference run's travellers and their
/// lines, the authored narratives, the voice lines and the triggers) from today's content
/// tables (<see cref="ContentSheetsMenu.Tables"/>, the content spreadsheet's own) and a
/// reference run played through the balance simulation's steps
/// (<see cref="BalanceSimulation.Observe"/>: CaseFactory's travellers, their lines built
/// by the interview's own code); Import applies the workbook's edited cells to those
/// tables (<see cref="NarrativeImport"/>), imports them through the content
/// spreadsheet's path (<see cref="ContentSheetsMenu.ImportTables"/>), runs Generate World
/// when world_source.json changed (restoring it when Generate World refuses) and the
/// validator, and writes ContentSheets/NarrativeWorkbook_import.txt. The batch entries
/// (<see cref="ExportBatch"/>, <see cref="ImportBatch"/>) do the same without asking.
/// </summary>
public static class NarrativeWorkbookMenu
{
    /// <summary>The workbook (project-relative), next to the content spreadsheet.</summary>
    public const string WorkbookPath = "ContentSheets/NarrativeWorkbook.xlsx";

    /// <summary>The import's report (project-relative).</summary>
    public const string ReportPath = "ContentSheets/NarrativeWorkbook_import.txt";

    private const string SourcePath = "Assets/Data/World/world_source.json";
    private const int MaxReportedLogs = 200;

    /// <summary>Exports the workbook (asking before replacing one) and opens it.</summary>
    [MenuItem("Time Sorter/Narrative Workbook/Export")]
    public static void ExportMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[NarrativeWorkbook] Leave play mode first: the export plays reference runs of its own.");
            return;
        }
        if (File.Exists(Full(WorkbookPath)) && !EditorUtility.DisplayDialog("Replace the narrative workbook?",
                $"{WorkbookPath} already exists. Exporting replaces it with today's content, and edits made there since the last import are lost.", "Replace", "Cancel"))
            return;
        if (Export(WorkbookPath))
            EditorUtility.OpenWithDefaultApp(Full(WorkbookPath));
    }

    /// <summary>Imports the workbook and shows the report.</summary>
    [MenuItem("Time Sorter/Narrative Workbook/Import")]
    public static void ImportMenu()
    {
        Import(WorkbookPath, ReportPath);
        EditorUtility.RevealInFinder(Full(ReportPath));
    }

    /// <summary>Shows the workbook's folder.</summary>
    [MenuItem("Time Sorter/Narrative Workbook/Show in Explorer")]
    public static void ShowMenu() => EditorUtility.RevealInFinder(File.Exists(Full(WorkbookPath)) ? Full(WorkbookPath) : Full(Path.GetDirectoryName(WorkbookPath)));

    /// <summary>The export for the job runner and batch mode: no questions.</summary>
    public static void ExportBatch() => Export(WorkbookPath);

    /// <summary>The import for the job runner and batch mode: no questions.</summary>
    public static void ImportBatch() => Import(WorkbookPath, ReportPath);

    // =====================================================================
    // Export
    // =====================================================================

    /// <summary>Writes the workbook for today's content and reference run to <paramref name="path"/>; false (logged) when the content cannot be exported or the file cannot be written.</summary>
    public static bool Export(string path)
    {
        List<RowTable> tables = ContentSheetsMenu.Tables();
        if (tables == null)
        {
            Debug.LogError("[NarrativeWorkbook] Today's world_source.json cannot be exported to the content tables (see the errors above); nothing was written.");
            return false;
        }

        List<RowTable> book = NarrativeWorkbook.Build(ContentSheetMap.World, tables, Context());
        try
        {
            string full = Full(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, Xlsx.Write(book));
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogError($"[NarrativeWorkbook] Could not write {path} (is it open in Excel? Close it and export again): {e.Message}");
            return false;
        }
        Debug.Log($"[NarrativeWorkbook] Wrote {path}: {string.Join(", ", book.Select(t => $"{t.Name} {t.Rows.Count} rows"))}.");
        return true;
    }

    /// <summary>What the workbook reads from the generated assets: the reference run, the effects a dialog choice may name (Generate World's rule: no op that acts while active, no history op) and the waiver form's fine print.</summary>
    private static NarrativeContext Context()
    {
        var context = new NarrativeContext();
        RunConfigSO run = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        ContentLibrarySO lib = run != null ? run.contentLibrary : null;
        if (lib != null)
            foreach (EffectSO fx in lib.Effects.Where(e => e != null && e.ops.All(o => o == null || (!EffectOps.ActsWhileActive(o.type) && !EffectOps.HistoryOnly(o.type)))))
            {
                context.Effects[fx.name] = Describe(fx);
                context.EffectFlags[fx.name] = fx.ops.Where(o => o != null && o.type == EffectOpType.SetFlag).Select(o => o.stringParam).ToList();
            }
        AddFormNotes(context);
        PlayReference(context);
        return context;
    }

    /// <summary>An effect's ops in words.</summary>
    private static string Describe(EffectSO fx)
    {
        IEnumerable<string> ops = fx.ops.Where(o => o != null).Select(o =>
        {
            string amount = o.floatParam.ToString("0.##", CultureInfo.InvariantCulture);
            switch (o.type)
            {
                case EffectOpType.SetFlag: return $"sets {o.stringParam}";
                case EffectOpType.ClearFlag: return $"clears {o.stringParam}";
                case EffectOpType.AddMoney: return $"{(o.floatParam >= 0 ? "+" : string.Empty)}{amount} cr";
                case EffectOpType.AddStability: return $"stability {(o.floatParam >= 0 ? "+" : string.Empty)}{amount}";
                case EffectOpType.UnlockUpgrade: return $"unlocks {o.stringParam}";
                default: return $"{o.type} {o.stringParam} {amount}".Trim();
            }
        });
        return $"{(string.IsNullOrEmpty(fx.displayName) ? fx.name : fx.displayName)}: {string.Join(", ", ops)}";
    }

    /// <summary>The Stranding Waiver's printed promise and Time Police clause (its template's fine print and foot), shown read-only in the Strandings block.</summary>
    private static void AddFormNotes(NarrativeContext context)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:DocumentTemplateSO"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var template = AssetDatabase.LoadAssetAtPath<DocumentTemplateSO>(path);
            if (template == null || template.formNumber != Directives.Waiver || template.form == null)
                continue;
            for (int i = 0; i < template.form.blocks.Length; i++)
            {
                FormBlock block = template.form.blocks[i];
                if (block == null || string.IsNullOrWhiteSpace(block.text) || (block.kind != FormBlockKind.FinePrint && block.kind != FormBlockKind.Footer))
                    continue;
                context.Notes.Add(new NarrativeNote
                {
                    Narrative = "strandings",
                    When = $"printed on the {template.displayName} ({template.formNumber})",
                    Part = "waiver form",
                    Field = block.kind == FormBlockKind.FinePrint ? "fine print (the promise)" : "foot (the Time Police clause)",
                    Text = block.text,
                    Where = $"{path}, form.blocks[{i}].text (the template's Inspector)"
                });
            }
        }
    }

    /// <summary>Plays the settings' seeds (NarrativeWorkbookSettingsSO) and records every traveller with their lines, the dialogs offered and the story beats fired.</summary>
    private static void PlayReference(NarrativeContext context)
    {
        var settings = AssetDatabase.LoadAssetAtPath<NarrativeWorkbookSettingsSO>(NarrativeWorkbookSettingsSO.AssetPath);
        NarrativeWorkbookSettingsSO defaults = settings == null ? ScriptableObject.CreateInstance<NarrativeWorkbookSettingsSO>() : null;
        int[] seeds = (settings ?? defaults).seeds ?? new int[0];
        PlayStyle style = (settings ?? defaults).style;
        if (defaults != null)
            UnityEngine.Object.DestroyImmediate(defaults);

        var endings = new List<string>();
        var events = new HashSet<(int, int, string, string)>();
        foreach (int seed in seeds)
        {
            var fired = new HashSet<string>();
            var observer = new BalanceSimulation.RunObserver
            {
                DayStarted = (day, interview, cases) =>
                {
                    foreach (AuthoredDialog d in interview.OfferedDialogs(null))
                        events.Add((seed, day, NarrativeEvent.DialogOffered, d.id));
                },
                Presented = (day, slot, inst, interview) =>
                {
                    context.Cases.Add(Describe(seed, day, slot, inst, interview));
                    if (!string.IsNullOrEmpty(inst.premadeDialogId) && interview.OfferedDialogs(inst.premadeDialogId).Any(d => d.id == inst.premadeDialogId))
                        events.Add((seed, day, NarrativeEvent.DialogOffered, inst.premadeDialogId));
                },
                NightTurned = (day, world) =>
                {
                    foreach (string flag in world.flags.Where(f => f.StartsWith("trig:history_", StringComparison.Ordinal) && f.EndsWith(":fired", StringComparison.Ordinal) && fired.Add(f)))
                        events.Add((seed, day, NarrativeEvent.BeatFired, flag.Substring(13, flag.Length - 19)));
                },
                Ended = (day, ending) => endings.Add($"seed {seed} ended on day {day} ({ending})")
            };
            EditorUtility.DisplayProgressBar("Narrative workbook", $"Reference run, seed {seed}", 0f);
            try
            {
                if (!BalanceSimulation.Observe(seed, style, observer))
                    break;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        foreach ((int seed, int day, string kind, string id) in events.OrderBy(e => e.Item1).ThenBy(e => e.Item2).ThenBy(e => e.Item3, StringComparer.Ordinal).ThenBy(e => e.Item4, StringComparer.Ordinal))
            context.Events.Add(new NarrativeEvent(seed, day, kind, id));
        context.RunNote = seeds.Length == 0
            ? string.Empty
            : $"seeds {string.Join(", ", seeds)} · {style} play (the balance simulation's steps) · the whole queue each day" +
              (endings.Count > 0 ? " · " + string.Join("; ", endings) : string.Empty) + $" · edit them in {NarrativeWorkbookSettingsSO.AssetPath}";
    }

    /// <summary>One traveller as the reference run met them, with every line they would say (the interview's own graph: InterviewScript over InterviewPresenter.CaseFor).</summary>
    private static NarrativeCase Describe(int seed, int day, int slot, CaseInstance inst, InterviewDay interview)
    {
        var c = new NarrativeCase
        {
            Seed = seed,
            Day = day,
            Slot = slot,
            Source = inst.forcedAppearance != null ? NarrativeCaseSource.Forced : inst.isLegendary ? NarrativeCaseSource.Pool : NarrativeCaseSource.Generated,
            Appearance = inst.forcedAppearance == null ? string.Empty : string.IsNullOrEmpty(inst.forcedAppearance.id) ? "(slot)" : inst.forcedAppearance.id,
            Premade = inst.legendarySource != null ? inst.legendarySource.id : string.Empty,
            Name = inst.visitorDisplayName ?? string.Empty,
            Kind = inst.kind.ToString(),
            Personality = inst.personality ?? string.Empty,
            Claimed = inst.originLabel ?? string.Empty,
            TrueHome = inst.trueHomeLabel ?? string.Empty,
            Lie = inst.lie.HasValue ? inst.lie.Value.ToString() : string.Empty,
            Fault = string.Join(" · ", new[]
            {
                inst.FaultReason,
                inst.directiveFault != DirectiveFault.None ? inst.directiveFault.ToString() : null,
                inst.costumeFault != CostumeError.None ? "costume: " + inst.CostumeItem : null,
                inst.IsForger ? "forged record" : null
            }.Where(s => !string.IsNullOrEmpty(s))),
            Correct = inst.ShouldAccept ? "Accept" : "Deny",
            Intro = inst.introLine ?? string.Empty
        };

        List<CaseDocument> documents = inst.documents.Select(CaseDocumentsPresenter.DocumentOf).ToList();
        InterviewCase interviewCase = InterviewPresenter.CaseFor(inst, documents, interview, null, true, true);
        IReadOnlyList<AuthoredDialog> dialogs = interview.OfferedDialogs(inst.premadeDialogId);
        DialogGraph graph = InterviewScript.Build(interview.Lines, interview.Questions, dialogs, interviewCase);
        c.Claim = InterviewScript.Claim(interview.Lines, interviewCase);

        IEnumerable<DialogChoice> Choices(string node) => graph.Node(node)?.Choices ?? Enumerable.Empty<DialogChoice>();
        string Said(DialogChoice choice)
        {
            string said = string.Join(" / ", choice.Lines.Where(l => l.Speaker == DialogSpeaker.Traveller).Select(l => l.Text));
            return said.Length > 0 ? said : "(no reply)";
        }
        string Menu(IEnumerable<DialogChoice> choices) => string.Join("\n", choices.Select(ch => $"{ch.Label}: {Said(ch)}"));

        IEnumerable<DialogChoice> requests = Choices(InterviewScript.HubNodeId).Concat(Choices(InterviewScript.PapersNodeId))
            .Where(ch => ch.Id.StartsWith("request:", StringComparison.Ordinal) || ch.Id == InterviewScript.PadChoiceId);
        c.Papers = Menu(requests);
        c.Spoken = Menu(Choices(InterviewScript.HubNodeId).Where(ch => ch.Id.StartsWith("act:", StringComparison.Ordinal)));
        c.Answers = Menu(Choices(InterviewScript.AskNodeId).Where(ch => ch.Id.StartsWith("q:", StringComparison.Ordinal)));
        List<string> smallTalk = Choices(InterviewScript.AskNodeId).Where(ch => ch.Id == "smalltalk")
            .SelectMany(ch => ch.Lines.Where(l => l.Speaker == DialogSpeaker.Traveller).Select(l => l.Text)).ToList();
        c.SmallTalk = smallTalk.Count > 0 ? smallTalk[0] : string.Empty;
        c.Slip = smallTalk.Count > 1 ? smallTalk[1] : string.Empty;

        ReactionIntent intent = ReactionIntents.Of(inst.IsLiar, inst.IsForger);
        string React(ReactionVerdict verdict) => string.Join(" / ", InterviewScript.Reaction(interview.Lines, interviewCase, verdict, intent, inst.FaultReason).Select(l => l.Text));
        c.IfAccepted = React(ReactionVerdict.Accepted);
        c.IfDenied = React(ReactionVerdict.Denied);
        c.Dialogs = string.Join("|", dialogs.Select(d => d.id));
        return c;
    }

    // =====================================================================
    // Import
    // =====================================================================

    /// <summary>
    /// Imports the workbook at <paramref name="path"/>: its edited cells onto today's
    /// content tables, those into world_source.json through the content spreadsheet's
    /// import, then Generate World (when the source changed; the source is restored when
    /// it refuses) and the validator. Writes the report to <paramref name="reportPath"/>.
    /// True when the import went through (with no edits, world_source.json is untouched).
    /// </summary>
    public static bool Import(string path, string reportPath)
    {
        var report = new StringBuilder();
        report.AppendLine($"Narrative workbook import, {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
        report.AppendLine($"Workbook: {path}");
        var logs = new List<string>();
        void Capture(string message, string stack, LogType type)
        {
            if (type != LogType.Log)
                logs.Add($"{type}: {message}");
        }

        bool ok = false;
        Application.logMessageReceived += Capture;
        try
        {
            ok = ImportSteps(path, report, logs);
        }
        finally
        {
            Application.logMessageReceived -= Capture;
            report.AppendLine();
            report.AppendLine(ok ? "RESULT: imported." : "RESULT: stopped; see above.");
            if (logs.Count > 0)
            {
                report.AppendLine();
                report.AppendLine($"Warnings and errors logged meanwhile ({logs.Count}; the validator's may predate this import):");
                foreach (string line in logs.Take(MaxReportedLogs))
                    report.AppendLine("  " + line.Replace("\n", "\n    "));
                if (logs.Count > MaxReportedLogs)
                    report.AppendLine($"  ... and {logs.Count - MaxReportedLogs} more.");
            }
            string full = Full(reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, report.ToString(), new UTF8Encoding(false));
        }
        Debug.Log($"[NarrativeWorkbook] Import of {path}: {(ok ? "done" : "stopped")}; the report is {reportPath}.");
        return ok;
    }

    private static bool ImportSteps(string path, StringBuilder report, List<string> logs)
    {
        byte[] data;
        try
        {
            using var stream = new FileStream(Full(path), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            data = memory.ToArray();
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            report.AppendLine($"Could not read the workbook: {e.Message} (export one first: Time Sorter > Narrative Workbook > Export).");
            return false;
        }

        var readErrors = new List<string>();
        List<RowTable> workbook = Xlsx.Read(data, readErrors);
        if (Stop(report, readErrors, "reading the workbook"))
            return false;
        List<RowTable> content = ContentSheetsMenu.Tables();
        if (content == null)
        {
            report.AppendLine("Today's world_source.json cannot be exported to the content tables; nothing was written.");
            return false;
        }

        NarrativeImport result = NarrativeImport.Apply(content, workbook);
        report.AppendLine($"Bound rows read: {result.BoundRows}. Cells changed: {result.Edited}. Lines added: {result.Added}. Lines removed: {result.Removed}. Rows skipped because the source moved on and they were not edited: {result.Stale}.");
        if (Stop(report, result.Errors, "reading the edits"))
            return false;
        if (result.Changes.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Edits read from the workbook:");
            foreach (string change in result.Changes)
                report.AppendLine("  " + change);
        }

        string source = Full(SourcePath);
        byte[] before = File.ReadAllBytes(source);
        int logged = logs.Count;
        if (!ContentSheetsMenu.ImportTables(result.Tables, path, false, result.Explain, out bool changed))
        {
            report.AppendLine();
            report.AppendLine("The content import refused the edits (its errors are listed below); nothing was written.");
            return false;
        }
        report.AppendLine();
        report.AppendLine(changed ? "world_source.json: changed." : "world_source.json: unchanged (0 bytes changed); Generate World not needed.");

        if (changed)
        {
            logged = logs.Count;
            WorldContentGenerator.Generate();
            if (logs.Skip(logged).Any(l => l.StartsWith("Error: [WorldContentGenerator]", StringComparison.Ordinal)))
            {
                File.WriteAllBytes(source, before);
                AssetDatabase.ImportAsset(SourcePath);
                report.AppendLine("Generate World refused the edits (its errors are listed below); world_source.json was restored as it was.");
                return false;
            }
            report.AppendLine("Generate World: done.");
        }

        logged = logs.Count;
        ContentLibraryValidator.Validate();
        report.AppendLine($"Validator: {logs.Count - logged} warning(s) or error(s) (listed below).");
        return true;
    }

    private static bool Stop(StringBuilder report, List<string> errors, string what)
    {
        if (errors.Count == 0)
            return false;
        report.AppendLine();
        report.AppendLine($"Stopped while {what}: {errors.Count} problem(s); nothing was written.");
        foreach (string e in errors)
            report.AppendLine("  " + e);
        return true;
    }

    /// <summary>A project-relative path made absolute (an absolute path stays as it is).</summary>
    private static string Full(string path) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
}
