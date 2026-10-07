using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RETURN and DETAIN, the desk machine prototype's plain kit buttons by the
/// counter (the desk machine spec §2, build-order step 1: their hardware, a
/// big amber push button and a red mushroom under a safety cover, comes in
/// step 2). RETURN commits a DENIED passport handed back (the traveller
/// leaves the way they came: a clunk and a buzzer); for anything else it
/// refuses with a thunk and a note. DETAIN commits the third verdict at any
/// time a traveller is at the desk, stamp or not (an alarm chirp, the screen
/// flashes red once; Saleh: "detain only if the traveller breaks the law",
/// and detaining anyone who broke no law is the one citation:
/// VerdictRules). Both show while a traveller stands at the desk with their
/// case undecided and the booth shows the desk props. Build Office UI
/// builds them on the office overlay.
/// </summary>
public sealed class VerdictButtons : MonoBehaviour
{
    /// <summary>The stamps: the verdict, the hand-back and the commit.</summary>
    [SerializeField] private DeskStampTray stamps;

    /// <summary>RETURN (the kit's brass plate).</summary>
    [SerializeField] private Button returnButton;

    /// <summary>DETAIN (the kit's red plate).</summary>
    [SerializeField] private Button detainButton;

    /// <summary>The red flash over the screen as guards take a detained traveller (a full-screen image, no raycasts, clear at rest).</summary>
    [SerializeField] private Image flash;

    /// <summary>Plays the clunk, the chirp and the refusal's thunk (optional).</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The flash's strongest alpha.</summary>
    private const float FlashAlpha = 0.35f;

    private Spring _flash;
    private bool _shown = true;

    private void Awake()
    {
        if (returnButton != null)
            returnButton.onClick.AddListener(Return);
        if (detainButton != null)
            detainButton.onClick.AddListener(Detain);
        if (stamps != null)
            stamps.Changed += Show;
        Show();
    }

    private void OnDestroy()
    {
        if (stamps != null)
            stamps.Changed -= Show;
    }

    /// <summary>RETURN: commits a DENIED passport handed back; else the thunk and the note.</summary>
    public void Return()
    {
        if (stamps != null && stamps.Commit(DeskStamp.Denied))
        {
            CueSounds.Play(CueSounds.Return, sound);
            return;
        }
        CueSounds.Play(SoundCues.UiError, sound);
        if (stamps != null)
            stamps.Note("hardware.refused.return");
    }

    /// <summary>DETAIN: commits the third verdict (any time a traveller is here): the chirp and the red flash.</summary>
    public void Detain()
    {
        if (stamps == null || !stamps.Commit(DeskStamp.Detained))
            return;
        CueSounds.Play(CueSounds.Detain, sound);
        _flash.Snap(1f);
        _flash.Target = 0f;
    }

    /// <summary>The buttons show while a traveller is at the desk with their case undecided.</summary>
    private void Show()
    {
        bool shown = stamps != null && stamps.TravellerHere;
        if (shown == _shown)
            return;
        _shown = shown;
        foreach (Button b in new[] { returnButton, detainButton })
            if (b != null && b.gameObject.activeSelf != shown)
                b.gameObject.SetActive(shown);
    }

    /// <summary>The flash fades on its spring (the Paper feel, in real time).</summary>
    private void Update()
    {
        if (flash == null || _flash.AtRest)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        _flash.Step(UiMotion.Delta(Time.unscaledDeltaTime), knobs.Get(knobs.paperFeel), 0.002f, 0.01f);
        Color c = flash.color;
        c.a = Mathf.Clamp01(_flash.Value) * FlashAlpha;
        flash.color = c;
    }
}
