// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// One Temporal Customs agency form (traveller types F1, §3): an authored
/// asset listed on its kind's blueprint. Pure data: its number and name, its
/// fields in form order (a field's index is its place on the form, the forms
/// engine's FormCell.field), when it is handed over, whether it carries the
/// photo, the kinds the desk may ask for it, and its form (redesign phase 4:
/// where the paper prints each field, FormLayout).
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
    /// The kinds the desk may ask for this form (traveller types I2): every
    /// form a blueprint hands over on request must be askable by its kind
    /// (Validate Content Library). Empty for a form handed over on arrival.
    /// </summary>
    public TravellerKind[] askableBy;

    /// <summary>True when the desk may ask a traveller of <paramref name="kind"/> for this form.</summary>
    public bool IsAskableBy(TravellerKind kind) => askableBy != null && System.Array.IndexOf(askableBy, kind) >= 0;

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
