using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The desk's small shake on a stamp's slam (Saleh 2026-10-07: "a quick
/// squash on impact, a small desk shake"): the main camera jolts down and
/// wobbles back on a spring (MotionKnobs.shakeFeel), drawn only while the
/// camera renders (the offset is added as rendering begins and taken off as
/// it ends), so the camera's own driver (Cinemachine, the desk view) never
/// sees it and nothing drifts. Scaled by the Motion intensity; none under
/// Reduced Motion. Stepped by UiMotion only while it moves.
/// </summary>
public sealed class DeskShake : IMotionTick
{
    private static DeskShake _shake;
    private Spring _y, _x;
    private Vector3 _offset;
    private bool _hooked;

    /// <summary>Jolts the main camera by <paramref name="metres"/> (scaled by the amount of motion; nothing when still).</summary>
    public static void Kick(float metres)
    {
        MotionAmount amount = UiMotion.Amount;
        if (amount.Still || metres <= 0f || !Application.isPlaying)
            return;
        _shake ??= new DeskShake();
        MotionKnobs knobs = UiMotion.Knobs;
        SpringTuning tuning = knobs.Get(knobs.shakeFeel);
        _shake._y.Kick(-tuning.KickFor(metres * amount.Share));
        _shake._x.Kick(tuning.KickFor(metres * 0.35f * amount.Share));
        if (!_shake._hooked)
        {
            RenderPipelineManager.beginCameraRendering += _shake.Begin;
            RenderPipelineManager.endCameraRendering += _shake.End;
            _shake._hooked = true;
        }
        UiMotion.Run(_shake);
    }

    /// <summary>Steps the shake; unhooks from rendering once it settled.</summary>
    public bool TickMotion(float dt)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        SpringTuning tuning = knobs.Get(knobs.shakeFeel);
        bool moving = _y.Step(dt, tuning, 1e-5f, 1e-4f) | _x.Step(dt, tuning, 1e-5f, 1e-4f);
        _offset = new Vector3(_x.Value, _y.Value, 0f);
        if (moving)
            return true;
        _offset = Vector3.zero;
        if (_hooked)
        {
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
            _hooked = false;
        }
        return false;
    }

    /// <summary>The main camera starts rendering: the offset goes on, in its own right and up.</summary>
    private void Begin(ScriptableRenderContext context, Camera camera)
    {
        if (camera != null && camera.CompareTag("MainCamera"))
            camera.transform.position += camera.transform.rotation * _offset;
    }

    /// <summary>It finished: the offset comes off again.</summary>
    private void End(ScriptableRenderContext context, Camera camera)
    {
        if (camera != null && camera.CompareTag("MainCamera"))
            camera.transform.position -= camera.transform.rotation * _offset;
    }

    /// <summary>Forgets the shake when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (_shake != null && _shake._hooked)
        {
            RenderPipelineManager.beginCameraRendering -= _shake.Begin;
            RenderPipelineManager.endCameraRendering -= _shake.End;
        }
        _shake = null;
    }
}
