using TMPro;
using UnityEngine;

/// <summary>
/// The office's diegetic readouts: the day calendar (Day), the timeline
/// stability monitor (Stability, its text tinted by band) and the cash till
/// (Credits, with a "ding" when the value rises). The stability text keeps
/// its own colour while stability is healthy. The texts are the art
/// office's own (the office binder hands them over through Bind), or the
/// gameplay layer's fallback HUD where the art has none. Polls WorldState each
/// frame from the RunManager so it stays decoupled from GameManager. Every
/// target is optional and null-safe.
/// </summary>
public sealed class OfficeReadouts : MonoBehaviour
{
    [Header("Stability bands")]
    /// <summary>Below this stability, the text is amber.</summary>
    [SerializeField] private float amberBelow = 60f;

    /// <summary>Below this stability, the text is red.</summary>
    [SerializeField] private float redBelow = 30f;

    /// <summary>The text's colour in the warning band.</summary>
    [SerializeField] private Color amberColor = new Color(0.95f, 0.75f, 0.3f);

    /// <summary>The text's colour when stability is critical.</summary>
    [SerializeField] private Color redColor = new Color(0.9f, 0.35f, 0.3f);

    [Header("Credits")]
    /// <summary>Plays a "ding" when credits increase (clip is a placeholder to assign).</summary>
    [SerializeField] private AudioSource creditsDing;

    private TMP_Text _dayText;
    private TMP_Text _stabilityText;
    private TMP_Text _creditsText;
    private Color _stabilityColour;

    /// <summary>Last money value seen, to detect increases for the ding.</summary>
    private int _lastMoney;

    /// <summary>True once we have a baseline money value.</summary>
    private bool _hasLast;

    /// <summary>Sets the texts the readouts write (the office binder: the art's, or the fallback HUD's; null skips one).</summary>
    public void Bind(TMP_Text day, TMP_Text stability, TMP_Text credits)
    {
        _dayText = day;
        _stabilityText = stability;
        _creditsText = credits;
        if (stability != null)
            _stabilityColour = stability.color;
    }

    private void Update()
    {
        if (!RunManager.HasInstance)
            return;

        Apply(RunManager.Instance.World);
    }

    /// <summary>Refreshes every readout from world state (null-safe).</summary>
    private void Apply(WorldState world)
    {
        if (world == null)
            return;

        if (_dayText != null)
            _dayText.text = world.day.ToString("00");

        if (_stabilityText != null)
        {
            _stabilityText.text = $"{world.timelineStability:0}%";
            _stabilityText.color =
                world.timelineStability < redBelow ? redColor :
                world.timelineStability < amberBelow ? amberColor :
                _stabilityColour;
        }

        if (_creditsText != null)
            _creditsText.text = world.money.ToString();

        if (_hasLast && world.money > _lastMoney &&
            creditsDing != null && creditsDing.clip != null)
            creditsDing.Play();

        _lastMoney = world.money;
        _hasLast = true;
    }
}
