using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>The kind of a <see cref="ContentNode"/>.</summary>
public enum ContentNodeKind
{
    /// <summary>An object: members in their written order.</summary>
    Object,

    /// <summary>An array.</summary>
    Array,

    /// <summary>A string.</summary>
    String,

    /// <summary>A number, kept as its literal.</summary>
    Number,

    /// <summary>true or false.</summary>
    Bool,

    /// <summary>null.</summary>
    Null
}

/// <summary>
/// One value of the content JSON (world_source.json). Objects keep their key order and
/// numbers keep their literal ("1.0" stays "1.0"), so a document read and written back
/// through <see cref="ContentJson"/> is byte-identical.
/// </summary>
public sealed class ContentNode
{
    private readonly List<KeyValuePair<string, ContentNode>> _members;
    private readonly List<ContentNode> _items;

    private ContentNode(ContentNodeKind kind, string text)
    {
        Kind = kind;
        Text = text;
        if (kind == ContentNodeKind.Object)
            _members = new List<KeyValuePair<string, ContentNode>>();
        else if (kind == ContentNodeKind.Array)
            _items = new List<ContentNode>();
    }

    /// <summary>What this value is.</summary>
    public ContentNodeKind Kind { get; }

    /// <summary>A string's text, a number's literal, "true"/"false" for a bool; null otherwise.</summary>
    public string Text { get; }

    /// <summary>An object's members in order (empty for other kinds).</summary>
    public IReadOnlyList<KeyValuePair<string, ContentNode>> Members => _members ?? (IReadOnlyList<KeyValuePair<string, ContentNode>>)System.Array.Empty<KeyValuePair<string, ContentNode>>();

    /// <summary>An array's items in order (empty for other kinds).</summary>
    public IReadOnlyList<ContentNode> Items => _items ?? (IReadOnlyList<ContentNode>)System.Array.Empty<ContentNode>();

    /// <summary>A number written without a point or an exponent (a JSON integer).</summary>
    public bool IsWholeNumber => Kind == ContentNodeKind.Number && Text.IndexOfAny(new[] { '.', 'e', 'E' }) < 0;

    /// <summary>A new empty object.</summary>
    public static ContentNode NewObject() => new ContentNode(ContentNodeKind.Object, null);

    /// <summary>A new empty array.</summary>
    public static ContentNode NewArray() => new ContentNode(ContentNodeKind.Array, null);

    /// <summary>A string value.</summary>
    public static ContentNode FromString(string text) => new ContentNode(ContentNodeKind.String, text ?? string.Empty);

    /// <summary>A number from its JSON literal (written back as is).</summary>
    public static ContentNode FromNumber(string literal) => new ContentNode(ContentNodeKind.Number, literal);

    /// <summary>true or false.</summary>
    public static ContentNode FromBool(bool value) => new ContentNode(ContentNodeKind.Bool, value ? "true" : "false");

    /// <summary>null.</summary>
    public static ContentNode Null() => new ContentNode(ContentNodeKind.Null, null);

    /// <summary>An object's member by key; null when absent (or when this is not an object).</summary>
    public ContentNode Get(string key)
    {
        if (_members == null)
            return null;
        foreach (KeyValuePair<string, ContentNode> m in _members)
            if (m.Key == key)
                return m.Value;
        return null;
    }

    /// <summary>Appends a member to an object; a key already present is an error.</summary>
    public void Add(string key, ContentNode value)
    {
        if (_members == null)
            throw new InvalidOperationException("Not an object.");
        if (Get(key) != null)
            throw new InvalidOperationException($"The key '{key}' is already set.");
        _members.Add(new KeyValuePair<string, ContentNode>(key, value));
    }

    /// <summary>Appends an item to an array.</summary>
    public void Add(ContentNode item)
    {
        if (_items == null)
            throw new InvalidOperationException("Not an array.");
        _items.Add(item);
    }

    /// <summary>Whether two values mean the same: numbers by value, objects member by member in order.</summary>
    public static bool Same(ContentNode a, ContentNode b)
    {
        if (a == null || b == null)
            return a == b;
        if (a.Kind != b.Kind)
            return false;
        switch (a.Kind)
        {
            case ContentNodeKind.Number:
                return double.Parse(a.Text, CultureInfo.InvariantCulture) == double.Parse(b.Text, CultureInfo.InvariantCulture);
            case ContentNodeKind.Array:
                if (a._items.Count != b._items.Count)
                    return false;
                for (int i = 0; i < a._items.Count; i++)
                    if (!Same(a._items[i], b._items[i]))
                        return false;
                return true;
            case ContentNodeKind.Object:
                if (a._members.Count != b._members.Count)
                    return false;
                for (int i = 0; i < a._members.Count; i++)
                    if (a._members[i].Key != b._members[i].Key || !Same(a._members[i].Value, b._members[i].Value))
                        return false;
                return true;
            default:
                return a.Text == b.Text;
        }
    }
}

/// <summary>
/// Reads and writes the content JSON in the layout of Python's
/// <c>json.dumps(indent=2, ensure_ascii=False)</c> plus a final newline, the layout
/// world_source.json is kept in: two-space indent, one item per line, "[]" and "{}"
/// for empty containers, text unescaped except quotes, backslashes and control
/// characters, and floats in Python's shortest repr ("1.0", "0.05", "1e-05").
/// </summary>
public static class ContentJson
{
    /// <summary>Parses JSON; a syntax error or a repeated key throws a <see cref="FormatException"/> naming the line and column.</summary>
    public static ContentNode Parse(string text)
    {
        var p = new Parser(text ?? string.Empty);
        p.SkipSpace();
        ContentNode root = p.Value();
        p.SkipSpace();
        if (!p.AtEnd)
            throw p.Error("unexpected text after the document");
        return root;
    }

    /// <summary>Writes a value in the source layout (LF line endings, a final newline).</summary>
    public static string Write(ContentNode root)
    {
        var sb = new StringBuilder();
        WriteValue(sb, root, 0);
        sb.Append('\n');
        return sb.ToString();
    }

    /// <summary>A double as Python's repr writes it: the shortest round-trip digits, fixed notation from 1e-4 up to 1e16 (with ".0" when whole), else "1.5e-07" style.</summary>
    public static string FloatRepr(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("JSON has no NaN or infinity.", nameof(value));
        if (value == 0)
            return (1 / value) < 0 ? "-0.0" : "0.0";

        string r = value.ToString("R", CultureInfo.InvariantCulture);
        bool negative = r[0] == '-';
        if (negative)
            r = r.Substring(1);

        // Split "d.dddE+xx" or "ddd.ddd" into significant digits and the exponent of the first digit.
        int exp = 0;
        int e = r.IndexOfAny(new[] { 'E', 'e' });
        if (e >= 0)
        {
            exp = int.Parse(r.Substring(e + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            r = r.Substring(0, e);
        }
        int dot = r.IndexOf('.');
        string intPart = dot < 0 ? r : r.Substring(0, dot);
        string fracPart = dot < 0 ? string.Empty : r.Substring(dot + 1);
        string digits = (intPart + fracPart).TrimStart('0');
        int leadingZeros = (intPart + fracPart).Length - digits.Length;
        int pointPos = intPart.Length - leadingZeros + exp;   // digits before the point
        digits = digits.TrimEnd('0');
        if (digits.Length == 0)
            digits = "0";
        int exponent = pointPos - 1;

        var sb = new StringBuilder();
        if (negative)
            sb.Append('-');
        if (exponent >= -4 && exponent < 16)
        {
            if (pointPos <= 0)
                sb.Append("0.").Append('0', -pointPos).Append(digits);
            else if (pointPos >= digits.Length)
                sb.Append(digits).Append('0', pointPos - digits.Length).Append(".0");
            else
                sb.Append(digits, 0, pointPos).Append('.').Append(digits, pointPos, digits.Length - pointPos);
        }
        else
        {
            sb.Append(digits[0]);
            if (digits.Length > 1)
                sb.Append('.').Append(digits, 1, digits.Length - 1);
            sb.Append('e').Append(exponent < 0 ? '-' : '+').Append(Math.Abs(exponent).ToString("00", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static void WriteValue(StringBuilder sb, ContentNode v, int depth)
    {
        switch (v.Kind)
        {
            case ContentNodeKind.Object:
                if (v.Members.Count == 0)
                {
                    sb.Append("{}");
                    return;
                }
                sb.Append('{');
                for (int i = 0; i < v.Members.Count; i++)
                {
                    sb.Append(i == 0 ? "\n" : ",\n").Append(' ', (depth + 1) * 2);
                    WriteString(sb, v.Members[i].Key);
                    sb.Append(": ");
                    WriteValue(sb, v.Members[i].Value, depth + 1);
                }
                sb.Append('\n').Append(' ', depth * 2).Append('}');
                return;
            case ContentNodeKind.Array:
                if (v.Items.Count == 0)
                {
                    sb.Append("[]");
                    return;
                }
                sb.Append('[');
                for (int i = 0; i < v.Items.Count; i++)
                {
                    sb.Append(i == 0 ? "\n" : ",\n").Append(' ', (depth + 1) * 2);
                    WriteValue(sb, v.Items[i], depth + 1);
                }
                sb.Append('\n').Append(' ', depth * 2).Append(']');
                return;
            case ContentNodeKind.String:
                WriteString(sb, v.Text);
                return;
            case ContentNodeKind.Null:
                sb.Append("null");
                return;
            default:
                sb.Append(v.Text);
                return;
        }
    }

    private static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    /// <summary>A strict JSON reader that remembers where it is for error messages.</summary>
    private sealed class Parser
    {
        private readonly string _s;
        private int _i;

        public Parser(string s) => _s = s;

        public bool AtEnd => _i >= _s.Length;

        public FormatException Error(string what)
        {
            int line = 1, col = 1;
            for (int k = 0; k < _i && k < _s.Length; k++)
            {
                if (_s[k] == '\n')
                {
                    line++;
                    col = 1;
                }
                else
                    col++;
            }
            return new FormatException($"JSON line {line}, column {col}: {what}.");
        }

        public void SkipSpace()
        {
            while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r' || _s[_i] == '﻿'))
                _i++;
        }

        public ContentNode Value()
        {
            if (AtEnd)
                throw Error("unexpected end of the document");
            char c = _s[_i];
            switch (c)
            {
                case '{': return Object();
                case '[': return Array();
                case '"': return ContentNode.FromString(String());
                case 't': Word("true"); return ContentNode.FromBool(true);
                case 'f': Word("false"); return ContentNode.FromBool(false);
                case 'n': Word("null"); return ContentNode.Null();
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                        return ContentNode.FromNumber(Number());
                    throw Error($"unexpected '{c}'");
            }
        }

        private void Word(string w)
        {
            if (string.CompareOrdinal(_s, _i, w, 0, w.Length) != 0)
                throw Error($"expected '{w}'");
            _i += w.Length;
        }

        private ContentNode Object()
        {
            var obj = ContentNode.NewObject();
            _i++;
            SkipSpace();
            if (!AtEnd && _s[_i] == '}')
            {
                _i++;
                return obj;
            }
            while (true)
            {
                SkipSpace();
                if (AtEnd || _s[_i] != '"')
                    throw Error("expected a key in quotes");
                int keyAt = _i;
                string key = String();
                if (obj.Get(key) != null)
                {
                    _i = keyAt;
                    throw Error($"the key '{key}' appears twice");
                }
                SkipSpace();
                if (AtEnd || _s[_i] != ':')
                    throw Error("expected ':'");
                _i++;
                SkipSpace();
                obj.Add(key, Value());
                SkipSpace();
                if (AtEnd)
                    throw Error("unexpected end inside an object");
                if (_s[_i] == ',')
                {
                    _i++;
                    continue;
                }
                if (_s[_i] == '}')
                {
                    _i++;
                    return obj;
                }
                throw Error("expected ',' or '}'");
            }
        }

        private ContentNode Array()
        {
            var arr = ContentNode.NewArray();
            _i++;
            SkipSpace();
            if (!AtEnd && _s[_i] == ']')
            {
                _i++;
                return arr;
            }
            while (true)
            {
                SkipSpace();
                arr.Add(Value());
                SkipSpace();
                if (AtEnd)
                    throw Error("unexpected end inside an array");
                if (_s[_i] == ',')
                {
                    _i++;
                    continue;
                }
                if (_s[_i] == ']')
                {
                    _i++;
                    return arr;
                }
                throw Error("expected ',' or ']'");
            }
        }

        private string String()
        {
            var sb = new StringBuilder();
            _i++;
            while (true)
            {
                if (AtEnd)
                    throw Error("unterminated string");
                char c = _s[_i++];
                if (c == '"')
                    return sb.ToString();
                if (c < 0x20)
                {
                    _i--;
                    throw Error("a control character inside a string");
                }
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (AtEnd)
                    throw Error("unterminated escape");
                char e = _s[_i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (_i + 4 > _s.Length || !int.TryParse(_s.Substring(_i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                            throw Error("a bad \\u escape");
                        sb.Append((char)code);
                        _i += 4;
                        break;
                    default:
                        _i--;
                        throw Error($"an unknown escape '\\{e}'");
                }
            }
        }

        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private string Number()
        {
            int start = _i;
            if (_s[_i] == '-')
                _i++;
            if (AtEnd || !IsDigit(_s[_i]))
                throw Error("a bad number");
            if (_s[_i] == '0' && _i + 1 < _s.Length && IsDigit(_s[_i + 1]))
                throw Error("a number with a leading zero");
            while (!AtEnd && IsDigit(_s[_i]))
                _i++;
            if (!AtEnd && _s[_i] == '.')
            {
                _i++;
                if (AtEnd || !IsDigit(_s[_i]))
                    throw Error("a bad number");
                while (!AtEnd && IsDigit(_s[_i]))
                    _i++;
            }
            if (!AtEnd && (_s[_i] == 'e' || _s[_i] == 'E'))
            {
                _i++;
                if (!AtEnd && (_s[_i] == '+' || _s[_i] == '-'))
                    _i++;
                if (AtEnd || !IsDigit(_s[_i]))
                    throw Error("a bad number");
                while (!AtEnd && IsDigit(_s[_i]))
                    _i++;
            }
            return _s.Substring(start, _i - start);
        }
    }
}
