using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>What a search jump found in a view: the row or box to mark, and what takes the keyboard focus there (null: nothing can).</summary>
public readonly struct FoundTarget
{
    /// <summary>A found <paramref name="rect"/>, focusing <paramref name="focus"/>.</summary>
    public FoundTarget(RectTransform rect, Selectable focus)
    {
        Rect = rect;
        Focus = focus;
    }

    /// <summary>The found row or box (null: nothing found).</summary>
    public RectTransform Rect { get; }

    /// <summary>The found row's button (Space picks it next), or null.</summary>
    public Selectable Focus { get; }
}

/// <summary>
/// The found flash of a search jump (the PC redesign SE4): a clone of the
/// app's inactive template laid over the found row or box (it goes with it
/// when the row is redrawn), pulsing in the form style's found colour
/// (FoundFlash: DesktopConfigSO's pulses over their time), then a steady
/// outline until the next click or the next navigation (the app removes it);
/// reduced motion shows the outline only. It is not the compare's highlight:
/// another colour, and it never picks.
/// </summary>
public sealed class FoundMark : MonoBehaviour
{
    /// <summary>The fill that pulses (clear when still).</summary>
    [SerializeField] private Image pulse;

    /// <summary>The outline's edges.</summary>
    [SerializeField] private Graphic[] outline = new Graphic[0];

    /// <summary>The forms' style: the found colour.</summary>
    [SerializeField] private FormStyleSO style;

    /// <summary>The desktop's knobs: the pulses, their time and strength.</summary>
    [SerializeField] private DesktopConfigSO config;

    private float _start;
    private int _placedFrame;
    private bool _pulsing;

    /// <summary>A clone of <paramref name="template"/> over <paramref name="target"/> (its last child, filling it), flashing; null when there is no target.</summary>
    public static FoundMark Place(FoundMark template, RectTransform target, bool reducedMotion)
    {
        if (template == null || target == null)
            return null;
        FoundMark mark = Instantiate(template, target, false);
        mark.name = "FoundMark";
        var rt = (RectTransform)mark.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();
        mark.gameObject.SetActive(true);
        mark.Play(reducedMotion);
        return mark;
    }

    /// <summary>Starts the pulses (none with reduced motion) and shows the outline in the found colour.</summary>
    private void Play(bool reducedMotion)
    {
        Color found = style != null ? style.found : Color.cyan;
        foreach (Graphic edge in outline)
            if (edge != null)
                edge.color = found;
        _start = Time.unscaledTime;
        _placedFrame = Time.frameCount;
        _pulsing = !reducedMotion && config != null;
        SetPulse(0f);
    }

    /// <summary>The pulses while they run; the next press anywhere removes the mark.</summary>
    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (Time.frameCount > _placedFrame && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Destroy(gameObject);
            return;
        }
        if (!_pulsing)
            return;
        float elapsed = Time.unscaledTime - _start;
        SetPulse(FoundFlash.Pulse(elapsed, config.foundSeconds, config.foundPulses));
        if (FoundFlash.Done(elapsed, config.foundSeconds))
        {
            _pulsing = false;
            SetPulse(0f);
        }
    }

    /// <summary>The fill at <paramref name="strength"/> (0 to 1) of the found colour's pulse alpha.</summary>
    private void SetPulse(float strength)
    {
        if (pulse == null)
            return;
        Color c = style != null ? style.found : Color.cyan;
        c.a = strength * (config != null ? config.foundPulseAlpha : 0.4f);
        pulse.color = c;
    }
}
