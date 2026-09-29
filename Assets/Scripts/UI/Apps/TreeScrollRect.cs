using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Orders tree's view (Saleh 2026-09-29: "zoom drag and scroll"): a
/// ScrollRect over the tree (the wheel scrolls it, Shift+wheel sideways, a
/// left drag pans it with its inertia, the vertical scrollbar) that also
/// zooms and pans with the middle button. Ctrl+wheel zooms one level a notch
/// about the pointer (the spot under it stays under it); <see cref="Step"/>
/// (the window's − and + buttons, Ctrl+=, Ctrl+- and Ctrl+0) zooms about the
/// view's centre. The zoom scales the content from its top-left corner, and
/// the scroll stays inside the zoomed tree (TreeZoom). The levels are the
/// readable ones OrdersWindow hands over (TreeZoom.ReadableLevels). A drag
/// that starts on a node only pans (its click is dropped once the drag
/// begins, and nothing in the tree orders: only the action button does).
/// <see cref="Reveal"/> scrolls just enough to show a node whole (the arrows'
/// selection). It allocates nothing per frame.
/// </summary>
public sealed class TreeScrollRect : ScrollRect
{
    /// <summary>The room kept around a node scrolled into view (content units at 100 %, zoomed with it).</summary>
    [SerializeField, Min(0f)] private float revealMargin = 12f;

    private readonly Vector3[] _corners = new Vector3[4];
    private IReadOnlyList<int> _levels = new[] { AppZoom.Normal };
    private bool _panning;

    /// <summary>The zoom level shown (percent).</summary>
    public int Level { get; private set; } = AppZoom.Normal;

    /// <summary>Raised after the level changes (the window's percent line and buttons follow it).</summary>
    public event Action LevelChanged;

    /// <summary>Whether a zoom <paramref name="direction"/> (1 in, -1 out) has a level to go to.</summary>
    public bool CanStep(int direction) => AppZoom.Step(_levels, Level, direction) != Level;

    /// <summary>The levels the tree may take (readable ones, TreeZoom.ReadableLevels); a level shown that is not among them goes to the reset level.</summary>
    public void SetLevels(IReadOnlyList<int> levels)
    {
        _levels = levels != null && levels.Count > 0 ? levels : new[] { AppZoom.Normal };
        foreach (int level in _levels)
            if (level == Level)
            {
                Apply(Level, Level, Vector2.zero);
                return;
            }
        Apply(Level, TreeZoom.ResetLevel(_levels), Vector2.zero);
    }

    /// <summary>Zooms one level in (1), out (-1), or back to the reset level (0), about the view's centre.</summary>
    public void Step(int direction)
    {
        int level = direction == 0 ? TreeZoom.ResetLevel(_levels) : AppZoom.Step(_levels, Level, direction);
        Apply(Level, level, viewRect.rect.size / 2f);
    }

    /// <summary>Scrolls back to the tree's top-left corner.</summary>
    public void ShowCorner() => SetScroll(Vector2.zero);

    /// <summary>Scrolls just enough to show <paramref name="item"/> (a node in the content) whole, with the margin around it.</summary>
    public void Reveal(RectTransform item)
    {
        if (item == null || content == null)
            return;
        item.GetWorldCorners(_corners);
        Vector2 min = content.InverseTransformPoint(_corners[0]);
        Vector2 max = content.InverseTransformPoint(_corners[2]);
        float scale = AppZoom.Scale(Level), margin = revealMargin * scale;
        Rect view = viewRect.rect;
        Vector2 scroll = Scroll;
        scroll.x = TreeZoom.Reveal(scroll.x, min.x * scale, max.x * scale, view.width, margin);
        scroll.y = TreeZoom.Reveal(scroll.y, -max.y * scale, -min.y * scale, view.height, margin);
        SetScroll(scroll);
    }

    /// <summary>Ctrl+wheel zooms a level about the pointer; Shift+wheel scrolls sideways; the wheel alone scrolls.</summary>
    public override void OnScroll(PointerEventData data)
    {
        if (!IsActive())
            return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.ctrlKey.isPressed)
        {
            if (data.scrollDelta.y != 0f &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, data.position, data.enterEventCamera, out Vector2 local))
                Apply(Level, AppZoom.Step(_levels, Level, data.scrollDelta.y > 0f ? 1 : -1), FromCorner(local));
            return;
        }
        if (keyboard != null && keyboard.shiftKey.isPressed)
        {
            Vector2 wheel = data.scrollDelta;
            data.scrollDelta = new Vector2(wheel.y, 0f);
            base.OnScroll(data);
            data.scrollDelta = wheel;
            return;
        }
        base.OnScroll(data);
    }

    /// <summary>A middle drag pans (without inertia); a left drag is the ScrollRect's.</summary>
    public override void OnBeginDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Middle)
        {
            base.OnBeginDrag(data);
            return;
        }
        _panning = IsActive();
        StopMovement();
    }

    /// <summary>Moves the tree with a middle drag; a left drag is the ScrollRect's.</summary>
    public override void OnDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Middle)
        {
            base.OnDrag(data);
            return;
        }
        if (!_panning ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, data.position, data.pressEventCamera, out Vector2 now) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, data.position - data.delta, data.pressEventCamera, out Vector2 before))
            return;
        Vector2 moved = now - before;
        SetScroll(Scroll - new Vector2(moved.x, -moved.y));
    }

    /// <summary>Ends a middle drag's pan; a left drag is the ScrollRect's.</summary>
    public override void OnEndDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Middle)
        {
            base.OnEndDrag(data);
            return;
        }
        _panning = false;
    }

    /// <summary>The scroll from the tree's top-left corner (right and down, view units).</summary>
    private Vector2 Scroll => content != null ? new Vector2(-content.anchoredPosition.x, content.anchoredPosition.y) : Vector2.zero;

    /// <summary>Shows <paramref name="level"/> (from <paramref name="from"/>), keeping the tree's spot at <paramref name="pointer"/> (view units from its top-left) in place.</summary>
    private void Apply(int from, int level, Vector2 pointer)
    {
        Level = level;
        if (content == null)
            return;
        float before = AppZoom.Scale(from), after = AppZoom.Scale(level);
        content.localScale = new Vector3(after, after, 1f);
        Vector2 scroll = Scroll;
        scroll.x = TreeZoom.ZoomAbout(scroll.x, pointer.x, before, after);
        scroll.y = TreeZoom.ZoomAbout(scroll.y, pointer.y, before, after);
        SetScroll(scroll);
        LevelChanged?.Invoke();
    }

    /// <summary>Sets the scroll, kept inside the zoomed tree, and stops any glide.</summary>
    private void SetScroll(Vector2 scroll)
    {
        if (content == null)
            return;
        StopMovement();
        Rect view = viewRect.rect;
        Vector2 size = content.rect.size * AppZoom.Scale(Level);
        scroll.x = TreeZoom.ClampScroll(scroll.x, size.x, view.width);
        scroll.y = TreeZoom.ClampScroll(scroll.y, size.y, view.height);
        content.anchoredPosition = new Vector2(-scroll.x, scroll.y);
    }

    /// <summary>A point in the view's local space as view units from its top-left corner (right and down).</summary>
    private Vector2 FromCorner(Vector2 local)
    {
        Rect view = viewRect.rect;
        return new Vector2(local.x - view.xMin, view.yMax - local.y);
    }
}
