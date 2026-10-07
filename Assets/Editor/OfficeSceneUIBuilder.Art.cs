using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// The office builder's art slots (redesign phase 27, ArtSlots): each
/// Tier-2 image of the gameplay layer gets its slot, which shows the slot's
/// file from Assets/Art/UI/Resources/ when it exists and keeps today's
/// code-drawn look when it does not (the speech bubble, the briefing's and
/// the ledger's sheets and the citation slip are the UI kit's since run 7:
/// OfficeSceneUIBuilder.Kit): a reference book's cover at the top of its register page in
/// the Investigation app's Reference tab (inactive until the cover is found
/// at runtime; the tab's book chips can show it through SlotArt.CoverFor);
/// and the
/// desk paper's art quads, unlit and see-through, drawn over the printed
/// texts: the photo frame's art over the photo (inactive until found) and the
/// verdict's ink mark (inactive until the verdict).
/// The paper's face, the seal and the desktop icons look their art up
/// themselves. Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The book cover's box on the register page (units): its width, as tall as the title band, its right edge this far left of the title band's end.</summary>
    private const float BookCoverWidth = 40f, BookCoverGap = 8f;

    /// <summary>The paper's art quads' render queue: over the paper's texts and the photo (3000).</summary>
    private const int PaperArtQueue = 3005;

    /// <summary>How far the photo frame's art lies in front of the photo frame (metres, the frame's local z; the photo is at 0.0005).</summary>
    private const float FrameArtLift = 0.0008f;

    /// <summary>
    /// Gives the gameplay layer's Tier-2 images their art slots (rebuilt with
    /// their hosts each run): each pane's Reference tab's book cover, and the
    /// desk paper template's photo frame and ink mark.
    /// </summary>
    private static void BuildArtSlots(IEnumerable<ReferenceView> references, OfficeViewController officeView)
    {
        foreach (ReferenceView reference in references)
            BuildBookCover(reference);
        BuildPaperArt(officeView.transform.Find("Desk/PaperTemplate"));
    }

    /// <summary>
    /// The Reference tab's book cover: in the view's strip, its left edge a gap
    /// in from the view's left (clear of "Claimed place only" at the right;
    /// ReferenceView shows it for the chosen book when the book's cover art
    /// exists), inactive.
    /// </summary>
    private static void BuildBookCover(ReferenceView reference)
    {
        if (reference == null)
            return;
        Transform cover = Panel(reference.transform, "Cover", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, Color.white, ThemeRoleId.DiegeticPaper);
        PlaceRect(cover, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(BookCoverGap, -(AppViewStripGap + AppViewStripHeight)),
                  new Vector2(BookCoverGap + BookCoverWidth, -AppViewStripGap));
        Image image = cover.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        cover.gameObject.SetActive(false);

        var so = new SerializedObject(reference);
        SetRef(so, "cover", image);
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// The paper template's art quads, inactive, in an unlit see-through
    /// material drawn over the texts: the photo frame's art over the photo
    /// (the frame's size; the grey frame behind stays the fallback) and the
    /// verdict's ink mark (a unit quad the paper places in its stamp area).
    /// </summary>
    private static void BuildPaperArt(Transform template)
    {
        DeskDocument doc = template != null ? template.GetComponent<DeskDocument>() : null;
        Transform sheet = template != null ? template.Find("Sheet") : null;
        if (doc == null || sheet == null)
            return;

        Material overlay = EnsureMaterial("Paper_Overlay", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_QueueOffset", PaperArtQueue - (int)RenderQueue.Transparent);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(m);
        });
        MeshRenderer frameArt = PaperArtQuad(sheet.Find("PhotoSlot"), "FrameArt", new Vector3(0f, 0f, -FrameArtLift), new Vector3(LookCanvas.PhotoAspect, 1f, 1f), overlay);
        MeshRenderer inkMark = PaperArtQuad(sheet, "Ink", Vector3.zero, Vector3.one, overlay);

        var so = new SerializedObject(doc);
        SetRef(so, "photoFrame", frameArt);
        SetRef(so, "inkMark", inkMark);
        so.ApplyModifiedProperties();
    }

    /// <summary>An inactive, shadowless art quad under <paramref name="parent"/>.</summary>
    private static MeshRenderer PaperArtQuad(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        PrimitivePart(parent, name, PrimitiveType.Quad, position, size, material);
        MeshRenderer quad = parent.Find(name).GetComponent<MeshRenderer>();
        quad.shadowCastingMode = ShadowCastingMode.Off;
        quad.gameObject.SetActive(false);
        return quad;
    }
}
