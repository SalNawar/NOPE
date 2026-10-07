using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A kit control's physical feel (Saleh 2026-10-07: "every single button,
/// every single action to feel this satisfying"), on every kit control
/// (UiKitSO.Show adds it, the one hook: the builders' SceneUiKit.Skin and the
/// views that build kit controls at run time both come through it; a kit
/// control a view draws without it calls On): under the pointer it lifts
/// (the kit's hover face adds the star glint), pressed it squashes wider and
/// shorter while its face swaps to the pressed one, let go it springs back
/// past its rest and wobbles in, a confirmed click (or Submit) pops it, and a
/// click on a disabled control shakes it sideways, a short "no", with the
/// error cue. Each moment plays its cue (ui_hover, ui_press, ui_release,
/// ui_toggle, ui_error: Sounds; silent until the bank has a clip). The motion is ControlMotion's springs, stepped by UiMotion only
/// while they move; the control's hit area stays its rest rect whatever its
/// scale (the graphic's raycast padding cancels it), so a squash never loses
/// the click. Reduced Motion keeps it still; the Motion intensity scales it.
/// </summary>
[DisallowMultipleComponent]
public sealed class UiJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
                              IPointerClickHandler, ISubmitHandler, IMotionTick
{
    private readonly ControlMotion _motion = new ControlMotion();
    private Selectable _control;
    private Graphic _hit;
    private RectTransform _rect;
    private Vector3 _restScale = Vector3.one;
    private Vector4 _restPadding;
    private float _appliedOffset;
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

    /// <summary>Gives <paramref name="control"/> its UiJuice (kept when present; nothing for none): UiKitSO.Show, the one hook every kit control comes through.</summary>
    public static void On(Selectable control)
    {
        if (control != null && !control.TryGetComponent(out UiJuice _))
            control.gameObject.AddComponent<UiJuice>();
    }

    private void Awake()
    {
        _control = GetComponent<Selectable>();
        _hit = GetComponent<Graphic>();
        _rect = transform as RectTransform;
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

    /// <summary>The pointer left: it comes back down (or stays squashed while held).</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        Begin();
        _motion.Hover(false, UiMotion.Knobs, UiMotion.Amount);
    }

    /// <summary>Pressed with the left button: the anticipation squash and the press cue.</summary>
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

    /// <summary>Back at rest at once when it hides mid-motion (a control is never left squashed).</summary>
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

    /// <summary>Takes note of the rest pose when a motion starts from rest (the builder's or a layout's scale, the graphic's padding) and hands the springs to the driver.</summary>
    private void Begin()
    {
        if (!_posed)
        {
            _restScale = transform.localScale;
            _restPadding = _hit != null ? _hit.raycastPadding : Vector4.zero;
            _appliedOffset = 0f;
            _posed = true;
        }
        UiMotion.Run(this);
    }

    /// <summary>Draws the springs: the scale over the rest scale, the shake as a sideways offset (only its change is applied, so a layout keeps the place), the hit area held at the rest rect; once at rest, the rest pose exactly.</summary>
    private void Apply()
    {
        float sx = _motion.ScaleX, sy = _motion.ScaleY, offset = _motion.OffsetX;
        transform.localScale = new Vector3(_restScale.x * sx, _restScale.y * sy, _restScale.z);
        if (!Mathf.Approximately(offset, _appliedOffset))
        {
            Vector3 p = transform.localPosition;
            p.x += offset - _appliedOffset;
            transform.localPosition = p;
            _appliedOffset = offset;
        }
        if (_hit != null && _rect != null)
        {
            Vector2 size = _rect.rect.size;
            float px = sx > 0.01f ? size.x * (1f - 1f / sx) * 0.5f : 0f;
            float py = sy > 0.01f ? size.y * (1f - 1f / sy) * 0.5f : 0f;
            _hit.raycastPadding = _restPadding + new Vector4(px, py, px, py);
        }
        if (_motion.Moving)
            return;
        transform.localScale = _restScale;
        if (_hit != null)
            _hit.raycastPadding = _restPadding;
        _posed = false;
    }
}
