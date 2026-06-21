using System;
using UnityEngine;

/// <summary>
/// Designer-authored spec for one labeled field on a document template
/// (e.g., category=Currency, label="Payment", page=0). The runtime value is
/// filled by CaseFactory from the reference data for the case's nation+era.
/// </summary>
[Serializable]
public sealed class DocumentFieldSpec
{
    /// <summary>Which reference category this field is checked against.</summary>
    public ClueCategory category;

    /// <summary>Field label shown on the document (e.g., "Currency").</summary>
    public string label = "Field";

    /// <summary>Which page of the document this field appears on (0-based).</summary>
    [Min(0)] public int page = 0;
}

/// <summary>
/// A runtime, concrete field on a generated document: the label the visitor's
/// papers show, the value printed, and whether that value is an anachronism
/// (inconsistent with the claimed nation+era per the reference books).
/// Reference type so generation can flag forgeries after the list is built.
/// </summary>
public sealed class DocumentField
{
    /// <summary>Reference category this field is checked against.</summary>
    public ClueCategory category;

    /// <summary>Field label shown on the document (e.g., "Currency").</summary>
    public string label;

    /// <summary>Printed value on the visitor's document.</summary>
    public string value;

    /// <summary>0-based page this field is on.</summary>
    public int page;

    /// <summary>True if this value is forged/anachronistic for the claim.</summary>
    public bool isAnachronism;
}
