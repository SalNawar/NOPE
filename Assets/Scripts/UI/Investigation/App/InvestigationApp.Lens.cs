using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Lens screen's sides, inspect mode and drags (the PC Lens spec, sections
/// 1 and 2). The app holds one LensSides (the pure place rule) and draws it
/// after every change: a side with a document goes there (AppPane.Go), an empty
/// side shows its empty state, and the selected side is the target. Inspect
/// (Space, or the Inspect key) is off at each new case and when the window
/// goes; with it off no value is picked (PcInspect) and the notice says "Press
/// Space to inspect."; turning it off lets go of a held pick and clears the
/// result line. A drag from a dock item (LensDockItem) drops its document on
/// the side under the pointer (a drop elsewhere does nothing); a drag that
/// starts on the document of a full side, with inspect off, moves it: over the
/// other side the two change places, outside both sides it is put away, on its
/// own side nothing happens (LensDocDrag takes the drag before the views'
/// scroll rects do).
/// </summary>
public sealed partial class InvestigationApp
{
    private static readonly LensSide[] Sides = { LensSide.Left, LensSide.Right };

    private readonly LensSides _sides = new LensSides();
    private readonly LinkTarget[] _applied = new LinkTarget[2];
    private readonly List<ScrollRect> _scrollScratch = new List<ScrollRect>();
    private LensSide? _moving;

    /// <summary>True while inspect is on (Space or the Inspect key): the PC's values, rule lines, date, chip lines and Registry line can be picked.</summary>
    public bool Inspecting { get; private set; }

    /// <summary>True while a pick is held or a result line shows (Escape's first step, then inspect off).</summary>
    public bool HasPick => Holding || (board != null && board.Line.a != null);

    /// <summary>Draws the sides once and makes them take the drags (once, from Init).</summary>
    private void InitLens()
    {
        foreach (AppPane pane in Panes())
            if (!pane.TryGetComponent(out LensDocDrag _))
                pane.gameObject.AddComponent<LensDocDrag>();
        for (int i = 0; i < _applied.Length; i++)
            _applied[i] = LinkTarget.None;
        ApplySides();
        RefreshChip();
        RefreshInspect();
    }

    /// <summary>The side of a pane.</summary>
    private LensSide SideOfPane(AppPane pane) => pane == leftPane ? LensSide.Left : LensSide.Right;

    /// <summary>The pane of a side.</summary>
    private AppPane PaneOf(LensSide side) => side == LensSide.Left ? leftPane : rightPane;

    /// <summary>The pane the keys act on: the selected side's, or the other's when only that one holds a document.</summary>
    private AppPane TargetPane
    {
        get
        {
            LensSide selected = _sides.Selected;
            return _sides[selected].IsNone && !_sides[LensSides.Other(selected)].IsNone ? PaneOf(LensSides.Other(selected)) : PaneOf(selected);
        }
    }

    /// <summary>The other pane.</summary>
    private AppPane Other(AppPane pane) => pane == leftPane ? rightPane : leftPane;

    /// <summary>A side's tag strip clicked (or F6): that side is selected.</summary>
    private void PaneClicked(AppPane pane)
    {
        if (pane == null)
            return;
        _sides.Select(SideOfPane(pane));
        Targets();
    }

    /// <summary>The selected side's strip shows it.</summary>
    private void Targets()
    {
        if (leftPane != null)
            leftPane.SetTarget(_sides.Selected == LensSide.Left);
        if (rightPane != null)
            rightPane.SetTarget(_sides.Selected == LensSide.Right);
    }

    private bool ApplySides() => ApplySides(null, null);

    private bool ApplySides(LensSide at) => ApplySides(at, null);

    /// <summary>
    /// Draws the sides: a side whose document changed goes there (and a side in
    /// <paramref name="forceA"/> or <paramref name="forceB"/> always does, to
    /// show a row), an empty side shows its empty state; then the strips and the
    /// dock's tags follow. False when a document went to is gone (its view says
    /// so; the source shows all the same).
    /// </summary>
    private bool ApplySides(LensSide? forceA, LensSide? forceB)
    {
        bool there = true;
        foreach (LensSide side in Sides)
        {
            AppPane pane = PaneOf(side);
            if (pane == null)
                continue;
            LinkTarget doc = _sides[side];
            int at = (int)side;
            pane.SetEmpty(doc.IsNone);
            if (!doc.IsNone && (side == forceA || side == forceB || !_applied[at].Equals(doc)))
                there &= pane.Go(doc);
            _applied[at] = doc;
        }
        Targets();
        MarkShelf();
        return there;
    }

    /// <summary>Space or the Inspect key: inspect on or off (only while the app shows).</summary>
    public void ToggleInspect()
    {
        if (IsShowing)
            SetInspect(!Inspecting);
    }

    /// <summary>Inspect on or off. Off lets go of a held pick and clears the result line (the notice is not needed then).</summary>
    public void SetInspect(bool on)
    {
        if (Inspecting == on)
            return;
        Inspecting = on;
        if (!on)
            ReleaseHold();
        else if (toast != null)
            toast.Hide(); // "Press Space to inspect." is done
        RefreshInspect();
    }

    /// <summary>PcInspect's question: true with inspect on; else the notice says "Press Space to inspect." and nothing is picked.</summary>
    public bool AllowPick()
    {
        if (Inspecting)
            return true;
        Notice(UiText.Pc.Get("lens.inspect.hint"));
        return false;
    }

    /// <summary>The Inspect key's face shows while inspect is on.</summary>
    private void RefreshInspect()
    {
        if (inspectOn != null && inspectOn.activeSelf != Inspecting)
            inspectOn.SetActive(Inspecting);
    }

    /// <summary>The findings chip counts the findings ("No findings", "1 finding", "3 findings"); the list closes when none is left.</summary>
    private void RefreshChip()
    {
        int count = board != null ? board.Log.Items.Count : 0;
        if (findingsChipText != null)
            findingsChipText.text = count == 0 ? UiText.Pc.Get("lens.findings.none") : count == 1 ? UiText.Pc.Get("lens.findings.one") : UiText.Pc.Format("lens.findings.many", count);
        if (count == 0 && _findingsShown)
            ApplyFindings(false);
    }

    /// <summary>Every scroll rect under the sides takes the document drag too, so a drag that starts on a document is not eaten by its page's scrolling (LensDocDrag; the mouse wheel still scrolls).</summary>
    private void AdoptDrags()
    {
        foreach (AppPane pane in Panes())
        {
            pane.GetComponentsInChildren(true, _scrollScratch);
            foreach (ScrollRect scroll in _scrollScratch)
                if (!scroll.TryGetComponent(out LensDocDrag _))
                    scroll.gameObject.AddComponent<LensDocDrag>();
        }
    }

    /// <summary>The side under the screen point (the PC canvas's camera maps it), or null.</summary>
    private LensSide? SideAt(Vector2 screen, Camera camera)
    {
        foreach (LensSide side in Sides)
        {
            AppPane pane = PaneOf(side);
            if (pane != null && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)pane.transform, screen, camera))
                return side;
        }
        return null;
    }

    /// <summary>The side a drag is over shows the strong frame.</summary>
    private void HoverSide(LensSide? side)
    {
        if (leftPane != null)
            leftPane.SetDrop(side == LensSide.Left);
        if (rightPane != null)
            rightPane.SetDrop(side == LensSide.Right);
    }

    /// <summary>A drag that will not end (the dock redrew, a case began or ended): the ghost and the drop frame go, nothing is being moved.</summary>
    private void CancelDrag()
    {
        _moving = null;
        HoverSide(null);
        if (menus != null)
            menus.HideGhost();
    }

    /// <summary>The document as a side shows it when it is on a side (its pane's Current carries the Registry's lookup); else <paramref name="doc"/>.</summary>
    private LinkTarget Carry(LinkTarget doc)
    {
        foreach (LensSide side in Sides)
            if (PaneOf(side) != null && LensSides.Same(_sides[side], doc))
                return PaneOf(side).Current;
        return doc;
    }

    /// <summary>A dock item is dragged: the side under the pointer shows the strong frame.</summary>
    private void DockDragging(Vector2 screen, Camera camera) => HoverSide(SideAt(screen, camera));

    /// <summary>A dock item is dropped: over a side its document opens there; elsewhere nothing happens.</summary>
    private void DockDropped(ShelfItem item, Vector2 screen, Camera camera)
    {
        HoverSide(null);
        LensSide? side = SideAt(screen, camera);
        if (!side.HasValue || item.Target.IsNone)
            return;
        _sides.Place(Carry(item.Target), side.Value);
        ApplySides(side.Value);
    }

    /// <summary>A drag starts on the document of <paramref name="pane"/> (LensDocDrag): true when it moves that document (inspect off, the side full).</summary>
    public bool BeginMove(AppPane pane, PointerEventData e)
    {
        if (Inspecting || pane == null || _sides[SideOfPane(pane)].IsNone)
            return false;
        _moving = SideOfPane(pane);
        LinkTarget doc = _sides[_moving.Value];
        foreach (ShelfItem item in _items)
            if (item.Action == ShelfAction.Open && LensSides.Same(item.Target, doc))
            {
                menus.ShowGhost(item);
                menus.MoveGhost(e.position, e.pressEventCamera);
                break;
            }
        return true;
    }

    /// <summary>The moved document follows the pointer: its ghost, and the strong frame on the side under it.</summary>
    public void MoveDrag(PointerEventData e)
    {
        if (!_moving.HasValue)
            return;
        menus.MoveGhost(e.position, e.pressEventCamera);
        HoverSide(SideAt(e.position, e.pressEventCamera));
    }

    /// <summary>The moved document is dropped: on the other side the two change places; outside both sides it is put away; on its own side nothing happens.</summary>
    public void EndMove(PointerEventData e)
    {
        if (!_moving.HasValue)
            return;
        LensSide from = _moving.Value;
        _moving = null;
        HoverSide(null);
        menus.HideGhost();
        LensSide? to = SideAt(e.position, e.pressEventCamera);
        if (!to.HasValue)
            _sides.PutAway(from);
        else if (to.Value != from)
            _sides.Place(Carry(_sides[from]), to.Value, from);
        ApplySides();
    }
}
