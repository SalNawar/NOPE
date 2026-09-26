/// <summary>A page component that lists rows a page at a time (the Investigation app's lists until they scroll): PgUp and PgDn, and the arrows past a page's end, turn its pages (the PC redesign KB4).</summary>
public interface IPagedRows
{
    /// <summary>Turns to the next page (<paramref name="direction"/> 1) or the previous one (-1); false when there is none that way.</summary>
    bool TurnPage(int direction);
}
