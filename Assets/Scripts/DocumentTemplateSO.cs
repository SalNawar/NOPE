// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// One Temporal Customs agency form (traveller types F1, §3): an authored
/// asset listed on its kind's blueprint. Pure data: its number and name, its
/// fields in form order (a field's index is its place on the form, the forms
/// engine's FormCell.field), when it is handed over, whether it carries the
/// photo, and its form (redesign phase 4: where the paper prints each field,
/// FormLayout). A form handed over on request is on the papers menu of every
/// traveller from the first day a blueprint lists it (the personalities
/// spec's W4, TimelineService.AgencyForms).
/// </summary>
[CreateAssetMenu(fileName = "DocTemplate_", menuName = "TimeDesk/Document Template", order = 11)]
public sealed class DocumentTemplateSO : ScriptableObject
{
    /// <summary>The form's name ("Displacement Certificate"): the document's header, its request entry and its compare label.</summary>
    public string displayName = "Document";

    /// <summary>The agency form number ("TC-610"), printed in the form's title before its name; one per template ("TC-" and three digits, checked by Validate Content Library).</summary>
    public string formNumber;

    /// <summary>
    /// Structured fields this document presents (investigation feature). Each
    /// spec's value is filled at runtime from the reference data for the case's
    /// claimed nation+era; a liar's tells rewrite every field of a tell category.
    /// </summary>
    [Header("Investigation fields")]
    public DocumentFieldSpec[] fieldSpecs;

    /// <summary>When the traveller hands this document over: when they step up (OnArrival) or when asked (OnRequest).</summary>
    [Header("Desk")]
    public DocumentHandOver handOver = DocumentHandOver.OnRequest;

    /// <summary>The first page carries the traveller's photo (a 4:5 crop of how they look), on the scanned page and on the paper: the kind's primary form (TC-610 for the displaced).</summary>
    public bool showsPhoto;

    /// <summary>
    /// The request group the form belongs to (traveller types I2), or blank:
    /// the forms of a group are one "Request" entry on the traveller wheel
    /// (the group's label, world_source.json interview.askGroups), and a
    /// traveller carries one of them, the one their account holds
    /// (AccountMaker.ProofGroup, "proof": TC-415, TC-416 and TC-417).
    /// </summary>
    public string askGroup = string.Empty;

    /// <summary>
    /// The paper's form (PC spec FO3): its blocks and where each field is
    /// printed, on the desk paper and its scanned copy alike. A field's page is
    /// the page the form places it on (FO4). Build Office UI and the content
    /// validator check that it places every field once and fits the paper
    /// (FormLayout.Check).
    /// </summary>
    [Header("Form")]
    public FormSpec form = new FormSpec();
}
