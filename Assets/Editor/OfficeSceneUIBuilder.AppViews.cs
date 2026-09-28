using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app views on forms (redesign phase 16,
/// the PC spec's AP5, AP7, FO9, §2.5-§2.9): each tab of a pane draws its page
/// kind on a FormPage (OfficeSceneUIBuilder.PcForms: a scroll whose content
/// is a FormView at the pane's width) under the view's own strip: Records
/// (the lookup field and LOOK UP over the Record Extract, Form_RecordExtract),
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

    /// <summary>A view's strip (the lookup, the toggles) at its top, and where its page starts under it (shares of the view's height).</summary>
    private const float AppViewStripBottom = 0.91f, AppViewStripTop = 0.985f, AppViewPageTop = 0.9f;

    /// <summary>A page kind asset by its path; a missing one is an error (the view then shows nothing).</summary>
    private static FormSpecSO PageKind(string path)
    {
        var form = AssetDatabase.LoadAssetAtPath<FormSpecSO>(path);
        if (form == null)
            Debug.LogError($"[TimeDesk] The page kind {path} is missing; author it in Assets/Data/Forms (TimeDesk/Form (PC page kind)).");
        return form;
    }

    /// <summary>A page kind's FormPage under <paramref name="root"/>: a scroll from the root's bottom up to <paramref name="top"/> (a share of the height), its form fitting the viewport.</summary>
    private static FormPage BuildPageKind(Transform root, string name, float top)
    {
        FormPage page = BuildFormPage(root, name, PcPageWidth, true);
        PlaceRect(page.transform, Vector2.zero, new Vector2(1f, top), new Vector2(DocMargin, DocMargin), new Vector2(-DocMargin, 0f));
        return page;
    }

    /// <summary>The Records tab (§2.5): the lookup (a name or a number, LOOK UP) over the Record Extract; its evidence boxes pick into <paramref name="compare"/>.</summary>
    private static RecordsView BuildRecordsView(Transform content, CompareController compare)
    {
        Transform root = ViewRoot(content, "RecordsView", Paper, ThemeRoleId.WindowBody);
        TMP_InputField input = BuildInputField(root, "SearchInput", "records.placeholder", new Vector2(0.03f, AppViewStripBottom), new Vector2(0.7f, AppViewStripTop));
        Button search = MakeButton(root, "SearchButton", null, new Vector2(0.72f, AppViewStripBottom), new Vector2(0.97f, AppViewStripTop), new Color(0.15f, 0.3f, 0.5f, 1f),
                                   ThemeRoleId.SearchButton, "records.search");
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
        Toggle claimedOnly = BuildToggle(root, "ClaimedOnly", "app.ref.claimedOnly", new Vector2(0.62f, AppViewStripBottom), new Vector2(0.98f, AppViewStripTop));
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
        Toggle answersOnly = BuildToggle(root, "AnswersOnly", "app.transcript.answersOnly", new Vector2(0.62f, AppViewStripBottom), new Vector2(0.98f, AppViewStripTop));
        answersOnly.isOn = false;
        Button newLine = MakeButton(root, "NewLineButton", null, new Vector2(0.3f, AppViewStripBottom), new Vector2(0.6f, AppViewStripTop), null, ThemeRoleId.Button,
                                    "app.transcript.newLine");
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
        FormPage page = BuildPageKind(root, "Report", 1f);
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
        FormPage page = BuildPageKind(root, "Memo", 1f);
        RulesView view = root.gameObject.AddComponent<RulesView>();
        var so = new SerializedObject(view);
        Wire(so, "page", page);
        Wire(so, "memoForm", PageKind(DirectiveMemoFormPath));
        so.ApplyModifiedProperties();
        return view;
    }
}
