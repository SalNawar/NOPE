using System;

/// <summary>What the stamp rack's cradles did in one step (DrawerSequence.Step): the moments its clicks play on.</summary>
[Flags]
public enum DrawerEvents
{
    /// <summary>Nothing worth a sound.</summary>
    None = 0,

    /// <summary>The DENIED cradle locked upright (a click).</summary>
    LockedDenied = 4,

    /// <summary>The APPROVED cradle locked upright (a click).</summary>
    LockedApproved = 8,

    /// <summary>The DENIED cradle came down flat on the bed.</summary>
    FoldedDenied = 16,

    /// <summary>The APPROVED cradle came down flat on the bed.</summary>
    FoldedApproved = 32
}

/// <summary>
/// The fixed stamp rack's cradles (the brass drawer's mechanism, Track BR,
/// Saleh 2026-10-08: the daters "lay flat when closed and stand upright when
/// opened", "like a mechanical contraption"; since the desk plan, Saleh's
/// D4, the rack is fixed at its home and never slides: BUG-73b removed the
/// drawer's invisible slide, which held the cradles back about 0.34 s every
/// case). Each cradle's raise runs 0 (the dater flat on its back) to 1
/// (upright). A case opening stands them at once, DENIED then APPROVED
/// drawerRaiseStagger later, each on the Cradle spring until it hits its
/// upright stop: it locks there (a click) and bounces back by
/// drawerLockBounce of its speed. A case ending folds them (APPROVED first,
/// DENIED drawerFoldStagger later; not while the caller holds them, a dater
/// still on its way back). A reversal mid-way picks up from where it is.
/// Under Reduced Motion it snaps (the events still come). Pure: the stamp
/// tray steps it and poses the mechanism from it.
/// </summary>
public sealed class DrawerSequence
{
    /// <summary>The DENIED cradle's lane (it stands first).</summary>
    public const int Denied = 0;

    /// <summary>The APPROVED cradle's lane.</summary>
    public const int Approved = 1;

    private float _stagger;
    private bool _open;
    private readonly Spring[] _raise = { Spring.At(0f), Spring.At(0f) };
    private readonly bool[] _locked = new bool[2];
    private readonly bool[] _down = { true, true };

    /// <summary>How far <paramref name="lane"/>'s cradle stands: 0 flat, 1 upright (a little under in its bounce).</summary>
    public float Raise(int lane) => _raise[lane].Value;

    /// <summary>True while <paramref name="lane"/>'s cradle is locked upright (its dater takes input) until its fold begins.</summary>
    public bool Locked(int lane) => _locked[lane];

    /// <summary>True when no case is open and both daters lie flat and still.</summary>
    public bool Closed => !_open && _down[0] && _down[1] && _raise[0].AtRest && _raise[1].AtRest;

    /// <summary>
    /// Advances the cradles by <paramref name="dt"/> seconds toward upright
    /// (<paramref name="open"/>: a case is open) or flat, with the
    /// MotionKnobs' cradle tunings; <paramref name="still"/> (Reduced Motion)
    /// snaps them; while folding, <paramref name="holdFold"/> keeps them up (a
    /// dater is still away from its cradle). Returns what happened.
    /// </summary>
    public DrawerEvents Step(float dt, bool open, MotionKnobs knobs, bool still, bool holdFold)
    {
        if (open != _open)
        {
            _open = open;
            _stagger = 0f;
        }
        if (still)
            return Snap(holdFold);
        if (open)
        {
            for (int lane = Denied; lane <= Approved; lane++)
                if (_stagger >= (lane == Denied ? 0f : knobs.drawerRaiseStagger) && _raise[lane].Target != 1f)
                {
                    _raise[lane].Target = 1f;
                    _down[lane] = false;
                }
            _stagger += dt;
        }
        else if (!holdFold)
        {
            for (int lane = Approved; lane >= Denied; lane--)
                if (_stagger >= (lane == Approved ? 0f : knobs.drawerFoldStagger) && _raise[lane].Target != 0f)
                {
                    _raise[lane].Target = 0f;
                    _locked[lane] = false;
                }
            _stagger += dt;
        }
        return Lanes(dt, knobs);
    }

    /// <summary>The cradles on the Cradle spring, each against its stops: upright it locks (once) and bounces back; flat it lands (once) and bounces up.</summary>
    private DrawerEvents Lanes(float dt, MotionKnobs knobs)
    {
        DrawerEvents e = DrawerEvents.None;
        SpringTuning tuning = knobs.Get(knobs.cradleFeel);
        float bounce = Clamp01(knobs.drawerLockBounce);
        for (int lane = Denied; lane <= Approved; lane++)
        {
            ref Spring s = ref _raise[lane];
            s.Step(dt, tuning, 1e-3f, 0.05f);
            if (s.Target >= 1f && s.Value >= 1f && s.Velocity >= 0f)
            {
                s.Value = 1f;
                s.Velocity = -s.Velocity * bounce;
                s.Settle(1e-3f, 0.05f);
                if (!_locked[lane])
                {
                    _locked[lane] = true;
                    e |= lane == Denied ? DrawerEvents.LockedDenied : DrawerEvents.LockedApproved;
                }
            }
            else if (s.Target <= 0f && s.Value <= 0f && s.Velocity <= 0f)
            {
                s.Value = 0f;
                s.Velocity = -s.Velocity * bounce;
                s.Settle(1e-3f, 0.05f);
                if (!_down[lane])
                {
                    _down[lane] = true;
                    e |= lane == Denied ? DrawerEvents.FoldedDenied : DrawerEvents.FoldedApproved;
                }
            }
        }
        return e;
    }

    /// <summary>Reduced Motion: straight to both cradles locked upright, or (once no dater is away) straight to both flat; each change still says what happened.</summary>
    private DrawerEvents Snap(bool holdFold)
    {
        DrawerEvents e = DrawerEvents.None;
        if (_open)
        {
            for (int lane = Denied; lane <= Approved; lane++)
            {
                _raise[lane].Snap(1f);
                _down[lane] = false;
                if (!_locked[lane])
                {
                    _locked[lane] = true;
                    e |= lane == Denied ? DrawerEvents.LockedDenied : DrawerEvents.LockedApproved;
                }
            }
            return e;
        }
        if (holdFold && !(_down[Denied] && _down[Approved]))
            return e;
        for (int lane = Denied; lane <= Approved; lane++)
        {
            _raise[lane].Snap(0f);
            _locked[lane] = false;
            if (!_down[lane])
            {
                _down[lane] = true;
                e |= lane == Denied ? DrawerEvents.FoldedDenied : DrawerEvents.FoldedApproved;
            }
        }
        return e;
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}

/// <summary>
/// The art-or-fallback rule of a prop contract (the brass drawer, the
/// scanner): a model replaces the built stand-in only when it carries every
/// part the contract names; one missing part keeps the stand-in (and the
/// builder says which).
/// </summary>
public static class PropArt
{
    /// <summary>The names of <paramref name="required"/> the model lacks (<paramref name="has"/> says whether it carries one); empty when the model is complete.</summary>
    public static System.Collections.Generic.List<string> Missing(System.Collections.Generic.IEnumerable<string> required, Func<string, bool> has)
    {
        var missing = new System.Collections.Generic.List<string>();
        if (required == null)
            return missing;
        foreach (string name in required)
            if (has == null || !has(name))
                missing.Add(name);
        return missing;
    }

    /// <summary>True when there is a model (<paramref name="present"/>) and it carries every part of <paramref name="required"/>: the art is used, else the built stand-in.</summary>
    public static bool UseArt(bool present, System.Collections.Generic.IEnumerable<string> required, Func<string, bool> has) =>
        present && Missing(required, has).Count == 0;

    /// <summary>The cream scanner's parts (tools/props/scanner/scanner_model.py): its body, the hinged lid, the bed's glass, the sweep bar and the readout.</summary>
    public static readonly string[] Scanner = { "Body", "Lid", "Glass", "SweepBar", "Readout" };

    /// <summary>The chip reader's parts (tools/props/machines/chip_reader_model.py; the desk machines spec §1), as paths under the model's root: the body, the pad and its centre, the slot's flap, the readout and the three lamps.</summary>
    public static readonly string[] ChipReader =
    {
        "Body", "Body/Pad", "Body/PadCentre", "Body/SlotFlap", "Body/Readout", "Body/LED_Power", "Body/LED_Read", "Body/LED_Fault"
    };

    /// <summary>The UV torch's parts (tools/props/machines/uv_torch_model.py): the torch with its thumb switch, lens and beam, and the cradle.</summary>
    public static readonly string[] UvTorch = { "UvTorch", "UvTorch/Switch", "UvTorch/Lens", "UvTorch/BeamFX", "Cradle" };

    /// <summary>The upgrades' desk objects' parts (tools/props/machines/upgrade_props_model.py; the desk machines spec §4), as paths under each model's root. The supply crate: its four flaps, the tape, the label and the point its tag rises from.</summary>
    public static readonly string[] SupplyCrate =
    {
        "Crate", "Crate/Flap_Left", "Crate/Flap_Right", "Crate/Flap_Front", "Crate/Flap_Back", "Crate/Tape", "Crate/Label", "Crate/ItemPoint"
    };

    /// <summary>The Auto-Feed tray (on the scanner, in its frame): the rollers and the feed lamp.</summary>
    public static readonly string[] ScannerFeeder = { "Feeder", "Feeder/Rollers", "Feeder/FeedLED" };

    /// <summary>The Analysis beacon (on the scanner, in its frame): the pod, its mirror and its lamp.</summary>
    public static readonly string[] AnalysisBeacon = { "Beacon", "Beacon/BeaconPod", "Beacon/BeaconPod/Reflector", "Beacon/BeaconPod/BeaconLamp" };

    /// <summary>The Interview Protocols' card ring: the stand, the ring and its four cards.</summary>
    public static readonly string[] ProtocolRing =
    {
        "ProtocolStand", "ProtocolStand/Ring", "ProtocolStand/Ring/Card_0", "ProtocolStand/Ring/Card_1", "ProtocolStand/Ring/Card_2", "ProtocolStand/Ring/Card_3"
    };

    /// <summary>The BABEL rack: the needle and the four cartridges with their lamps.</summary>
    public static readonly string[] BabelRack =
    {
        "BabelRack", "BabelRack/Needle",
        "BabelRack/Cartridge_0", "BabelRack/Cartridge_1", "BabelRack/Cartridge_2", "BabelRack/Cartridge_3",
        "BabelRack/Cartridge_0/CartridgeLED_0", "BabelRack/Cartridge_1/CartridgeLED_1", "BabelRack/Cartridge_2/CartridgeLED_2", "BabelRack/Cartridge_3/CartridgeLED_3"
    };

    /// <summary>The Rolodex: the drum and the knob.</summary>
    public static readonly string[] Rolodex = { "Rolodex", "Rolodex/Drum", "Rolodex/Knob" };

    /// <summary>The RING SERVICE panel: per module (1 to 5) its lamp, breaker and sticker.</summary>
    public static readonly string[] RingPanel =
    {
        "RingPanel",
        "RingPanel/Lamp_1", "RingPanel/Lamp_2", "RingPanel/Lamp_3", "RingPanel/Lamp_4", "RingPanel/Lamp_5",
        "RingPanel/Breaker_1", "RingPanel/Breaker_2", "RingPanel/Breaker_3", "RingPanel/Breaker_4", "RingPanel/Breaker_5",
        "RingPanel/Sticker_1", "RingPanel/Sticker_2", "RingPanel/Sticker_3", "RingPanel/Sticker_4", "RingPanel/Sticker_5"
    };

    /// <summary>The brass stamp drawer's parts (tools/props/brass_drawer.py): the tray, and per lane its cradle, pinion, rack, lever and nameplate.</summary>
    public static readonly string[] BrassDrawer =
    {
        "Tray",
        "CradleDenied", "CradleApproved",
        "PinionDenied", "PinionApproved",
        "RackDenied", "RackApproved",
        "LeverDenied", "LeverApproved",
        "PlateDenied", "PlateApproved"
    };
}
