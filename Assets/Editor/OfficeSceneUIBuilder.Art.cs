using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// The office builder's art slots (redesign phase 27, ArtSlots): each
/// Tier-2 image of the gameplay layer gets its slot, which shows the slot's
/// file from Assets/Art/UI/Resources/ when it exists and keeps today's
/// code-drawn look when it does not. The speech bubble's body (tinted cream,
/// 9-slice) and its tail (hidden without art); the briefing's and the
/// ledger's sheets and the citation slip (tinted by their roles, as the flat
/// panels are); a reference book's cover on its case tile and at the top of
/// its window (both inactive until the cover is found at runtime); and the
/// desk paper's art quads, unlit and see-through, drawn over the printed
/// texts: the photo frame's art over the photo (inactive until found) and the
/// verdict's ink mark (inactive until the verdict).
/// The paper's face, the seal and the desktop icons look their art up
/// themselves. Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The speech bubble tail's size (reference px) under the bubble's bottom centre.</summary>
    private static readonly Vector2 BubbleTailSize = new Vector2(32f, 24f);

    /// <summary>A case tile's cover: its width (reference units) and its inset from the tile's left edge; it spans 80 % of the tile's height.</summary>
    private const float TileCoverWidth = 28f, TileCoverInset = 5f;

    /// <summary>The paper's art quads' render queue: over the paper's texts and the photo (3000).</summary>
    private const int PaperArtQueue = 3005;

    /// <summary>How far the photo frame's art lies in front of the photo frame (metres, the frame's local z; the photo is at 0.0005).</summary>
    private const float FrameArtLift = 0.0008f;

    /// <summary>
    /// Gives the gameplay layer's Tier-2 images their art slots (rebuilt with
    /// their hosts each run): the bubble, the two newsletters' sheets, the
    /// citation slip, the book tile and book window covers, and the desk
    /// paper template's photo frame and ink mark.
    /// </summary>
    private static void BuildArtSlots(Transform overlay, OverlayCallout bubble, Button caseTileTemplate, ReferenceBookWindowController bookTemplate, OfficeViewController officeView)
    {
        BuildBubbleArt(bubble);
        SlotOn(overlay.Find("BriefingPanel/Paper"), ArtSlots.BriefingPaper);
        SlotOn(overlay.Find("ResultsPanel/Paper"), ArtSlots.LedgerPaper);
        SlotOn(overlay.Find("CitationPanel"), ArtSlots.CitationSlip);
        BuildTileCover(caseTileTemplate);
        BuildBookCover(bookTemplate);
        BuildPaperArt(officeView.transform.Find("Desk/PaperTemplate"));
    }

    /// <summary>A light art slot on an existing image (its colour stays the art's tint).</summary>
    private static void SlotOn(Transform host, string slot)
    {
        if (host == null)
            return;
        GetOrAdd<ArtSlotImage>(host.gameObject).Configure(slot, null, false, false);
    }

    /// <summary>The bubble's body slot and its tail: a cream image under the bubble's bottom centre, pointing down, hidden until its art is found.</summary>
    private static void BuildBubbleArt(OverlayCallout bubble)
    {
        Transform panel = bubble != null ? bubble.transform.Find("Panel") : null;
        if (panel == null)
            return;
        SlotOn(panel, ArtSlots.SpeechBubble);

        Image body = panel.GetComponent<Image>();
        Transform tail = Panel(panel, "Tail", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 1f), BubbleTailSize, body.color, ThemeRoleId.DiegeticBubble);
        ((RectTransform)tail).pivot = new Vector2(0.5f, 1f);
        Image tailImage = tail.GetComponent<Image>();
        tailImage.raycastTarget = false;
        tailImage.preserveAspect = true;
        GetOrAdd<ArtSlotImage>(tail.gameObject).Configure(ArtSlots.SpeechBubbleTail, null, false, true);
    }

    /// <summary>The case tile template's cover (a reference book's tile shows it when the cover's art exists: DesktopTiles.Add), inactive, left of the label.</summary>
    private static void BuildTileCover(Button tileTemplate)
    {
        if (tileTemplate == null)
            return;
        Transform cover = Panel(tileTemplate.transform, "Cover", new Vector2(0f, 0.1f), new Vector2(0f, 0.9f), new Vector2(TileCoverInset, 0f), new Vector2(TileCoverWidth, 0f),
                                Color.white, ThemeRoleId.DiegeticPaper);
        ((RectTransform)cover).pivot = new Vector2(0f, 0.5f);
        Image image = cover.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        cover.gameObject.SetActive(false);
    }

    /// <summary>The book window's cover at its top left, under the title bar and over the rows (ReferenceBookWindowController shows it when the book's cover art exists), inactive.</summary>
    private static void BuildBookCover(ReferenceBookWindowController bookTemplate)
    {
        if (bookTemplate == null)
            return;
        var win = (RectTransform)bookTemplate.transform;
        float top = EnsureDesktopConfig().titleBarHeight;
        float rowsTop = win.sizeDelta.y * (1f - ((RectTransform)win.Find("Rows")).anchorMax.y);
        float height = Mathf.Max(8f, rowsTop - top - 4f);
        Transform cover = Panel(win, "Cover", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -(top + 2f)), new Vector2(height * 0.72f, height),
                                Color.white, ThemeRoleId.DiegeticPaper);
        ((RectTransform)cover).pivot = new Vector2(0f, 1f);
        Image image = cover.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        cover.gameObject.SetActive(false);

        var so = new SerializedObject(bookTemplate);
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
