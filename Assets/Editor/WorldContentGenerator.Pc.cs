using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's PC block (P spec IN2, §4.8): world_source.json "pc"
/// (the Internet's sites, the Static sites' authored pages and the Lineage
/// Archive's people and relations) is checked (Sites.Problems,
/// AncestryPages.Problems, the UI string keys the pages write with) and
/// written into the content library's pc block (ContentLibrarySO.Pc).
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>
    /// Reads the pc block into the library's shape and checks it: every site
    /// and block kind known, the content rules, and every Sites.WordKeys key
    /// in ui.strings. Returns what WritePc writes.
    /// </summary>
    private static PcContent CheckPc(WorldSource src, List<string> errors)
    {
        var pc = new PcContent();
        PcData data = src.pc;
        foreach (SiteData s in data?.sites ?? Array.Empty<SiteData>())
        {
            if (s == null)
                continue;
            if (!ParseEnum(s.kind, out SiteKind kind))
                errors.Add($"pc.sites '{s.id}': kind '{s.kind}' is not one of {string.Join(", ", Enum.GetNames(typeof(SiteKind)))}.");
            pc.sites.Add(new SiteSpec { id = s.id, kind = kind, name = s.name, domain = s.domain, glyph = s.glyph, blurb = s.blurb, fromDay = s.fromDay });
        }

        foreach (StaticPageData p in data?.pages ?? Array.Empty<StaticPageData>())
        {
            if (p == null)
                continue;
            var page = new StaticPage { site = p.site, path = p.path ?? string.Empty, title = p.title };
            foreach (StaticBlockData b in p.blocks ?? Array.Empty<StaticBlockData>())
            {
                if (b == null)
                    continue;
                if (!ParseEnum(b.kind, out PageBlockKind kind))
                    errors.Add($"pc.pages '{p.site}/{p.path}': block kind '{b.kind}' is not one of {string.Join(", ", Sites.StaticKinds)}.");
                page.blocks.Add(new StaticBlock { kind = kind, text = b.text, address = b.address, lines = (b.lines ?? Array.Empty<string>()).ToList() });
            }
            pc.pages.Add(page);
        }

        pc.ancestry = new AncestryContent
        {
            includePremades = data?.ancestry == null || data.ancestry.includePremades,
            people = (data?.ancestry?.people ?? Array.Empty<PersonEntry>()).Where(p => p != null).ToList(),
            relations = (data?.ancestry?.relations ?? Array.Empty<RelationEntry>()).Where(r => r != null).ToList()
        };

        errors.AddRange(Sites.Problems(pc));
        PremadeData[] premades = src.premades ?? Array.Empty<PremadeData>();
        IEnumerable<string> travellerNames = src.places.SelectMany(p => (p.maleNames ?? Array.Empty<string>()).Concat(p.femaleNames ?? Array.Empty<string>()))
                                                .Concat(premades.Select(m => m.name));
        errors.AddRange(AncestryPages.Problems(pc.ancestry, premades.Select(m => m.id).ToList(), new HashSet<string>(src.places.Select(PlaceId)), travellerNames));

        var keys = new HashSet<string>((src.ui?.strings ?? Array.Empty<StringData>()).Where(s => s != null).Select(s => s.key));
        foreach (string key in Sites.WordKeys)
            RequireKey(keys, key, "the Internet's pages (Sites.WordKeys)", errors);
        return pc;
    }

    /// <summary>Writes the checked pc block into the library.</summary>
    private static void WritePc(ContentLibrarySO lib, PcContent pc)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("pc").boxedValue = pc;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }

    // -----------------------------
    // Source file shape (JsonUtility)
    // -----------------------------

    /// <summary>world_source.json "pc".</summary>
    [Serializable] private sealed class PcData
    {
        public SiteData[] sites;
        public StaticPageData[] pages;
        public AncestryData ancestry;
    }

    /// <summary>One site; kind is a SiteKind name.</summary>
    [Serializable] private sealed class SiteData
    {
        public string id;
        public string kind;
        public string name;
        public string domain;
        public string glyph;
        public string blurb;
        public int fromDay = 1;
    }

    /// <summary>One authored page of a Static site.</summary>
    [Serializable] private sealed class StaticPageData { public string site; public string path; public string title; public StaticBlockData[] blocks; }

    /// <summary>One authored block; kind is a PageBlockKind name (Sites.StaticKinds).</summary>
    [Serializable] private sealed class StaticBlockData { public string kind; public string text; public string address; public string[] lines; }

    /// <summary>The Lineage Archive's content.</summary>
    [Serializable] private sealed class AncestryData
    {
        public bool includePremades = true;
        public PersonEntry[] people;
        public RelationEntry[] relations;
    }
}
