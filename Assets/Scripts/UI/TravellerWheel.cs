using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// The traveller wheel: the interview's choices on an elliptical ring around
/// the traveller (the ring's InteractionPanelController lays them out with
/// RadialLayoutGroup; "&lt; Back" sits in the centre slot), opened by clicking
/// the traveller or the desk intercom while BoothCoordinator allows it, each
/// choice with its kind's icon; and the traveller's speech: their lines in a
/// speech bubble beside them, one after another, typed out and paced
/// (SpeechQueue), each changing a premade's expression as it starts. This host
/// is always active (so it wakes at load and paces the bubble while the ring is
/// closed); its Catcher child, a full-screen click-to-close area holding the
/// ring, is shown only while the wheel is open. Escape or a click outside the
/// ring closes it.
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

    private bool _canOpen;
    private Camera _camera;
    private RectTransform _canvasRect;
    private SpeechQueue _speech;

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
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            Close();
    }

    /// <summary>The office camera the ring and the bubble are placed through (the office binder's, from the art office).</summary>
    public void SetCamera(Camera office) => _camera = office;

    /// <summary>Opens the wheel over the traveller, unless it may not open or is open (the traveller hit zone's and the desk intercom's persistent call).</summary>
    public void Open()
    {
        if (!_canOpen || IsOpen || catcher == null)
            return;

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
    /// among them.
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
            if (pending != null && traveller != null)
                traveller.SetExpression(pending);
        }

        if (bubble != null)
            bubble.Hide();
        _bubbleUp = false;
    }

    /// <summary>
    /// The traveller says <paramref name="lines"/> in the speech bubble, after
    /// whatever they are saying (each through DisplayText as spoken); nothing
    /// while the wheel may not open (the transcript holds every line anyway).
    /// </summary>
    public void Say(IReadOnlyList<DialogLine> lines)
    {
        if (!_canOpen || _speech == null || lines == null)
            return;

        foreach (DialogLine line in lines)
            if (line != null)
                _speech.Say(DisplayText.For(line.Text, TextMedium.Spoken), line.Expression);

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
    /// ended within one long frame) and the bubble shows the current line; the
    /// line types out; the bubble hides once nothing is being said.
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
                bubble.Hide();
            _bubbleUp = false;
            return;
        }

        if (started || !_bubbleUp)
        {
            bubble.Show(_speech.Text, traveller != null ? traveller.Anchor : null, config.bubbleOffset, float.PositiveInfinity);
            _bubbleUp = true;
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
