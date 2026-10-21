using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Reading the CITY-R17 data files (<c>Assets/Data/City/R17/*.json</c>, written by
/// <c>tools/art/city_r17/export_r17.js</c> from the running r17 page) through <see cref="ContentJson"/>:
/// typed getters that name the missing member in their error, so a cut with a renamed field fails loudly at
/// load instead of drawing nothing.
/// </summary>
public static class CityR17Json
{
    /// <summary>A required member.</summary>
    public static ContentNode Req(ContentNode node, string key)
    {
        ContentNode v = node?.Get(key);
        if (v == null)
            throw new FormatException($"city r17 data: missing '{key}'.");
        return v;
    }

    /// <summary>A number's value (the JSON literal parsed exactly, invariant culture).</summary>
    public static double Num(ContentNode node)
    {
        if (node == null || node.Kind != ContentNodeKind.Number)
            throw new FormatException("city r17 data: expected a number.");
        return double.Parse(node.Text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>A required number member.</summary>
    public static double Num(ContentNode node, string key) => Num(Req(node, key));

    /// <summary>A number member, or a default when absent or null.</summary>
    public static double Num(ContentNode node, string key, double fallback)
    {
        ContentNode v = node?.Get(key);
        return v == null || v.Kind != ContentNodeKind.Number ? fallback : Num(v);
    }

    /// <summary>A required whole-number member.</summary>
    public static int Int(ContentNode node, string key) => (int)Math.Round(Num(node, key));

    /// <summary>A string member, or null when absent or null.</summary>
    public static string Str(ContentNode node, string key)
    {
        ContentNode v = node?.Get(key);
        return v == null || v.Kind != ContentNodeKind.String ? null : v.Text;
    }

    /// <summary>A required string member.</summary>
    public static string ReqStr(ContentNode node, string key) => Str(node, key) ?? throw new FormatException($"city r17 data: missing text '{key}'.");

    /// <summary>A number array.</summary>
    public static double[] Nums(ContentNode node)
    {
        IReadOnlyList<ContentNode> items = node?.Items ?? throw new FormatException("city r17 data: expected an array.");
        var a = new double[items.Count];
        for (int i = 0; i < a.Length; i++)
            a[i] = Num(items[i]);
        return a;
    }

    /// <summary>A required number array member.</summary>
    public static double[] Nums(ContentNode node, string key) => Nums(Req(node, key));

    /// <summary>A whole-number array.</summary>
    public static int[] Ints(ContentNode node)
    {
        double[] d = Nums(node);
        var a = new int[d.Length];
        for (int i = 0; i < a.Length; i++)
            a[i] = (int)Math.Round(d[i]);
        return a;
    }

    /// <summary>A string array.</summary>
    public static string[] Strs(ContentNode node)
    {
        IReadOnlyList<ContentNode> items = node?.Items ?? throw new FormatException("city r17 data: expected an array.");
        var a = new string[items.Count];
        for (int i = 0; i < a.Length; i++)
            a[i] = items[i].Text;
        return a;
    }

    /// <summary>A required array member's items.</summary>
    public static IReadOnlyList<ContentNode> Items(ContentNode node, string key) => Req(node, key).Items;

    /// <summary>A rectangle [x, y, w, h].</summary>
    public static CityR17Rect Rect(ContentNode node)
    {
        double[] r = Nums(node);
        if (r.Length < 4)
            throw new FormatException("city r17 data: a rectangle needs 4 numbers.");
        return new CityR17Rect(r[0], r[1], r[2], r[3]);
    }

    /// <summary>A required rectangle member.</summary>
    public static CityR17Rect Rect(ContentNode node, string key) => Rect(Req(node, key));

    /// <summary>The look-dev's modulo (always in [0, n)), for doubles.</summary>
    public static double Mod(double a, double n) => ((a % n) + n) % n;

    /// <summary>The look-dev's modulo for whole numbers.</summary>
    public static long Mod(long a, long n) => ((a % n) + n) % n;

    /// <summary>JavaScript's Math.round (half rounds up, toward +infinity), unlike Math.Round's banker's rounding.</summary>
    public static double JsRound(double x) => Math.Floor(x + 0.5);

    /// <summary>Clamps a value.</summary>
    public static double Clamp(double x, double a, double b) => x < a ? a : x > b ? b : x;

    /// <summary>"#rrggbb" to 0..1 components (the look-dev's rgb()).</summary>
    public static double[] Hex(string h) => new[]
    {
        int.Parse(h.Substring(1, 2), NumberStyles.HexNumber) / 255.0,
        int.Parse(h.Substring(3, 2), NumberStyles.HexNumber) / 255.0,
        int.Parse(h.Substring(5, 2), NumberStyles.HexNumber) / 255.0,
    };
}

/// <summary>A rectangle on the plate (plate px, y down) or in a sheet (px).</summary>
public readonly struct CityR17Rect
{
    public readonly double X, Y, W, H;

    public CityR17Rect(double x, double y, double w, double h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public override string ToString() => $"[{X}, {Y}, {W}, {H}]";
}
