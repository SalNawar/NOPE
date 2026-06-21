using TMPro;
using UnityEngine;

/// <summary>
/// Diegetic in-world readouts that replace the overlay HUD: a wall calendar
/// (Day), a TVA-style timeline monitor (Stability, colour-graded), and a cash
/// till (Credits, with a "ding" when the value rises). Polls WorldState each
/// frame from the RunManager so it stays decoupled from GameManager. All target
/// fields are optional and null-safe.
/// </summary>
public sealed class OfficeReadouts : MonoBehaviour
{
    [Header("Day — wall calendar")]
    /// <summary>Shows the current day number.</summary>
    [SerializeField] private TMP_Text dayText;

    [Header("Stability — timeline monitor")]
    /// <summary>Shows timeline stability as a percentage.</summary>
    [SerializeField] private TMP_Text stabilityText;

    /// <summary>Monitor sprite tinted by the stability band (green/amber/red).</summary>
    [SerializeField] private SpriteRenderer stabilityLamp;

    /// <summary>Below this stability, the lamp is amber.</summary>
    [SerializeField] private float amberBelow = 60f;

    /// <summary>Below this stability, the lamp is red.</summary>
    [SerializeField] private float redBelow = 30f;

    /// <summary>Lamp colour when stability is healthy.</summary>
    [SerializeField] private Color greenColor = new Color(0.42f, 0.9f, 0.5f);

    /// <summary>Lamp colour when stability is in the warning band.</summary>
    [SerializeField] private Color amberColor = new Color(0.95f, 0.75f, 0.3f);

    /// <summary>Lamp colour when stability is critical.</summary>
    [SerializeField] private Color redColor = new Color(0.9f, 0.35f, 0.3f);

    [Header("Credits — cash till")]
    /// <summary>Shows current credits.</summary>
    [SerializeField] private TMP_Text creditsText;

    /// <summary>Plays a "ding" when credits increase (clip is a placeholder to assign).</summary>
    [SerializeField] private AudioSource creditsDing;

    /// <summary>Last money value seen, to detect increases for the ding.</summary>
    private int _lastMoney;

    /// <summary>True once we have a baseline money value.</summary>
    private bool _hasLast;

    private void Update()
    {
        if (!RunManager.HasInstance)
            return;

        Apply(RunManager.Instance.World);
    }

    /// <summary>Refreshes every readout from world state (null-safe).</summary>
    public void Apply(WorldState world)
    {
        if (world == null)
            return;

        if (dayText != null)
            dayText.text = world.day.ToString("00");

        if (stabilityText != null)
            stabilityText.text = $"{world.timelineStability:0}%";

        if (stabilityLamp != null)
            stabilityLamp.color =
                world.timelineStability < redBelow ? redColor :
                world.timelineStability < amberBelow ? amberColor :
                greenColor;

        if (creditsText != null)
            creditsText.text = world.money.ToString();

        if (_hasLast && world.money > _lastMoney &&
            creditsDing != null && creditsDing.clip != null)
            creditsDing.Play();

        _lastMoney = world.money;
        _hasLast = true;
    }
}
