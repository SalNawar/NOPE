using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads the by-name art slots (redesign phase 27; ArtSlots names them and
/// holds the lookup rule): each slot is a file under Assets/Art/UI/Resources/
/// loaded through Resources, so a delivered PNG shows with no code change and
/// no rebuild. Every method returns null when the file is missing, and the
/// caller keeps its code-drawn look (the fallback). Nothing is cached here:
/// Resources keeps what it loaded, and a file added in the editor shows on the
/// next play.
/// </summary>
public static class SlotArt
{
    /// <summary>The first of <paramref name="slots"/> whose sprite exists (the slot's import is a Sprite: ArtSlotImporter), else null.</summary>
    public static Sprite Sprite(params string[] slots) => ArtSlots.First(slots, s => Resources.Load<Sprite>(s));

    /// <summary>The first of <paramref name="slots"/> whose texture exists, else null (the desk's 3D papers draw textures).</summary>
    public static Texture2D Texture(IEnumerable<string> slots) => ArtSlots.First(slots, s => Resources.Load<Texture2D>(s));

    /// <summary>
    /// A reference book's cover (Assets/Art/UI/Resources/Investigation/refbook_cover_&lt;id&gt;.png,
    /// the asset list's book ids), else null: the book's tile and its window's
    /// header show it, and the Investigation app's reference view can.
    /// </summary>
    public static Sprite CoverFor(ReferenceBookSO book) => book != null ? Sprite(ArtSlots.BookCover(book.category)) : null;
}
