using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The traveller wheel: the interview's choices on an elliptical ring around
/// the traveller (the ring's InteractionPanelController lays them out with
/// RadialLayoutGroup; "&lt; Back" sits in the centre slot), opened by clicking
/// the traveller or the desk intercom while BoothCoordinator allows it, each
/// choice with its kind's icon; and the traveller's speech: their lines in a
/// speech bubble beside them, one after another, typed out and paced
/// (SpeechQueue), each changing a premade's expression as it starts, and the
/// traveller's pose frame on each beat (TravellerPose.ForBeat: the line
/// they say, thinking while the wheel is open, else neutral); from
/// translation's first day a line is in the claimed place's tongue, and with
/// the region's Speech translator its letters flip into English behind the
/// typing (its hold starts once the flip ends; opening the wheel finishes the
/// flip at once); without it the line stays in the script but for its key
/// words (DialogLine.English), which show in English and never flip. The
/// bubble's answer lines are evidence (piece 10): while
/// an answer shows, the bubble's button takes a click (LineClicked, the
/// DialogLine said; the pick lights the bubble while that line shows), and
/// hovering the bubble holds its line (SpeechQueue.Hold). This host
/// is always active (so it wakes at load and paces the bubble while the ring is
/// closed); its Catcher child, a full-screen click-to-close area holding the
/// ring, is shown only while the wheel is open. A left-click outside the ring
/// closes it, and so do a right-click and Esc (the one input model's back-out,
/// ControlRules: OfficeControls closes it).
/// </summary>
public sealed class TravellerWheel : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Where final icon art is loaded from, by DialogChoiceKinds.IconName (Assets/Art/UI/Resources/WheelIcons).</summary>
    private const string IconFolder = "WheelIcons";

    /// <summary>The full-screen transparent click catcher, active only while open; the ring sits under it.</summary>
    [SerializeField] private GameObject catcher;

    /// <summary>The UI kit: a choice's icon is its pictogram tile (optional; without it the icon art or the placeholder).</summary>
    [SerializeField] private UiKitSO kit;

    /// <summary>The ring's root (anchors and pivot (0.5, 0.5)), placed over the traveller.</summary>
    [SerializeField] private RectTransform ring;

    /// <summary>The band at the overlay's top the ring never covers (canvas reference px): the office case HUD's strips, when the desk view puts the traveller's head above the screen.</summary>
    [SerializeField] private float ringTopInset;

    /// <summary>The ring's layout.</summary>
    [SerializeField] private RadialLayoutGroup layout;

    /// <summary>The ring's centre slot ("&lt; Back").</summary>
    [SerializeField] private RectTransform centreSlot;

    /// <summary>The traveller: its anchor places the ring and the speech bubble, and its figure shows a premade's expression.</summary>
    [SerializeField] private TravellerView traveller;

    /// <summary>The speech bubble (an overlay callout).</summary>
    [SerializeField] private OverlayCallout bubble;

    /// <summary>The wheel's layout and the bubble's pacing knobs.</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The bubble panel's button (piece 10; optional): interactable only while an answer shows, so the answer can be picked; its image lights while the picked line shows.</summary>
    [SerializeField] private Button bubbleButton;

    /// <summary>A line said in the bubble as a compare highlight: the bubble lights while that line shows.</summary>
    private sealed class BubbleHighlight : ICompareHighlight
    {
        private readonly TravellerWheel _wheel;
        private readonly int _tag;

        public BubbleHighlight(TravellerWheel wheel, int tag)
        {
            _wheel = wheel;
            _tag = tag;
        }

        public void Show(bool picked, Color colour)
        {
            if (_wheel != null)
                _wheel.SetPicked(_tag, picked, colour);
        }
    }

    /// <summary>The lines said to the bubble this case, in order (a line's tag is its index here).</summary>
    private readonly List<DialogLine> _said = new List<DialogLine>();

    /// <summary>The bubble panel's own colour, tinted while the picked line shows.</summary>
    private ImageHighlight _bubbleTint;

    /// <summary>The picked line's tag (-1: none) and its highlight colour.</summary>
    private int _pickedTag = -1;
    private Color _pickColour;

    private bool _canOpen;
    private Camera _camera;
    private RectTransform _canvasRect;
    private SpeechQueue _speech;

    /// <summary>The current traveller's translation (their lines' tongue and the Speech translator).</summary>
    private CaseTranslation _translation = CaseTranslation.None;

    /// <summary>Drives the bubble's text through the translation flip.</summary>
    private readonly TextFlip _flip = new TextFlip();

    /// <summary>The speech queue's LineNumber last drawn (a change means a new line started).</summary>
    private int _drawnLine;

    /// <summary>True while the bubble shows a line this wheel put up.</summary>
    private bool _bubbleUp;
    private readonly Dictionary<DialogChoiceKind, Sprite> _icons = new Dictionary<DialogChoiceKind, Sprite>();

    /// <summary>The open wheel's pills in canvas space, the speech bubble's obstacles (reused each frame).</summary>
    private readonly List<FaceRect> _pills = new List<FaceRect>();

    /// <summary>A pill's world corners (reused).</summary>
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>The bubble's tail (its panel's Tail child: the kit's), found once, with its length out of the box and how far it tucks under the box's ink line (the builder's).</summary>
    private RectTransform _tail;
    private float _tailLength, _tailTuck;
    private readonly List<UnityEngine.Object> _generated = new List<UnityEngine.Object>();

    /// <summary>True while the wheel is open.</summary>
    public bool IsOpen => catcher != null && catcher.activeSelf;

    /// <summary>Raised when the wheel opens or closes.</summary>
    public event Action OpenChanged;

    /// <summary>Raised when the bubble's answer is clicked, with the line said.</summary>
    public event Action<DialogLine> LineClicked;

    /// <summary>The line the bubble shows, or null.</summary>
    public DialogLine CurrentLine => _speech != null && _speech.Tag >= 0 && _speech.Tag < _said.Count ? _said[_speech.Tag] : null;

    /// <summary>Where a pick of the line the bubble shows lights up (the bubble, while that line shows).</summary>
    public ICompareHighlight BubbleHighlightNow => new BubbleHighlight(this, _speech != null ? _speech.Tag : -1);

    /// <summary>
    /// Runs at load (the host is active): applies the knobs to the ring, its
    /// centre and the speech pacing, sizes the ring to the box around every
    /// item (so the projection keeps it on screen), caches the overlay canvas's
    /// rect (the per-frame placement looks nothing up) and closes the wheel
    /// (never deactivating this host). The office camera comes from the office
    /// binder (SetCamera), once the art office is there.
    /// </summary>
    private void Awake()
    {
        if (config != null)
        {
            if (layout != null)
            {
                layout.Radii = config.wheelRadii;
                layout.ItemSize = config.wheelItemSize;
            }

            if (ring != null)
            {
                (float width, float height) = RadialLayout.Extent(config.wheelRadii.x, config.wheelRadii.y, config.wheelItemSize.x, config.wheelItemSize.y);
                ring.sizeDelta = new Vector2(width, height);
            }

            if (centreSlot != null)
                centreSlot.sizeDelta = config.wheelCentreSize;

            _speech = new SpeechQueue(config.bubbleCharsPerSecond, config.bubbleMinSeconds, config.bubbleSeconds);
        }

        _canvasRect = OverlayProjection.CanvasRectOf(this);
        if (catcher != null)
            catcher.SetActive(false);
        if (bubble != null)
        {
            bubble.PlacedByOwner = true; // the bubble belongs to the traveller and keeps clear of the wheel: PlaceBubble
            _tail = bubble.Panel != null ? bubble.Panel.Find("Tail") as RectTransform : null;
            if (_tail != null)
            {
                _tailTuck = Mathf.Max(0f, _tail.anchoredPosition.y);
                _tailLength = Mathf.Max(0f, _tail.rect.height - _tailTuck);
            }
        }
        if (bubbleButton != null)
        {
            bubbleButton.interactable = false;
            _bubbleTint = new ImageHighlight(bubbleButton.image);
        }
    }

    /// <summary>Destroys the placeholder icons this wheel drew.</summary>
    private void OnDestroy()
    {
        foreach (UnityEngine.Object o in _generated)
            if (o != null)
                Destroy(o);
        _generated.Clear();
        _icons.Clear();
    }

    /// <summary>Paces the speech bubble and sets the traveller's pose for the beat; only while open, follows the traveller; places the bubble while it shows (after the ring, which it keeps clear of).</summary>
    private void LateUpdate()
    {
        if (_speech != null)
        {
            _speech.Tick(Time.deltaTime);
            ShowSpeech();
        }
        if (traveller != null)
            traveller.SetPose(TravellerPose.ForBeat(_speech != null && _speech.Showing ? CurrentLine : null, IsOpen));

        if (IsOpen)
            Place();
        PlaceBubble();
    }

    /// <summary>
    /// Places the speech bubble by the traveller (BubbleLayout, Saleh
    /// 2026-10-07: the bubble belongs to the traveller and never covers the
    /// wheel): beside and above the head, moved the least it must to clear
    /// the face and the open wheel's pills (their rects this frame, canvas
    /// space) and stay on the screen under the HUD's band; its tail turned to
    /// point at the mouth. Only while the bubble shows; no allocation.
    /// </summary>
    private void PlaceBubble()
    {
        if (bubble == null || !_bubbleUp || traveller == null || bubble.Panel == null || !bubble.Panel.gameObject.activeSelf)
            return;
        RectTransform canvas = bubble.CanvasRect;
        Camera cam = bubble.Camera != null ? bubble.Camera : _camera;
        if (!OverlayProjection.TryToCanvas(canvas, cam, traveller.HeadTop, true, out Vector2 head)
            || !OverlayProjection.TryToCanvas(canvas, cam, traveller.Mouth, true, out Vector2 mouth))
            return;

        _pills.Clear();
        if (IsOpen && layout != null)
        {
            Transform pills = layout.transform;
            for (int i = 0; i < pills.childCount; i++)
                AddObstacle(pills.GetChild(i) as RectTransform, canvas);
            AddObstacle(centreSlot, canvas);
        }

        Rect bounds = canvas.rect;
        var screen = new FaceRect(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax - bubble.TopInset);
        Vector2 size = bubble.Panel.rect.size;
        BubblePlacement p = BubbleLayout.Place(head.x, head.y, mouth.x, mouth.y, size.x, size.y, _tailLength, screen, _pills, BubbleGap);
        bubble.Panel.anchoredPosition = new Vector2(p.Box.CentreX, p.Box.CentreY);
        if (_tail == null)
            return;
        // The tail leaves the edge facing the mouth (its pivot, its top, at that edge, tucked under the box's ink line), turned toward the mouth.
        _tail.anchorMin = _tail.anchorMax = new Vector2((p.TailBaseX - p.Box.XMin) / Mathf.Max(1f, p.Box.Width), (p.TailBaseY - p.Box.YMin) / Mathf.Max(1f, p.Box.Height));
        var toMouth = new Vector2(p.TailTipX - p.TailBaseX, p.TailTipY - p.TailBaseY);
        _tail.anchoredPosition = -toMouth.normalized * _tailTuck;
        _tail.localRotation = Quaternion.Euler(0f, 0f, p.TailDegrees);
    }

    /// <summary>The least gap between the bubble and a pill (canvas reference px).</summary>
    private const float BubbleGap = 8f;

    /// <summary>Adds an active pill's rect in canvas space to the bubble's obstacles.</summary>
    private void AddObstacle(RectTransform pill, RectTransform canvas)
    {
        if (pill == null || !pill.gameObject.activeInHierarchy)
            return;
        pill.GetWorldCorners(_corners);
        Vector3 a = canvas.InverseTransformPoint(_corners[0]), b = canvas.InverseTransformPoint(_corners[2]);
        _pills.Add(new FaceRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)));
    }

    /// <summary>The office camera the ring and the bubble are placed through (the office binder's, from the art office).</summary>
    public void SetCamera(Camera office) => _camera = office;

    /// <summary>The current traveller's translation (InvestigationUIController sets it before they speak).</summary>
    public void SetTranslation(CaseTranslation translation) => _translation = translation ?? CaseTranslation.None;

    /// <summary>Opens the wheel over the traveller, unless it may not open or is open (the traveller hit zone's and the desk intercom's persistent call); a line flipping in the bubble shows its English at once.</summary>
    public void Open()
    {
        if (!_canOpen || IsOpen || catcher == null)
            return;

        _flip.Complete();
        if (_speech != null)
            _speech.EndReveal();
        catcher.SetActive(true);
        Place();
        PopPills();
        OpenChanged?.Invoke();
    }

    /// <summary>The game feel's opening: the ring's pills pop out one after another (MotionKnobs.staggerSeconds apart, UiAppear) with the wheel_open cue; the ring lays them out, so they grow in place.</summary>
    private void PopPills()
    {
        Sounds.Play(SoundCues.WheelOpen);
        Transform pills = layout != null ? layout.transform : ring;
        if (pills == null)
            return;
        float stagger = UiMotion.Knobs.staggerSeconds;
        int shown = 0;
        for (int i = 0; i < pills.childCount; i++)
        {
            GameObject pill = pills.GetChild(i).gameObject;
            if (pill.activeSelf)
                UiAppear.Of(pill, AppearStyle.Pop).Open(stagger * shown++);
        }
    }

    /// <summary>Closes the ring (the speech bubble plays on).</summary>
    public void Close()
    {
        if (!IsOpen)
            return;

        catcher.SetActive(false);
        OpenChanged?.Invoke();
    }

    /// <summary>
    /// Whether the wheel may be open (BoothRules.WheelAllowed). False (the view
    /// left the booth or the traveller left) closes it and silences the
    /// traveller: the bubble hides, the lines not yet said are dropped (the PC
    /// transcript keeps them), and a premade shows at once the last expression
    /// among them; the lines said this case and a bubble pick are forgotten.
    /// </summary>
    public void SetCanOpen(bool can)
    {
        _canOpen = can;
        if (can)
            return;

        Close();
        if (!_reacting)
            Silence();
    }

    /// <summary>True while the traveller says their reaction after the stamp (the wheel may not open, yet the bubble speaks).</summary>
    private bool _reacting;

    /// <summary>
    /// The traveller's reaction to the stamp (the personalities spec's R1, R4):
    /// <paramref name="lines"/> said in the bubble in turn although the wheel
    /// may not open (the traveller has been decided). Returns how long the
    /// traveller stays: until the last line is fully shown, plus the desk's
    /// reactionSeconds; 0 (the traveller leaves at once, nothing said) when
    /// reactionSeconds is 0 or nothing is said. <see cref="EndReaction"/> silences it.
    /// </summary>
    public float React(IReadOnlyList<DialogLine> lines)
    {
        float linger = config != null ? config.reactionSeconds : 0f;
        if (_speech == null || lines == null || lines.Count == 0 || linger <= 0f)
            return 0f;

        Silence();
        _reacting = true;
        Speak(lines);
        return _speech.SecondsUntilShown + linger;
    }

    /// <summary>The traveller has left (or the next is called): the reaction's bubble hides and its lines are dropped.</summary>
    public void EndReaction()
    {
        if (!_reacting)
            return;
        _reacting = false;
        Silence();
    }

    /// <summary>Silences the traveller: the bubble hides, the lines not yet said are dropped (the PC transcript keeps them), a premade shows at once the last expression among them; the lines said this case and a bubble pick are forgotten.</summary>
    private void Silence()
    {
        if (_speech != null)
        {
            string pending = _speech.Clear();
            _speech.Hold(false);
            if (pending != null && traveller != null)
                traveller.SetExpression(pending);
        }

        if (bubble != null)
            bubble.Hide();
        _flip.Release();
        _bubbleUp = false;
        _said.Clear();
        _pickedTag = -1;
        RefreshBubbleInput();
    }

    /// <summary>The pointer is on the bubble (SpeechBubbleInput): its line holds while hovered, and its hold runs from the release.</summary>
    public void SetBubbleHovered(bool hovered)
    {
        if (_speech != null)
            _speech.Hold(hovered);
    }

    /// <summary>The bubble was clicked (SpeechBubbleInput): an answer showing is raised as LineClicked; any other line does nothing.</summary>
    public void ClickLine()
    {
        DialogLine line = CurrentLine;
        if (line != null && line.IsAnswer)
            LineClicked?.Invoke(line);
    }

    /// <summary>Marks the line with <paramref name="tag"/> picked (lit in <paramref name="colour"/> while it shows) or not.</summary>
    private void SetPicked(int tag, bool picked, Color colour)
    {
        if (picked)
        {
            _pickedTag = tag;
            _pickColour = colour;
        }
        else if (_pickedTag == tag)
        {
            _pickedTag = -1;
        }
        RefreshBubbleInput();
    }

    /// <summary>The bubble's button takes clicks only while an answer shows; the bubble is lit while the picked line shows.</summary>
    private void RefreshBubbleInput()
    {
        bool showing = _speech != null && _speech.Showing;
        if (bubbleButton != null)
        {
            DialogLine line = showing ? CurrentLine : null;
            bubbleButton.interactable = line != null && line.IsAnswer;
        }
        _bubbleTint?.Show(showing && _pickedTag >= 0 && _speech.Tag == _pickedTag, _pickColour);
    }

    /// <summary>
    /// The traveller says <paramref name="lines"/> in the speech bubble, after
    /// whatever they are saying; a line that flips into English counts as
    /// fully shown once its flip ends; nothing while the wheel may not open
    /// (the transcript holds every line anyway).
    /// </summary>
    public void Say(IReadOnlyList<DialogLine> lines)
    {
        if (!_canOpen || _speech == null || lines == null)
            return;

        Speak(lines);
    }

    /// <summary>Queues <paramref name="lines"/> in the bubble (each flipping line counting as shown once its flip ends) and draws.</summary>
    private void Speak(IReadOnlyList<DialogLine> lines)
    {
        SpeechTranslation speech = _translation.Speech;
        foreach (DialogLine line in lines)
        {
            if (line == null)
                continue;
            _said.Add(line);
            Reveal flip = _translation.Bubble(line, 0f);
            _speech.Say(line.Text, line.Expression,
                flip.Kind == RevealKind.Flipping ? DisplayText.Remaining(line.Text, flip, speech.Timing, speech.ReducedMotion) : 0f,
                _said.Count - 1);
        }

        ShowSpeech();
    }

    /// <summary>The kind's icon: the UI kit's pictogram tile (UiKitNames.WheelTile), else final art from Resources/WheelIcons by DialogChoiceKinds.IconName, else a generated placeholder (kept in memory, destroyed with the wheel); null when none exists.</summary>
    public Sprite IconFor(DialogChoiceKind kind)
    {
        if (_icons.TryGetValue(kind, out Sprite cached) && cached != null)
            return cached;

        string name = DialogChoiceKinds.IconName(kind);
        Sprite sprite = kit != null ? kit.Get(UiKitNames.WheelTile(kind), KitState.Rest) : null;
        if (sprite == null)
            sprite = Resources.Load<Sprite>($"{IconFolder}/{name}");
        byte[] rgba = sprite == null ? WheelIconPlaceholder.Render(name) : null;
        if (rgba != null)
        {
            var texture = new Texture2D(WheelIconPlaceholder.Size, WheelIconPlaceholder.Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.LoadRawTextureData(rgba);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, WheelIconPlaceholder.Size, WheelIconPlaceholder.Size), new Vector2(0.5f, 0.5f));
            sprite.name = name;
            _generated.Add(texture);
            _generated.Add(sprite);
        }

        _icons[kind] = sprite;
        return sprite;
    }

    /// <summary>A left-click on the catcher (outside the ring's buttons) closes the wheel (a right-click backs out through OfficeControls, once).</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            Close();
    }

    /// <summary>
    /// Draws the speech queue: when a line has started since the last draw, a
    /// premade shows the latest expression said (even if that line already
    /// ended within one long frame) and the bubble shows the current line in
    /// its translation (DisplayText: plain, untranslated, or flipping on the
    /// line's clock); the line types out; the bubble hides once nothing is
    /// being said.
    /// </summary>
    private void ShowSpeech()
    {
        bool started = _speech.LineNumber != _drawnLine;
        _drawnLine = _speech.LineNumber;
        if (started && traveller != null)
            traveller.SetExpression(_speech.Expression ?? LookKeys.NeutralExpression);

        if (bubble == null)
            return;

        if (!_speech.Showing)
        {
            if (_bubbleUp)
            {
                bubble.Hide();
                _flip.Release();
                RefreshBubbleInput();
            }
            _bubbleUp = false;
            return;
        }

        if (started || !_bubbleUp)
        {
            bubble.Show(_speech.Text, traveller != null ? traveller.Anchor : null, Vector2.zero, float.PositiveInfinity); // placed by PlaceBubble
            _flip.Show(bubble.Label, _speech.Text, _translation.Bubble(CurrentLine, _speech.LineSeconds), _translation);
            _bubbleUp = true;
            RefreshBubbleInput();
        }
        else
        {
            _flip.Tick(_speech.LineSeconds);
        }

        _flip.Type(_speech.VisibleCharacters);
    }

    /// <summary>Centres the ring on the traveller's anchor, kept on the screen and below the case HUD's strips when the anchor is above it (the desk view); with no traveller or anchor it stays centred on the screen.</summary>
    private void Place()
    {
        if (ring != null && traveller != null && traveller.Anchor != null)
            OverlayProjection.TryPlace(ring, _canvasRect, _camera, traveller.Anchor.position, Vector2.zero, true, ringTopInset);
    }
}
