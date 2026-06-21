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

    /// <summary>Which clue categories this document prefers to contain.</summary>
    public ClueCategory[] preferredCategories;

    /// <summary>Soft limit for how many clues fit in this document.</summary>
    [Min(0)]
    public int maxClues = 3;

    /// <summary>
    /// Structured fields this document presents (investigation feature). Each
    /// spec's value is filled at runtime from the reference data for the case's
    /// claimed nation+era; a forged case flips one to an anachronism.
    /// </summary>
    [Header("Investigation fields")]
    public DocumentFieldSpec[] fieldSpecs;
}
