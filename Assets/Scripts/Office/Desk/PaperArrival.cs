using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A paper delivered onto the desk (Saleh 2026-10-07, the citation: "when it
/// shows on your screen it should slide and twist, then become a document on
/// the desk"): flies the object it sits on from a point off the screen's edge
/// to its spot along PaperFlight's path (a slight arc, a twist about the
/// vertical and a tumble about its long axis, a springy settle, a soft squash
/// as it lands), a soft shadow on the desk under it that sharpens as it comes
/// down, and its landing when it touches (the sound bank's cue, else a code-made
/// paper thud, and FeelDirector.Hit's punch at its strength); then it lies exactly on its spot
/// and calls back. Reduced Motion: a short straight slide. Reusable for any
/// delivered paper (the citations today; delivery notes later): Fly is the
/// whole interface.
/// </summary>
public sealed class PaperArrival : MonoBehaviour
{
    /// <summary>The shadow's darkest alpha and how much larger it spreads at the arc's top.</summary>
    private const float ShadowAlpha = 0.35f, ShadowSpread = 0.6f;

    /// <summary>The shadow's height over the desk (metres), under the papers' first stack step.</summary>
    private const float ShadowLift = 0.0003f;

    /// <summary>The blob texture's side in pixels.</summary>
    private const int BlobPixels = 64;

    private static Material _shadowMaterial;
    private static Texture2D _blob;
    private static AudioClip _thud;

    private Vector3 _from, _to, _scale;
    private Quaternion _rest;
    private Vector2 _size;
    private float _twist, _tumble, _arc, _delay, _t, _length;
    private bool _reduced, _thudded;

    /// <summary>The landing's sound cue (null: the code thud) and punch (FeelDirector.Hit's strength).</summary>
    private string _landCue;
    private float _landHit;
    private Action _landed;
    private Transform _shadow;
    private Renderer _shadowRenderer;
    private MaterialPropertyBlock _block;

    /// <summary>Where the paper lands (its flight's end).</summary>
    public Vector3 To => _to;

    /// <summary>True while the paper flies (it takes no input then).</summary>
    public bool Flying { get; private set; }

    /// <summary>
    /// Flies this object from <paramref name="from"/> to <paramref name="to"/>
    /// (world points; it ends lying as it lies now) after
    /// <paramref name="delay"/> seconds, twisted by <paramref name="twist"/> and
    /// tumbled by <paramref name="tumble"/> degrees at the start, up to
    /// <paramref name="arc"/> metres over the straight way, its shadow
    /// <paramref name="size"/> metres; <paramref name="landed"/> once it lies
    /// still. Reduced Motion (MotionPreference) slides it straight and short.
    /// As it touches it plays <paramref name="landCue"/> (SoundCues; the code thud
    /// without one or without its clip) and punches with <paramref name="landHit"/>
    /// (FeelDirector.Hit, 0 to 1; 0 none).
    /// </summary>
    public void Fly(Vector3 from, Vector3 to, Vector2 size, float twist, float tumble, float arc, float delay, Action landed, string landCue = null, float landHit = 0f)
    {
        _landCue = landCue;
        _landHit = landHit;
        _from = from;
        _to = to;
        _rest = transform.rotation;
        _scale = transform.localScale;
        _size = size;
        _twist = twist;
        _tumble = tumble;
        _arc = arc;
        _delay = Mathf.Max(0f, delay);
        _reduced = MotionPreference.Reduced;
        _length = PaperFlight.Length(_reduced);
        _t = 0f;
        _thudded = false;
        _landed = landed;
        Flying = true;
        Pose(PaperFlight.At(0f, _twist, _tumble, _reduced));
    }

    private void Update()
    {
        if (!Flying)
            return;
        if (_delay > 0f)
        {
            _delay -= Time.deltaTime;
            return;
        }
        _t = Mathf.Min(1f, _t + Time.deltaTime / Mathf.Max(0.01f, _length));
        FlightPose pose = PaperFlight.At(_t, _twist, _tumble, _reduced);
        Pose(pose);
        if (pose.Landed && !_thudded)
        {
            _thudded = true;
            if (string.IsNullOrEmpty(_landCue) || !Sounds.Play(_landCue))
            {
                _thud ??= CodeTones.Tone("PaperThud", 95f, 0.12f, 0.35f);
                AudioSource.PlayClipAtPoint(_thud, _to, 0.6f);
            }
            FeelDirector.Hit(_landHit);
        }
        if (_t >= 1f)
            Land();
    }

    private void OnDestroy()
    {
        if (_shadow != null)
            Destroy(_shadow.gameObject);
    }

    /// <summary>Puts the paper and its shadow where <paramref name="pose"/> says.</summary>
    private void Pose(FlightPose pose)
    {
        Vector3 at = Vector3.LerpUnclamped(_from, _to, pose.Along) + Vector3.up * (_arc * pose.Lift);
        transform.position = at;
        transform.rotation = Quaternion.AngleAxis(pose.Twist, Vector3.up) * Quaternion.AngleAxis(pose.Tumble, _rest * Vector3.forward) * _rest;
        transform.localScale = _scale * pose.Squash;
        Shadow(new Vector3(at.x, _to.y + ShadowLift, at.z), 1f + ShadowSpread * pose.Lift, ShadowAlpha * pose.Shadow);
    }

    /// <summary>It lies exactly on its spot: square, its own size, no shadow of its own; then the call back.</summary>
    private void Land()
    {
        Flying = false;
        transform.position = _to;
        transform.rotation = _rest;
        transform.localScale = _scale;
        if (_shadow != null)
            _shadow.gameObject.SetActive(false);
        Action landed = _landed;
        _landed = null;
        landed?.Invoke();
    }

    /// <summary>The soft shadow on the desk at <paramref name="at"/>, <paramref name="spread"/> times the paper's size, at <paramref name="alpha"/> (made on the first flight: a quad lying flat with a round blob, see-through, casting nothing).</summary>
    private void Shadow(Vector3 at, float spread, float alpha)
    {
        if (_shadow == null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name + "_Shadow";
            Destroy(quad.GetComponent<Collider>());
            _shadowRenderer = quad.GetComponent<Renderer>();
            _shadowRenderer.sharedMaterial = ShadowMaterial();
            _shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _shadowRenderer.receiveShadows = false;
            _shadow = quad.transform;
        }
        _shadow.gameObject.SetActive(true);
        _shadow.position = at;
        _shadow.rotation = Quaternion.LookRotation(Vector3.down, _rest * Vector3.forward);
        _shadow.localScale = new Vector3(_size.x * spread, _size.y * spread, 1f);
        _block ??= new MaterialPropertyBlock();
        _shadowRenderer.GetPropertyBlock(_block);
        _block.SetColor("_BaseColor", new Color(0f, 0f, 0f, alpha));
        _shadowRenderer.SetPropertyBlock(_block);
    }

    /// <summary>The shadows' material: URP's unlit shader, see-through, the blob as its map (made once).</summary>
    private static Material ShadowMaterial()
    {
        if (_shadowMaterial != null)
            return _shadowMaterial;
        _blob = new Texture2D(BlobPixels, BlobPixels, TextureFormat.RGBA32, false) { name = "PaperShadow", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[BlobPixels * BlobPixels];
        for (int y = 0; y < BlobPixels; y++)
            for (int x = 0; x < BlobPixels; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - BlobPixels / 2f) / (BlobPixels / 2f), dy = Mathf.Abs(y + 0.5f - BlobPixels / 2f) / (BlobPixels / 2f);
                float edge = Mathf.Clamp01((1f - Mathf.Max(dx, dy)) * 4f);
                pixels[y * BlobPixels + x] = new Color32(255, 255, 255, (byte)(255f * edge * edge));
            }
        _blob.SetPixels32(pixels);
        _blob.Apply(false, true);
        _shadowMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "PaperShadow", renderQueue = (int)RenderQueue.Transparent };
        _shadowMaterial.SetTexture("_BaseMap", _blob);
        _shadowMaterial.SetFloat("_Surface", 1f);
        _shadowMaterial.SetFloat("_Blend", 0f);
        _shadowMaterial.SetFloat("_ZWrite", 0f);
        _shadowMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        _shadowMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        _shadowMaterial.SetOverrideTag("RenderType", "Transparent");
        _shadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return _shadowMaterial;
    }
}
