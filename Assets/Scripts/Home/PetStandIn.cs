using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The pet as the player sees it (the Home pet spec PS7): its picture for how
/// it looks in its coat when the art exists (ArtSlots.PetSprite: Home/pet_&lt;kind&gt;_&lt;coat&gt;_&lt;look&gt;,
/// else the kind's Home/pet_&lt;kind&gt;_&lt;look&gt;),
/// else a stand-in drawn in code from a few shapes (a body, a head, ears, eyes,
/// a nose and a tail; a dog's ears droop, a cat's stand; a sick pet under a
/// blanket), on the corner's backdrop (ArtSlots.PetCorner, else a plain
/// plate), dimmed while the electricity is off. The picture is a puppet
/// (PetPuppet on its look's rig, ArtSlots.PetRigs; Saleh 2026-10-17: a real
/// animation, not a squash): its mood's loop (PetAnimator.Pose: an idle pet
/// breathes calmly, sways its head, flicks an ear and blinks; a happy one
/// wags hard and hops; a sad one droops, barely wags and blinks slowly; a sick
/// one breathes heavily and shivers), the blink laid over it from its blink
/// picture (ArtSlots.PetBlink); petted it leans into the hand, eyes shut, with
/// a heart; fed it bobs its head down to the bowl; played with it hops. The
/// stand-in wags and hops the same way. The Home pet corner and the Title's
/// adoption panel each host one; it builds its children on demand and owns
/// them.
/// </summary>
public sealed class PetStandIn : MonoBehaviour
{
    /// <summary>The plate behind the pet when the corner has no backdrop art.</summary>
    [SerializeField] private Color plateColour = new Color(0.86f, 0.8f, 0.68f, 1f);

    /// <summary>The kind drawn.</summary>
    private PetKind _kind;

    /// <summary>Its coat's id (the art's name; the code-drawn stand-in has one coat).</summary>
    private string _coat;

    /// <summary>How it looks.</summary>
    private PetLook _look;

    /// <summary>Whether the lights are on (the electricity paid).</summary>
    private bool _lit = true;

    /// <summary>The drawn parts (rebuilt on Show).</summary>
    private readonly List<GameObject> _parts = new();

    /// <summary>The figure's root: fitted to the host's rect (the stand-in hops it).</summary>
    private RectTransform _figure;

    /// <summary>The picture as a puppet, when the art exists.</summary>
    private PetPuppet _puppet;

    /// <summary>The eyes shut over it (a blink), when its blink picture exists.</summary>
    private PetPuppet _blink;

    /// <summary>The stand-in's tail (wags), when drawn.</summary>
    private RectTransform _tail;

    /// <summary>The heart shown after a pat.</summary>
    private Image _heart;

    /// <summary>The rig of the picture shown (null: none, the stand-in).</summary>
    private PetRig _rig;

    /// <summary>Its look's loop (null: still).</summary>
    private PetMoodMotion _mood;

    /// <summary>The reaction under way (None: the loop alone).</summary>
    private PetReaction _reaction;

    /// <summary>The reaction's motion (null: none).</summary>
    private PetReactionMotion _reactionMotion;

    /// <summary>When the reaction began (unscaled seconds).</summary>
    private float _reactionStart;

    /// <summary>When it was shown (unscaled seconds): its loop's time starts there.</summary>
    private float _shownAt;

    /// <summary>The rigs and motion knobs (Home/pet_rigs.json), read once.</summary>
    private static PetRigs _rigs;

    /// <summary>True once the rigs were read (the file may be missing).</summary>
    private static bool _rigsRead;

    /// <summary>The generated shapes, made once.</summary>
    private static Sprite _disc, _triangle, _heartShape;

    /// <summary>The figure's design box (reference px): the shapes are placed in it and it is scaled to the host's rect.</summary>
    private const float Box = 400f;

    /// <summary>Shows <paramref name="kind"/> in its <paramref name="coat"/> looking <paramref name="look"/>, the room lit or dark (<paramref name="lit"/>).</summary>
    public void Show(PetKind kind, string coat, PetLook look, bool lit)
    {
        _kind = kind;
        _coat = coat;
        _look = look;
        _lit = lit;
        Build();
    }

    /// <summary>A pat: it leans into the hand, eyes shut, tail going, and a heart rises.</summary>
    public void Pat()
    {
        React(PetReaction.Pat);
        if (_heart != null)
        {
            _heart.gameObject.SetActive(true);
            _heart.color = new Color(0.9f, 0.3f, 0.42f, 1f);
        }
    }

    /// <summary>A toy: it hops, tail going.</summary>
    public void Play() => React(PetReaction.Play);

    /// <summary>Fed (the night's food paid): its head goes down to the bowl, bobbing as it eats.</summary>
    public void Feed() => React(PetReaction.Fed);

    /// <summary>Starts <paramref name="reaction"/> now (its motion from the rigs' knobs; none: the loop goes on).</summary>
    private void React(PetReaction reaction)
    {
        _reaction = reaction;
        _reactionMotion = Rigs()?.Reaction(reaction);
        _reactionStart = Time.unscaledTime;
    }

    /// <summary>The pose now (its mood's loop and the reaction under way) on the puppet and its blink, or on the stand-in's tail and figure; the heart rising while petted.</summary>
    private void Update()
    {
        if (_figure == null)
            return;
        float now = Time.unscaledTime;
        float since = _reaction != PetReaction.None ? now - _reactionStart : -1f;
        PetPose pose = PetAnimator.Pose(_mood, _reactionMotion, since, now - _shownAt, (int)_kind * 7 + (int)_look);
        float fit = FitScale();
        _figure.localScale = new Vector3(fit, fit, 1f);
        if (_puppet != null)
        {
            _puppet.Pose(pose);
            if (_blink != null)
            {
                _blink.enabled = pose.Blink;
                _blink.Pose(pose);
            }
        }
        else
        {
            _figure.anchoredPosition = new Vector2(pose.Shiver * Box * fit, pose.Hop * Box * fit);
            if (_tail != null)
                _tail.localEulerAngles = new Vector3(0f, 0f, TailAngle() - pose.TailAngle);
        }
        if (_heart != null && _heart.gameObject.activeSelf)
        {
            float length = _reaction == PetReaction.Pat && _reactionMotion != null ? _reactionMotion.length : 1f;
            float k = _reaction == PetReaction.Pat ? Mathf.Clamp01(1f - since / length) : 0f;
            _heart.rectTransform.anchoredPosition = new Vector2(-70f, 150f + (1f - k) * 60f);
            _heart.color = new Color(0.9f, 0.3f, 0.42f, k);
            if (k <= 0f)
                _heart.gameObject.SetActive(false);
        }
        if (since >= 0f && (_reactionMotion == null || since >= _reactionMotion.length))
            _reaction = PetReaction.None;
    }

    /// <summary>The rigs and motion knobs (Home/pet_rigs.json through JsonUtility), read once; null without the file.</summary>
    private static PetRigs Rigs()
    {
        if (!_rigsRead)
        {
            _rigsRead = true;
            TextAsset json = Resources.Load<TextAsset>(ArtSlots.PetRigs);
            _rigs = json != null ? JsonUtility.FromJson<PetRigs>(json.text) : null;
        }
        return _rigs;
    }

    /// <summary>The figure's scale: the design box fitted into the host's rect.</summary>
    private float FitScale()
    {
        Rect r = ((RectTransform)transform).rect;
        return Mathf.Max(0.05f, Mathf.Min(r.width, r.height) / Box);
    }

    /// <summary>Rebuilds the backdrop and the figure (the art when it exists, else the shapes).</summary>
    private void Build()
    {
        foreach (GameObject part in _parts)
            if (part != null)
                Destroy(part);
        _parts.Clear();
        _tail = null;
        _heart = null;
        _puppet = null;
        _blink = null;
        _reaction = PetReaction.None;
        _shownAt = Time.unscaledTime;
        _mood = Rigs()?.Mood(_look);
        _rig = null;

        Image back = Part(transform, "Backdrop", Vector2.zero, Vector2.zero, null, _lit ? plateColour : new Color(plateColour.r * 0.45f, plateColour.g * 0.45f, plateColour.b * 0.5f, 1f));
        Stretch(back.rectTransform);
        Sprite corner = SlotArt.Sprite(ArtSlots.PetCorner);
        if (corner != null)
        {
            back.sprite = corner;
            back.color = _lit ? Color.white : new Color(0.45f, 0.45f, 0.5f, 1f);
        }

        var figureGo = new GameObject("Figure", typeof(RectTransform));
        figureGo.transform.SetParent(transform, false);
        _parts.Add(figureGo);
        _figure = (RectTransform)figureGo.transform;
        _figure.anchorMin = _figure.anchorMax = _figure.pivot = new Vector2(0.5f, 0.5f);
        _figure.sizeDelta = new Vector2(Box, Box);

        Sprite art = SlotArt.Sprite(ArtSlots.PetSprite(_kind, _coat, _look));
        if (art != null)
        {
            _rig = Rigs()?.Rig(_kind, _look);
            Color tint = _lit ? Color.white : new Color(0.55f, 0.55f, 0.6f, 1f);
            _puppet = Puppet("Picture", art, tint);
            string blinkSlot = ArtSlots.PetBlink(_kind, _coat, _look);
            Sprite shut = _rig != null && _rig.CanBlink && blinkSlot != null ? SlotArt.Sprite(blinkSlot) : null;
            if (shut != null)
            {
                _blink = Puppet("Blink", shut, tint);
                _blink.enabled = false;
            }
        }
        else
            DrawStandIn();

        _heart = Part(_figure, "Heart", new Vector2(-70f, 150f), new Vector2(56f, 50f), HeartShape(), new Color(0.9f, 0.3f, 0.42f, 0f));
        _heart.gameObject.SetActive(false);
        Update();
    }

    /// <summary>The code-drawn stand-in: a dog or a cat from discs and triangles, posed for how it looks.</summary>
    private void DrawStandIn()
    {
        bool dog = _kind == PetKind.Dog;
        bool sad = _look == PetLook.Sad || _look == PetLook.Sick;
        Color fur = dog ? new Color(0.66f, 0.45f, 0.27f, 1f) : new Color(0.47f, 0.49f, 0.55f, 1f);
        if (_look == PetLook.Sick)
            fur = Color.Lerp(fur, new Color(0.62f, 0.7f, 0.55f, 1f), 0.35f);
        if (!_lit)
            fur *= 0.6f;
        fur.a = 1f;
        Color dark = new Color(fur.r * 0.6f, fur.g * 0.6f, fur.b * 0.6f, 1f);
        Color light = Color.Lerp(fur, Color.white, 0.45f);
        Color ink = new Color(0.12f, 0.1f, 0.1f, 1f);
        float headDrop = sad ? -18f : 0f;

        Part(_figure, "Shadow", new Vector2(0f, -140f), new Vector2(300f, 46f), Disc(), new Color(0f, 0f, 0f, 0.18f));
        _tail = Part(_figure, "Tail", dog ? new Vector2(115f, -55f) : new Vector2(95f, -60f), dog ? new Vector2(34f, 110f) : new Vector2(24f, 140f), Disc(), dark).rectTransform;
        _tail.pivot = new Vector2(0.5f, 0f);
        Part(_figure, "Body", new Vector2(10f, -70f), new Vector2(dog ? 250f : 220f, dog ? 150f : 130f), Disc(), fur);
        Part(_figure, "Chest", new Vector2(-60f, -80f), new Vector2(90f, 90f), Disc(), light);

        float hy = 40f + headDrop;
        if (dog)
        {
            Part(_figure, "EarBack", new Vector2(-145f, hy - 10f), new Vector2(52f, 100f), Disc(), dark).rectTransform.localEulerAngles = new Vector3(0f, 0f, sad ? 25f : 12f);
            Part(_figure, "Head", new Vector2(-90f, hy), new Vector2(150f, 140f), Disc(), fur);
            Part(_figure, "EarFront", new Vector2(-30f, hy - 10f), new Vector2(52f, 100f), Disc(), dark).rectTransform.localEulerAngles = new Vector3(0f, 0f, sad ? -25f : -12f);
            Part(_figure, "Snout", new Vector2(-110f, hy - 35f), new Vector2(80f, 56f), Disc(), light);
            Part(_figure, "Nose", new Vector2(-122f, hy - 22f), new Vector2(26f, 18f), Disc(), ink);
        }
        else
        {
            float tilt = sad ? 22f : 0f;
            Part(_figure, "EarBack", new Vector2(-140f, hy + 60f - (sad ? 10f : 0f)), new Vector2(54f, 62f), Triangle(), dark).rectTransform.localEulerAngles = new Vector3(0f, 0f, 12f + tilt);
            Part(_figure, "EarFront", new Vector2(-50f, hy + 60f - (sad ? 10f : 0f)), new Vector2(54f, 62f), Triangle(), dark).rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f - tilt);
            Part(_figure, "Head", new Vector2(-95f, hy), new Vector2(140f, 125f), Disc(), fur);
            Part(_figure, "Muzzle", new Vector2(-95f, hy - 30f), new Vector2(60f, 36f), Disc(), light);
            Part(_figure, "Nose", new Vector2(-95f, hy - 16f), new Vector2(16f, 11f), Triangle(), new Color(0.85f, 0.5f, 0.55f, 1f)).rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            for (int i = 0; i < 2; i++)
                for (int s = -1; s <= 1; s += 2)
                    Part(_figure, "Whisker", new Vector2(-95f + s * 52f, hy - 26f - i * 9f), new Vector2(46f, 3f), null, ink).rectTransform.localEulerAngles = new Vector3(0f, 0f, s * (i == 0 ? 6f : -6f));
        }

        // Eyes: open; squinting with joy; low and half shut when sad or sick.
        float eyeHeight = _look == PetLook.Happy ? 7f : sad ? 10f : 20f;
        float eyeY = hy + (dog ? 18f : 14f);
        foreach (float x in dog ? new[] { -122f, -70f } : new[] { -122f, -68f })
            Part(_figure, "Eye", new Vector2(x, eyeY), new Vector2(18f, eyeHeight), Disc(), ink);

        if (_look == PetLook.Sick)
            Part(_figure, "Blanket", new Vector2(35f, -95f), new Vector2(240f, 105f), Disc(), new Color(0.36f, 0.5f, 0.72f, 1f));
    }

    /// <summary>A puppet named <paramref name="name"/> filling the figure: <paramref name="sprite"/> on the rig, tinted <paramref name="tint"/>.</summary>
    private PetPuppet Puppet(string name, Sprite sprite, Color tint)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_figure, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Box, Box);
        PetPuppet puppet = go.AddComponent<PetPuppet>();
        puppet.color = tint;
        puppet.Show(sprite, _rig);
        return puppet;
    }

    /// <summary>The tail's resting angle for how it looks (down when sad or sick).</summary>
    private float TailAngle() => _look == PetLook.Sad || _look == PetLook.Sick ? -150f : _kind == PetKind.Dog ? -35f : -15f;

    /// <summary>A child image at <paramref name="centre"/> (design box, y up) of <paramref name="size"/>, its sprite (null: a plain rectangle) and colour; no raycasts.</summary>
    private Image Part(Transform parent, string name, Vector2 centre, Vector2 size, Sprite sprite, Color colour)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (parent == transform)
            _parts.Add(go);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = centre;
        rt.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }

    /// <summary>Fills its parent.</summary>
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>A soft-edged white disc (an ellipse once sized).</summary>
    private static Sprite Disc() => _disc != null ? _disc : _disc = Shape("PetDisc", (x, y) => 1f - Mathf.Sqrt(x * x + y * y));

    /// <summary>A white triangle, point up (an ear, a nose turned over).</summary>
    private static Sprite Triangle() => _triangle != null ? _triangle : _triangle = Shape("PetTriangle", (x, y) => Mathf.Min(y + 1f, 1f - y - 2f * Mathf.Abs(x)) * 0.5f);

    /// <summary>A white heart.</summary>
    private static Sprite HeartShape() => _heartShape != null ? _heartShape : _heartShape = Shape("PetHeart", (x, y) =>
    {
        float a = x * 1.2f, b = y * 1.2f + 0.25f;
        float f = Mathf.Pow(a * a + b * b - 1f, 3f) - a * a * b * b * b;
        return -f * 4f;
    });

    /// <summary>A 128 px white sprite whose alpha is <paramref name="inside"/> (positive inside, about the distance to the edge in half-widths) over -1..1, antialiased.</summary>
    private static Sprite Shape(string name, System.Func<float, float, float> inside)
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(inside(x, y) * size * 0.5f);
                pixels[py * size + px] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
