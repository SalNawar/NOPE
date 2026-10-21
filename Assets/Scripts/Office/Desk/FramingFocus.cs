using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// The D6 framing focus (Saleh, D6: "when they slide in, we subtly zoom in a
/// bit not too hard just a small focus to frame the shot"; DESK-HOMES P3,
/// UNITY_PLAN 5.2): after a machine has slid in for the cursor plus a 150 ms
/// dwell, the reading view narrows by at most 3 % of its bound field of view
/// (8 % with the push-in) and turns at most 3 % of the frame toward it,
/// re-aiming to the next machine without letting go and letting go when the
/// cursor leaves (SlideFocus decides, from the slide-in's assist). Only in
/// the reading view (DESK-HOMES Q5's default), never while a paper or a tool
/// is held, while a camera blend (the drop tilt) runs, under Reduced Motion
/// or at Motion intensity 0 (it scales with the intensity). It picks the
/// target only: CameraFeel is the one lens writer (CameraFeel.Focus).
/// </summary>
public sealed class FramingFocus : MonoBehaviour
{
    private DeskSlideIn _slideIn;
    private DeskView _view;
    private DeskController _papers;
    private SlideFocus _focus;
    private Camera _camera;
    private string _shown;

    /// <summary>Binds the focus to <paramref name="slideIn"/>'s assist, the reading <paramref name="view"/>, the office's output <paramref name="camera"/> (where the machine shows) and the desk's <paramref name="papers"/> (a held paper turns it off).</summary>
    public void Bind(DeskSlideIn slideIn, DeskView view, Camera camera, DeskController papers)
    {
        _slideIn = slideIn;
        _view = view;
        _camera = camera;
        _papers = papers;
        _focus = slideIn != null && slideIn.Slides != null ? new SlideFocus(slideIn.Slides) : null;
        Let();
    }

    /// <summary>After the slide-in's Update: the target for this frame, handed to CameraFeel when it changes (its aim taken from where the machine shows then).</summary>
    private void LateUpdate()
    {
        if (_focus == null || _slideIn == null || _slideIn.Assist == null)
            return;
        MotionAmount amount = UiMotion.Amount;
        bool held = HoldRegistry.Desk.IsHolding || (_papers != null && _papers.IsDragging);
        bool allowed = _view != null && _view.IsOn && !held && !Blending() && !amount.Still;
        _focus.Step(UiMotion.Now, _slideIn.Assist, allowed);
        string target = _focus.Target;
        if (target == _shown)
            return;
        MachineSlideIn machine = target != null ? _slideIn.Find(target) : null;
        if (machine == null || _camera == null || !machine.TryCentre(out Vector3 centre))
        {
            Let();
            return;
        }
        DeskSlides slides = _slideIn.Slides;
        Vector3 at = _camera.WorldToViewportPoint(centre);
        (float x, float y) aim = SlideFocus.Aim(at.x, at.y, slides.focusAim);
        CameraFeel.Focus(slides.focusShare * amount.Share, new Vector2(aim.x, aim.y), slides.focusCap);
        _shown = target;
    }

    private void OnDisable() => Let();

    /// <summary>No focus: CameraFeel eases it out.</summary>
    private void Let()
    {
        if (_shown != null)
            CameraFeel.ReleaseFocus();
        _shown = null;
    }

    /// <summary>True while any Cinemachine brain blends (the drop tilt in or out of the reading view).</summary>
    private static bool Blending()
    {
        for (int i = 0; i < CinemachineBrain.ActiveBrainCount; i++)
        {
            CinemachineBrain brain = CinemachineBrain.GetActiveBrain(i);
            if (brain != null && brain.IsBlending)
                return true;
        }
        return false;
    }
}
