using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// The published content (Assets/Data/World/world_source.json) as the pure
/// tests read it: the fault canon and the issuing offices (the document
/// design spec, D4, D9), read the way Generate World reads them (every field
/// verbatim), so the tests hold the real table rather than a copy of it.
/// </summary>
public static class ContentFixture
{
    private const string SourcePath = "Assets/Data/World/world_source.json";

    /// <summary>The source's root.</summary>
    private static ContentNode Root([CallerFilePath] string here = "")
    {
        string path = File.Exists(SourcePath)
            ? SourcePath
            : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", SourcePath);
        return ContentJson.Parse(File.ReadAllText(path));
    }

    /// <summary>A member's text (blank when absent).</summary>
    private static string Text(ContentNode node, string key) => node.Get(key)?.Text ?? string.Empty;

    /// <summary>The published fault canon (agency.faults), in order.</summary>
    public static List<FaultEntry> Faults() =>
        Root().Get("agency").Get("faults").Items.Select(n => new FaultEntry
        {
            id = Text(n, "id"),
            form = Text(n, "form"),
            field = Text(n, "field"),
            lie = Text(n, "lie"),
            directive = Text(n, "directive"),
            variant = Text(n, "variant"),
            optional = Text(n, "optional") == "true",
            against = Text(n, "against"),
            note = Text(n, "note")
        }).ToList();

    /// <summary>The issuing offices (agency.offices), in order.</summary>
    public static List<AgencyOffice> Offices() =>
        Root().Get("agency").Get("offices").Items.Select(n => new AgencyOffice
        {
            id = Text(n, "id"),
            name = Text(n, "name"),
            shape = Text(n, "shape"),
            ink = Text(n, "ink"),
            legend = Text(n, "legend"),
            forgedLegend = Text(n, "forgedLegend"),
            forms = n.Get("forms").Items.Select(f => f.Text).ToList()
        }).ToList();
}
