using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The content spreadsheet's menus (Tools > TimeDesk): export today's world_source.json
/// to a workbook (ContentSheets/TimeDesk_Content.xlsx, a README sheet then one sheet per
/// table) or to one UTF-8 CSV per sheet (ContentSheets/csv/), write the template (the
/// same sheets with a few example rows), and import either back into world_source.json
/// (in its usual layout and line endings) followed by Generate World. The rules live in
/// <see cref="ContentSheets"/> and the map in <see cref="ContentSheetMap"/>; this file
/// only reads and writes files. An import writes nothing when any check fails, and
/// refuses while the current source holds content the map does not cover, so it never
/// drops a section another change added. The narrative workbook (NarrativeWorkbookMenu)
/// reads today's tables here (<see cref="Tables"/>) and imports through the same path
/// (<see cref="ImportTables"/>).
/// </summary>
public static class ContentSheetsMenu
{
    private const string SourcePath = "Assets/Data/World/world_source.json";
    private const string Folder = "ContentSheets";

    /// <summary>The workbook the export writes and the import reads (project-relative).</summary>
    public const string WorkbookPath = Folder + "/TimeDesk_Content.xlsx";

    /// <summary>The template workbook (project-relative).</summary>
    public const string TemplatePath = Folder + "/TimeDesk_Content_Template.xlsx";

    /// <summary>The folder of one CSV per sheet (project-relative).</summary>
    public const string CsvFolder = Folder + "/csv";

    private const int MaxLoggedErrors = 100;

    /// <summary>Exports today's content to the workbook (asks before replacing one).</summary>
    [MenuItem("Tools/TimeDesk/Export Content Spreadsheet")]
    public static void ExportSpreadsheetMenu()
    {
        if (ConfirmReplace(WorkbookPath, File.Exists(Full(WorkbookPath))))
            ExportSpreadsheet(WorkbookPath);
    }

    /// <summary>Exports today's content to one CSV per sheet (asks before replacing them).</summary>
    [MenuItem("Tools/TimeDesk/Export Content Sheets (CSV)")]
    public static void ExportCsvMenu()
    {
        bool any = Directory.Exists(Full(CsvFolder)) && Directory.GetFiles(Full(CsvFolder), "*.csv").Length > 0;
        if (ConfirmReplace(CsvFolder, any))
            ExportCsv(CsvFolder);
    }

    /// <summary>Writes the template workbook: every sheet with its headers and a few example rows.</summary>
    [MenuItem("Tools/TimeDesk/Write Content Spreadsheet Template")]
    public static void WriteTemplateMenu() => WriteTemplate(TemplatePath);

    /// <summary>Imports the workbook into world_source.json, then runs Generate World.</summary>
    [MenuItem("Tools/TimeDesk/Import Content Spreadsheet")]
    public static void ImportSpreadsheetMenu() => Import(WorkbookPath, true);

    /// <summary>Imports the CSV folder into world_source.json, then runs Generate World.</summary>
    [MenuItem("Tools/TimeDesk/Import Content Sheets (CSV)")]
    public static void ImportCsvMenu() => Import(CsvFolder, true);

    /// <summary>Writes the workbook for today's source to <paramref name="path"/> (project-relative or absolute); false when the source cannot be exported.</summary>
    public static bool ExportSpreadsheet(string path)
    {
        List<RowTable> book = Workbook(0);
        if (book == null)
            return false;
        WriteBytes(path, Xlsx.Write(book));
        Debug.Log($"[ContentSheets] Wrote {path}: {book.Count - 1} sheets and the README, {book.Skip(1).Sum(t => t.Rows.Count)} rows.");
        return true;
    }

    /// <summary>Writes one CSV per sheet (UTF-8 with a byte-order mark, so spreadsheets read it as UTF-8) and README.csv into <paramref name="folder"/>.</summary>
    public static bool ExportCsv(string folder)
    {
        List<RowTable> book = Workbook(0);
        if (book == null)
            return false;
        Directory.CreateDirectory(Full(folder));
        foreach (RowTable t in book)
            File.WriteAllText(Path.Combine(Full(folder), t.Name + ".csv"), Csv.Write(t), new UTF8Encoding(true));
        Debug.Log($"[ContentSheets] Wrote {book.Count} CSV files to {folder}/.");
        return true;
    }

    /// <summary>Writes the template workbook to <paramref name="path"/>.</summary>
    public static bool WriteTemplate(string path)
    {
        List<RowTable> book = Workbook(ContentSheets.TemplateExamples);
        if (book == null)
            return false;
        WriteBytes(path, Xlsx.Write(book));
        Debug.Log($"[ContentSheets] Wrote the template {path}: {book.Count - 1} sheets with up to {ContentSheets.TemplateExamples} example rows per table.");
        return true;
    }

    /// <summary>
    /// Imports a workbook (.xlsx) or a folder of CSVs into world_source.json, then runs
    /// Generate World when <paramref name="generate"/>. Writes nothing and returns false
    /// when anything is wrong (every error logged with its sheet, row and column).
    /// </summary>
    public static bool Import(string path, bool generate)
    {
        var errors = new List<string>();
        List<RowTable> tables = ReadTables(path, errors);
        if (!Check(errors, $"reading {path}") || tables == null)
            return false;
        return ImportTables(tables, path, generate, null, out _);
    }

    /// <summary>Today's source as the content tables (one per sheet of the map, no README): what the narrative workbook is built on and edits; null (errors logged) when the source cannot be exported.</summary>
    public static List<RowTable> Tables() => Workbook(0)?.Skip(1).ToList();

    /// <summary>
    /// Imports content tables (a workbook's or a CSV folder's, or today's tables with the
    /// narrative workbook's edits applied) into world_source.json, then runs Generate World
    /// when <paramref name="generate"/>. Writes nothing and returns false when anything is
    /// wrong (every error logged, first passed through <paramref name="explain"/> when given:
    /// the narrative workbook names the row each came from). <paramref name="changed"/> says
    /// whether world_source.json changed. <paramref name="what"/> names the source in the log.
    /// </summary>
    public static bool ImportTables(IReadOnlyList<RowTable> tables, string what, bool generate, Func<List<string>, List<string>> explain, out bool changed)
    {
        changed = false;
        var errors = new List<string>();
        ContentNode current = LoadSource(errors);
        if (!Check(errors, "reading world_source.json") || current == null)
            return false;
        var unmapped = new List<string>();
        ContentSheets.Export(ContentSheetMap.World, current, unmapped);
        if (!Check(unmapped, "world_source.json holds content the sheets do not map (map it in ContentSheetMap first, or the import would drop it)"))
            return false;

        ContentNode imported = ContentSheets.Import(ContentSheetMap.World, tables, errors);
        if (explain != null)
            errors = explain(errors);
        if (!Check(errors, $"importing {what}") || imported == null)
            return false;

        string full = Full(SourcePath);
        string existing = File.ReadAllText(full);
        string text = ContentJson.Write(imported);
        if (existing.Replace("\r\n", "\n") == text)
            Debug.Log($"[ContentSheets] Imported {what} ({tables.Count} sheets): world_source.json is unchanged.");
        else
        {
            File.WriteAllText(full, existing.Contains("\r\n") ? text.Replace("\n", "\r\n") : text, new UTF8Encoding(false));
            changed = true;
            AssetDatabase.ImportAsset(SourcePath);
            Debug.Log($"[ContentSheets] Imported {what} ({tables.Count} sheets) into world_source.json.");
        }

        if (generate)
            WorldContentGenerator.Generate();
        return true;
    }

    private static List<RowTable> ReadTables(string path, List<string> errors)
    {
        string full = Full(path);
        if (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(full))
                return Xlsx.Read(File.ReadAllBytes(full), errors);
            Debug.LogError($"[ContentSheets] There is no workbook at {path}: export one first (Tools > TimeDesk > Export Content Spreadsheet).");
            return null;
        }
        if (!Directory.Exists(full) || Directory.GetFiles(full, "*.csv").Length == 0)
        {
            Debug.LogError($"[ContentSheets] There are no CSV files in {path}: export them first (Tools > TimeDesk > Export Content Sheets (CSV)).");
            return null;
        }
        return Directory.GetFiles(full, "*.csv").OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => Csv.Read(Path.GetFileNameWithoutExtension(f), File.ReadAllText(f, Encoding.UTF8), errors))
            .ToList();
    }

    /// <summary>The workbook tables for today's source; null (errors logged) when the map or the source is unsound.</summary>
    private static List<RowTable> Workbook(int examples)
    {
        var errors = new List<string>();
        ContentNode world = LoadSource(errors);
        if (!Check(errors, "reading world_source.json") || world == null)
            return null;
        List<RowTable> book = ContentSheets.Workbook(ContentSheetMap.World, world, errors, examples);
        return Check(errors, "exporting world_source.json (it holds content the sheets cannot: map it in ContentSheetMap)") ? book : null;
    }

    /// <summary>Today's source, after the map's own checks; null with the reason in <paramref name="errors"/>.</summary>
    private static ContentNode LoadSource(List<string> errors)
    {
        errors.AddRange(ContentSheets.MapProblems(ContentSheetMap.World));
        if (errors.Count > 0)
            return null;
        try
        {
            return ContentJson.Parse(File.ReadAllText(Full(SourcePath)));
        }
        catch (Exception e) when (e is FormatException || e is IOException)
        {
            errors.Add($"{SourcePath}: {e.Message}");
            return null;
        }
    }

    /// <summary>Logs the errors (at most <see cref="MaxLoggedErrors"/>) and a summary; true when there are none.</summary>
    private static bool Check(List<string> errors, string what)
    {
        if (errors.Count == 0)
            return true;
        foreach (string e in errors.Take(MaxLoggedErrors))
            Debug.LogError($"[ContentSheets] {e}");
        if (errors.Count > MaxLoggedErrors)
            Debug.LogError($"[ContentSheets] ... and {errors.Count - MaxLoggedErrors} more.");
        Debug.LogError($"[ContentSheets] Stopped while {what}: {errors.Count} error(s); nothing was written.");
        return false;
    }

    private static bool ConfirmReplace(string what, bool exists) =>
        !exists || EditorUtility.DisplayDialog("Replace the content sheets?",
            $"{what} already exists. Exporting replaces it with today's world_source.json, and edits made there since the last import are lost.",
            "Replace", "Cancel");

    private static void WriteBytes(string path, byte[] bytes)
    {
        string full = Full(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllBytes(full, bytes);
    }

    /// <summary>A project-relative path made absolute (an absolute path stays as it is).</summary>
    private static string Full(string path) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
}
