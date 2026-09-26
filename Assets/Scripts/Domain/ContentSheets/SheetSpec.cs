using System.Collections.Generic;
using System.Linq;

/// <summary>What a column's cells hold and the JSON value each becomes.</summary>
public enum CellType
{
    /// <summary>Any text (a JSON string). Blank means "".</summary>
    Text,

    /// <summary>A whole number (a JSON integer). Blank means 0.</summary>
    Int,

    /// <summary>A decimal (a JSON float, "1" written as 1.0). Blank means 0.0.</summary>
    Float,

    /// <summary>A number written as typed: whole without a point, else a decimal. Blank means 0.</summary>
    Number,

    /// <summary>true or false (any case). Blank means false.</summary>
    Bool,

    /// <summary>A list of texts in one cell, "a|b|c" (a JSON array of strings). Blank means an empty list.</summary>
    TextList,

    /// <summary>A list of numbers in one cell, "1|0.5" (each kept whole or decimal as typed). Blank means an empty list.</summary>
    NumberList
}

/// <summary>How a sheet's rows sit in the JSON.</summary>
public enum SheetShape
{
    /// <summary>One object: the sheet holds exactly one row.</summary>
    Single,

    /// <summary>An array of objects: one row per item, in row order.</summary>
    Rows,

    /// <summary>An object of objects: one row per member, the key column naming it.</summary>
    Keyed,

    /// <summary>An array of plain values: one row per value, in row order.</summary>
    Values
}

/// <summary>A field of a sheet's row object: a column, or a child sheet.</summary>
public abstract class SheetField
{
    /// <summary>The JSON path of the field inside the row object (dots name nested objects: "culture.language").</summary>
    public string Path { get; protected set; }

    /// <summary>A note for the README sheet.</summary>
    public string Doc { get; protected set; }
}

/// <summary>The key of a keyed-rows sheet: the text that names a row (a column, or a template of columns) and the header child sheets use for it.</summary>
public sealed class RowKey
{
    internal RowKey(string template, string header)
    {
        Template = template;
        Header = header;
    }

    /// <summary>A column header ("id") or a template of headers ("{country}_{era}").</summary>
    public string Template { get; }

    /// <summary>The column that names a row of this sheet in its child sheets ("dialog"); null when it has none.</summary>
    public string Header { get; }

    /// <summary>The headers the template reads.</summary>
    public IEnumerable<string> Columns =>
        Template.Contains("{")
            ? Template.Split('{').Skip(1).Select(part => part.Substring(0, part.IndexOf('}')))
            : new[] { Template };

    /// <summary>The key of a row, from its cells by header.</summary>
    public string Of(System.Func<string, string> cell)
    {
        if (!Template.Contains("{"))
            return cell(Template);
        string key = Template;
        foreach (string c in Columns)
            key = key.Replace("{" + c + "}", cell(c));
        return key;
    }
}

/// <summary>
/// One column: one cell per row, one JSON field. The rules are data: the type, what a
/// blank cell means, whether the field is left out of the JSON when it holds that
/// default, whether a value is required, the allowed values, and the sheet whose row
/// keys the value must name.
/// </summary>
public sealed class ColumnSpec : SheetField
{
    private ColumnSpec(string path, CellType type)
    {
        Path = path;
        Header = path;
        Type = type;
    }

    /// <summary>The header in the sheet (the JSON path).</summary>
    public string Header { get; }

    /// <summary>What the cells hold.</summary>
    public CellType Type { get; }

    /// <summary>The cell text a blank cell stands for (null: the type's own default).</summary>
    public string DefaultText { get; private set; }

    /// <summary>Leave the field out of the JSON when it holds its default (a blank cell leaves it out).</summary>
    public bool OmitDefault { get; private set; }

    /// <summary>A blank cell is an error.</summary>
    public bool IsRequired { get; private set; }

    /// <summary>The sheet whose row keys this value (or each list item) must be; blank stays allowed unless required.</summary>
    public string RefSheet { get; private set; }

    /// <summary>The only values allowed (null: any).</summary>
    public string[] Allowed { get; private set; }

    /// <summary>A text column.</summary>
    public static ColumnSpec Text(string path) => new ColumnSpec(path, CellType.Text);

    /// <summary>A whole-number column.</summary>
    public static ColumnSpec Int(string path) => new ColumnSpec(path, CellType.Int);

    /// <summary>A decimal column.</summary>
    public static ColumnSpec Float(string path) => new ColumnSpec(path, CellType.Float);

    /// <summary>A number column kept whole or decimal as typed.</summary>
    public static ColumnSpec Num(string path) => new ColumnSpec(path, CellType.Number);

    /// <summary>A true/false column.</summary>
    public static ColumnSpec Bool(string path) => new ColumnSpec(path, CellType.Bool);

    /// <summary>A "a|b|c" text-list column.</summary>
    public static ColumnSpec List(string path) => new ColumnSpec(path, CellType.TextList);

    /// <summary>A "1|0.5" number-list column.</summary>
    public static ColumnSpec Nums(string path) => new ColumnSpec(path, CellType.NumberList);

    /// <summary>Leaves the field out of the JSON when it holds its default.</summary>
    public ColumnSpec Omit()
    {
        OmitDefault = true;
        return this;
    }

    /// <summary>Makes a blank cell an error.</summary>
    public ColumnSpec Required()
    {
        IsRequired = true;
        return this;
    }

    /// <summary>The value must be a row key of <paramref name="sheet"/>.</summary>
    public ColumnSpec Ref(string sheet)
    {
        RefSheet = sheet;
        return this;
    }

    /// <summary>The only values allowed.</summary>
    public ColumnSpec OneOf(params string[] values)
    {
        Allowed = values;
        return this;
    }

    /// <summary>What a blank cell stands for, as cell text.</summary>
    public ColumnSpec Default(string cellText)
    {
        DefaultText = cellText;
        return this;
    }

    /// <summary>A note for the README sheet.</summary>
    public ColumnSpec Note(string text)
    {
        Doc = text;
        return this;
    }
}

/// <summary>
/// One sheet: where its rows sit in the JSON (a path inside the parent sheet's row
/// object), their shape, their key, and their fields in JSON order (columns and child
/// sheets interleaved as the JSON orders them). A child sheet's rows name their parent
/// row in one column per keyed ancestor, before their own columns. The whole map is a
/// tree of these rooted at a <see cref="SheetShape.Single"/> sheet for the document.
/// </summary>
public sealed class SheetSpec : SheetField
{
    private SheetSpec(string name, string path, SheetShape shape, RowKey key, ColumnSpec keyColumn, IEnumerable<SheetField> fields)
    {
        Name = name;
        Path = path;
        Shape = shape;
        RowKey = key;
        KeyColumn = keyColumn;
        Fields = fields.ToList();
    }

    /// <summary>The sheet (and CSV file) name.</summary>
    public string Name { get; }

    /// <summary>How the rows sit in the JSON.</summary>
    public SheetShape Shape { get; }

    /// <summary>The key of a keyed-rows sheet (null otherwise).</summary>
    public RowKey RowKey { get; }

    /// <summary>A keyed sheet's key column: its value is the member name (null otherwise).</summary>
    public ColumnSpec KeyColumn { get; }

    /// <summary>The fields of a row object in JSON order; a values sheet has its one value column here.</summary>
    public IReadOnlyList<SheetField> Fields { get; }

    /// <summary>Leave the array or object out of the JSON when it has no rows.</summary>
    public bool OmitWhenEmpty { get; private set; }

    /// <summary>The header naming this sheet's rows in its child sheets (null when rows have no key).</summary>
    public string KeyHeader => Shape == SheetShape.Keyed ? KeyColumn.Header : RowKey?.Header;

    /// <summary>The columns of the row object, in order.</summary>
    public IEnumerable<ColumnSpec> Columns => Fields.OfType<ColumnSpec>();

    /// <summary>The child sheets, in order.</summary>
    public IEnumerable<SheetSpec> Children => Fields.OfType<SheetSpec>();

    /// <summary>A key for <see cref="Rows(string, string, RowKey, SheetField[])"/>: a column ("id") or a template ("{country}_{era}"), and the header child sheets name a row by.</summary>
    public static RowKey Key(string template, string childHeader = null) => new RowKey(template, childHeader);

    /// <summary>One object at <paramref name="path"/> ("" for the document itself): a one-row sheet.</summary>
    public static SheetSpec Single(string name, string path, params SheetField[] fields) =>
        new SheetSpec(name, path, SheetShape.Single, null, null, fields);

    /// <summary>An array of objects at <paramref name="path"/>, one row each, without a key.</summary>
    public static SheetSpec Rows(string name, string path, params SheetField[] fields) =>
        new SheetSpec(name, path, SheetShape.Rows, null, null, fields);

    /// <summary>An array of objects at <paramref name="path"/>, one row each, named by <paramref name="key"/> (unique under the parent row).</summary>
    public static SheetSpec Rows(string name, string path, RowKey key, params SheetField[] fields) =>
        new SheetSpec(name, path, SheetShape.Rows, key, null, fields);

    /// <summary>An object of objects at <paramref name="path"/> ("" for the parent row object's remaining members), one row per member named in <paramref name="keyColumn"/>.</summary>
    public static SheetSpec Keyed(string name, string path, ColumnSpec keyColumn, params SheetField[] fields) =>
        new SheetSpec(name, path, SheetShape.Keyed, null, keyColumn, fields);

    /// <summary>An array of plain values at <paramref name="path"/>, one row each in <paramref name="value"/>'s column.</summary>
    public static SheetSpec Values(string name, string path, ColumnSpec value) =>
        new SheetSpec(name, path, SheetShape.Values, null, null, new SheetField[] { value });

    /// <summary>Leaves the array or object out of the JSON when it has no rows.</summary>
    public SheetSpec OmitEmpty()
    {
        OmitWhenEmpty = true;
        return this;
    }

    /// <summary>A note for the README sheet.</summary>
    public SheetSpec Note(string text)
    {
        Doc = text;
        return this;
    }
}
