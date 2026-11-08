using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The rulebook folder's home in the painted pocket (DESK-HOMES P4, DESK-V3's
/// open call: the painted book and the open folder are one book; Saleh's
/// playtest of 2026-10-28, point 5: "isnt the big red binder for the folder?
/// why is the folder by default on the desk? still no place for it"). On the
/// painted desk (SetPocket, the office binder) the folder starts each day in
/// its pocket: shrunk under the painting's red book (RulebookPocket.Fit), its
/// shut board centred under the book (MeasureFit: the root is on the spine,
/// so a root laid at the book's centre stood the board out through the
/// pocket's rim; Saleh's playtest of 2026-11-07, item 3), the book shown,
/// the mat empty. A click on the book brings the folder out onto
/// the mat at its reading spot (TakeOut, eased over the pocket's lip); a drag
/// from the book takes it out under the pointer (TakeOutUnder, PocketBook);
/// the guide's page brings it out too (ShowPage). A folder let go on the
/// pocket goes back in (PutAway: DeskController.RulebookDropped, by
/// RulebookPocket.Takes): it shuts, rises over the lip, shrinks and settles
/// under the book, which shows again. While it is out the book is hidden, so
/// its lift neither rises nor sounds. Every move eases (DeskCarry, and the
/// size on RulebookPocket.Ease); under Reduced Motion they cut. Without a
/// pocket (the art desk) the folder lies on the desk as before.
/// </summary>
public sealed partial class DeskRulebook
{
    /// <summary>How high over the desk the folder passes the pocket's lip on its way in or out (metres).</summary>
    private const float PocketLift = 0.07f;

    /// <summary>How long the folder taken out under the pointer takes to grow to its size (seconds).</summary>
    private const float GrowSeconds = 0.18f;

    /// <summary>True once the office binder gave the folder its pocket (the painted desk).</summary>
    private bool _hasPocket;

    /// <summary>True while the folder lies in its pocket.</summary>
    private bool _pocketed;

    /// <summary>Where the folder's root lies in the pocket (world, on the desk plane) and the book's centre (world).</summary>
    private Vector3 _pocketAt, _bookPoint;

    /// <summary>The pocket's footprint (the desk's frame).</summary>
    private DeskRect _pocketRect;

    /// <summary>The painted book's home (shown while the folder is in).</summary>
    private GameObject _book;

    /// <summary>The share of its size the folder takes in the pocket, the book's footprint it fits (metres across and along) and whether the fit was measured (on the folder's own box: MeasureFit).</summary>
    private float _pocketFit = 1f;
    private Vector2 _bookSize;
    private bool _fitMeasured;

    /// <summary>The shut board's footprint centre from the folder's root (its own space, on its plane, at its own size; MeasureFit): the root is on the spine, not under the board's middle.</summary>
    private Vector3 _boardCentre;

    /// <summary>The folder's reading spot on the desk (Place): where TakeOut lays it.</summary>
    private Vector3 _readAt;
    private Quaternion _readTurn = Quaternion.identity;

    /// <summary>The size's ease between out (0) and in the pocket (1): from, to, its time and how long it takes.</summary>
    private float _inFrom, _inTo, _inElapsed, _inSeconds;

    /// <summary>The share the folder is in its pocket now (0 out at its own size, 1 at its fit).</summary>
    private float InShare => _inSeconds > 0f ? Mathf.Lerp(_inFrom, _inTo, RulebookPocket.Ease(_inElapsed / _inSeconds)) : _inTo;

    /// <summary>The folder's size now against its own (1 out; its fit in the pocket).</summary>
    private float PocketSize => Mathf.Lerp(1f, _pocketFit, InShare);

    /// <summary>True when the folder has a pocket (the painted desk).</summary>
    public bool HasPocket => _hasPocket;

    /// <summary>True while the folder lies in its pocket.</summary>
    public bool Pocketed => _pocketed;

    /// <summary>True while the folder goes into or out of its pocket (its carry or its size still moving): the QA rig waits for it.</summary>
    public bool PocketMoving => _carry.Running || (_inSeconds > 0f && _inElapsed < _inSeconds);

    /// <summary>Where the folder's root lies in its pocket: the book's centre less the board's centre offset at its fit, so the board, not the spine, lies under the book's middle.</summary>
    private Vector3 PocketRoot => _pocketAt - _readTurn * (_boardCentre * _pocketFit);

    /// <summary>
    /// The folder's pocket (the office binder, on the painted desk, after
    /// Place): its root lies at <paramref name="at"/> in it (world, the desk
    /// plane), the pocket's footprint is <paramref name="pocket"/> (the desk's
    /// frame), the painting's book (<paramref name="book"/>, its home, centred
    /// at <paramref name="bookPoint"/>) covers <paramref name="bookSize"/>
    /// (metres across and along). The folder goes in at once.
    /// </summary>
    public void SetPocket(Vector3 at, DeskRect pocket, Vector2 bookSize, GameObject book, Vector3 bookPoint)
    {
        _hasPocket = true;
        _pocketAt = at;
        _pocketRect = pocket;
        _book = book;
        _bookPoint = bookPoint;
        _bookSize = bookSize;
        _pocketFit = 1f;
        _fitMeasured = false;
        StowNow();
    }

    /// <summary>
    /// The folder's fit under the book (RulebookPocket.Fit) and its board's
    /// centre from its root (_boardCentre), measured once on its own box at
    /// its own size (the shut board's side), as soon as the box is there: a
    /// folder not on the desk yet (before its day, inactive, its box not
    /// taken) is measured when it first comes (Show, at the day's start).
    /// Until then it keeps its size.
    /// </summary>
    private void MeasureFit()
    {
        if (_fitMeasured)
            return;
        var corners = new Vector3[8];
        if (!TryBoxCorners(corners))
            return;
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        Vector3 right = _readTurn * Vector3.right, forward = _readTurn * Vector3.forward, centre = Vector3.zero;
        foreach (Vector3 c in corners)
        {
            x0 = Mathf.Min(x0, Vector3.Dot(c, right));
            x1 = Mathf.Max(x1, Vector3.Dot(c, right));
            z0 = Mathf.Min(z0, Vector3.Dot(c, forward));
            z1 = Mathf.Max(z1, Vector3.Dot(c, forward));
            centre += c / 8f;
        }
        _pocketFit = RulebookPocket.Fit(_bookSize.x, _bookSize.y, x1 - x0, z1 - z0);
        _boardCentre = Vector3.ProjectOnPlane(transform.InverseTransformPoint(centre), Vector3.up);
        _fitMeasured = true;
    }

    /// <summary>True when the folder let go with the pointer at <paramref name="pointer"/> or its footprint's centre at <paramref name="centre"/> (world, on the desk) goes into its pocket (RulebookPocket.Takes); false without one.</summary>
    public bool IntoPocket(Vector3 pointer, Vector3 centre)
    {
        if (!_hasPocket || _pocketed || !DeskLayout.Bound)
            return false;
        Vector2 p = DeskLayout.Frame.Local(pointer), c = DeskLayout.Frame.Local(centre);
        return RulebookPocket.Takes(_pocketRect, p.x, p.y, c.x, c.y);
    }

    /// <summary>The folder into its pocket at once (the day's start: the day's rules arrive in a shut folder in its pocket; the binder's first lay).</summary>
    private void StowNow()
    {
        if (!_hasPocket)
            return;
        _carry.Stop();
        MeasureFit(); // at its own size: until measured its fit is 1
        transform.SetPositionAndRotation(PocketRoot, _readTurn);
        SizeTo(1f, 0f);
        InPocket();
    }

    /// <summary>The folder lies in its pocket: the book shows over it.</summary>
    private void InPocket()
    {
        _pocketed = true;
        if (_book != null)
            _book.SetActive(true);
        ShowMotion();
    }

    /// <summary>The folder leaves its pocket: the book hides (its lift no longer rises or sounds).</summary>
    private void OutOfPocket()
    {
        _pocketed = false;
        if (_book != null)
            _book.SetActive(false);
    }

    /// <summary>Eases the folder's size toward <paramref name="share"/> in the pocket (1 its fit, 0 its own) over <paramref name="seconds"/> (a cut under Reduced Motion or with none).</summary>
    private void SizeTo(float share, float seconds)
    {
        _inFrom = InShare;
        _inTo = share;
        _inElapsed = 0f;
        _inSeconds = UiMotion.Amount.Still ? 0f : Mathf.Max(0f, seconds);
    }

    /// <summary>Steps the size's ease (Update); true while it moves.</summary>
    private bool StepPocket(float dt)
    {
        if (_inSeconds <= 0f || _inElapsed >= _inSeconds)
            return false;
        _inElapsed = Mathf.Min(_inSeconds, _inElapsed + dt);
        return true;
    }

    /// <summary>The seconds a carry along <paramref name="way"/> from where the folder lies takes (the desk's slide, a longer way at the docks' speed).</summary>
    private float CarrySeconds(IReadOnlyList<Vector3> way)
    {
        float length = 0f;
        Vector3 at = transform.position;
        foreach (Vector3 p in way)
        {
            length += Vector3.Distance(at, p);
            at = p;
        }
        DeskConfigSO c = _shakeConfig != null ? _shakeConfig : _config;
        return DeskCarry.Seconds(length, c != null ? c.dockSpeed : 0.9f, c != null ? c.paperSlideSeconds : 0.25f);
    }

    /// <summary>
    /// The folder out of its pocket onto the mat at its reading spot (a click
    /// on the book; the guide's page): the book hides, the folder rises over
    /// the lip, grows to its size and settles there. False when it is not in
    /// its pocket or not on the desk today (the book only ticks then).
    /// </summary>
    public bool TakeOut()
    {
        if (!_pocketed || !isActiveAndEnabled)
            return false;
        OutOfPocket();
        var way = new List<Vector3> { PocketRoot + Vector3.up * PocketLift, _readAt + Vector3.up * PocketLift, _readAt };
        float seconds = UiMotion.Amount.Still ? 0f : CarrySeconds(way);
        _carry.Start(transform, way, _readTurn, seconds, null);
        SizeTo(0f, seconds);
        ShowMotion();
        return true;
    }

    /// <summary>The folder out of its pocket under the pointer (a drag begun on the book: PocketBook hands the drag to the folder's own): the book hides and the folder grows to its size as it is carried; false when it is not in its pocket or not on the desk today.</summary>
    public bool TakeOutUnder()
    {
        if (!_pocketed || !isActiveAndEnabled)
            return false;
        OutOfPocket();
        SizeTo(0f, GrowSeconds);
        ShowMotion();
        return true;
    }

    /// <summary>
    /// The folder let go on its pocket goes back in (DeskController): it
    /// shuts, rises over the lip, shrinks to its fit and settles under the
    /// book, which shows once it lies there.
    /// </summary>
    public void PutAway()
    {
        if (!_hasPocket || _pocketed)
            return;
        if (folderCover != null && !folderCover.Shut)
            folderCover.Close();
        var way = new List<Vector3> { transform.position + Vector3.up * PocketLift, PocketRoot + Vector3.up * PocketLift, PocketRoot };
        float seconds = UiMotion.Amount.Still ? 0f : CarrySeconds(way);
        if (seconds <= 0f)
        {
            StowNow();
            return;
        }
        _carry.Start(transform, way, _readTurn, seconds, InPocket);
        SizeTo(1f, seconds);
        ShowMotion();
    }
}
