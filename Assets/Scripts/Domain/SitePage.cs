using System.Collections.Generic;
using System.Linq;

/// <summary>
/// What a block of a site page shows (the Internet, P spec IN1-IN6). Static
/// pages (world_source.json pc.pages) serialize it as an int: append only.
/// </summary>
public enum PageBlockKind
{
    /// <summary>The site's name band at the top of a page (Text).</summary>
    Masthead,

    /// <summary>The page's lead line: a headline, an article's or a card's title (Text).</summary>
    Headline,

    /// <summary>A section heading (Text).</summary>
    Heading,

    /// <summary>Body text (Text).</summary>
    Paragraph,

    /// <summary>Small print: a subtitle, a count, a caption (Text).</summary>
    Note,

    /// <summary>One link on its own line (Text, Address).</summary>
    Link,

    /// <summary>A boxed column: a title (Text), plain lines (Lines), then links (Links).</summary>
    Box,

    /// <summary>An infobox or a record card: boxed label and value cells (Fields).</summary>
    Fields,

    /// <summary>A table: column heads (Columns) and rows of cells (Rows); a cell may link.</summary>
    Table,

    /// <summary>The start page's site tiles (Links: the name, its blurb in Detail, its glyph).</summary>
    Tiles,

    /// <summary>A row of filter links, the chosen one Active (Text is the row's caption).</summary>
    Chips
}

/// <summary>A link on a page: its words and where it goes (a chronet:// address).</summary>
public sealed class PageLink
{
    /// <summary>The words the link shows.</summary>
    public string Text;

    /// <summary>Where it goes (Sites address); blank = shown as plain text.</summary>
    public string Address;

    /// <summary>A second line (a tile's blurb); blank = none.</summary>
    public string Detail;

    /// <summary>The art key of a tile's glyph (site_news, ...); blank = none.</summary>
    public string Glyph;

    /// <summary>True for the chosen chip of a filter row.</summary>
    public bool Active;
}

/// <summary>One boxed cell of an infobox or record card: a small-capitals label over its value.</summary>
public sealed class PageField
{
    /// <summary>The box's label ("CAPITAL", "BORN").</summary>
    public string Label;

    /// <summary>The value.</summary>
    public string Value;

    /// <summary>A second, smaller line under the value ("Revised on day 5 (was ...)"); blank = none.</summary>
    public string Note;

    /// <summary>Where the value links to; blank = no link.</summary>
    public string Address;
}

/// <summary>One cell of a table row.</summary>
public sealed class PageCell
{
    /// <summary>The cell's text.</summary>
    public string Text;

    /// <summary>Where the cell links to; blank = no link.</summary>
    public string Address;
}

/// <summary>One block of a site page (PageBlockKind says which of its lists it uses).</summary>
public sealed class PageBlock
{
    /// <summary>What the block shows.</summary>
    public PageBlockKind Kind;

    /// <summary>The block's text (a heading, a paragraph, a box's or chip row's caption, a link's words).</summary>
    public string Text;

    /// <summary>A Link block's address.</summary>
    public string Address;

    /// <summary>A Box's plain lines.</summary>
    public List<string> Lines = new List<string>();

    /// <summary>A Fields block's boxes.</summary>
    public List<PageField> Fields = new List<PageField>();

    /// <summary>A Table's column heads.</summary>
    public List<string> Columns = new List<string>();

    /// <summary>A Table's rows, each a list of cells.</summary>
    public List<List<PageCell>> Rows = new List<List<PageCell>>();

    /// <summary>A Box's, Tiles' or Chips' links.</summary>
    public List<PageLink> Links = new List<PageLink>();

    /// <summary>A text block of <paramref name="kind"/> (Masthead, Headline, Heading, Paragraph or Note).</summary>
    public static PageBlock Of(PageBlockKind kind, string text) => new PageBlock { Kind = kind, Text = text };

    /// <summary>A link on its own line.</summary>
    public static PageBlock LinkTo(string text, string address) => new PageBlock { Kind = PageBlockKind.Link, Text = text, Address = address };

    /// <summary>A boxed column with a title, lines and links.</summary>
    public static PageBlock Box(string title, IEnumerable<string> lines, IEnumerable<PageLink> links = null) => new PageBlock
    {
        Kind = PageBlockKind.Box, Text = title,
        Lines = lines?.ToList() ?? new List<string>(),
        Links = links?.ToList() ?? new List<PageLink>()
    };

    /// <summary>Every address this block links to (blank ones left out), in reading order.</summary>
    public IEnumerable<string> Addresses()
    {
        var all = new List<string> { Address };
        all.AddRange(Fields.Select(f => f.Address));
        all.AddRange(Rows.SelectMany(r => r.Select(c => c.Address)));
        all.AddRange(Links.Select(l => l.Address));
        return all.Where(a => !string.IsNullOrWhiteSpace(a));
    }
}

/// <summary>
/// One page of a site, as blocks: what the page builders (NewsPages,
/// HistoryPages, AncestryPages, a static page) return and the browser draws.
/// </summary>
public sealed class SitePage
{
    /// <summary>The page's own address (what the address field shows, what Back returns to).</summary>
    public string Address;

    /// <summary>The page's title (the browser's title bar).</summary>
    public string Title;

    /// <summary>The site the page belongs to (SiteSpec.id); null for the start page and a missing page.</summary>
    public string SiteId;

    /// <summary>False for the "page not found" page.</summary>
    public bool Found = true;

    /// <summary>The blocks, top to bottom.</summary>
    public List<PageBlock> Blocks = new List<PageBlock>();

    /// <summary>Every address the page links to, in reading order.</summary>
    public IEnumerable<string> Links() => Blocks.SelectMany(b => b.Addresses());
}

/// <summary>
/// The words a page builder writes (headings, captions, templates): the UI
/// string tables' "site.*" keys (world_source.json ui.strings) in the game,
/// fakes in the tests. Templates take {0}-style arguments.
/// </summary>
public interface IPageWords
{
    /// <summary>The words for a key, with its template's arguments filled.</summary>
    string Get(string key, params object[] args);
}
