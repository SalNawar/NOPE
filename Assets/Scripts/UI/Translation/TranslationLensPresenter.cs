using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The Translation Lens on screen (Saleh 2026-10-06: "translates any word the
/// player hovers on. When a word is being translated the letters flip back
/// to English"). Lives on the persistent culture host (CultureThemeBootstrap)
/// and reads every text of the loaded scenes, the UI's and the desk's printed
/// words (a paper's, the rulebook folder's: world-space TextMeshPro under the
/// office camera's PhysicsRaycaster; Saleh's 1008a playtest, "hovering didn't
/// translate"): wherever a culture label
/// (UiStrings.Phrases) is shown, hovering it with the lens flips what the
/// hover covers (TranslationLens.Covered) into English letter by letter
/// (LensFlip) and back when the pointer leaves: at level 1 the word under the
/// pointer (its glossary English, LensWords), at level 2 the whole label as
/// its English sentence, at level 3 every label of the object (its window, its
/// top panel such as the morning paper, or the desk object: the paper or the
/// folder, the DeskDraggable it lies on), one after another. A desk word is
/// read only when no UI is over it and it lies on the desk object the pointer
/// is over (the top paper of a stack). Reduced
/// motion swaps at once. While a label is translated its auto-size is held and
/// its size eases to fit the English (TranslationLensSettings.resizeSeconds), so nothing
/// around it moves. Nothing runs while the pointer rests and nothing flips:
/// no allocation per idle frame. The rules are TranslationLens's; this class
/// only finds texts under the pointer and writes them.
/// </summary>
public sealed class TranslationLensPresenter : MonoBehaviour
{
    /// <summary>Seconds between two rescans of the scenes' texts while the pointer moves (runtime-built UI).</summary>
    private const float RescanSeconds = 5f;

    /// <summary>One flip, a word's or a whole label's: towards English or back, and when it started (NaN: native and at rest).</summary>
    private struct Flip
    {
        public bool ToEnglish;
        public float Started;

        public bool Resting => float.IsNaN(Started);

        public static Flip Rest => new Flip { Started = float.NaN };
    }

    /// <summary>A culture label inside a text: its words' flips (level 1) and its whole flip (levels 2 and 3).</summary>
    private sealed class Occurrence
    {
        public readonly LensPhrase Phrase;
        public readonly int Start;
        public readonly Flip[] Words;
        public readonly string[] Shown;
        public Flip Whole = Flip.Rest;
        public string WholeShown;
        public int ShownStart;
        public int ShownLength;
        public readonly List<int> SegmentOf = new List<int>();

        public Occurrence(LensPhrase phrase, int start)
        {
            Phrase = phrase;
            Start = start;
            Words = new Flip[phrase.WordCount];
            for (int i = 0; i < Words.Length; i++)
                Words[i] = Flip.Rest;
            Shown = new string[phrase.WordCount];
            ShownStart = start;
            ShownLength = phrase.Visual.Length;
            phrase.Compose(null, SegmentOf);
        }
    }

    /// <summary>A text the lens knows: the text as the game set it, its labels, and (while translated) its held size.</summary>
    private sealed class TextState
    {
        public TMP_Text Text;
        public string Source;
        public string Written;
        public readonly List<Occurrence> Occurrences = new List<Occurrence>();
        public bool Held;
        public bool AutoSize;
        public float FontSize;
        public float SizeFrom;
        public float SizeTo;
        public float SizeStarted;
        public bool Settled;
    }

    private readonly List<TMP_Text> _candidates = new List<TMP_Text>();
    private readonly Dictionary<TMP_Text, TextState> _states = new Dictionary<TMP_Text, TextState>();
    private readonly List<TextState> _active = new List<TextState>();
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();
    private readonly List<TMP_Text> _objectTexts = new List<TMP_Text>();
    private readonly List<(TextState state, Occurrence occurrence)> _objectPhrases = new List<(TextState, Occurrence)>();
    private readonly List<int> _wordsPerPhrase = new List<int>();
    private readonly List<LensWordRef> _covered = new List<LensWordRef>();
    private readonly HashSet<(Occurrence, int)> _wantedWords = new HashSet<(Occurrence, int)>();
    private readonly HashSet<Occurrence> _wantedWhole = new HashSet<Occurrence>();
    private readonly StringBuilder _builder = new StringBuilder(256);
    private readonly List<LensPhrase> _byLength = new List<LensPhrase>();

    private PointerEventData _pointer;
    private EventSystem _pointerSystem;

    /// <summary>The desk object under the pointer (the office camera's first physics hit, ObjectRoot), or null; and the camera that found it.</summary>
    private Transform _deskHit;
    private Camera _deskCamera;
    private UiStrings _strings;
    private int _phraseCount;
    private Vector2 _lastPosition = new Vector2(float.NaN, float.NaN);
    private bool _stale = true;
    private float _lastScan = float.NegativeInfinity;
    private LensReach _reach;

    /// <summary>The one presenter (on the culture host), or null.</summary>
    public static TranslationLensPresenter Instance { get; private set; }

    /// <summary>How far a hover reads right now (TranslationLens.Reach for the run's day and owned upgrades); None with no run.</summary>
    public static LensReach CurrentReach
    {
        get
        {
            if (!RunManager.HasInstance || RunManager.Instance.World == null || RunManager.Instance.Library == null)
                return LensReach.None;
            WorldState world = RunManager.Instance.World;
            ContentLibrarySO library = RunManager.Instance.Library;
            return TranslationLens.Reach(world.day, library.Introductions, library.Translation.lens.rules.levelIds, world.HasUpgrade);
        }
    }

    /// <summary>
    /// A screen point the lens reads instead of the pointer's (the feature
    /// probe drives the lens with it, the editor running unfocused); null
    /// returns it to the real pointer.
    /// </summary>
    public Vector2? PointerOverride { get; set; }

    private void Awake() => Instance = this;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>A new scene: its texts are found again on the next pointer move.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _stale = true;

    private void Update()
    {
        CultureThemeService service = CultureThemeService.Instance;
        UiStrings strings = service != null ? service.Strings : null;
        if (!ReferenceEquals(strings, _strings))
            Forget(strings);

        float now = Time.unscaledTime;
        if (_active.Count > 0)
            Animate(now);

        Vector2 position;
        if (PointerOverride.HasValue)
            position = PointerOverride.Value;
        else if (Pointer.current != null)
            position = Pointer.current.position.ReadValue();
        else
            return;
        if (position == _lastPosition)
            return;
        _lastPosition = position;

        _reach = CurrentReach;
        if (_reach == LensReach.None || strings == null || strings.Phrases.Count == 0)
        {
            Release(now);
            return;
        }

        if (_stale || strings.Phrases.Count != _phraseCount || now - _lastScan > RescanSeconds)
            Rescan(strings, now);
        Hover(position, now);
    }

    /// <summary>
    /// The theme's strings changed (a new scene's culture): every state is
    /// dropped and its size given back; a text still showing the lens's frame
    /// gets the game's text back (a themed label already carries the new one).
    /// </summary>
    private void Forget(UiStrings strings)
    {
        foreach (TextState state in _active)
        {
            if (state.Text == null)
                continue;
            if (state.Written != null && ReferenceEquals(state.Text.text, state.Written))
                state.Text.text = state.Source;
            RestoreSize(state);
        }
        _active.Clear();
        _states.Clear();
        _strings = strings;
        _stale = true;
    }

    /// <summary>Finds the scenes' UI texts again and orders the known labels longest first (a long label wins over a short one inside it).</summary>
    private void Rescan(UiStrings strings, float now)
    {
        _candidates.Clear();
        _candidates.AddRange(FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        _byLength.Clear();
        _byLength.AddRange(strings.Phrases);
        _byLength.Sort((a, b) => b.Visual.Length.CompareTo(a.Visual.Length));
        _phraseCount = strings.Phrases.Count;
        foreach (TextState state in _states.Values)
            if (!_active.Contains(state))
                state.Source = null; // labels may be new: they are found again on use
        _stale = false;
        _lastScan = now;
    }

    /// <summary>What the pointer is over and what the lens covers there.</summary>
    private void Hover(Vector2 position, float now)
    {
        Transform top = TopHit(position);
        Transform topRoot = top != null ? ObjectRoot(top) : null;
        Transform deskRoot = topRoot == null && _deskHit != null ? ObjectRoot(_deskHit) : null;

        TextState hoveredState = null;
        Occurrence hovered = null;
        int hoveredWord = -1;
        foreach (TMP_Text text in _candidates)
        {
            if (text == null || !text.isActiveAndEnabled)
                continue;
            Camera cam;
            if (text is TextMeshPro desk)
            {
                // A desk word: never under the UI, only on the desk object the pointer is over, and only when drawn.
                if (deskRoot == null || !desk.renderer.enabled || ObjectRoot(text.transform) != deskRoot)
                    continue;
                cam = _deskCamera;
            }
            else
            {
                if (topRoot != null && ObjectRoot(text.transform) != topRoot)
                    continue; // under another window
                cam = CameraOf(text);
            }
            // A printed paper word may overflow its box (it shrinks to fit only so far): a desk word is found by its letters alone.
            if (!(text is TextMeshPro) && !RectTransformUtility.RectangleContainsScreenPoint(text.rectTransform, position, cam))
                continue;
            TextState state = StateOf(text);
            if (state.Occurrences.Count == 0)
                continue;
            int index = text is TextMeshPro ? DeskCharacterAt(text, position, cam) : TMP_TextUtilities.FindIntersectingCharacter(text, position, cam, false);
            if (index < 0 || index >= text.textInfo.characterCount)
                continue;
            int source = text.textInfo.characterInfo[index].index;
            foreach (Occurrence occurrence in state.Occurrences)
            {
                if (source < occurrence.ShownStart || source >= occurrence.ShownStart + occurrence.ShownLength)
                    continue;
                hoveredState = state;
                hovered = occurrence;
                int at = source - occurrence.ShownStart;
                int segment = at < occurrence.SegmentOf.Count ? occurrence.SegmentOf[at] : -1;
                hoveredWord = segment >= 0 ? occurrence.Phrase.WordRank(segment) : -1;
                break;
            }
            if (hovered != null)
                break;
        }

        _objectPhrases.Clear();
        _wordsPerPhrase.Clear();
        int phrase = -1;
        bool overObject = false;
        if (_reach == LensReach.Object)
        {
            Transform root = topRoot != null ? topRoot : deskRoot != null ? deskRoot : hoveredState != null ? ObjectRoot(hoveredState.Text.transform) : null;
            overObject = root != null;
            if (root != null)
            {
                root.GetComponentsInChildren(false, _objectTexts);
                foreach (TMP_Text text in _objectTexts)
                {
                    if (ObjectRoot(text.transform) != root)
                        continue; // a window open over a panel is an object of its own
                    TextState state = StateOf(text);
                    foreach (Occurrence occurrence in state.Occurrences)
                    {
                        if (occurrence == hovered)
                            phrase = _objectPhrases.Count;
                        _objectPhrases.Add((state, occurrence));
                        _wordsPerPhrase.Add(occurrence.Words.Length);
                    }
                }
            }
        }
        else if (hovered != null)
        {
            phrase = 0;
            _objectPhrases.Add((hoveredState, hovered));
            _wordsPerPhrase.Add(hovered.Words.Length);
        }

        TranslationLens.Covered(_reach, overObject, phrase, hoveredWord, _wordsPerPhrase, _covered);
        Apply(now);
    }

    /// <summary>
    /// Starts what the hover covers towards English (level 1 its word; levels
    /// 2 and 3 each covered label whole, a cascade step apart) and everything
    /// else that is translated back.
    /// </summary>
    private void Apply(float now)
    {
        TranslationLensSettings knobs = Knobs();
        bool reduced = MotionPreference.Reduced;
        bool whole = _reach >= LensReach.Sentence;
        _wantedWords.Clear();
        _wantedWhole.Clear();

        int lastPhrase = -1, phrases = -1;
        foreach (LensWordRef w in _covered)
        {
            (TextState state, Occurrence occurrence) = _objectPhrases[w.Phrase];
            if (!whole)
            {
                _wantedWords.Add((occurrence, w.Word));
                string english = occurrence.Phrase.EnglishWord(w.Word);
                if (english != null)
                    Turn(ref occurrence.Words[w.Word], true, occurrence.Phrase.NativeWord(w.Word), english, 0f, knobs, reduced, now, state);
                continue;
            }
            if (w.Phrase == lastPhrase)
                continue;
            lastPhrase = w.Phrase;
            phrases++;
            _wantedWhole.Add(occurrence);
            for (int i = 0; i < occurrence.Words.Length; i++)
                occurrence.Words[i] = Flip.Rest; // the whole label takes over from any word
            Turn(ref occurrence.Whole, true, occurrence.Phrase.Logical, occurrence.Phrase.English, phrases * Mathf.Max(0f, knobs.cascadeSeconds), knobs, reduced, now, state);
        }

        foreach (TextState state in _active)
            foreach (Occurrence occurrence in state.Occurrences)
            {
                if (occurrence.Whole.ToEnglish && !_wantedWhole.Contains(occurrence))
                    Turn(ref occurrence.Whole, false, occurrence.Phrase.Logical, occurrence.Phrase.English, 0f, knobs, reduced, now, state);
                for (int i = 0; i < occurrence.Words.Length; i++)
                    if (occurrence.Words[i].ToEnglish && !_wantedWords.Contains((occurrence, i)))
                        Turn(ref occurrence.Words[i], false, occurrence.Phrase.NativeWord(i), occurrence.Phrase.EnglishWord(i), 0f, knobs, reduced, now, state);
            }
    }

    /// <summary>
    /// Turns a flip towards English (<paramref name="toEnglish"/>) or back: from
    /// rest it starts after <paramref name="delay"/>; one already turning the
    /// other way continues from about where it is (as far into the new flip as
    /// the old one had left to run). Reduced motion lands it at once.
    /// </summary>
    private void Turn(ref Flip flip, bool toEnglish, string native, string english, float delay, TranslationLensSettings knobs, bool reduced, float now, TextState state)
    {
        if (flip.ToEnglish == toEnglish && !(toEnglish && flip.Resting))
            return;
        if (!toEnglish && flip.Resting)
            return;

        float started;
        if (reduced)
        {
            started = float.NegativeInfinity;
        }
        else if (flip.Resting)
        {
            started = now + delay;
        }
        else
        {
            string from = flip.ToEnglish ? native : english, to = flip.ToEnglish ? english : native;
            float old = LensFlip.Duration(from, to, knobs.flip);
            float elapsed = float.IsNegativeInfinity(flip.Started) ? old : Mathf.Clamp(now - flip.Started, 0f, old);
            started = now - Mathf.Clamp(old - elapsed, 0f, LensFlip.Duration(to, from, knobs.flip));
        }
        flip.ToEnglish = toEnglish;
        flip.Started = started;
        if (toEnglish)
            Activate(state, now);
        state.Settled = false;
    }

    /// <summary>The pointer left everything (or the lens is gone): everything translated flips back.</summary>
    private void Release(float now)
    {
        if (_active.Count == 0)
            return;
        _covered.Clear();
        _objectPhrases.Clear();
        Apply(now);
    }

    /// <summary>Marks a text active: its size is held at what it shows and eased to fit its English.</summary>
    private void Activate(TextState state, float now)
    {
        if (_active.Contains(state))
            return;
        _active.Add(state);
        TMP_Text text = state.Text;
        state.Held = true;
        state.AutoSize = text.enableAutoSizing;
        state.FontSize = text.fontSize; // with auto-size on, the size it settled on
        text.enableAutoSizing = false;
        state.SizeFrom = state.FontSize;
        state.SizeTo = FitSize(state, state.FontSize);
        state.SizeStarted = now;
    }

    /// <summary>The size at which the text, its labels all in English, fits its width (never larger than now, never below the auto-size floor).</summary>
    private float FitSize(TextState state, float size)
    {
        TMP_Text text = state.Text;
        if (text.textWrappingMode != TextWrappingModes.NoWrap)
            return size;
        string english = Compose(state, true);
        float width = text.rectTransform.rect.width;
        float preferred = text.GetPreferredValues(english, float.PositiveInfinity, float.PositiveInfinity).x;
        if (preferred <= width || preferred <= 0f)
            return size;
        float floor = state.AutoSize ? text.fontSizeMin : size * 0.5f;
        return Mathf.Max(floor, size * width / preferred);
    }

    /// <summary>Writes every active text's current frame; a text whose labels are all back and at rest gets its own text and size back.</summary>
    private void Animate(float now)
    {
        TranslationLensSettings knobs = Knobs();
        for (int a = _active.Count - 1; a >= 0; a--)
        {
            TextState state = _active[a];
            TMP_Text text = state.Text;
            if (text == null)
            {
                _active.RemoveAt(a);
                continue;
            }
            if (state.Written != null && !ReferenceEquals(text.text, state.Written) && text.text != state.Written)
            {
                // The game set the text meanwhile: it wins; the lens finds its labels again.
                RestoreSize(state);
                state.Source = null;
                state.Written = null;
                _active.RemoveAt(a);
                continue;
            }
            if (state.Settled)
                continue;

            bool translated = false, moving = false;
            foreach (Occurrence occurrence in state.Occurrences)
            {
                occurrence.WholeShown = Frame(ref occurrence.Whole, occurrence.Phrase.Logical, occurrence.Phrase.English, knobs, now, ref translated, ref moving);
                for (int i = 0; i < occurrence.Words.Length; i++)
                {
                    string native = occurrence.Phrase.NativeWord(i);
                    occurrence.Shown[i] = Frame(ref occurrence.Words[i], native, occurrence.Phrase.EnglishWord(i) ?? native, knobs, now, ref translated, ref moving);
                }
            }

            if (!translated)
            {
                Write(state, state.Source);
                RestoreSize(state);
                _active.RemoveAt(a);
                continue;
            }

            Write(state, Compose(state, false));
            bool easing = EaseSize(state, knobs, now);
            state.Settled = !moving && !easing; // nothing changes until the next hover: no work per frame
        }
    }

    /// <summary>One flip's text now (null when native and at rest; a reverse that lands comes to rest).</summary>
    private static string Frame(ref Flip flip, string native, string english, TranslationLensSettings knobs, float now, ref bool translated, ref bool moving)
    {
        if (flip.Resting)
            return null;
        float elapsed = now - flip.Started;
        string from = flip.ToEnglish ? native : english, to = flip.ToEnglish ? english : native;
        bool landed = elapsed >= LensFlip.Duration(from, to, knobs.flip);
        if (landed && !flip.ToEnglish)
        {
            flip = Flip.Rest;
            return null;
        }
        translated = true;
        moving |= !landed;
        return landed ? to : LensFlip.Frame(from, to, knobs.flip, elapsed);
    }

    /// <summary>The text with each label as it is shown now (or, with <paramref name="allEnglish"/>, every covered label as its English); updates each label's place in it.</summary>
    private string Compose(TextState state, bool allEnglish)
    {
        _builder.Clear();
        int cursor = 0;
        bool whole = _reach >= LensReach.Sentence;
        foreach (Occurrence occurrence in state.Occurrences)
        {
            _builder.Append(state.Source, cursor, occurrence.Start - cursor);
            string label;
            if (allEnglish)
            {
                if (whole)
                {
                    label = occurrence.Phrase.ComposeWhole(occurrence.Phrase.English, null);
                }
                else
                {
                    var english = new string[occurrence.Words.Length];
                    for (int i = 0; i < english.Length; i++)
                        english[i] = occurrence.Phrase.EnglishWord(i);
                    label = occurrence.Phrase.Compose(english, null);
                }
            }
            else
            {
                label = occurrence.WholeShown != null
                    ? occurrence.Phrase.ComposeWhole(occurrence.WholeShown, occurrence.SegmentOf)
                    : occurrence.Phrase.Compose(occurrence.Shown, occurrence.SegmentOf);
                occurrence.ShownStart = _builder.Length;
                occurrence.ShownLength = label.Length;
            }
            _builder.Append(label);
            cursor = occurrence.Start + occurrence.Phrase.Visual.Length;
        }
        _builder.Append(state.Source, cursor, state.Source.Length - cursor);
        return _builder.ToString();
    }

    /// <summary>Sets the text (remembering it, so a change by the game is noticed); the game's own text puts each label back at its place.</summary>
    private static void Write(TextState state, string value)
    {
        if (state.Text.text != value)
            state.Text.text = value;
        if (!ReferenceEquals(value, state.Source))
        {
            state.Written = state.Text.text;
            return;
        }

        state.Written = null;
        foreach (Occurrence occurrence in state.Occurrences)
        {
            occurrence.ShownStart = occurrence.Start;
            occurrence.ShownLength = occurrence.Phrase.Visual.Length;
            occurrence.Phrase.Compose(null, occurrence.SegmentOf);
        }
    }

    /// <summary>Eases the held size towards the English's fit (at once with reduced motion or no resize time); true while still on the way.</summary>
    private static bool EaseSize(TextState state, TranslationLensSettings knobs, float now)
    {
        float t = knobs.resizeSeconds <= 0f || MotionPreference.Reduced ? 1f : Mathf.Clamp01((now - state.SizeStarted) / knobs.resizeSeconds);
        float size = Mathf.Lerp(state.SizeFrom, state.SizeTo, t);
        if (!Mathf.Approximately(state.Text.fontSize, size))
            state.Text.fontSize = size;
        return t < 1f;
    }

    /// <summary>Gives a text its auto-size and size back.</summary>
    private static void RestoreSize(TextState state)
    {
        if (!state.Held || state.Text == null)
            return;
        state.Text.fontSize = state.FontSize;
        state.Text.enableAutoSizing = state.AutoSize;
        state.Held = false;
    }

    /// <summary>The lens's state of a text, its labels found again when the game changed it.</summary>
    private TextState StateOf(TMP_Text text)
    {
        if (!_states.TryGetValue(text, out TextState state))
        {
            state = new TextState { Text = text };
            _states.Add(text, state);
        }
        if (_active.Contains(state))
            return state;

        string current = text.text;
        if (state.Source != null && ReferenceEquals(current, state.Source))
            return state;
        state.Source = current ?? string.Empty;
        state.Written = null;
        state.Occurrences.Clear();
        FindLabels(state);
        return state;
    }

    /// <summary>Every known label inside the text at word boundaries, longest first, never overlapping, in text order.</summary>
    private void FindLabels(TextState state)
    {
        string source = state.Source;
        if (source.Length == 0)
            return;
        foreach (LensPhrase phrase in _byLength)
        {
            if (phrase.WordCount == 0 || phrase.Visual.Length == 0)
                continue;
            int from = 0;
            while (from < source.Length)
            {
                int at = source.IndexOf(phrase.Visual, from, System.StringComparison.Ordinal);
                if (at < 0)
                    break;
                from = at + phrase.Visual.Length;
                if (!Bounded(source, at, phrase.Visual.Length) || Overlaps(state, at, phrase.Visual.Length))
                    continue;
                state.Occurrences.Add(new Occurrence(phrase, at));
            }
        }
        state.Occurrences.Sort((a, b) => a.Start.CompareTo(b.Start));
    }

    /// <summary>True when the stretch is not part of a longer word (no letter right before or after it).</summary>
    private static bool Bounded(string text, int start, int length) =>
        (start == 0 || !char.IsLetter(text[start - 1])) && (start + length >= text.Length || !char.IsLetter(text[start + length]));

    /// <summary>True when the stretch overlaps a label already found.</summary>
    private static bool Overlaps(TextState state, int start, int length)
    {
        foreach (Occurrence o in state.Occurrences)
            if (start < o.Start + o.Phrase.Visual.Length && o.Start < start + length)
                return true;
        return false;
    }

    /// <summary>The topmost UI element under the point (null without an event system or over no UI); notes the first desk object hit (_deskHit, by a PhysicsRaycaster).</summary>
    private Transform TopHit(Vector2 position)
    {
        _deskHit = null;
        _deskCamera = null;
        EventSystem system = EventSystem.current;
        if (system == null)
            return null;
        if (_pointer == null || _pointerSystem != system)
        {
            _pointer = new PointerEventData(system);
            _pointerSystem = system;
        }
        _pointer.position = position;
        _hits.Clear();
        system.RaycastAll(_pointer, _hits);
        foreach (RaycastResult hit in _hits)
        {
            if (hit.gameObject == null)
                continue;
            if (hit.module is UnityEngine.UI.GraphicRaycaster)
                return hit.gameObject.transform;
            if (_deskHit == null && hit.module is PhysicsRaycaster && !(hit.module is Physics2DRaycaster))
            {
                _deskHit = hit.gameObject.transform;
                _deskCamera = hit.module.eventCamera;
            }
        }
        return null;
    }

    /// <summary>The object a UI element belongs to: its window (DesktopWindow), else its top panel (the child of its root canvas); a desk object's (no canvas): the DeskDraggable it is part of (a paper, the rulebook folder), else its top object.</summary>
    private static Transform ObjectRoot(Transform t)
    {
        Canvas canvas = t.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            DeskDraggable desk = t.GetComponentInParent<DeskDraggable>();
            return desk != null ? desk.transform : t.root;
        }
        Transform rootCanvas = canvas != null ? canvas.rootCanvas.transform : null;
        if (t == rootCanvas)
            return t;
        for (Transform walk = t; walk != null; walk = walk.parent)
        {
            if (walk.TryGetComponent(out DesktopWindow _))
                return walk;
            if (walk.parent == rootCanvas || walk.parent == null)
                return walk;
        }
        return t;
    }

    /// <summary>
    /// The character of a desk word under the screen point, or -1: each
    /// letter's box as the camera draws it (its four corners on the screen),
    /// so a word printed on a paper lying at any angle is found where it shows
    /// (TMP's own test found nothing on some of the papers' and the folder's words).
    /// </summary>
    private static int DeskCharacterAt(TMP_Text text, Vector2 position, Camera cam)
    {
        if (cam == null)
            return -1;
        Transform t = text.transform;
        TMP_TextInfo info = text.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            Vector2 bl = cam.WorldToScreenPoint(t.TransformPoint(c.bottomLeft));
            Vector2 tl = cam.WorldToScreenPoint(t.TransformPoint(new Vector3(c.bottomLeft.x, c.topRight.y, c.bottomLeft.z)));
            Vector2 tr = cam.WorldToScreenPoint(t.TransformPoint(c.topRight));
            Vector2 br = cam.WorldToScreenPoint(t.TransformPoint(new Vector3(c.topRight.x, c.bottomLeft.y, c.bottomLeft.z)));
            if (InQuad(position, bl, tl, tr, br))
                return i;
        }
        return -1;
    }

    /// <summary>True when <paramref name="p"/> lies inside the convex quad a-b-c-d (either winding; a degenerate quad holds nothing).</summary>
    private static bool InQuad(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float s1 = Cross(a, b, p), s2 = Cross(b, c, p), s3 = Cross(c, d, p), s4 = Cross(d, a, p);
        bool anyArea = Mathf.Abs(Cross(a, b, c)) > 1e-6f;
        return anyArea && ((s1 >= 0f && s2 >= 0f && s3 >= 0f && s4 >= 0f) || (s1 <= 0f && s2 <= 0f && s3 <= 0f && s4 <= 0f));
    }

    /// <summary>The z of (b - a) x (p - a).</summary>
    private static float Cross(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

    /// <summary>The camera a text's canvas is drawn by (null for an overlay).</summary>
    private static Camera CameraOf(TMP_Text text)
    {
        Canvas canvas = text.canvas;
        if (canvas == null)
            return null;
        Canvas root = canvas.rootCanvas;
        if (root.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return root.worldCamera != null ? root.worldCamera : Camera.main;
    }

    /// <summary>The library's lens knobs (defaults without a run).</summary>
    private static TranslationLensSettings Knobs() =>
        RunManager.HasInstance && RunManager.Instance.Library != null ? RunManager.Instance.Library.Translation.lens ?? new TranslationLensSettings() : new TranslationLensSettings();
}
