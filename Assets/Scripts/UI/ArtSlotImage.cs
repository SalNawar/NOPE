using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A UI image's art slot (redesign phase 27): when the slot's file exists
/// (SlotArt), the image shows it on wake, sliced when the art has 9-slice
/// borders, and the art's companions come on (a button's label printed over a
/// text-free face); a button's hover face swaps in when its own slot exists.
/// When the file is missing the image keeps its code-drawn look, or hides when
/// the slot has no look without art (the bubble's tail, the slot machine).
/// The image's colour stays the art's tint (light art takes the panel's colour
/// and the theme's, as the flat panel did) unless the art is a full-colour
/// picture, which shows untinted.
/// </summary>
[RequireComponent(typeof(Image))]
public sealed class ArtSlotImage : MonoBehaviour
{
    /// <summary>The slot (ArtSlots): its file is Assets/Art/UI/Resources/&lt;slot&gt;.png.</summary>
    [SerializeField] private string slot;

    /// <summary>A button's face under the pointer (its own slot; optional): with both faces the button swaps sprites, with the normal face only it tints; without a hover slot the button's transition is left alone.</summary>
    [SerializeField] private string hoverSlot;

    /// <summary>True for full-colour art (a picture): shown white, untinted; false keeps the image's colour as the tint of light art.</summary>
    [SerializeField] private bool fullColour;

    /// <summary>True when the image has no look without its art: it hides then.</summary>
    [SerializeField] private bool hideWithoutArt;

    /// <summary>What comes on with the art (a text-free face's label).</summary>
    [SerializeField] private Behaviour[] showWithArt = new Behaviour[0];

    private void Awake() => Apply();

    /// <summary>Configures the slot (the builders; a scene keeps it). Nothing is shown until Awake.</summary>
    public void Configure(string artSlot, string hoverArtSlot, bool picture, bool hideWhenMissing, params Behaviour[] companions)
    {
        slot = artSlot;
        hoverSlot = hoverArtSlot;
        fullColour = picture;
        hideWithoutArt = hideWhenMissing;
        showWithArt = companions ?? new Behaviour[0];
    }

    /// <summary>Looks the slot's art up and shows it, or keeps the fallback.</summary>
    private void Apply()
    {
        var image = GetComponent<Image>();
        Sprite art = string.IsNullOrEmpty(slot) ? null : SlotArt.Sprite(slot);
        if (art == null)
        {
            if (hideWithoutArt)
                gameObject.SetActive(false);
            return;
        }

        image.sprite = art;
        image.type = art.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        if (fullColour)
            image.color = Color.white;

        if (!string.IsNullOrEmpty(hoverSlot) && TryGetComponent(out Button button))
        {
            Sprite hover = SlotArt.Sprite(hoverSlot);
            if (hover != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState { highlightedSprite = hover, pressedSprite = hover, selectedSprite = hover };
            }
            else
                button.transition = Selectable.Transition.ColorTint;
        }

        foreach (Behaviour companion in showWithArt)
            if (companion != null)
                companion.enabled = true;
    }
}
