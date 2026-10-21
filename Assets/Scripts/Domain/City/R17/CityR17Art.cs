using System;
using System.Collections.Generic;

/// <summary>
/// Where the imported r17 art sits (CITY-R17): <c>Assets/Data/City/R17/city_r17_art.json</c>, written by
/// <c>tools/art/city_r17/import_r17_art.py</c>. Each texture's padded size (the import pads to a multiple of 4 on the
/// right and bottom, so the data's pixel rects are unchanged and uv = px / size), and for the planes and the viaduct
/// paint, which are cropped to their painted pixels, the rect the image covers on the plate. Pure; tested in EditMode.
/// </summary>
public sealed class CityR17Art
{
    /// <summary>One imported file.</summary>
    public sealed class Entry
    {
        public string Path;
        public double SizeW, SizeH, UsedW, UsedH;
        public bool HasPlate;
        public CityR17Rect Plate;
    }

    public readonly Dictionary<string, Entry> Files = new Dictionary<string, Entry>(StringComparer.Ordinal);

    /// <summary>Parses the art map.</summary>
    public static CityR17Art Parse(string json)
    {
        ContentNode root = ContentJson.Parse(json);
        var art = new CityR17Art();
        foreach (KeyValuePair<string, ContentNode> kv in CityR17Json.Req(root, "files").Members)
        {
            double[] size = CityR17Json.Nums(kv.Value, "size"), used = CityR17Json.Nums(kv.Value, "used");
            var e = new Entry { Path = kv.Key, SizeW = size[0], SizeH = size[1], UsedW = used[0], UsedH = used[1] };
            ContentNode plate = kv.Value.Get("plate");
            if (plate != null && plate.Kind == ContentNodeKind.Array)
            {
                e.HasPlate = true;
                e.Plate = CityR17Json.Rect(plate);
            }
            art.Files[kv.Key] = e;
        }
        return art;
    }

    /// <summary>A file's entry by "Folder/name.png", or null.</summary>
    public Entry Get(string path) => Files.TryGetValue(path, out Entry e) ? e : null;

    /// <summary>
    /// The plate rect a cropped image covers, for a plane: the used pixels of the padded texture map to it, so the
    /// whole padded texture maps to a rect grown right and down by the padding at the same scale.
    /// </summary>
    public static CityR17Rect PaddedPlate(Entry e)
    {
        double sx = e.Plate.W / e.UsedW, sy = e.Plate.H / e.UsedH;
        return new CityR17Rect(e.Plate.X, e.Plate.Y, e.SizeW * sx, e.SizeH * sy);
    }
}
