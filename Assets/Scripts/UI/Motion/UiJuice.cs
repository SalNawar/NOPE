using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A kit control's physical feel (Saleh 2026-10-07: "every single button,
/// every single action to feel this satisfying"; round 2: "they go out of
/// bounds of their borders, buttons overlap", "too fast"), on every kit
/// control (UiKitSO.Show adds it, the one hook: the builders' SceneUiKit.Skin
/// and the views that build kit controls at run time both come through it; a
/// kit control a view draws without it calls On). The control itself (its
/// rect, its hit area, its place in a layout) never moves or grows: its FACE
/// (every child: the kit face, the label, an icon) moves inside it as
/// ControlMotion says. Under the pointer the face lifts a pixel or two (the
/// kit's hover face adds the star glint); pressed, it goes down into its
/// bezel, shortens and darkens while its sprite swaps to the pressed one; let
/// go, it springs back past its rest and wobbles in; a confirmed click (or
/// Submit) pops it; a click on a disabled control shakes it, a short "no",
/// with the error cue; a screen-edge pull tab slides out, stretched along its
/// travel. The face's reach past the rest rect is capped by its room
/// (ControlRoom: the kit's border inset, never into a sibling's rest rect).
/// Each moment plays its cue (ui_hover, ui_press, ui_release, ui_toggle,
/// ui_error: Sounds). Stepped by UiMotion only while it moves; the face's
/// parts are moved by deltas, so a layout that moves them meanwhile keeps
/// its say. Reduced Motion keeps the face still; the Motion intensity scales it.
/// </summary>
[DisallowMultipleComponent]
public sealed class UiJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
                              IPointerClickHandler, ISubmitHandler, IMotionTick
{
    /// <summary>The siblings' rest rects while a room is measured (reused: no allocation).</summary>
    private static readonly List<FaceRect> Neighbours = new List<FaceRect>(16);

    private readonly ControlMotion _motion = new ControlMotion();
    private readonly List<Transform> _parts = new List<Transform>(4);
    private readonly List<Vector3> _appliedMove = new List<Vector3>(4);
    private readonly List<Vector2> _appliedScale = new List<Vector2>(4);
    private Selectable _control;
    private RectTransform _rect;
    private Graphic _face;
    private float _appliedDarken;
    private bool _posed;
    private bool _chosen;

    /// <summary>
    /// Marks <paramref name="control"/> as the chosen side of a choice (a view
    /// whose chosen option takes no clicks: the Title's adoption cards, a
    /// Settings pair): a click on it is no refusal (no "no" shake), and the
    /// moment it becomes chosen it pops (the "bounce when chosen") with the
    /// toggle cue, unless <paramref name="bounce"/> is false (a pair whose pill
    /// slides instead). Nothing for a control without UiJuice.
    /// </summary>
    public static void Choose(Selectable control, bool chosen, bool bounce = true)
    {
        if (control == null || !control.TryGetComponent(out UiJuice juice))
            return;
        bool was = juice._chosen;
        juice._chosen = chosen;
        if (!chosen || was || !bounce || !juice.isActiveAndEnabled)
            return;
        juice.Begin();
        juice._motion.Confirm(UiMotion.Knobs, UiMotion.Amount);
        Sounds.Play(SoundCues.UiToggle);
    }

    /// <summary>True while the control takes clicks (no Selectable: always).</summary>
    private bool Live => _control == null || _control.IsInteractable();

    /// <summary>True while its springs move (the probes wait for it).</summary>
    public bool Moving => _motion.Moving;

    /// <summary>
    /// Gives <paramref name="control"/> its UiJuice (kept when present; nothing
    /// for none): UiKitSO.Show, the one hook every kit control comes through.
    /// A kit pull tab (<paramref name="piece"/> "pulltab_left" on the screen's
    /// left edge, "pulltab_right" on its right) slides out toward the screen.
    /// </summary>
    public static void On(Selectable control, string piece = null)
    {
        if (control == null)
            return;
        if (!control.TryGetComponent(out UiJuice juice))
            juice = control.gameObject.AddComponent<UiJuice>();
        if (piece != null && piece.StartsWith(UiKitNames.PullTab, System.StringComparison.Ordinal))
            juice._motion.SetPull(piece.StartsWith(UiKitNames.PullTabLeft, System.StringComparison.Ordinal) ? 1 : -1, 0);
    }

    private void Awake()
    {
        _control = GetComponent<Selectable>();
        _rect = transform as RectTransform;
        Transform face = transform.Find(UiKitSO.FaceName);
        _face = face != null ? face.GetComponent<Graphic>() : null;
    }

    /// <summary>The pointer came over it: the lift and the hover cue (a live control only).</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Live)
            return;
        Begin();
        _motion.Hover(true, UiMotion.Knobs, UiMotion.Amount);
        Sounds.Play(SoundCues.UiHover);
    }

    /// <summary>The pointer left: the face comes back (or stays down while held).</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        Begin();
        _motion.Hover(false, UiMotion.Knobs, UiMotion.Amount);
    }

    /// <summary>Pressed with the left button: the face goes into its bezel, and the press cue.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !Live)
            return;
        Begin();
        _motion.Press(UiMotion.Knobs, UiMotion.Amount);
        Sounds.Play(SoundCues.UiPress);
    }

    /// <summary>Let go: the spring back and the release cue.</summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_motion.Pressed)
            return;
        Begin();
        _motion.Release(UiMotion.Knobs, UiMotion.Amount);
        Sounds.Play(SoundCues.UiRelease);
    }

    /// <summary>A left click: the pop (a toggle's cue on a toggle), or on a disabled control the "no" shake and the error cue.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            Click();
    }

    /// <summary>Submit (Enter on a selected control) clicks it.</summary>
    public void OnSubmit(BaseEventData eventData) => Click();

    /// <summary>Back at rest at once when it hides mid-motion (a face is never left down).</summary>
    private void OnDisable()
    {
        _motion.Reset();
        if (_posed)
            Apply();
    }

    /// <summary>Steps its springs and draws them; false once they settled.</summary>
    public bool TickMotion(float dt)
    {
        if (this == null)
            return false;
        bool moving = _motion.Step(dt, UiMotion.Knobs);
        Apply();
        return moving;
    }

    /// <summary>The click's pop or the refusal's shake.</summary>
    private void Click()
    {
        Begin();
        if (Live)
        {
            _motion.Confirm(UiMotion.Knobs, UiMotion.Amount);
            if (_control is Toggle)
                Sounds.Play(SoundCues.UiToggle);
        }
        else if (!_chosen)
        {
            _motion.Refuse(UiMotion.Knobs, UiMotion.Amount);
            Sounds.Play(SoundCues.UiError);
        }
    }

    /// <summary>When a motion starts from rest: the face's parts (every child, as they lie now) and the room among the siblings (ControlRoom); then the springs go to the driver.</summary>
    private void Begin()
    {
        if (!_posed && _rect != null)
        {
            _parts.Clear();
            _appliedMove.Clear();
            _appliedScale.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                _parts.Add(transform.GetChild(i));
                _appliedMove.Add(Vector3.zero);
                _appliedScale.Add(Vector2.one);
            }
            _appliedDarken = 0f;
            MeasureRoom();
            _posed = true;
        }
        UiMotion.Run(this);
    }

    /// <summary>The face's room on each side: the kit's border inset (a pull tab's slide and pop on its open side), cut to the gap to each active sibling it faces.</summary>
    private void MeasureRoom()
    {
        MotionKnobs knobs = UiMotion.Knobs;
        Neighbours.Clear();
        Transform parent = transform.parent;
        if (parent != null)
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);
                if (sibling != transform && sibling.gameObject.activeSelf && sibling is RectTransform r)
                    Neighbours.Add(InParent(r));
            }
        float room = knobs.faceRoom, pull = knobs.pullHover + knobs.pullPop + knobs.faceRoom;
        bool pulls = _motion.Pulls;
        var dir = new Vector2(_motion.PullX, _motion.PullY);
        ControlRoom.Of(InParent(_rect), Neighbours, pulls && dir.x < 0f ? pull : room, pulls && dir.x > 0f ? pull : room,
                       pulls && dir.y < 0f ? pull : room, pulls && dir.y > 0f ? pull : room, out float l, out float r2, out float b, out float t);
        _motion.SetRoom(l, r2, b, t);
        Neighbours.Clear();
    }

    /// <summary>A rect transform's rect in its parent's space (position and scale; no rotation in the kit).</summary>
    private static FaceRect InParent(RectTransform r)
    {
        Rect rect = r.rect;
        Vector3 p = r.localPosition, s = r.localScale;
        return new FaceRect(p.x + rect.xMin * s.x, p.y + rect.yMin * s.y, p.x + rect.xMax * s.x, p.y + rect.yMax * s.y);
    }

    /// <summary>
    /// Draws the face: each part mapped from the rest rect to the face's rect
    /// (ControlMotion.Edges: moved and scaled about the rect's centre, by the
    /// change from what was applied, so a layout keeps its say), the kit face
    /// darkened; once back at rest, the parts exactly as they lie at rest.
    /// </summary>
    private void Apply()
    {
        if (_rect == null)
            return;
        Rect rest = _rect.rect;
        FaceEdges e = _motion.AtRestPose ? FaceEdges.Rest : _motion.Edges(rest.width, rest.height, UiMotion.Knobs);
        var face = Rect.MinMaxRect(rest.xMin - e.Left, rest.yMin - e.Bottom, rest.xMax + e.Right, rest.yMax + e.Top);
        var s = new Vector2(rest.width > 0f ? face.width / rest.width : 1f, rest.height > 0f ? face.height / rest.height : 1f);
        Vector2 c = rest.center, c2 = face.center;
        for (int i = 0; i < _parts.Count; i++)
        {
            Transform part = _parts[i];
            if (part == null)
                continue;
            Vector3 restPos = part.localPosition - _appliedMove[i];
            Vector2 applied = _appliedScale[i];
            Vector3 scale = part.localScale;
            var restScale = new Vector2(applied.x != 0f ? scale.x / applied.x : scale.x, applied.y != 0f ? scale.y / applied.y : scale.y);
            var moved = new Vector3(c2.x + (restPos.x - c.x) * s.x, c2.y + (restPos.y - c.y) * s.y, restPos.z);
            _appliedMove[i] = moved - restPos;
            _appliedScale[i] = s;
            part.localPosition = moved;
            part.localScale = new Vector3(restScale.x * s.x, restScale.y * s.y, scale.z);
        }
        if (_face != null && !Mathf.Approximately(e.Darken, _appliedDarken))
        {
            Color now = _face.color;
            float back = 1f - _appliedDarken, to = 1f - e.Darken;
            float k = back > 0.01f ? to / back : 1f;
            _face.color = new Color(now.r * k, now.g * k, now.b * k, now.a);
            _appliedDarken = e.Darken;
        }
        if (_motion.AtRestPose)
            _posed = false;
    }
}
