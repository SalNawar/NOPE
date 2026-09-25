using UnityEngine;

/// <summary>
/// A traveller's look drawn with one SpriteRenderer per LookLayer (children
/// sorted in stack order inside a SortingGroup): the booth figure (full
/// sprites) and the paper's passport photo (crop sprites). A premade's whole
/// picture can swap its expression.
/// </summary>
public sealed class LookSpriteStack : MonoBehaviour
{
    /// <summary>One renderer per LookLayer (index = the layer), wired by Build Office UI.</summary>
    [SerializeField] private SpriteRenderer[] layers;

    /// <summary>True for a passport photo: each layer shows its photo crop (CharacterArt.GetPhoto).</summary>
    [SerializeField] private bool photo;

    private TravellerLook _look;
    private CharacterArt _art;

    private void Awake()
    {
        if (layers == null || layers.Length != System.Enum.GetValues(typeof(LookLayer)).Length)
            Debug.LogWarning($"[LookSpriteStack] '{name}' has {(layers != null ? layers.Length : 0)} layer renderers, not one per LookLayer; rebuild the scene (Tools > TimeDesk > Build Office UI).", this);
        Clear();
    }

    /// <summary>Shows a look; a null look or art clears the stack.</summary>
    public void Show(TravellerLook look, CharacterArt art)
    {
        if (look == null || art == null)
        {
            Clear();
            return;
        }

        _look = look;
        _art = art;
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (layers[i] == null)
                continue;

            LookPart? part = look.PartOn((LookLayer)i);
            layers[i].sprite = part.HasValue ? SpriteOf(part.Value.Key) : null;
            layers[i].enabled = part.HasValue;
        }
    }

    /// <summary>A premade's whole picture changes to an expression (blank or unknown = neutral); nothing for a generated traveller or no look.</summary>
    public void SetExpression(string expression)
    {
        if (_look == null || _look.PremadeId == null || layers == null || layers.Length <= (int)LookLayer.Whole || layers[(int)LookLayer.Whole] == null)
            return;

        layers[(int)LookLayer.Whole].sprite = SpriteOf(_look.WholeKey(expression));
    }

    /// <summary>Empties every layer and forgets the look.</summary>
    public void Clear()
    {
        _look = null;
        _art = null;
        if (layers == null)
            return;

        foreach (SpriteRenderer layer in layers)
        {
            if (layer == null)
                continue;
            layer.sprite = null;
            layer.enabled = false;
        }
    }

    /// <summary>The key's sprite: its photo crop on a photo, else the full canvas.</summary>
    private Sprite SpriteOf(LookKey key) => photo ? _art.GetPhoto(key) : _art.Get(key);
}
