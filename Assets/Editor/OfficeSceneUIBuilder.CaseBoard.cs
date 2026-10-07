using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's case board (the scanner app spec §2; CaseBoardView):
/// a view of each pane on the workbench's paper, one scroll the board fills
/// at run time from its templates (inactive, outside the scroll): a section's
/// band, a wrapping line, the three plates (a record on file in the match
/// colours, NO RECORD in the difference colours with a double frame like a
/// stamp, the seen-before flag in the holding colours), a rules-check row
/// (its subject, its value and its chip, VALID on a match plate, a failing
/// chip on a difference plate) and the cross-check table's row, header cell,
/// cell and glowing cell. Every graphic carries its theme role, every text
/// sits on an opaque plate of its own role (the contrast check). Built with
/// each pane (fresh, the app's one convergence policy). Part of
/// <see cref="OfficeSceneUIBuilder"/>; BuildAppPane calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The case board's row heights: a rules-check row, a table row, a plate, a section band (desktop units).</summary>
    private const float BoardRuleRow = 72f, BoardCell = 48f, BoardPlateHeight = 56f, BoardSection = 44f;

    /// <summary>A chip's width in a rules-check row.</summary>
    private const float BoardChip = 150f;

    /// <summary>The case board view (Board): a scroll over the pane, its templates and the workbench it logs on (wired by BuildInvestigationApp).</summary>
    private static CaseBoardView BuildCaseBoardView(Transform content, CompareController compare)
    {
        Transform root = ViewRoot(content, "CaseBoardView", WbScreen, ThemeRoleId.WindowBody);
        RectTransform list = BuildScrollList(root, "Board", Vector2.zero, Vector2.one, PcSize.S, WbScreen, ThemeRoleId.WindowBody);
        PlaceRect(list.parent.parent, Vector2.zero, Vector2.one, new Vector2(PcSize.L, PcSize.L), new Vector2(-PcSize.L, -PcSize.L));
        VerticalLayoutGroup stack = list.GetComponent<VerticalLayoutGroup>();
        stack.padding = new RectOffset(4, 12, 4, 24);

        Transform templates = Panel(root, "Templates", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        templates.gameObject.SetActive(false);

        // A section's band.
        Transform section = Panel(templates, "SectionTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbInfoBg, ThemeRoleId.Info);
        section.GetComponent<Image>().raycastTarget = false;
        BoardHeight(section, BoardSection);
        TMP_Text sectionText = WbText(section, "Text", null, "RULES CHECK", PcType.Caption, ThemeRoleId.Info, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        sectionText.characterSpacing = 3f;
        PlaceRect(sectionText.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.M, 0f), new Vector2(-PcSize.M, 0f));

        // A wrapping line on the board's paper (the scroll's own fill).
        TMP_Text line = LayoutText(templates, "LineTemplate", PcType.Body, FontStyles.Normal, ThemeRoleId.WindowBody);
        line.color = WbInk;
        line.margin = new Vector4(12f, 6f, 12f, 6f);

        GameObject plateMatch = BoardPlate(templates, "PlateMatchTemplate", WbOkBg, WbOk, ThemeRoleId.FindingMatch, true, false);
        GameObject plateDiffer = BoardPlate(templates, "PlateDifferTemplate", WbWarnBg, WbWarn, ThemeRoleId.FindingDiffer, false, true);
        GameObject plateHold = BoardPlate(templates, "PlateHoldTemplate", WbHoldBg, WbHold, ThemeRoleId.Holding, false, false);

        // A rules-check row: the subject over its value, the chip at the right.
        Button ruleRow = MakeButton(templates, "RuleRowTemplate", null, Vector2.zero, Vector2.one, WbSurface, ThemeRoleId.Surface);
        DestroyChildIfPresent(ruleRow.transform, "Label");
        KeepWhenDisabled(ruleRow);
        BoardHeight(ruleRow.transform, BoardRuleRow);
        HairlineFrame(ruleRow.transform);
        TMP_Text subject = WbText(ruleRow.transform, "Subject", null, "Valid Until (Travel Passport)", PcType.Body, ThemeRoleId.Surface, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        PlaceRect(subject.transform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(PcSize.M, 0f), new Vector2(-(BoardChip + PcSize.M * 2f), -4f));
        TMP_Text value = WbText(ruleRow.transform, "Value", null, "14 Mar 2150", PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.TopLeft);
        value.color = WbMuted;
        PlaceRect(value.transform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(PcSize.M, 4f), new Vector2(-(BoardChip + PcSize.M * 2f), 0f));
        BoardChipPlate(ruleRow.transform, "ChipValid", WbOkBg, WbOk, ThemeRoleId.FindingMatch, "VALID");
        BoardChipPlate(ruleRow.transform, "ChipFail", WbWarnBg, WbWarn, ThemeRoleId.FindingDiffer, "EXPIRED").gameObject.SetActive(false);

        // The cross-check table: a row (no graphic), a header cell, a cell, a glowing cell.
        var gridRow = (RectTransform)Panel(templates, "GridRowTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        HorizontalLayoutGroup cells = GetOrAdd<HorizontalLayoutGroup>(gridRow.gameObject);
        cells.spacing = 4f;
        cells.childControlWidth = cells.childControlHeight = true;
        cells.childForceExpandWidth = true;
        cells.childForceExpandHeight = true;
        GetOrAdd<LayoutElement>(gridRow.gameObject).minHeight = BoardCell;

        // A cross-check row (responsive): the detail's word over its cells, which wrap to the pane's width.
        var crossRow = (RectTransform)Panel(templates, "CrossRowTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        VerticalLayoutGroup crossStack = GetOrAdd<VerticalLayoutGroup>(crossRow.gameObject);
        crossStack.spacing = 4f;
        crossStack.padding = new RectOffset(4, 4, 4, 8);
        crossStack.childControlWidth = crossStack.childControlHeight = true;
        crossStack.childForceExpandWidth = true;
        crossStack.childForceExpandHeight = false;
        TMP_Text crossLabel = LayoutText(crossRow, "Label", PcType.Caption, FontStyles.Bold, ThemeRoleId.WindowBody);
        crossLabel.color = WbMuted;
        Transform crossCells = Panel(crossRow, "Cells", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        GetOrAdd<FlowLayoutGroup>(crossCells.gameObject);

        GameObject head = BoardCellTemplate(templates, "CellHeadTemplate", WbInfoBg, WbMuted, ThemeRoleId.Info, FontStyles.Bold, false);
        Button cell = BoardCellTemplate(templates, "CellTemplate", WbSurface, WbInk, ThemeRoleId.Surface, FontStyles.Normal, true).GetComponent<Button>();
        Button glow = BoardCellTemplate(templates, "CellGlowTemplate", WbWarnBg, WbWarn, ThemeRoleId.FindingDiffer, FontStyles.Bold, true).GetComponent<Button>();
        HairlineFrame(glow.transform, WbWarn, ThemeRoleId.FindingDiffer, 2f, "Glow");

        // The overlay: a chosen source's chip, a line's two values one over the other (a shimmer under a real difference), the fade slider.
        Button pick = BoardCellTemplate(templates, "CellPickTemplate", WbHoldBg, WbHold, ThemeRoleId.Holding, FontStyles.Bold, true).GetComponent<Button>();
        GameObject overlayCell = BoardOverlayCell(templates, "OverlayCellTemplate", WbSurface, WbInk, ThemeRoleId.Surface, false);
        GameObject overlayShimmer = BoardOverlayCell(templates, "OverlayShimmerTemplate", WbWarnBg, WbWarn, ThemeRoleId.FindingDiffer, true);
        Slider fade = BoardFadeSlider(templates);

        CaseBoardView view = root.gameObject.AddComponent<CaseBoardView>();
        var so = new SerializedObject(view);
        Wire(so, "content", list);
        Wire(so, "sectionTemplate", section.gameObject);
        Wire(so, "lineTemplate", line);
        Wire(so, "plateMatchTemplate", plateMatch);
        Wire(so, "plateDifferTemplate", plateDiffer);
        Wire(so, "plateHoldTemplate", plateHold);
        Wire(so, "ruleRowTemplate", ruleRow);
        Wire(so, "gridRowTemplate", gridRow);
        Wire(so, "crossRowTemplate", crossRow);
        Wire(so, "cellHeadTemplate", head);
        Wire(so, "cellTemplate", cell);
        Wire(so, "cellGlowTemplate", glow);
        Wire(so, "cellPickTemplate", pick);
        Wire(so, "overlayCellTemplate", overlayCell);
        Wire(so, "overlayShimmerTemplate", overlayShimmer);
        Wire(so, "fadeTemplate", fade);
        Wire(so, "compare", compare);
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>
    /// An overlay line's value cell: a plate in <paramref name="role"/> whose
    /// two texts ("Text", the first source's, and "TextB", the second's) lie
    /// one over the other, the slider fading one into the other; with
    /// <paramref name="shimmer"/>, a breathing glint behind them (a real difference).
    /// </summary>
    private static GameObject BoardOverlayCell(Transform parent, string name, Color fill, Color ink, ThemeRoleId role, bool shimmer)
    {
        Transform cell = Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
        cell.GetComponent<Image>().raycastTarget = false;
        LayoutElement size = GetOrAdd<LayoutElement>(cell.gameObject);
        size.minHeight = BoardCell;
        size.preferredHeight = BoardCell;
        size.flexibleWidth = 3f;
        if (shimmer)
        {
            Transform glint = Panel(cell, "Shimmer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(WbWarn.r, WbWarn.g, WbWarn.b, 0.25f), role);
            glint.GetComponent<Image>().raycastTarget = false;
        }
        foreach (string text in new[] { "Text", "TextB" })
        {
            TMP_Text value = WbText(cell, text, null, "4 Jul 2124", PcType.Caption, role, TextAlignmentOptions.MidlineLeft, shimmer ? FontStyles.Bold : FontStyles.Normal);
            value.color = ink;
            PlaceRect(value.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.S, 0f), new Vector2(-PcSize.S, 0f));
        }
        return cell.gameObject;
    }

    /// <summary>The overlay's fade slider: a hairline track, the filled part and the handle in the primary colour, 40 units tall in the board's stack.</summary>
    private static Slider BoardFadeSlider(Transform parent)
    {
        Transform root = Panel(parent, "FadeTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        BoardHeight(root, 40f);
        Transform track = Panel(root, "Background", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-24f, 6f), WbLineStrong, ThemeRoleId.HairlineStrong);
        Transform fillArea = Panel(root, "Fill Area", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-24f, 6f), null);
        Transform fill = Panel(fillArea, "Fill", Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, WbAction, ThemeRoleId.PrimaryAction);
        Transform slideArea = Panel(root, "Handle Slide Area", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, 0f), null);
        Transform handle = Panel(slideArea, "Handle", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(24f, -8f), WbAction, ThemeRoleId.PrimaryAction);
        track.GetComponent<Image>().raycastTarget = true;
        fill.GetComponent<Image>().raycastTarget = false;
        Slider slider = GetOrAdd<Slider>(root.gameObject);
        slider.fillRect = (RectTransform)fill;
        slider.handleRect = (RectTransform)handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        return slider;
    }

    /// <summary>A layer tab's size (desktop units).</summary>
    private static readonly Vector2 LayerTab = new Vector2(104f, 40f);

    /// <summary>The strip over a Documents view's copies that holds the layer tabs.</summary>
    private const float LayerStripHeight = 56f;

    /// <summary>
    /// A scanned copy's layer switch (the scanner app spec §2.4;
    /// ScanLayerSwitch): over the Documents view, PRINT, UV and CHIP at its top
    /// right (each a quiet button with a bar under its word while chosen),
    /// and the layer's panel over the copy below them, on the scanner's dark
    /// backing (the form style's): its heading, its lines (a scroll; a line
    /// that glows sits on a difference plate) and the wipe's bar. Hidden until
    /// a copy shows.
    /// </summary>
    private static ScanLayerSwitch BuildScanLayers(Transform view, FormStyleSO style)
    {
        DestroyChildIfPresent(view, "Layers");
        Transform root = Panel(view, "Layers", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        var tabs = new Button[3];
        string[] keys = { "layers.tab.print", "layers.tab.uv", "layers.tab.chip" };
        for (int i = 0; i < 3; i++)
        {
            Button tab = MakeButton(root, "Tab_" + keys[i].Substring(11), null, Vector2.one, Vector2.one, WbSurface, ThemeRoleId.Button, keys[i]);
            ButtonLabel(tab, PcType.Caption).fontStyle = FontStyles.Bold;
            HairlineFrame(tab.transform);
            float right = PcSize.M + (2 - i) * (LayerTab.x + PcSize.S);
            PlaceRect(tab.transform, Vector2.one, Vector2.one, new Vector2(-(right + LayerTab.x), -(PcSize.S + LayerTab.y)), new Vector2(-right, -PcSize.S));
            Transform selected = Panel(tab.transform, "Selected", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 2f), new Vector2(-12f, 4f), WbAction, ThemeRoleId.PrimaryAction);
            selected.GetComponent<Image>().raycastTarget = false;
            selected.gameObject.SetActive(i == 0);
            tabs[i] = tab;
        }

        Transform overlay = Panel(root, "Overlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, style.backing, ThemeRoleId.DiegeticBacking);
        PlaceRect(overlay, Vector2.zero, Vector2.one, new Vector2(PcSize.M, PcSize.M), new Vector2(-PcSize.M, -(PcSize.S * 2f + LayerTab.y)));
        TMP_Text title = Text(overlay, "Title", "UV LIGHT", PcType.Title, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, style.backingInk, ThemeRoleId.DiegeticBacking,
                              null, FontStyles.Bold, ThemeTextKind.Heading);
        title.raycastTarget = false;
        title.characterSpacing = 3f;
        PlaceRect(title.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(PcSize.L, -52f), new Vector2(-PcSize.L, -PcSize.S));
        RectTransform rows = BuildScrollList(overlay, "Rows", Vector2.zero, Vector2.one, PcSize.S, style.backing, ThemeRoleId.DiegeticBacking);
        PlaceRect(rows.parent.parent, Vector2.zero, Vector2.one, new Vector2(PcSize.L, PcSize.L), new Vector2(-PcSize.L, -60f));

        Transform templates = Panel(overlay, "Templates", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        templates.gameObject.SetActive(false);
        TMP_Text row = Text(templates, "RowTemplate", "Name: Pell Quimby", PcType.Body, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, style.backingInk,
                            ThemeRoleId.DiegeticBacking);
        row.textWrappingMode = TextWrappingModes.Normal;
        row.raycastTarget = false;
        row.richText = true;
        Transform glow = Panel(templates, "RowGlowTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbWarnBg, ThemeRoleId.FindingDiffer);
        glow.GetComponent<Image>().raycastTarget = false;
        VerticalLayoutGroup pad = GetOrAdd<VerticalLayoutGroup>(glow.gameObject);
        pad.padding = new RectOffset(10, 10, 4, 4);
        pad.childControlWidth = pad.childControlHeight = true;
        pad.childForceExpandWidth = true;
        pad.childForceExpandHeight = false;
        TMP_Text glowText = LayoutText(glow, "Text", PcType.Body, FontStyles.Bold, ThemeRoleId.FindingDiffer);
        glowText.color = WbWarn;

        Transform wipe = Panel(overlay, "Wipe", Vector2.zero, new Vector2(0.04f, 1f), Vector2.zero, Vector2.zero, WbHoldBg, ThemeRoleId.Holding);
        wipe.GetComponent<Image>().raycastTarget = false;
        wipe.gameObject.SetActive(false);
        overlay.gameObject.SetActive(false);

        ScanLayerSwitch layers = root.gameObject.AddComponent<ScanLayerSwitch>();
        var so = new SerializedObject(layers);
        Wire(so, "printTab", tabs[0]);
        Wire(so, "uvTab", tabs[1]);
        Wire(so, "chipTab", tabs[2]);
        Wire(so, "overlay", overlay.gameObject);
        Wire(so, "title", title);
        Wire(so, "rows", rows);
        Wire(so, "rowTemplate", row);
        Wire(so, "rowGlowTemplate", glow.gameObject);
        Wire(so, "wipe", wipe);
        so.ApplyModifiedProperties();
        root.gameObject.SetActive(false);
        return layers;
    }

    /// <summary>A layout height for a board part (its least and preferred height).</summary>
    private static void BoardHeight(Transform part, float height)
    {
        LayoutElement size = GetOrAdd<LayoutElement>(part.gameObject);
        size.minHeight = height;
        size.preferredHeight = height;
    }

    /// <summary>A plate of the board: <paramref name="fill"/> in <paramref name="role"/>, its text in <paramref name="ink"/> (bold); a button (the record's link) when <paramref name="button"/>; a double frame (a stamp) when <paramref name="stamp"/>.</summary>
    private static GameObject BoardPlate(Transform parent, string name, Color fill, Color ink, ThemeRoleId role, bool button, bool stamp)
    {
        Transform plate;
        if (button)
        {
            Button b = MakeButton(parent, name, null, Vector2.zero, Vector2.one, fill, role);
            DestroyChildIfPresent(b.transform, "Label");
            plate = b.transform;
        }
        else
        {
            plate = Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
            plate.GetComponent<Image>().raycastTarget = false;
        }
        BoardHeight(plate, BoardPlateHeight);
        TMP_Text text = WbText(plate, "Text", null, "NO RECORD ON FILE", PcType.Title, role, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        text.enableAutoSizing = true;
        text.fontSizeMax = PcType.Title;
        text.fontSizeMin = PcType.Caption;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.lineSpacing = -8f;
        text.color = ink;
        text.characterSpacing = stamp ? 4f : 1f;
        PlaceRect(text.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.L + 4f, 0f), new Vector2(-PcSize.L, 0f));
        if (stamp)
        {
            HairlineFrame(plate, ink, role, 3f, "Stamp");
            Transform inner = Panel(plate, "StampInner", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12f, -12f), null);
            HairlineFrame(inner, ink, role, 1f, "Frame");
        }
        return plate.gameObject;
    }

    /// <summary>A chip at a rules-check row's right: <paramref name="fill"/> in <paramref name="role"/> with its word in <paramref name="ink"/>.</summary>
    private static Transform BoardChipPlate(Transform row, string name, Color fill, Color ink, ThemeRoleId role, string sample)
    {
        Transform chip = Panel(row, name, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(BoardChip / 2f + PcSize.M), 0f), new Vector2(BoardChip, 34f), fill, role);
        chip.GetComponent<Image>().raycastTarget = false;
        TMP_Text word = WbText(chip, "Text", null, sample, PcType.Caption, role, TextAlignmentOptions.Center, FontStyles.Bold);
        word.color = ink;
        word.characterSpacing = 2f;
        return chip;
    }

    /// <summary>A cell of the table: a plate (a button when <paramref name="button"/>) with a wrapping text that grows the row.</summary>
    private static GameObject BoardCellTemplate(Transform parent, string name, Color fill, Color ink, ThemeRoleId role, FontStyles style, bool button)
    {
        Transform host;
        if (button)
        {
            Button cell = MakeButton(parent, name, null, Vector2.zero, Vector2.one, fill, role);
            DestroyChildIfPresent(cell.transform, "Label");
            KeepWhenDisabled(cell);
            host = cell.transform;
        }
        else
        {
            host = Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
            host.GetComponent<Image>().raycastTarget = false;
        }
        LayoutElement size = GetOrAdd<LayoutElement>(host.gameObject);
        size.minHeight = BoardCell;
        size.flexibleWidth = 1f;
        size.minWidth = 60f;
        VerticalLayoutGroup pad = GetOrAdd<VerticalLayoutGroup>(host.gameObject);
        pad.padding = new RectOffset(8, 8, 6, 6);
        pad.childAlignment = TextAnchor.MiddleLeft;
        pad.childControlWidth = pad.childControlHeight = true;
        pad.childForceExpandWidth = true;
        pad.childForceExpandHeight = false;
        TMP_Text text = LayoutText(host, "Text", PcType.Caption, style, role);
        text.color = ink;
        return host.gameObject;
    }

    /// <summary>A board button keeps its plate's colours while it cannot be clicked (a VALID row, a cell that agrees: shown, never dimmed).</summary>
    private static void KeepWhenDisabled(Button button)
    {
        ColorBlock colours = button.colors;
        colours.disabledColor = Color.white;
        button.colors = colours;
    }
}
