using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The traveller wheel: the interview's choices on an elliptical ring around
/// the traveller (the ring's InteractionPanelController lays them out with
/// RadialLayoutGroup; "&lt; Back" sits in the centre slot), opened by clicking
/// the traveller or the desk intercom while BoothCoordinator allows it, each
/// choice with its kind's icon; and the traveller's speech: their lines in a
/// speech bubble beside them, one after another, typed out and paced
/// (SpeechQueue), each changing a premade's expression as it starts; from
/// translation's first day a line is in the claimed place's tongue, and with
/// the region's Speech translator its letters flip into English behind the
/// typing (its hold starts once the flip ends; opening the wheel finishes the
/// flip at once). The bubble's answer lines are evidence (piece 10): while
/// an answer shows, the bubble's button takes a click (LineClicked, the
/// DialogLine said; the pick lights the bubble while that line shows), and
/// hovering the bubble holds its line (SpeechQueue.Hold). This host
/// is always active (so it wakes at load and paces the bubble while the ring is
/// closed); its Catcher child, a full-screen click-to-close area holding the
/// ring, is shown only while the wheel is open. Escape (not the one that opened
/// it in the same frame) or a click outside the ring closes it.
/// </summary>
public sealed class TravellerWheel : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Where final icon art is loaded from, by DialogChoiceKinds.IconName (Assets/Art/UI/Resources/WheelIcons).</summary>
    private const string IconFolder = "WheelIcons";

    /// <summary>The full-screen transparent click catcher, active only while open; the ring sits under it.</summary>
    [SerializeField] private GameObject catcher;

    /// <summary>The ring's root (anchors and pivot (0.5, 0.5)), placed over the traveller.</summary>
    [SerializeField] private RectTransform ring;

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

    /// <summary>The frame the wheel opened (the Escape of that frame does not close it).</summary>
    private int _openedFrame;

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

    /// <summary>Paces the speech bubble; only while open, follows the traveller, and Escape closes.</summary>
    private void LateUpdate()
    {
        if (_speech != null)
        {
            _speech.Tick(Time.deltaTime);
            ShowSpeech();
        }

        if (!IsOpen)
            return;

        Place();
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && _openedFrame < Time.frameCount)
            Close();
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
        _openedFrame = Time.frameCount;
        catcher.SetActive(true);
        Place();
        OpenChanged?.Invoke();
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

        Reveal flip = _translation.Bubble(0f);
        foreach (DialogLine line in lines)
        {
            if (line == null)
                continue;
            _said.Add(line);
            _speech.Say(line.Text, line.Expression,
                flip.Kind == RevealKind.Flipping ? DisplayText.Remaining(line.Text, flip, _translation.Timing, _translation.ReducedMotion) : 0f,
                _said.Count - 1);
        }

        ShowSpeech();
    }

    /// <summary>The kind's icon: final art from Resources/WheelIcons by DialogChoiceKinds.IconName, else a generated placeholder (kept in memory, destroyed with the wheel); null when neither exists.</summary>
    public Sprite IconFor(DialogChoiceKind kind)
    {
        if (_icons.TryGetValue(kind, out Sprite cached) && cached != null)
            return cached;

        string name = DialogChoiceKinds.IconName(kind);
        Sprite sprite = Resources.Load<Sprite>($"{IconFolder}/{name}");
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

    /// <summary>A click on the catcher (outside the ring's buttons) closes the wheel.</summary>
    public void OnPointerClick(PointerEventData eventData) => Close();

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
        if (started && traveller != null && _speech.Expression != null)
            traveller.SetExpression(_speech.Expression);

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
            bubble.Show(_speech.Text, traveller != null ? traveller.Anchor : null, config.bubbleOffset, float.PositiveInfinity);
            _flip.Show(bubble.Label, _speech.Text, _translation.Bubble(_speech.LineSeconds), _translation);
            _bubbleUp = true;
            RefreshBubbleInput();
        }
        else
        {
            _flip.Tick(_speech.LineSeconds);
        }

        bubble.Reveal(_speech.VisibleCharacters);
    }

    /// <summary>Centres the ring on the traveller's anchor (with no traveller or anchor it stays centred on the screen).</summary>
    private void Place()
    {
        if (ring != null && traveller != null && traveller.Anchor != null)
            OverlayProjection.TryPlace(ring, _canvasRect, _camera, traveller.Anchor.position, Vector2.zero);
    }
}
