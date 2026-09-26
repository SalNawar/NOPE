using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The speech bubble's input (piece 10 X11), on its panel: hovering the
/// bubble holds its line (TravellerWheel.SetBubbleHovered), and the panel's
/// button (interactable only while an answer shows, so it gives the hand
/// cursor and the hover ring then) picks the answer (TravellerWheel.ClickLine).
/// The panel hides between lines, so hiding releases the hold.
/// </summary>
public sealed class SpeechBubbleInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>The wheel that says the lines.</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>The panel's button.</summary>
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button != null && wheel != null)
            button.onClick.AddListener(wheel.ClickLine);
    }

    /// <summary>A hidden bubble no longer holds its line (the pointer's exit never arrives on an inactive panel).</summary>
    private void OnDisable()
    {
        if (wheel != null)
            wheel.SetBubbleHovered(false);
    }

    /// <summary>The pointer is on the bubble: its line holds.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (wheel != null)
            wheel.SetBubbleHovered(true);
    }

    /// <summary>The pointer left the bubble: its hold runs from now.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (wheel != null)
            wheel.SetBubbleHovered(false);
    }
}
