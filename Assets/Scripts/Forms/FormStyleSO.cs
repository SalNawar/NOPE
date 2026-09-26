using UnityEngine;

/// <summary>
/// The forms' one style (redesign phases 4-5, PC spec FO7, §4.9, §6.4): the
/// sizes every form is laid out at (FormMetrics, in page heights) and the
/// colours it is printed in, on the desk paper (DeskDocument) and on the PC
/// (FormView): the paper, the inks, the strokes, the hover tint on a box, and
/// the scanner's backing and strip behind a scanned copy. Forms are diegetic:
/// no theme or culture touches them, so Build Office UI checks these colours
/// once (FormContrast), with the hover tint and every theme's pick highlight
/// laid over a box. Created by Build Office UI
/// (Assets/Data/Forms/FormStyle_Agency.asset); a designer's edits are kept.
/// </summary>
[CreateAssetMenu(fileName = "FormStyle_", menuName = "TimeDesk/Form Style", order = 12)]
public sealed class FormStyleSO : ScriptableObject
{
    /// <summary>The sizes, in page heights (the desk paper's aspect included; Build Office UI checks it against Desk_Default's paper).</summary>
    public FormMetrics metrics = new FormMetrics();

    /// <summary>The paper tone the colours are checked against (the placeholder paper is drawn in it).</summary>
    [Header("Colours")]
    public Color paper = new Color(0.95f, 0.92f, 0.82f, 1f);

    /// <summary>Values, the agency line, the title, section heads, cells, the barcode, a ticked box.</summary>
    public Color ink = new Color(0.1f, 0.09f, 0.08f, 1f);

    /// <summary>Box labels, the programme line, the form number and captions (dark enough for the held paper under the art's tonemapping).</summary>
    public Color labelInk = new Color(0.115f, 0.105f, 0.09f, 1f);

    /// <summary>Fine print and the serial.</summary>
    public Color finePrintInk = new Color(0.18f, 0.165f, 0.14f, 1f);

    /// <summary>Box outlines, rules and a checkbox's outline.</summary>
    public Color rule = new Color(0.357f, 0.325f, 0.278f, 1f);

    /// <summary>A box's fill, a shade lighter than the paper.</summary>
    public Color boxFill = new Color(0.975f, 0.955f, 0.9f, 1f);

    /// <summary>A section head's band and a table's head row.</summary>
    public Color band = new Color(0.87f, 0.84f, 0.76f, 1f);

    /// <summary>The stamp area's dashed outline.</summary>
    public Color stampDash = new Color(0.42f, 0.39f, 0.345f, 1f);

    /// <summary>The seal behind the header (its alpha is its tint: 10 %).</summary>
    public Color seal = new Color(0.1f, 0.09f, 0.08f, 0.1f);

    /// <summary>The tint on a pickable box under the pointer, on the held paper and on the PC (a picked box shows the compare highlight instead).</summary>
    public Color hoverTint = new Color(0f, 0f, 0f, 0.06f);

    /// <summary>The scanner's dark backing a scanned copy lies on (the PC's document window).</summary>
    [Header("Scanner backing")]
    public Color backing = new Color(0.129f, 0.137f, 0.161f, 1f);

    /// <summary>The scan strip's text on the backing.</summary>
    public Color backingInk = new Color(0.851f, 0.859f, 0.878f, 1f);

    /// <summary>The scan strip's printed words (English, like every form's): {0} is the shift clock's time when the copy arrived.</summary>
    public string scanStrip = "SCANNED {0} · DESK SCANNER 1";

    /// <summary>How much heavier the printed words are than the font (TextMeshPro's face dilate on the paper's text material): a held paper's small print needs the weight to read at 720p as drawn. Build Office UI writes it into the material.</summary>
    [Header("Print")]
    [Range(0f, 0.5f)] public float inkWeight = 0.2f;

    /// <summary>The colour a text of <paramref name="role"/> is printed in.</summary>
    public Color Ink(FormTextRole role)
    {
        switch (role)
        {
            case FormTextRole.Label:
            case FormTextRole.Programme:
            case FormTextRole.FormNumber:
            case FormTextRole.Caption:
                return labelInk;
            case FormTextRole.FinePrint:
            case FormTextRole.Serial:
                return finePrintInk;
            default:
                return ink;
        }
    }

    /// <summary>The colours as the contrast check reads them.</summary>
    public FormPalette Palette() => new FormPalette
    {
        Paper = Rgb(paper),
        Ink = Rgb(ink),
        LabelInk = Rgb(labelInk),
        FinePrintInk = Rgb(finePrintInk),
        Rule = Rgb(rule),
        BoxFill = Rgb(boxFill),
        Band = Rgb(band),
        StampDash = Rgb(stampDash),
        Backing = Rgb(backing),
        BackingInk = Rgb(backingInk)
    };

    /// <summary>A Unity colour as an Rgba.</summary>
    public static Rgba Rgb(Color c) => new Rgba(c.r, c.g, c.b, c.a);
}
