using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The office builder's taskbar and tray, and the window controls (minimise,
/// maximise, close) with their glyphs.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.cs by P0-A (pure move). Owner: UI-PC.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    private static readonly Color XpGreen = new Color(0.18f, 0.49f, 0.2f, 1f);    // Start button (#2E7D32: white reads on it)

    private static readonly Color XpRed = new Color(0.77f, 0.235f, 0.17f, 1f);    // close button (#C43C2C: white reads on it)

    /// <summary>Where the taskbar's row of window buttons and tray starts: after the Menu and Back to desk buttons.</summary>
    private const float TrayRowFrom = PcSize.S + MenuButtonWidth + PcSize.S + DeskButtonWidth + PcSize.L;

    /// <summary>
    /// The taskbar (the PC UX redesign C7), rebuilt fresh: a flat bar in the
    /// Taskbar role (the culture's deep colour; no gloss), the Menu button
    /// (the StartButton role) at its left, the tray at its right with
    /// Credits, the Helix River's live strip (stability without a number;
    /// OfficeSceneUIBuilder.HelixRiver), today's date and the clock at Caption
    /// size in a row sized by their words, the tray as wide as they are in any
    /// language (QA2 2026-10-16: a fixed 840 let the Greek wallet run over the
    /// river and the date into the clock): it and the window buttons share a
    /// row (TrayRow) where the buttons take what the tray leaves. The date ("14 MAR 2150 · Day 1"; the
    /// desk-first redesign: "the date should be on the PC") sits beside the
    /// clock on a clear button: a click holds today on the workbench, to
    /// compare with an expiry or a ticket's date. Back to desk and the window
    /// buttons are added after it (BuildDesktopShell, BuildWindowManager).
    /// </summary>
    private static void BuildTaskbar(Transform root, out TMP_Text dayText, out Button dateButton, out TMP_Text moneyText, out TMP_Text clockText)
    {
        DestroyChildIfPresent(root, "Taskbar");
        Transform bar = Panel(root, "Taskbar", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, TaskbarHeight / 2f), new Vector2(0f, TaskbarHeight), XpBlue, ThemeRoleId.Taskbar);

        Transform start = Panel(bar, "StartButton", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(PcSize.S + MenuButtonWidth / 2f, 0f), new Vector2(MenuButtonWidth, -PcSize.S),
                                XpGreen, ThemeRoleId.StartButton);
        TMP_Text menu = Text(start, "Label", null, PcType.Body, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white,
                             ThemeRoleId.StartButton, "taskbar.start", FontStyles.Bold, ThemeTextKind.Button, true);
        menu.raycastTarget = false;

        // The row the window buttons (BuildWindowManager) and the tray share: the tray as wide as its words, the buttons the rest.
        Transform trayRow = Panel(bar, "TrayRow", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(trayRow, Vector2.zero, Vector2.one, new Vector2(TrayRowFrom, 0f), new Vector2(-PcSize.S, 0f));
        HorizontalLayoutGroup shared = GetOrAdd<HorizontalLayoutGroup>(trayRow.gameObject);
        shared.spacing = PcSize.L;
        shared.childAlignment = TextAnchor.MiddleRight;
        shared.childControlWidth = true;
        shared.childControlHeight = true;
        shared.childForceExpandWidth = false;
        shared.childForceExpandHeight = true;

        // System tray: Credits | the Helix River | Date | Clock, right to left from the bar's end, each as wide as its words (the river as its strip).
        Transform tray = Panel(trayRow, "Tray", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.1f, 0.32f, 0.78f, 1f), ThemeRoleId.Tray);
        tray.GetComponent<Image>().raycastTarget = false;
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(tray.gameObject);
        row.padding = new RectOffset(12, 12, 0, 0);
        row.spacing = 14f;
        row.childAlignment = TextAnchor.MiddleRight;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        moneyText = TrayText(tray, "MoneyText", "Credits: 0");
        BuildTrayRiver(tray);
        dateButton = MakeButton(tray, "DateButton", null, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f), ThemeRoleId.ClickCatcher);
        DestroyChildIfPresent(dateButton.transform, "Label");
        HorizontalLayoutGroup datePad = GetOrAdd<HorizontalLayoutGroup>(dateButton.gameObject);
        datePad.padding = new RectOffset(8, 8, 0, 0);
        datePad.childAlignment = TextAnchor.MiddleCenter;
        datePad.childControlWidth = true;
        datePad.childControlHeight = true;
        datePad.childForceExpandWidth = false;
        datePad.childForceExpandHeight = true;
        dayText = TrayText(dateButton.transform, "DayText", "14 MAR 2150 · Day 1");
        dayText.fontStyle = FontStyles.Underline;
        BuildHoverHint(dateButton, "tray.date.hint", null, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f));
        clockText = TrayText(tray, "ClockText", "09:00");
        // The row is NOT laid out here: the layout groups lay it out when the scene loads, as every
        // other layout group this builder makes. Laying it out before the save (round 4 did) marks the
        // readouts' rects as driven, and Unity writes driven rect values as zero: the saved scene then
        // differed from the rebuild held in memory (the final pass, round 5).

        bar.SetAsLastSibling();
    }

    /// <summary>A tray readout: one line at Caption size in the tray's ink, as wide as its text.</summary>
    private static TMP_Text TrayText(Transform tray, string name, string sample)
    {
        TMP_Text text = Text(tray, name, sample, PcType.Caption, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white, ThemeRoleId.Tray);
        Chrome(text, PcType.Caption);
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// Builds WORKING min/max/close buttons on a window header and wires an
    /// DesktopWindow on the window root. Shared by every window type so the
    /// chrome behaves identically everywhere. Destroys older decorative
    /// controls (pre-chrome builds used plain panels) before rebuilding.
    /// </summary>
    private static DesktopWindow BuildWinControls(Transform win, Transform header)
    {
        DestroyChildIfPresent(header, "MinBtn");
        DestroyChildIfPresent(header, "MaxBtn");
        DestroyChildIfPresent(header, "CloseBtn");

        Button minB = WindowControl(header, "MinBtn", 2, ThemeRoleId.Tab, "window.minimize");
        GlyphLine(minB.transform, "Bar", new Vector2(0f, -6f), new Vector2(16f, 2.5f), 0f);
        Button maxB = WindowControl(header, "MaxBtn", 1, ThemeRoleId.Tab, "window.maximize");
        GlyphLine(maxB.transform, "Top", new Vector2(0f, 7f), new Vector2(16f, 2.5f), 0f);
        GlyphLine(maxB.transform, "Bottom", new Vector2(0f, -7f), new Vector2(16f, 2.5f), 0f);
        GlyphLine(maxB.transform, "Left", new Vector2(-7f, 0f), new Vector2(2.5f, 16f), 0f);
        GlyphLine(maxB.transform, "Right", new Vector2(7f, 0f), new Vector2(2.5f, 16f), 0f);
        Button closeB = WindowControl(header, "CloseBtn", 0, ThemeRoleId.CloseButton, "window.close");
        GlyphLine(closeB.transform, "Stroke1", Vector2.zero, new Vector2(2.5f, 20f), 45f);
        GlyphLine(closeB.transform, "Stroke2", Vector2.zero, new Vector2(2.5f, 20f), -45f);

        DesktopWindow chrome = win.GetComponent<DesktopWindow>();
        if (chrome == null)
            chrome = win.gameObject.AddComponent<DesktopWindow>();
        var so = new SerializedObject(chrome);
        SetRef(so, "window", (RectTransform)win);
        SetRef(so, "minimizeButton", minB);
        SetRef(so, "maximizeButton", maxB);
        SetRef(so, "closeButton", closeB);
        so.ApplyModifiedProperties();
        return chrome;
    }

    /// <summary>
    /// A title-bar control (the PC UX redesign C1): a ghost button of
    /// PcSize.WindowControl, <paramref name="slot"/> places from the bar's
    /// right end (0 = the last), whose plate (<paramref name="role"/>: the
    /// steel Tab, or the close's red) shows only under the pointer (its tint's
    /// alpha is 0 at rest) and whose glyph is drawn by GlyphLine in the title
    /// bar's ink; a hover hint names it (<paramref name="hintKey"/>).
    /// </summary>
    private static Button WindowControl(Transform header, string name, int slot, ThemeRoleId role, string hintKey)
    {
        float w = PcSize.WindowControl;
        Transform plate = Panel(header, name, new Vector2(1f, 0f), Vector2.one, new Vector2(-(slot * w + w / 2f), 0f), new Vector2(w, 0f), role == ThemeRoleId.CloseButton ? XpRed : XpBlue, role);
        Button button = GetOrAdd<Button>(plate.gameObject);
        button.targetGraphic = plate.GetComponent<Image>();
        ColorBlock tint = button.colors;
        tint.normalColor = new Color(1f, 1f, 1f, 0f);
        tint.highlightedColor = Color.white;
        tint.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        tint.selectedColor = new Color(1f, 1f, 1f, 0f);
        tint.disabledColor = new Color(1f, 1f, 1f, 0f);
        tint.fadeDuration = 0.08f;
        button.colors = tint;
        BuildHoverHint(button, hintKey, null, new Vector2(0.5f, 0f), new Vector2(slot == 0 ? 1f : 0.5f, 1f));
        return button;
    }

    /// <summary>One stroke of a drawn chrome glyph (a window control's): a plain rect in the title bar's ink, rotated by <paramref name="angle"/>, taking no raycasts.</summary>
    private static void GlyphLine(Transform parent, string name, Vector2 centre, Vector2 size, float angle)
    {
        Transform bar = Panel(parent, name, Center, Center, centre, size, Color.white);
        bar.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = bar.GetComponent<Image>();
        image.raycastTarget = false;
        SceneUiKit.Tag(image, ThemeRoleId.TitleBar, ThemePart.Ink);
    }
}
