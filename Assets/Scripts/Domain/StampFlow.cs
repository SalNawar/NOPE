using System;

/// <summary>A desk stamp (the desk-first redesign, item 12): none, the APPROVED stamp or the DENIED stamp.</summary>
public enum DeskStamp
{
    /// <summary>No stamp (nothing held; no verdict on the passport yet).</summary>
    None,

    /// <summary>The APPROVED stamp (green): the traveller goes through.</summary>
    Approved,

    /// <summary>The DENIED stamp (red): the traveller is turned back.</summary>
    Denied
}

/// <summary>What a press of the held stamp leaves on a paper.</summary>
public enum StampMark
{
    /// <summary>Nothing (no stamp held).</summary>
    None,

    /// <summary>A faint, dry mark: the stamp was not inked, so the press counts for nothing.</summary>
    Faint,

    /// <summary>An inked mark: on the passport it is the verdict.</summary>
    Inked
}

/// <summary>
/// The physical stamps (the desk-first redesign, Saleh 2026-10-05, item 12:
/// "approve or reject are actual physical seals: the player picks stamps,
/// inks them, then stamps on the document"): the stamp tray slides out, the
/// player picks up the APPROVED or the DENIED stamp, presses it on the ink
/// pad (each inking lasts a number of presses: a knob, 1 by default), then
/// presses it on a paper. An inked press on the passport (the traveller's
/// first paper) is the passport's verdict; a later inked press of the other
/// stamp replaces it (a correction; both marks stay on the paper). A dry
/// press leaves a faint mark and counts for nothing. The papers handed back
/// with a verdict on the passport decide the case (deny is free; a denial
/// with no logged evidence earns the one citation: VerdictRules). Each stamp
/// keeps its own ink until it is pressed; a stamp put back on the tray keeps
/// it. A new case starts with no verdict and nothing held, the tray as it
/// was. Pure; tested headless; DeskStampTray applies it.
/// </summary>
public sealed class StampFlow
{
    private readonly int _pressesPerInking;
    private int _approvedInk;
    private int _deniedInk;

    /// <summary>A flow whose inking lasts <paramref name="pressesPerInking"/> presses (at least 1).</summary>
    public StampFlow(int pressesPerInking)
    {
        _pressesPerInking = Math.Max(1, pressesPerInking);
    }

    /// <summary>True while the stamp tray is out.</summary>
    public bool TrayOut { get; private set; }

    /// <summary>The stamp in the hand (None: none).</summary>
    public DeskStamp Held { get; private set; }

    /// <summary>The passport's verdict: the stamp of its last inked press (None: not stamped yet).</summary>
    public DeskStamp Verdict { get; private set; }

    /// <summary>True when the papers can be handed back: the passport carries a verdict.</summary>
    public bool CanHandBack => Verdict != DeskStamp.None;

    /// <summary>Presses left on <paramref name="stamp"/>'s ink (0: dry).</summary>
    public int InkOf(DeskStamp stamp) => stamp == DeskStamp.Approved ? _approvedInk : stamp == DeskStamp.Denied ? _deniedInk : 0;

    /// <summary>True when the held stamp is inked.</summary>
    public bool HeldInked => InkOf(Held) > 0;

    /// <summary>Slides the tray out (false when it is out already).</summary>
    public bool OpenTray()
    {
        if (TrayOut)
            return false;
        TrayOut = true;
        return true;
    }

    /// <summary>Slides the tray back in, the held stamp put back on it first (false when it is in already).</summary>
    public bool CloseTray()
    {
        if (!TrayOut)
            return false;
        Held = DeskStamp.None;
        TrayOut = false;
        return true;
    }

    /// <summary>Picks <paramref name="stamp"/> up from the tray (the tray out): the stamp held before goes back on the tray. False when nothing changes.</summary>
    public bool PickUp(DeskStamp stamp)
    {
        if (!TrayOut || stamp == DeskStamp.None || Held == stamp)
            return false;
        Held = stamp;
        return true;
    }

    /// <summary>Puts the held stamp back on the tray (false when none is held).</summary>
    public bool PutDown()
    {
        if (Held == DeskStamp.None)
            return false;
        Held = DeskStamp.None;
        return true;
    }

    /// <summary>Presses the held stamp on the ink pad: it is inked for the knob's presses (false when none is held).</summary>
    public bool Ink()
    {
        if (Held == DeskStamp.None)
            return false;
        if (Held == DeskStamp.Approved)
            _approvedInk = _pressesPerInking;
        else
            _deniedInk = _pressesPerInking;
        return true;
    }

    /// <summary>
    /// Presses the held stamp on a paper (<paramref name="onPassport"/>: the
    /// traveller's passport): an inked press uses one press of its ink and
    /// leaves an inked mark, which on the passport sets the verdict; a dry
    /// press leaves a faint mark and sets nothing. None without a stamp.
    /// </summary>
    public StampMark Press(bool onPassport)
    {
        if (Held == DeskStamp.None)
            return StampMark.None;
        if (!HeldInked)
            return StampMark.Faint;
        if (Held == DeskStamp.Approved)
            _approvedInk--;
        else
            _deniedInk--;
        if (onPassport)
            Verdict = Held;
        return StampMark.Inked;
    }

    /// <summary>A new traveller: no verdict and nothing in the hand (the stamps keep their ink, the tray stays as it is).</summary>
    public void BeginCase()
    {
        Verdict = DeskStamp.None;
        Held = DeskStamp.None;
    }
}
