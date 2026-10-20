using System;
using UnityEngine;

/// <summary>
/// The rulebook folder's front board (Saleh's D3: "yes" to the passport and
/// the folder opening and closing; FIX_PLAN DOC-LOOK: the folder opens to its
/// tabbed sheet; GAME_FEEL_BIBLE A3: T2 open). A child of the folder's
/// booklet in its card's plane: a hinge on the folder's spine carrying the
/// board's outside (manila, the Bureau's seal) and, back to back with it, the
/// board's inside, the left half of the folder's art (its cover page), so the
/// swing lays the cover page down where it lies open. Shut, the board lies
/// over the right sheet and its rows (the folder shows only that half,
/// DeskRulebook.ShowShut); open, the hinge hides and the folder's own face
/// shows. It swings on unscaled time (BookletFlip: a 15 degree lift, then
/// the swing, its overshoot a rebound off the desk, never through it; at once
/// under Reduced Motion), its free edge bowing 2 mm behind the swing (a
/// heavy board: GAME_FEEL_BIBLE A3, at most 3 mm on the flip; PaperCover.Bow;
/// the seal rides the bow, PaperCover.Ride, so the board never comes up
/// through it), with the bank's rulebook_page cue (FEEDBACK's). The
/// folder arrives shut each day (DeskRulebook.Show); a click on the shut
/// folder or on a tab opens it, a click on its open cover page (the
/// FolderCoverClick box over the left board) shuts it. DESK-HOMES' pocket
/// fold (Phase B) puts it away shut.
/// </summary>
public sealed class FolderCover : MonoBehaviour
{
    /// <summary>The hinge on the spine (it turns about its up axis, the spine's line: 0 shut over the right sheet, 180 open over the left).</summary>
    [SerializeField] private Transform hinge;

    /// <summary>The board's outside (a unit quad in the folder's manila, its free edge at its +x).</summary>
    [SerializeField] private Renderer front;

    /// <summary>The board's inside (a unit quad showing the left half of the folder's art, turned to face away while shut).</summary>
    [SerializeField] private Renderer back;

    /// <summary>The Bureau's seal on the board's outside (a quad over the front's middle): it rides the front's bow (PaperCover.Ride).</summary>
    [SerializeField] private Transform seal;

    /// <summary>The open folder's cover page as a click box (over the left board): a click shuts the folder; off while it is shut.</summary>
    [SerializeField] private Clickable coverClick;

    /// <summary>Where the spine runs across the folder's face (its unit space: -0.5 left edge, 0.5 right; measured on Forms/rulebook_folder: the crease between the rivets).</summary>
    [SerializeField] private float spine = 0.011f;

    /// <summary>How far over the folder's face the shut board lies (metres toward the viewer): over every printed row (the builder prints them 0.6 mm up) and the tab words.</summary>
    public const float Lift = 0.0012f;

    /// <summary>The bank's cue for the board's swing (SoundBank_Default; GAME_FEEL_BIBLE A3).</summary>
    private const string SwingCue = "rulebook_page";

    /// <summary>The board's bow at its swing's height (metres; its free edge bows twice it).</summary>
    public const float FlipBow = 0.001f;

    /// <summary>The bow last shown (metres).</summary>
    private float _bowShown = float.NaN;

    /// <summary>The seal's place on the flat board (the hinge's space) and its half size (metres), as built.</summary>
    private Vector3 _sealRest;
    private Vector2 _sealHalf;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private readonly BookletFlip _flip = new BookletFlip();
    private bool _shut = true;
    private MaterialPropertyBlock _block;

    /// <summary>Raised when the folder shows shut (true: the board over the right sheet or swinging) or lies open (false).</summary>
    public event Action<bool> ShutChanged;

    /// <summary>True while the board lies over the right sheet or swings (the rows are hidden and take no clicks).</summary>
    public bool Shut => _shut;

    /// <summary>The spine's place across the face (its unit space).</summary>
    public float Spine => spine;

    /// <summary>The cover page's click box (DeskRulebook adds it to the folder's clicks).</summary>
    public Clickable CoverClick => coverClick;

    private void Awake()
    {
        if (seal != null)
        {
            _sealRest = seal.localPosition;
            _sealHalf = new Vector2(Mathf.Abs(seal.localScale.x), Mathf.Abs(seal.localScale.y)) / 2f;
        }
        PaperCover.UseSheet(front);
        PaperCover.UseSheet(back);
        if (coverClick != null)
            coverClick.onClick.AddListener(() => Close());
        Show(true);
    }

    /// <summary>Shut at once, with no swing (the folder at a day's start).</summary>
    public void ShutNow()
    {
        _flip.Shut();
        Show(true);
    }

    /// <summary>Swings open (nothing when open or opening; at once under Reduced Motion); true when it started.</summary>
    public bool Open() => Swing(_flip.Open(UiMotion.Amount.Still));

    /// <summary>Swings shut (nothing when shut or shutting); true when it started.</summary>
    public bool Close() => Swing(_flip.Close(UiMotion.Amount.Still));

    /// <summary>The board's inside shows <paramref name="face"/> (the folder's face art now: its clean face while a culture's language is read); null: the material's own.</summary>
    public void Face(Texture2D face)
    {
        if (back == null)
            return;
        _block ??= new MaterialPropertyBlock();
        back.GetPropertyBlock(_block);
        Texture shown = face != null ? face : back.sharedMaterial != null ? back.sharedMaterial.GetTexture(BaseMapId) : null;
        if (shown != null)
            _block.SetTexture(BaseMapId, shown); // set by name: a cleared block would drop the board's bow
        back.SetPropertyBlock(_block);
    }

    private bool Swing(bool started)
    {
        if (started)
            Sounds.Play(SwingCue);
        Show(false);
        return started;
    }

    /// <summary>Only while it swings.</summary>
    private void Update()
    {
        if (_flip.Tick(Time.unscaledDeltaTime))
            Show(false);
    }

    /// <summary>The hinge at the board's angle (hidden while the folder lies open: its own face shows the cover page); ShutChanged when that changes.</summary>
    private void Show(bool force)
    {
        bool shut = _flip.State != CoverState.Open;
        if (hinge != null)
        {
            if (hinge.gameObject.activeSelf != shut)
                hinge.gameObject.SetActive(shut);
            hinge.localRotation = Quaternion.Euler(0f, _flip.Angle, 0f);
        }
        float bow = FlipBow * _flip.Flex;
        if (bow != _bowShown)
        {
            _bowShown = bow;
            _block ??= new MaterialPropertyBlock();
            // the outside's free edge is its +x; the inside is turned over the spine (its free edge its -x, its +z the other way)
            PaperCover.Bow(front, _block, Vector2.right, bow);
            PaperCover.Bow(back, _block, Vector2.left, -bow);
            if (front != null)
                PaperCover.Ride(seal, _sealRest, _sealHalf, front.transform, Vector2.right, bow);
        }
        if (coverClick != null && coverClick.TryGetComponent(out Collider box))
            box.enabled = !shut;
        if (shut == _shut && !force)
            return;
        _shut = shut;
        ShutChanged?.Invoke(shut);
    }
}
