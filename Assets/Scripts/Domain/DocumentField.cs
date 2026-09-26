using System;

/// <summary>
/// Designer-authored spec for one labeled field on a document template
/// (e.g., category=Currency, label="Payment"). The runtime value is filled by
/// CaseFactory from the reference data for the case's nation+era. Where it is
/// printed, and so its page, is the template's form's (PC spec FO4).
/// </summary>
[Serializable]
public sealed class DocumentFieldSpec
{
    /// <summary>Which reference category this field is checked against.</summary>
    public ClueCategory category;

    /// <summary>Field label shown on the document (e.g., "Currency").</summary>
    public string label = "Field";
}

/// <summary>
/// A runtime, concrete field on a generated document: the label the visitor's
/// papers show, the value printed, and whether that value is a liar's tell
/// (anachronistic for the claimed nation+era per the reference books or the
/// agency record). Reference type so generation can apply a lie's tells after
/// the list is built.
/// </summary>
public sealed class DocumentField
{
    /// <summary>Reference category this field is checked against.</summary>
    public ClueCategory category;

    /// <summary>Field label shown on the document (e.g., "Currency").</summary>
    public string label;

    /// <summary>Printed value on the visitor's document.</summary>
    public string value;

    /// <summary>0-based page this field is on: where the template's form places it (FormSpec.PageOf, set by CaseFactory).</summary>
    public int page;

    /// <summary>True if this value is a liar's tell (anachronistic for the claim).</summary>
    public bool isAnachronism;
}
