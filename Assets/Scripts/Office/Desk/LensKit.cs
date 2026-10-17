using UnityEngine;

/// <summary>
/// The Translation Lens's desk object (the desk machines spec §4): a brass
/// stand that gains a part with each level the lens reaches (LensReach): the
/// loupe (a word), the bar magnifier (its sentence), the Fresnel sheet on its
/// folding arm (the whole object). Each word the lens turns to English ticks
/// the loupe's ring round a notch (loupe_tick); a click on the stand folds
/// the Fresnel arm down over the desk or back up (arm_fold), or, without
/// the arm, spins the loupe. Built by Build Office UI from LensKit.fbx
/// (tools/props/machines).
/// </summary>
public sealed class LensKit : MonoBehaviour
{
    /// <summary>The level parts: the loupe (it spins about its local Y), the bar magnifier, the Fresnel arm and its hinged upper part (it folds about local X, 0 upright to -100 folded).</summary>
    [SerializeField] private Transform loupe, barMagnifier, fresnelArm, armUpper;

    /// <summary>The stand's click box.</summary>
    [SerializeField] private Clickable click;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>A tick's turn (degrees), the shortest gap between two ticks' sounds (seconds) and the arm's folded angle (degrees about X).</summary>
    [SerializeField] private float tickDegrees = 30f, tickGap = 0.05f, foldedAngle = -100f;

    private Quaternion _loupeRest, _armRest;
    private Spring _spin, _fold;
    private float _lastTick = float.NegativeInfinity, _now;
    private bool _folded, _rested;

    private void Awake()
    {
        Rest();
        if (click != null)
            click.onClick.AddListener(Clicked);
    }

    private void Rest()
    {
        if (_rested)
            return;
        _rested = true;
        if (loupe != null)
            _loupeRest = loupe.localRotation;
        if (armUpper != null)
            _armRest = armUpper.localRotation;
    }

    /// <summary>The day's reach: the parts of each level reached; false for none (the stand is not on the desk).</summary>
    public bool SetReach(LensReach reach)
    {
        Rest();
        if (loupe != null)
            loupe.gameObject.SetActive(reach >= LensReach.Word);
        if (barMagnifier != null)
            barMagnifier.gameObject.SetActive(reach >= LensReach.Sentence);
        if (fresnelArm != null)
            fresnelArm.gameObject.SetActive(reach >= LensReach.Object);
        _folded = false;
        _fold = Spring.At(0f);
        Pose();
        return reach > LensReach.None;
    }

    /// <summary>The lens turned a word to English: the loupe's ring ticks a notch (the sound at most every tickGap).</summary>
    public void Tick()
    {
        if (!isActiveAndEnabled || loupe == null || !loupe.gameObject.activeSelf)
            return;
        DeskProps.Aim(ref _spin, _spin.Target + 1f);
        if (_now - _lastTick < tickGap)
            return;
        _lastTick = _now;
        CueSounds.Play(SoundCues.LoupeTick, sound, 0.95f + 0.1f * Mathf.Repeat(_spin.Target * 0.37f, 1f));
    }

    private void Clicked()
    {
        if (fresnelArm != null && fresnelArm.gameObject.activeSelf)
        {
            _folded = !_folded;
            DeskProps.Aim(ref _fold, _folded ? 1f : 0f);
            CueSounds.Play(SoundCues.ArmFold, sound, _folded ? 1f : 1.1f);
            return;
        }
        for (int i = 0; i < 3; i++)
            Tick();
    }

    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        _now += dt;
        bool moved = DeskProps.Step(ref _spin, dt, MotionFeel.Firm);
        moved |= DeskProps.Step(ref _fold, dt, MotionFeel.Elastic);
        if (moved)
            Pose();
    }

    private void Pose()
    {
        if (loupe != null)
            loupe.localRotation = _loupeRest * Quaternion.AngleAxis(tickDegrees * _spin.Value, Vector3.up);
        if (armUpper != null)
            armUpper.localRotation = _armRest * Quaternion.AngleAxis(foldedAngle * _fold.Value, Vector3.right);
    }
}
