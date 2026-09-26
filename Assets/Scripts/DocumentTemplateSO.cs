// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Template describing a document type (passport, letter, manifest, etc.).
/// Determines how clues are routed and how many fit.
/// </summary>
[CreateAssetMenu(fileName = "DocTemplate_", menuName = "TimeDesk/Document Template", order = 11)]
public sealed class DocumentTemplateSO : ScriptableObject
{
    /// <summary>Name shown as the document header.</summary>
    public string displayName = "Document";

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

    /// <summary>The first page carries the traveller's photo (a 4:5 crop of how they look), on the scanned page and on the paper. Only the Travel Passport does.</summary>
    public bool showsPhoto;
}
