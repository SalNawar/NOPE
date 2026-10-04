using UnityEngine;

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
    private MaterialPropertyBlock _poseBlock;
    private Vector4 _restPose, _gesture, _currentPose;
    private float _phase;

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

        _look = look;
        _art = art;
        if (!photo)
        {
            int seed=17;
            foreach(char letter in look.Describe()) seed=unchecked(seed*31+letter);
            int stance=(seed&int.MaxValue)%5;
            _phase=(seed&1023)*.01f;
            _restPose=stance switch
            {
                0=>new Vector4(-18,.16f,-.08f,-.045f),
                1=>new Vector4(16,-.10f,-.22f,.055f),
                2=>new Vector4(-8,.27f,-.20f,.025f),
                3=>new Vector4(12,.06f,-.30f,-.06f),
                _=>new Vector4(0,.12f,-.12f,.02f)
            };
            _gesture=Vector4.zero;
            _currentPose=_restPose;
            var material=Resources.Load<Material>("CharacterPose");
            if(material!=null) foreach(var layer in layers) if(layer!=null) layer.sharedMaterial=material;
            ApplyPose();
        }
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (layers[i] == null)
                continue;

            LookPart? part = look.PartOn((LookLayer)i);
            layers[i].sprite = part.HasValue ? SpriteOf(part.Value.Key) : null;
            layers[i].enabled = layers[i].sprite != null;
        }
    }

    /// <summary>A premade's whole picture changes to an expression (blank or unknown = neutral; its neutral picture while that expression has no art); nothing for a generated traveller or no look.</summary>
    public void SetExpression(string expression)
    {
        if(!photo)
            _gesture=expression switch
            {
                "happy"=>new Vector4(25,.18f,-.14f,.085f),
                "angry"=>new Vector4(-22,-.14f,.18f,-.085f),
                "worried"=>new Vector4(-18,.14f,-.16f,-.075f),
                _=>Vector4.zero
            };
        if(!photo && !Application.isPlaying) {_currentPose=_restPose+_gesture;ApplyPose();}
        if (_look == null || _look.PremadeId == null || layers == null || layers.Length <= (int)LookLayer.Whole || layers[(int)LookLayer.Whole] == null)
            return;

        Sprite sprite = SpriteOf(_look.WholeKey(expression));
        if (sprite != null)
            layers[(int)LookLayer.Whole].sprite = sprite;
    }

    private void LateUpdate()
    {
        if(photo || _look==null) return;
        _currentPose=Vector4.Lerp(_currentPose,_restPose+_gesture,1-Mathf.Exp(-Time.deltaTime*8));
        ApplyPose();
    }

    private void ApplyPose()
    {
        _poseBlock??=new MaterialPropertyBlock();
        var pose=_currentPose;
        if(Application.isPlaying && !MotionPreference.Reduced)
        {
            pose.x+=Mathf.Sin(Time.time*.9f+_phase)*3;
            pose.w+=Mathf.Sin(Time.time*.65f+_phase)*.012f;
        }
        foreach(var layer in layers)
        {
            if(layer==null) continue;
            layer.GetPropertyBlock(_poseBlock);
            _poseBlock.SetVector("_CharacterPose",pose);
            layer.SetPropertyBlock(_poseBlock);
        }
    }

    /// <summary>Tints every layer (the art is unlit: the tint sits it into the room's light).</summary>
    public void SetTint(Color tint)
    {
        if (layers == null)
            return;
        foreach (SpriteRenderer layer in layers)
            if (layer != null)
                layer.color = tint;
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

    /// <summary>The key's sprite (its own art or its stand-in's; null for none): its photo crop on a photo, else the full canvas.</summary>
    private Sprite SpriteOf(LookKey key) => photo ? _art.GetPhoto(key) : _art.Get(key);
}
