using UnityEngine;

/// <summary>
/// The forms' one style (redesign phase 4, PC spec FO7, §4.9, §6.4): the sizes
/// every form is laid out at (FormMetrics, in page heights) and the colours it
/// is printed in, on the desk paper now and on the PC's scanned copy from
/// phase 5. Forms are diegetic: no theme or culture touches them, so Build
/// Office UI checks these colours once (FormContrast), with the hover tint and
/// every theme's pick highlight laid over a box. Created by Build Office UI
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
        StampDash = Rgb(stampDash)
    };

    /// <summary>A Unity colour as an Rgba.</summary>
    public static Rgba Rgb(Color c) => new Rgba(c.r, c.g, c.b, c.a);
}
