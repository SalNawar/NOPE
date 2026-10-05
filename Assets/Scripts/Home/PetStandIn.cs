using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The pet as the player sees it (the Home pet spec PS7): its picture for how
/// it looks when the art exists (ArtSlots.PetSprite, Home/pet_&lt;kind&gt;_&lt;look&gt;),
/// else a stand-in drawn in code from a few shapes (a body, a head, ears, eyes,
/// a nose and a tail; a dog's ears droop, a cat's stand; a sick pet under a
/// blanket), on the corner's backdrop (ArtSlots.PetCorner, else a plain
/// plate), dimmed while the electricity is off. It breathes while idle, hops
/// with a heart when petted and spins when played with. The Home pet corner
/// and the Title's adoption panel each host one; it builds its children on
/// demand and owns them.
/// </summary>
public sealed class PetStandIn : MonoBehaviour
{
    /// <summary>The plate behind the pet when the corner has no backdrop art.</summary>
    [SerializeField] private Color plateColour = new Color(0.86f, 0.8f, 0.68f, 1f);

    /// <summary>The kind drawn.</summary>
    private PetKind _kind;

    /// <summary>How it looks.</summary>
    private PetLook _look;

    /// <summary>Whether the lights are on (the electricity paid).</summary>
    private bool _lit = true;

    /// <summary>The drawn parts (rebuilt on Show).</summary>
    private readonly List<GameObject> _parts = new();

    /// <summary>The figure's root: what breathes, hops and spins.</summary>
    private RectTransform _figure;

    /// <summary>The tail (wags), when drawn.</summary>
    private RectTransform _tail;

    /// <summary>The heart shown after a pat.</summary>
    private Image _heart;

    /// <summary>Seconds left of the hop (after a pat) and of the spin (after a toy).</summary>
    private float _hop, _spin;

    /// <summary>The generated shapes, made once.</summary>
    private static Sprite _disc, _triangle, _heartShape;

    /// <summary>The figure's design box (reference px): the shapes are placed in it and it is scaled to the host's rect.</summary>
    private const float Box = 400f;

    /// <summary>Shows <paramref name="kind"/> looking <paramref name="look"/>, the room lit or dark (<paramref name="lit"/>).</summary>
    public void Show(PetKind kind, PetLook look, bool lit)
    {
        _kind = kind;
        _look = look;
        _lit = lit;
        Build();
    }

    /// <summary>A pat: a small hop and a heart.</summary>
    public void Pat()
    {
        _hop = 0.6f;
        if (_heart != null)
        {
            _heart.gameObject.SetActive(true);
            _heart.color = new Color(0.9f, 0.3f, 0.42f, 1f);
        }
    }

    /// <summary>A toy: a happy spin.</summary>
    public void Play() => _spin = 0.8f;

    /// <summary>Breathing, the tail's wag, the hop with its heart and the spin.</summary>
    private void Update()
    {
        if (_figure == null)
            return;
        float t = Time.unscaledTime;
        float breathe = 1f + 0.02f * Mathf.Sin(t * (_look == PetLook.Sick ? 1.2f : 2.4f));
        float hop = _hop > 0f ? Mathf.Sin((0.6f - _hop) / 0.6f * Mathf.PI) * 40f : 0f;
        _figure.localScale = new Vector3(breathe, 1f / breathe, 1f) * FitScale();
        _figure.anchoredPosition = new Vector2(0f, hop * FitScale());
        _figure.localEulerAngles = new Vector3(0f, _spin > 0f ? (0.8f - _spin) / 0.8f * 360f : 0f, 0f);
        if (_tail != null)
        {
            float speed = _look == PetLook.Happy ? 9f : _look == PetLook.Idle ? 4f : 1.5f;
            float swing = _look == PetLook.Sick || _look == PetLook.Sad ? 4f : 14f;
            _tail.localEulerAngles = new Vector3(0f, 0f, TailAngle() + swing * Mathf.Sin(t * speed));
        }
        if (_heart != null && _heart.gameObject.activeSelf)
        {
            float k = Mathf.Clamp01(_hop / 0.6f);
            _heart.rectTransform.anchoredPosition = new Vector2(-70f, 150f + (1f - k) * 60f);
            _heart.color = new Color(0.9f, 0.3f, 0.42f, k);
            if (_hop <= 0f)
                _heart.gameObject.SetActive(false);
        }
        _hop = Mathf.Max(0f, _hop - Time.unscaledDeltaTime);
        _spin = Mathf.Max(0f, _spin - Time.unscaledDeltaTime);
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

        Sprite art = SlotArt.Sprite(ArtSlots.PetSprite(_kind, _look));
        if (art != null)
        {
            Image picture = Part(_figure, "Picture", Vector2.zero, new Vector2(Box, Box), art, _lit ? Color.white : new Color(0.55f, 0.55f, 0.6f, 1f));
            picture.preserveAspect = true;
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
        _tail = Part(_figure, "Tail", dog ? new Vector2(115f, -55f) : new Vector2(120f, -40f), dog ? new Vector2(34f, 110f) : new Vector2(26f, 170f), Disc(), dark).rectTransform;
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
            Part(_figure, "Blanket", new Vector2(40f, -95f), new Vector2(210f, 90f), null, new Color(0.36f, 0.5f, 0.72f, 1f));
    }

    /// <summary>The tail's resting angle for how it looks (down when sad or sick).</summary>
    private float TailAngle() => _look == PetLook.Sad || _look == PetLook.Sick ? -120f : _kind == PetKind.Dog ? -35f : -15f;

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
