using System;

/// <summary>
/// A place on a document's art (the Canva documents, run 7): a rectangle as
/// shares of the face, from its top-left (x right, y down, 0 to 1), so it
/// holds at any size the face is drawn.
/// </summary>
[Serializable]
public struct ArtBox
{
    /// <summary>The left edge, as a share of the face's width.</summary>
    public float x0;

    /// <summary>The top edge, as a share of the face's height.</summary>
    public float y0;

    /// <summary>The right edge, as a share of the face's width.</summary>
    public float x1;

    /// <summary>The bottom edge, as a share of the face's height.</summary>
    public float y1;

    /// <summary>True when the box has an area.</summary>
    public bool IsSet => x1 > x0 && y1 > y0;

    /// <summary>True when the box lies on the face (every edge from 0 to 1).</summary>
    public bool OnFace => x0 >= 0f && y0 >= 0f && x1 <= 1f && y1 <= 1f;

    /// <summary>The box on a face <paramref name="width"/> by <paramref name="height"/> (form space).</summary>
    public FaceRect On(float width, float height) => new FaceRect(x0 * width, y0 * height, x1 * width, y1 * height);
}

/// <summary>What a print on the art shows that is no field of its own (ArtPrint): it is never picked, and never says more than the paper's fields and words do.</summary>
public enum ArtPrintKind
{
    /// <summary>Another place's copy of a field's shown value (a ticket's stub repeats its date).</summary>
    Copy = 0,

    /// <summary>A field's shown value in the holder's hand (a passport's signature is its name).</summary>
    Signature = 1,

    /// <summary>The holder's nation's three-letter code (FormData.NationCode: a passport's nationality).</summary>
    NationCode = 2,

    /// <summary>The paper's serial (FormData.Serial: a return order's number).</summary>
    Serial = 3,

    /// <summary>The issuing office's name in a hand (FormData.Programme: who authorised it).</summary>
    Office = 4,

    /// <summary>The print's own words (ArtPrint.text: a ticket stub's gate).</summary>
    Text = 5
}

/// <summary>One template field's places on the art.</summary>
[Serializable]
public sealed class ArtField
{
    /// <summary>The template field (its index in the template's fields).</summary>
    public int field = -1;

    /// <summary>Where its value prints and is picked (the seal's or the photo's window for those fields).</summary>
    public ArtBox value;

    /// <summary>Its label's place on the art (none: the art prints none): painted out from the blank face while the field is not introduced, and where the game prints the field's own label when it relabels.</summary>
    public ArtBox label;

    /// <summary>True when the art's baked label is not the field's (it is painted out of the face): the game prints the template's label there, in the art's label ink.</summary>
    public bool relabel;
}

/// <summary>A print on the art that is no field (ArtPrintKind): never picked.</summary>
[Serializable]
public sealed class ArtPrint
{
    /// <summary>What it prints.</summary>
    public ArtPrintKind kind;

    /// <summary>The field a Copy or a Signature repeats, else -1.</summary>
    public int field = -1;

    /// <summary>A Text print's words (English: documents are diegetic).</summary>
    public string text = string.Empty;

    /// <summary>Where it prints.</summary>
    public ArtBox rect;
}

/// <summary>
/// A document drawn on its art (the Canva documents, Saleh 2026-10-07: "use
/// Canva to design the documents"): the face is the kind's paper art
/// (ArtSlots.PaperFaces; the labels, frames and emblems are printed on it), and
/// the game prints only what changes on the art's places: each field's value,
/// the photo in its window, the issuing seal at its spot, the passport's ENTRY
/// VISA box and the prints that are no field. Held by the form's look
/// (FormLook.art), so the desk paper and its scanned copy draw the same; set,
/// it replaces the form's blocks in the layout (ArtLayout), which keep only
/// the fields' pages and the seal's and the photo's fields.
/// </summary>
[Serializable]
public sealed class FormArt
{
    /// <summary>The face's corner radius as a share of its width (a ticket's, a card's round corners: clear on the art, cut from the desk paper); 0: square.</summary>
    public float corner;

    /// <summary>The values' ink on this art ("#RRGGBB"; blank: the style's).</summary>
    public string ink = string.Empty;

    /// <summary>The ink of the labels the game prints (relabels) and the visa box's caption ("#RRGGBB"; blank: the style's label ink).</summary>
    public string labelInk = string.Empty;

    /// <summary>A value's type size as a share of its place's height (it shrinks to fit its place's width).</summary>
    public float valueShare = 0.5f;

    /// <summary>A printed label's type size as a share of its place's height.</summary>
    public float labelShare = 0.6f;

    /// <summary>The paper's size on the desk, full size, as a share of the desk's reading size (DeskConfigSO.readingHeight; 1: the same): a small or wide paper drawn with small print reads larger (the passport's booklet, the ticket, the card).</summary>
    public float reading = 1f;

    /// <summary>A photo window on a paper whose template shows no photo (the transponder card): the holder's photo fitted at 4:5, never picked (none: no such window).</summary>
    public ArtBox photo;

    /// <summary>Each field's places.</summary>
    public ArtField[] fields = new ArtField[0];

    /// <summary>The prints that are no field.</summary>
    public ArtPrint[] prints = new ArtPrint[0];

    /// <summary>The ENTRY VISA box where a verdict stamp goes (a passport's; none: no stamp area).</summary>
    public ArtBox stamp;

    /// <summary>The visa box's dash and caption ink ("#RRGGBB"; blank: the style's stamp dash and label ink): the kit's oxblood on the passport.</summary>
    public string stampInk = string.Empty;

    /// <summary>The visa box's caption, printed inside its top.</summary>
    public string stampCaption = string.Empty;

    /// <summary>A booklet's cover margins, drawn in its holder's nation's cover colour (FormData.Cover).</summary>
    public ArtBox[] covers = new ArtBox[0];

    /// <summary>A booklet's spine between its pages (none: no spine).</summary>
    public ArtBox spine;

    /// <summary>True when the form is drawn on its art.</summary>
    public bool IsSet => fields != null && fields.Length > 0;

    /// <summary>The places of template field <paramref name="field"/>, or null.</summary>
    public ArtField Of(int field)
    {
        foreach (ArtField f in fields ?? new ArtField[0])
            if (f != null && f.field == field)
                return f;
        return null;
    }
}
