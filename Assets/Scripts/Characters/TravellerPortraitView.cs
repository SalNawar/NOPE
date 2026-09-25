using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The passport photo on a scanned page: one uGUI Image per LookLayer (index =
/// the layer, stacked in order inside a 4:5 box), each showing its layer's
/// photo crop (CharacterArt.GetPhoto). Not clickable.
/// </summary>
public sealed class TravellerPortraitView : MonoBehaviour
{
    /// <summary>One Image per LookLayer, wired by Build Office UI.</summary>
    [SerializeField] private Image[] layers;

    /// <summary>Shows a look's photo; a null look or art clears it.</summary>
    public void Show(TravellerLook look, CharacterArt art)
    {
        if (look == null || art == null)
        {
            Clear();
            return;
        }

        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (layers[i] == null)
                continue;

            LookPart? part = look.PartOn((LookLayer)i);
            layers[i].sprite = part.HasValue ? art.GetPhoto(part.Value.Key) : null;
            layers[i].enabled = part.HasValue;
        }
    }

    /// <summary>Empties every layer.</summary>
    public void Clear()
    {
        if (layers == null)
            return;

        foreach (Image layer in layers)
        {
            if (layer == null)
                continue;
            layer.sprite = null;
            layer.enabled = false;
        }
    }
}
