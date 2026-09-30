using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app views on forms (redesign phase 16,
/// the PC spec's AP5, AP7, FO9, §2.5-§2.9): each tab of a pane draws its page
/// kind on a FormPage (OfficeSceneUIBuilder.PcForms: a scroll whose content
/// is a FormView at the pane's width) under the view's own strip: Records
/// (the lookup field and SEARCH over the Record Extract, Form_RecordExtract),
/// Reference ("Claimed place only" and the chosen book's cover over a
/// register page template, Form_Register, cloned per book by ReferenceView),
/// Transcript ("Answers only" and the "New line" pill over the Interview
/// Record, Form_InterviewRecord), Report (the Deviation Report,
/// Form_DeviationReport) and Rules (the Directive Memo, Form_DirectiveMemo).
/// The page kinds are authored assets in Assets/Data/Forms (a missing one is
/// an error). Built with each pane of the app (fresh on each run, its one
/// convergence policy); every reference is checked (Wire). Part of
/// <see cref="OfficeSceneUIBuilder"/>; BuildAppPane calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The page kinds the views draw.</summary>
    private const string RegisterFormPath = "Assets/Data/Forms/Form_Register.asset", InterviewFormPath = "Assets/Data/Forms/Form_InterviewRecord.asset",
                         ReportFormPath = "Assets/Data/Forms/Form_DeviationReport.asset", DirectiveMemoFormPath = "Assets/Data/Forms/Form_DirectiveMemo.asset";

    /// <summary>A view's strip (the lookup, the toggles) at its top: its gap from the top and its height; the page starts under it (desktop units; the PC UX redesign §3).</summary>
    private const float AppViewStripGap = 10f, AppViewStripHeight = 56f, AppViewPageTop = AppViewStripGap + AppViewStripHeight + 8f;

    /// <summary>The Records lookup's Search button width.</summary>
    private const float RecordsSearchWidth = 168f;

    /// <summary>A page kind asset by its path; a missing one is an error (the view then shows nothing).</summary>
    private static FormSpecSO PageKind(string path)
    {
        var form = AssetDatabase.LoadAssetAtPath<FormSpecSO>(path);
        if (form == null)
            Debug.LogError($"[TimeDesk] The page kind {path} is missing; author it in Assets/Data/Forms (TimeDesk/Form (PC page kind)).");
        return form;
    }

    /// <summary>A page kind's FormPage under <paramref name="root"/>: a scroll from the root's bottom up to <paramref name="top"/> units under its top, its form fitting the viewport.</summary>
    private static FormPage BuildPageKind(Transform root, string name, float top)
    {
        FormPage page = BuildFormPage(root, name, PcPageWidth, true);
        PlaceRect(page.transform, Vector2.zero, Vector2.one, new Vector2(DocMargin, DocMargin), new Vector2(-DocMargin, -Mathf.Max(top, DocMargin)));
        return page;
    }

    /// <summary>A control in a view's strip, from <paramref name="left"/> to <paramref name="right"/> units in from the view's sides (a negative <paramref name="left"/>: that far left of the right edge).</summary>
    private static void InStrip(Transform control, float left, float right)
    {
        bool fromRight = left < 0f;
        PlaceRect(control, fromRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f), Vector2.one,
                  new Vector2(left, -(AppViewStripGap + AppViewStripHeight)), new Vector2(-right, -AppViewStripGap));
    }

    /// <summary>The Records tab (§2.5): the lookup (a name or a number, SEARCH) over the Record Extract; its evidence boxes pick into <paramref name="compare"/>.</summary>
    private static RecordsView BuildRecordsView(Transform content, CompareController compare)
    {
        Transform root = ViewRoot(content, "RecordsView", Paper, ThemeRoleId.WindowBody);
        TMP_InputField input = BuildInputField(root, "SearchInput", "records.placeholder", Vector2.zero, Vector2.one);
        PlaceRect(input.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(PcSize.L, -(AppViewStripGap + AppViewStripHeight)),
                  new Vector2(-(PcSize.L + RecordsSearchWidth + PcSize.S), -AppViewStripGap));
        Button search = MakeButton(root, "SearchButton", null, Vector2.zero, Vector2.one, new Color(0.15f, 0.3f, 0.5f, 1f),
                                   ThemeRoleId.SearchButton, "records.search");
        InStrip(search.transform, -(PcSize.L + RecordsSearchWidth), PcSize.L);
        ButtonLabel(search, PcType.Body).lineSpacing = -6f;
        FormPage page = BuildPageKind(root, "Extract", AppViewPageTop);

        RecordsView view = root.gameObject.AddComponent<RecordsView>();
        var so = new SerializedObject(view);
        Wire(so, "searchInput", input);
        Wire(so, "searchButton", search);
        Wire(so, "page", page);
        Wire(so, "extractForm", PageKind(RecordExtractFormPath));
        Wire(so, "compareController", compare);
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>The Reference tab (§2.6): "Claimed place only" at its top right, and a register page template (inactive), cloned per book by ReferenceView; the book's cover is the art partial's (BuildBookCover).</summary>
    private static ReferenceView BuildReferenceView(Transform content)
    {
        Transform root = ViewRoot(content, "ReferenceView", Paper, ThemeRoleId.WindowBody);
        Toggle claimedOnly = BuildToggle(root, "ClaimedOnly", "app.ref.claimedOnly", Vector2.zero, Vector2.one);
        InStrip(claimedOnly.transform, -300f, PcSize.L);
        FormPage template = BuildPageKind(root, "PageTemplate", AppViewPageTop);
        template.gameObject.SetActive(false);

        ReferenceView view = root.gameObject.AddComponent<ReferenceView>();
        var so = new SerializedObject(view);
        Wire(so, "pageTemplate", template);
        Wire(so, "registerForm", PageKind(RegisterFormPath));
        Wire(so, "claimedOnly", claimedOnly);
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>The Transcript tab (§2.7): "Answers only" (off) at its top right and the "New line" pill (hidden) beside it, over the Interview Record.</summary>
    private static TranscriptView BuildTranscriptView(Transform content)
    {
        Transform root = ViewRoot(content, "TranscriptView", Paper, ThemeRoleId.WindowBody);
        Toggle answersOnly = BuildToggle(root, "AnswersOnly", "app.transcript.answersOnly", Vector2.zero, Vector2.one);
        InStrip(answersOnly.transform, -240f, PcSize.L);
        answersOnly.isOn = false;
        Button newLine = MakeButton(root, "NewLineButton", null, Vector2.zero, Vector2.one, null, ThemeRoleId.Button, "app.transcript.newLine");
        InStrip(newLine.transform, PcSize.L, PcSize.L + 240f + PcSize.S);
        ButtonLabel(newLine, PcType.Body);
        newLine.gameObject.SetActive(false);
        FormPage page = BuildPageKind(root, "Record", AppViewPageTop);

        TranscriptView view = root.gameObject.AddComponent<TranscriptView>();
        var so = new SerializedObject(view);
        Wire(so, "page", page);
        Wire(so, "interviewForm", PageKind(InterviewFormPath));
        Wire(so, "answersOnly", answersOnly);
        Wire(so, "newLineButton", newLine);
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>The Report tab (§2.8): the Deviation Report filling the view.</summary>
    private static ReportView BuildReportView(Transform content)
    {
        Transform root = ViewRoot(content, "ReportView", Paper, ThemeRoleId.WindowBody);
        FormPage page = BuildPageKind(root, "Report", 0f);
        ReportView view = root.gameObject.AddComponent<ReportView>();
        var so = new SerializedObject(view);
        Wire(so, "page", page);
        Wire(so, "reportForm", PageKind(ReportFormPath));
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>The Rules tab (§2.9): the Directive Memo filling the view.</summary>
    private static RulesView BuildRulesView(Transform content)
    {
        Transform root = ViewRoot(content, "RulesView", Paper, ThemeRoleId.WindowBody);
        FormPage page = BuildPageKind(root, "Memo", 0f);
        RulesView view = root.gameObject.AddComponent<RulesView>();
        var so = new SerializedObject(view);
        Wire(so, "page", page);
        Wire(so, "memoForm", PageKind(DirectiveMemoFormPath));
        so.ApplyModifiedProperties();
        return view;
    }
}
