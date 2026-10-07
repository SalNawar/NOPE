using UnityEngine;
using System.Linq;

/// <summary>
/// A traveller's look drawn with one SpriteRenderer per LookLayer (children
/// sorted in stack order inside a SortingGroup): the booth figure (full
/// sprites) and the paper's passport photo (crop sprites). A layer CharacterArt
/// has no art for (not even a stand-in) is switched off. A premade's whole
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
    private CharacterPoseLibrary.Entry _poses;
    private SpriteRenderer _posedFigure;

    /// <summary>Checks the wiring and starts empty, unless a look was shown before the first activation (the desk paper's photo slot is built inactive and shown as it wakes; audit R2-024).</summary>
    private void Awake()
    {
        if (layers == null || layers.Length != System.Enum.GetValues(typeof(LookLayer)).Length)
            Debug.LogWarning($"[LookSpriteStack] '{name}' has {(layers != null ? layers.Length : 0)} layer renderers, not one per LookLayer; rebuild the scene (Tools > TimeDesk > Build Office UI).", this);
        if (_look == null)
            Clear();
    }

    /// <summary>Shows a look (a layer with nothing to draw is off); a null look or art clears the stack.</summary>
    public void Show(TravellerLook look, CharacterArt art)
    {
        if (look == null || art == null)
        {
            Clear();
            return;
        }

        if (_posedFigure != null) _posedFigure.enabled = false;
        _look = look;
        _art = art;
        _poses = !photo ? Resources.Load<CharacterPoseLibrary>("CharacterPoseLibrary")?.Find(look) : null;
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (layers[i] == null)
                continue;

            LookPart? part = look.PartOn((LookLayer)i);
            layers[i].sprite = part.HasValue ? SpriteOf(part.Value.Key) : null;
            layers[i].enabled = layers[i].sprite != null;
        }
        if (_poses != null) DrawPose(_poses.explaining);
    }

    /// <summary>A premade's whole picture changes to an expression (blank or unknown = neutral; its neutral picture while that expression has no art); nothing for a generated traveller or no look.</summary>
    public void SetExpression(string expression)
    {
        if (_poses != null) DrawPose(expression == "angry" || expression == "worried" ? _poses.guarded : _poses.explaining);
        if (_look == null || _look.PremadeId == null || layers == null || layers.Length <= (int)LookLayer.Whole || layers[(int)LookLayer.Whole] == null)
            return;

        Sprite sprite = SpriteOf(_look.WholeKey(expression));
        if (sprite != null)
            layers[(int)LookLayer.Whole].sprite = sprite;
    }

    /// <summary>Tints every layer (the art is unlit: the tint sits it into the room's light).</summary>
    public void SetTint(Color tint)
    {
        if (_posedFigure != null) _posedFigure.color = tint;
        if (layers == null)
            return;
        foreach (SpriteRenderer layer in layers)
            if (layer != null)
                layer.color = tint;
    }

    /// <summary>Empties every layer and forgets the look.</summary>
    public void Clear()
    {
        _poses = null;
        if (_posedFigure != null) {_posedFigure.enabled=false;_posedFigure.sprite=null;}
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

    private void DrawPose(CharacterPoseLibrary.Pose pose)
    {
        if (pose == null || pose.sprite == null) return;
        if (_posedFigure == null)
        {
            var child = new GameObject("Complete authored pose");
            child.transform.SetParent(transform, false);
            _posedFigure = child.AddComponent<SpriteRenderer>();
            var reference = layers.FirstOrDefault(r => r != null);
            child.layer = reference.gameObject.layer;
            _posedFigure.sortingLayerID = reference.sortingLayerID;
            _posedFigure.sortingOrder = (int)LookLayer.Whole;
            _posedFigure.sharedMaterial = reference.sharedMaterial;
        }
        _posedFigure.sprite = pose.sprite;
        _posedFigure.color = layers.First(r => r != null).color;
        _posedFigure.transform.localScale = Vector3.one * pose.scale;
        _posedFigure.transform.localPosition = new Vector3(0, pose.footOffset, 0);
        _posedFigure.enabled = true;
        foreach (var layer in layers) if (layer != null) layer.enabled = false;
    }

    /// <summary>The key's sprite (its own art or its stand-in's; null for none): its photo crop on a photo, else the full canvas.</summary>
    private Sprite SpriteOf(LookKey key) => photo ? _art.GetPhoto(key) : _art.Get(key);
}
