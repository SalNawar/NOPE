/// <summary>A desk stamp (the desk-first redesign, item 12): none, the APPROVED stamp or the DENIED stamp.</summary>
public enum DeskStamp
{
    /// <summary>No stamp (no verdict on the passport yet).</summary>
    None,

    /// <summary>The APPROVED stamp (green): the traveller goes through.</summary>
    Approved,

    /// <summary>The DENIED stamp (red): the traveller is turned back.</summary>
    Denied
}

/// <summary>What a stamp's press did (StampFlow.Press). Not serialized.</summary>
public enum StampPress
{
    /// <summary>Nothing lay under the stamp (it thumps on the bare desk; no mark).</summary>
    Nothing,

    /// <summary>The passport lay under the stamp (anywhere on it: Saleh 2026-10-06, "I should be able to stamp anywhere on the document"): the mark prints in its ENTRY VISA box and is the passport's verdict.</summary>
    Stamped,

    /// <summary>A paper that is not the passport lay under the stamp: refused (no mark; other documents never take a verdict stamp).</summary>
    NotPassport,

    /// <summary>The passport already carries a verdict: refused (no mark; one verdict per passport, so approve and deny together is impossible).</summary>
    AlreadyStamped
}

/// <summary>
/// The stamps, Papers, Please's way (Saleh 2026-10-06, "copy the controls of
/// Papers, Please 1:1"; it replaces the desk-first redesign's pick-up and ink
/// pad): the stamp bar slides out at the desk's right edge (its grey tab, or
/// TAB) and back; it holds the APPROVED and the DENIED stamp. A stamp pressed
/// (dragged over a paper and let go, or clicked where it hangs: Saleh
/// 2026-10-06, "the stamp should be two stamps that I physically move ... I
/// should be able to stamp anywhere on the document") stamps whatever lies
/// under it: only the passport takes it, anywhere on it (the mark prints in
/// its ENTRY VISA box), and only once. The first press there is the
/// passport's verdict; any later press on the passport, of either stamp, is
/// refused (Saleh: "there is a bug that you can both approve and decline a
/// paper"), and so is a press on any other paper. The papers handed back (the stamped passport dropped on the counter)
/// decide the case with that verdict (deny is free; a denial with no logged
/// evidence earns the one citation: VerdictRules). A new case starts with no
/// verdict, the bar as it was. Pure; tested headless; DeskStampTray applies it.
/// </summary>
public sealed class StampFlow
{
    /// <summary>True while the stamp bar is out.</summary>
    public bool BarOut { get; private set; }

    /// <summary>The passport's verdict: the stamp of its one accepted press (None: not stamped yet).</summary>
    public DeskStamp Verdict { get; private set; }

    /// <summary>True when the papers can be handed back: the passport carries a verdict.</summary>
    public bool CanHandBack => Verdict != DeskStamp.None;

    /// <summary>Slides the bar out, or back in (TAB, the grey tab); returns whether it is out now.</summary>
    public bool ToggleBar()
    {
        BarOut = !BarOut;
        return BarOut;
    }

    /// <summary>Slides the bar back in (false when it is in already): the back-out (right-click, Esc) or the desk taken away.</summary>
    public bool StowBar()
    {
        if (!BarOut)
            return false;
        BarOut = false;
        return true;
    }

    /// <summary>
    /// Presses <paramref name="stamp"/> on what lies under it: nothing
    /// (<paramref name="onPaper"/> false) is Nothing; a paper that is not the
    /// passport is NotPassport; the passport with a verdict already is
    /// AlreadyStamped; else (<paramref name="onPassport"/>, anywhere on it)
    /// the press is Stamped and sets the verdict. A press while the bar is
    /// in, or of no stamp, is Nothing.
    /// </summary>
    public StampPress Press(DeskStamp stamp, bool onPaper, bool onPassport)
    {
        if (!BarOut || stamp == DeskStamp.None || !onPaper)
            return StampPress.Nothing;
        if (!onPassport)
            return StampPress.NotPassport;
        if (Verdict != DeskStamp.None)
            return StampPress.AlreadyStamped;
        Verdict = stamp;
        return StampPress.Stamped;
    }

    /// <summary>A new traveller: no verdict (the bar stays as it is).</summary>
    public void BeginCase() => Verdict = DeskStamp.None;
}
