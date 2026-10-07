using UnityEngine;
using System.Linq;

/// <summary>
/// A traveller's look drawn with one SpriteRenderer per LookLayer (children
/// sorted in stack order inside a SortingGroup): the booth figure (full
/// sprites) and the paper's passport photo (crop sprites). A layer CharacterArt
/// has no art for (not even a stand-in) is switched off. The booth figure
/// takes a pose frame on each dialogue beat (SetPose, TravellerPose: an
/// instant swap of still frames, the moving set as one or not at all), and a
/// premade's whole picture shows the expression it says over its frame.
/// </summary>
public sealed class LookSpriteStack : MonoBehaviour
{
    /// <summary>One renderer per LookLayer (index = the layer), wired by Build Office UI.</summary>
    [SerializeField] private SpriteRenderer[] layers;

    /// <summary>True for a passport photo: each layer shows its photo crop (CharacterArt.GetPhoto).</summary>
    [SerializeField] private bool photo;

    /// <summary>The look shown, in its neutral frame.</summary>
    private TravellerLook _look;

    /// <summary>The look as drawn: in the current pose's frame (TravellerPose.Posed), or the look itself.</summary>
    private TravellerLook _shown;

    /// <summary>The current pose category (TravellerPose).</summary>
    private string _pose = TravellerPose.Neutral;

    /// <summary>The expression a premade says (null: neutral).</summary>
    private string _expression;

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

    /// <summary>Shows a look in its neutral frame (a layer with nothing to draw is off); a null look or art clears the stack.</summary>
    public void Show(TravellerLook look, CharacterArt art)
    {
        if (look == null || art == null)
        {
            Clear();
            return;
        }

        _look = look;
        _shown = look;
        _art = art;
        _pose = TravellerPose.Neutral;
        _expression = null;
        _poses = !photo ? Resources.Load<CharacterPoseLibrary>("CharacterPoseLibrary")?.Find(look) : null;
        Draw();
    }

    /// <summary>
    /// The figure takes <paramref name="category"/>'s frame (TravellerPose:
    /// neutral, explaining, thinking or objecting) at once and holds it until
    /// the next call: the moving set of layers swaps for its frame, or stays
    /// neutral as one when any of it is not drawn (TravellerPose.Posed, with
    /// only the current art set's own drawings). A look the art side drew as
    /// complete authored poses (CharacterPoseLibrary) shows its explaining and
    /// guarded figures for explaining and objecting. Nothing on a photo, with
    /// no look, or for the category already shown.
    /// </summary>
    public void SetPose(string category)
    {
        if (photo || _look == null || _art == null || category == _pose)
            return;

        _pose = category;
        _shown = TravellerPose.Posed(_look, category, _art.HasOwnArt);
        Draw();
    }

    /// <summary>A premade's whole picture shows an expression (blank or unknown = neutral, which is its pose frame; its neutral picture while that expression has no art); nothing for a generated traveller or no look.</summary>
    public void SetExpression(string expression)
    {
        if (_look == null || _look.PremadeId == null)
            return;

        _expression = expression;
        Draw();
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
        _shown = null;
        _art = null;
        _pose = TravellerPose.Neutral;
        _expression = null;
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

    /// <summary>Draws the shown look: an authored complete pose when the look has one for the pose, else each layer's part (a premade's said expression, when not neutral, over its frame).</summary>
    private void Draw()
    {
        CharacterPoseLibrary.Pose authored = _poses == null ? null
            : _pose == TravellerPose.Explaining ? _poses.explaining
            : _pose == TravellerPose.Objecting ? _poses.guarded
            : null;
        if (authored != null && authored.sprite != null)
        {
            DrawPose(authored);
            return;
        }

        if (_posedFigure != null) _posedFigure.enabled = false;
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (layers[i] == null)
                continue;

            LookPart? part = _shown.PartOn((LookLayer)i);
            layers[i].sprite = part.HasValue ? SpriteOf(part.Value.Key) : null;
            layers[i].enabled = layers[i].sprite != null;
        }

        bool acted = !string.IsNullOrEmpty(_expression) && _expression != LookKeys.NeutralExpression;
        if (!acted || _look.PremadeId == null || layers == null || layers.Length <= (int)LookLayer.Whole || layers[(int)LookLayer.Whole] == null)
            return;

        Sprite sprite = SpriteOf(_look.WholeKey(_expression));
        if (sprite != null)
        {
            layers[(int)LookLayer.Whole].sprite = sprite;
            layers[(int)LookLayer.Whole].enabled = true;
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
