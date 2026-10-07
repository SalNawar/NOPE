using System;

/// <summary>
/// The Night Slots machine's tuning (MotionKnobs.slots, on MotionTuningSO:
/// Saleh edits it in the Inspector): the spin's timing (the anticipation,
/// the spin-up, when the first reel stops and the beat between stops, the
/// reels' speed), the landing's overshoot and its feel, when the symbols
/// blur, the win's hit-stop, flash, symbol pulse, coins and credits roll, the
/// loss's dim, the marquee's chase, the price plate's blink, and the lever
/// (how far a drag pulls it, its ratchet's notches, where a pull fires, its
/// spring back). The reels only show a result drawn before they move
/// (SlotSpinSchedule); nothing here changes an outcome.
/// </summary>
[Serializable]
public sealed class SlotSpinKnobs
{
    /// <summary>How far the reels kick back up before they spin (symbols), and over how many seconds (the anticipation; scaled by the Motion intensity).</summary>
    public float anticipation = 0.15f, anticipationSeconds = 0.1f;

    /// <summary>The seconds a reel takes to reach full speed.</summary>
    public float spinUpSeconds = 0.3f;

    /// <summary>When the first reel stops (seconds from the pull) and the beat between one reel's stop and the next's (left to right).</summary>
    public float firstStopSeconds = 1.2f, stopGapSeconds = 0.45f;

    /// <summary>The reels' cruising speed (symbols a second; each reel adjusts it slightly so it lands exactly on its face).</summary>
    public float reelSpeed = 14f;

    /// <summary>How far a landing reel swings past the payline before it settles back (symbols; under 0.5 so the payline never shows the next symbol; scaled by the Motion intensity).</summary>
    public float landOvershoot = 0.2f;

    /// <summary>The feel of a reel's landing bounce.</summary>
    public MotionFeel landFeel = MotionFeel.Elastic;

    /// <summary>Above this speed (symbols a second) a reel shows its symbols smeared (the motion blur).</summary>
    public float blurSpeed = 5f;

    /// <summary>How much a fast reel's symbols stretch along their travel per symbol a second of speed, and at most.</summary>
    public float stretchPerSpeed = 0.03f, maxStretch = 0.45f;

    /// <summary>The hit-stop as a win lands: the reels hold still this many seconds before the payout.</summary>
    public float winHitStopSeconds = 0.12f;

    /// <summary>The marquee bulbs' chase step at rest, the win's flash step, how long the win flashes, and how long a loss dims them (seconds).</summary>
    public float chaseStepSeconds = 0.11f, flashStepSeconds = 0.12f, winFlashSeconds = 1.6f, loseDimSeconds = 0.9f;

    /// <summary>How much a winning symbol pulses (1 + this at the peak) and how many pulses a second.</summary>
    public float winPulse = 0.12f, winPulseRate = 3f;

    /// <summary>The credits one payout coin stands for, and the most coins one win drops into the tray.</summary>
    public int coinValue = 10, maxCoins = 24;

    /// <summary>The coins' fall: gravity (reference px per second squared), how much of their speed a bounce keeps, and the beat between one coin and the next (seconds).</summary>
    public float coinGravity = 2600f, coinBounce = 0.35f, coinStaggerSeconds = 0.05f;

    /// <summary>The seconds the credits readouts take to roll to the new wallet.</summary>
    public float rollSeconds = 0.9f;

    /// <summary>How far a drag pulls the lever all the way down (reference px), how many ratchet notches it clicks through, how much the ratchet holds it behind the hand between notches (0 none, 1 notch to notch), and the pull at which it fires (0..1).</summary>
    public float leverTravel = 200f;

    /// <summary>The lever's ratchet notches over its full pull.</summary>
    public int leverNotches = 6;

    /// <summary>How much the ratchet holds the lever behind the hand between notches (0: none; 1: it jumps notch to notch).</summary>
    public float leverRatchet = 0.4f;

    /// <summary>The pull (0..1) at which the lever fires a spin.</summary>
    public float leverFireAt = 0.92f;

    /// <summary>The feel of the lever going down by itself (a click on it) and of its spring back when let go.</summary>
    public MotionFeel leverPullFeel = MotionFeel.Firm, leverReturnFeel = MotionFeel.Elastic;

    /// <summary>Every how many seconds the idle lever nods to invite a pull, and how far (a share of its full pull; scaled by the Motion intensity).</summary>
    public float leverInviteSeconds = 4f, leverInvite = 0.06f;

    /// <summary>The price plate's blink (seconds per half) when the wallet cannot pay for a spin.</summary>
    public float insertBlinkSeconds = 0.45f;
}

/// <summary>
/// One spin of the Night Slots machine's reels, as pure timing (Saleh
/// 2026-10-07: "make it a real slot machine"): from where each reel stands,
/// a short kick back (the anticipation), the spin-up, a cruise, and a stop
/// on the payline one reel after another, left to right, each landing
/// with an overshoot that springs back (the landing feel). The faces are
/// the result already drawn (Seeds.ForSlot, SlotReels.Faces): each reel's
/// travel is chosen so it lands exactly on its face, so the animation only
/// shows the outcome and never decides it. Positions are in symbols along
/// the strip (a position p shows face FaceAt(p) on the payline; growing p
/// moves the symbols down). Reduced Motion cuts straight to the faces; the
/// Motion intensity scales the anticipation and the overshoot.
/// </summary>
public sealed class SlotSpinSchedule
{
    /// <summary>A reel is settled once its bounce is within this of the payline (symbols).</summary>
    private const float SettleTolerance = 0.002f;

    private readonly float[] _start, _travel, _speed, _land, _settle;
    private readonly int[] _faces;
    private readonly float _antic, _anticSeconds, _spinUp, _overshoot;
    private readonly SpringTuning _tuning;

    /// <summary>True when the reels cut straight to their faces (Reduced Motion, or no symbols).</summary>
    public bool Cut { get; }

    /// <summary>The seconds until every reel has settled on its face (0 when cut).</summary>
    public float Duration { get; }

    /// <summary>
    /// The spin of reels standing at <paramref name="start"/> (positions) to
    /// <paramref name="faces"/> on a strip of <paramref name="symbols"/>
    /// symbols, timed by <paramref name="knobs"/>, landing with
    /// <paramref name="land"/>'s bounce, with <paramref name="amount"/> of
    /// motion.
    /// </summary>
    public SlotSpinSchedule(float[] start, int[] faces, int symbols, SlotSpinKnobs knobs, SpringTuning land, MotionAmount amount)
    {
        int count = faces.Length;
        _faces = (int[])faces.Clone();
        _start = new float[count];
        _travel = new float[count];
        _speed = new float[count];
        _land = new float[count];
        _settle = new float[count];
        _tuning = land;
        Cut = amount.Reduced || symbols <= 0;
        if (Cut)
            return;

        float share = amount.Share;
        _antic = knobs.anticipation * share;
        _anticSeconds = share > 0f ? MathF.Max(0f, knobs.anticipationSeconds) : 0f;
        _spinUp = MathF.Max(0.01f, knobs.spinUpSeconds);
        _overshoot = MathF.Max(0f, knobs.landOvershoot) * share;
        float duration = 0f;
        for (int i = 0; i < count; i++)
        {
            _start[i] = i < start.Length ? start[i] : 0f;
            _land[i] = MathF.Max(_anticSeconds + _spinUp + 0.05f, knobs.firstStopSeconds + i * MathF.Max(0f, knobs.stopGapSeconds));
            float moving = _land[i] - _anticSeconds - _spinUp / 2f;     // the cruise's worth of seconds at full speed
            float rest = Wrap(_faces[i] - _start[i], symbols);
            float wanted = MathF.Max(0f, knobs.reelSpeed) * moving;
            float turns = MathF.Max(1f, MathF.Ceiling((wanted - rest) / symbols));
            _travel[i] = rest + turns * symbols;
            _speed[i] = _travel[i] / moving;
            _settle[i] = SettleSeconds();
            duration = MathF.Max(duration, _land[i] + _settle[i]);
        }
        Duration = duration;
    }

    /// <summary>When reel <paramref name="reel"/> reaches the payline (seconds from the pull; 0 when cut).</summary>
    public float LandTime(int reel) => Cut ? 0f : _land[reel];

    /// <summary>Reel <paramref name="reel"/>'s position <paramref name="t"/> seconds after the pull (symbols; the face on the payline is FaceAt of it).</summary>
    public float Position(int reel, float t)
    {
        if (Cut)
            return _faces[reel];
        float p0 = _start[reel];
        if (t <= 0f)
            return p0;
        if (t < _anticSeconds)
            return p0 - _antic * MathF.Sin(MathF.PI * t / _anticSeconds);
        float v = _speed[reel];
        float tau = t - _anticSeconds;
        if (tau < _spinUp)
            return p0 + 0.5f * v / _spinUp * tau * tau;
        if (t < _land[reel])
            return p0 + 0.5f * v * _spinUp + v * (tau - _spinUp);
        return p0 + _travel[reel] + Bounce(t - _land[reel]);
    }

    /// <summary>How fast reel <paramref name="reel"/> turns <paramref name="t"/> seconds after the pull (symbols a second, unsigned).</summary>
    public float Speed(int reel, float t)
    {
        const float h = 0.002f;
        return MathF.Abs(Position(reel, t + h) - Position(reel, t - h)) / (2f * h);
    }

    /// <summary>The face a reel at <paramref name="position"/> shows on the payline (the nearest symbol, wrapped into the strip of <paramref name="symbols"/>; 0 for none).</summary>
    public static int FaceAt(float position, int symbols)
    {
        if (symbols <= 0)
            return 0;
        int nearest = (int)MathF.Round(position);
        return ((nearest % symbols) + symbols) % symbols;
    }

    /// <summary>The landing's swing past the payline <paramref name="s"/> seconds after it reached it: a spring kicked to peak at the overshoot, settling back.</summary>
    private float Bounce(float s)
    {
        if (_overshoot <= 0f)
            return 0f;
        float kick = _tuning.KickFor(_overshoot);
        float w = _tuning.Omega, z = _tuning.Ratio;
        if (w <= 0f)
            return 0f;
        if (z >= 1f)
            return kick * s * MathF.Exp(-w * s);
        float wd = w * MathF.Sqrt(1f - z * z);
        return kick / wd * MathF.Exp(-z * w * s) * MathF.Sin(wd * s);
    }

    /// <summary>The seconds a landing's bounce takes to come within the settle tolerance.</summary>
    private float SettleSeconds()
    {
        if (_overshoot <= SettleTolerance)
            return 0f;
        float w = _tuning.Omega, z = MathF.Min(1f, _tuning.Ratio);
        if (w <= 0f || z <= 0f)
            return 0f;
        float envelope = _tuning.KickFor(_overshoot) / (w * MathF.Sqrt(MathF.Max(1e-4f, 1f - z * z)));
        return MathF.Log(MathF.Max(1f, envelope / SettleTolerance)) / (z * w);
    }

    /// <summary><paramref name="value"/> wrapped into [0, <paramref name="length"/>).</summary>
    private static float Wrap(float value, int length)
    {
        float r = value % length;
        return r < 0f ? r + length : r;
    }
}

/// <summary>
/// The Night Slots machine's lever (Saleh 2026-10-07: "pull it by dragging
/// down; it resists, with a ratchet feel, and springs back on release"):
/// its pull from 0 (up) to 1 (all the way down). Dragged, it follows the
/// hand through the ratchet's notches (held behind the hand between them,
/// a click at each); at SlotSpinKnobs.leverFireAt it fires one spin; let go
/// it springs back past its rest and settles. A click pulls it all the way
/// by itself and fires the same way. Reduced Motion: a click fires at once
/// and the lever never moves by itself.
/// </summary>
public sealed class SlotLever
{
    /// <summary>What one drag or step did: the ratchet notches it crossed going down, and whether it fired a spin.</summary>
    public readonly struct Move
    {
        /// <summary>The notches crossed (a ratchet click each).</summary>
        public readonly int Notches;

        /// <summary>True when the pull fired a spin.</summary>
        public readonly bool Fire;

        /// <summary>A move of <paramref name="notches"/> notches that fired or not.</summary>
        public Move(int notches, bool fire)
        {
            Notches = notches;
            Fire = fire;
        }
    }

    private Spring _pull;
    private bool _held, _fired, _auto;
    private int _notch;

    /// <summary>The pull now: 0 up, 1 all the way down (springing back it can swing a little past 0).</summary>
    public float Pull => _pull.Value;

    /// <summary>True while the hand holds it.</summary>
    public bool Held => _held;

    /// <summary>True while it still moves by itself (the view keeps stepping it).</summary>
    public bool Moving => !_held && (!_pull.AtRest || _auto);

    /// <summary>The hand dragged it <paramref name="px"/> down from where it took hold (reference px): the lever follows through the ratchet, firing once at the fire point.</summary>
    public Move Drag(float px, SlotSpinKnobs knobs)
    {
        _held = true;
        _auto = false;
        float raw = Clamp01(px / MathF.Max(1f, knobs.leverTravel));
        int notches = Math.Max(1, knobs.leverNotches);
        int notch = (int)MathF.Floor(raw * notches + 1e-4f);
        float caught = (float)notch / notches;
        float shown = raw >= 1f ? 1f : caught + (raw - caught) * (1f - Clamp01(knobs.leverRatchet));
        _pull.Snap(shown);
        int crossed = Math.Max(0, notch - _notch);
        _notch = Math.Max(_notch, notch);
        return new Move(crossed, Fire(shown, knobs));
    }

    /// <summary>The hand let go: it springs back to its rest (at once under Reduced Motion).</summary>
    public void Release(MotionAmount amount)
    {
        _held = false;
        _auto = false;
        Return(amount);
    }

    /// <summary>A click on it: it pulls itself all the way down, fires, and springs back (Reduced Motion: it fires on the next step and stays up).</summary>
    public void PullAll(MotionAmount amount)
    {
        _held = false;
        _auto = true;
        _fired = false;
        _notch = 0;
        if (!amount.Still)
            _pull.Target = 1f;
    }

    /// <summary>A small nod down and back (the idle lever inviting a pull: a kick of <paramref name="share"/> of its pull), unless it is held or moving.</summary>
    public void Nod(float share, SlotSpinKnobs knobs, MotionKnobs motion, MotionAmount amount)
    {
        if (_held || _auto || !_pull.AtRest || amount.Still || share <= 0f)
            return;
        _pull.Kick(motion.Get(knobs.leverReturnFeel).KickFor(share * amount.Share));
    }

    /// <summary>Advances its own motion by <paramref name="dt"/> seconds (nothing while held): the pull of a click, the spring back.</summary>
    public Move Step(float dt, SlotSpinKnobs knobs, MotionKnobs motion, MotionAmount amount)
    {
        if (_held)
            return new Move(0, false);
        if (_auto && amount.Still)
        {
            _auto = false;
            _pull.Snap(0f);
            _fired = false;
            return new Move(0, true);
        }
        int crossed = 0;
        bool fire = false;
        if (_auto)
        {
            _pull.Step(dt, motion.Get(knobs.leverPullFeel), motion.settleValue, motion.settleSpeed);
            int notch = (int)MathF.Floor(Clamp01(_pull.Value) * Math.Max(1, knobs.leverNotches) + 1e-4f);
            crossed = Math.Max(0, notch - _notch);
            _notch = Math.Max(_notch, notch);
            if (_pull.Value >= knobs.leverFireAt)
            {
                fire = Fire(_pull.Value, knobs);
                _auto = false;
                Return(amount);
            }
            return new Move(crossed, fire);
        }
        _pull.Step(dt, motion.Get(knobs.leverReturnFeel), motion.settleValue, motion.settleSpeed);
        return new Move(0, false);
    }

    /// <summary>Back up at once (the panel closed, the lever locked).</summary>
    public void Reset()
    {
        _held = _auto = _fired = false;
        _notch = 0;
        _pull.Snap(0f);
    }

    /// <summary>Fires once per pull when <paramref name="pull"/> reaches the fire point.</summary>
    private bool Fire(float pull, SlotSpinKnobs knobs)
    {
        if (_fired || pull < knobs.leverFireAt)
            return false;
        _fired = true;
        return true;
    }

    /// <summary>Springs back to rest (snaps under Reduced Motion); the next pull may fire again.</summary>
    private void Return(MotionAmount amount)
    {
        _fired = false;
        _notch = 0;
        if (amount.Still)
            _pull.Snap(0f);
        else
            _pull.Target = 0f;
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
