using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The cel UI kit in one asset (docs/UI_KIT.md; Assets/Data/UI/UiKit_Default.asset):
/// every kit sprite by its name (UiKitNames), the kit's fonts, its label inks
/// and how big its sprites draw on each canvas. The builders skin buttons and
/// panels from it (SceneUiKit.Skin, re-applied every build); a view that
/// changes a control's look at run time (an Orders card's state, the verdict
/// ribbon, a selected icon) asks it for the sprite with <see cref="Show"/>.
/// The sprite list is refreshed from the kit's manifest by the builders
/// (UiKitAssets); the fonts, inks and scales are designer knobs kept across
/// refreshes.
/// </summary>
[CreateAssetMenu(fileName = "UiKit_", menuName = "TimeDesk/UI/UI Kit", order = 21)]
public sealed class UiKitSO : ScriptableObject
{
    /// <summary>The child of a skinned control or panel that draws its kit sprite (SceneUiKit.Skin); a view finds it by this name to swap its piece.</summary>
    public const string FaceName = "KitFace";

    /// <summary>One role of the type scale (KitText): its share of its component's height, its floor and its ceiling (canvas units).</summary>
    [Serializable]
    public sealed class TextStyle
    {
        /// <summary>The role.</summary>
        public KitText kind;

        /// <summary>Its size as a share of its component's height (0: its ceiling, for the reading roles).</summary>
        [Range(0f, 1f)] public float share;

        /// <summary>The smallest it is ever drawn (a label that does not fit grows its plate instead).</summary>
        [Min(1f)] public float min = 14f;

        /// <summary>The largest it is drawn.</summary>
        [Min(1f)] public float max = 24f;
    }

    /// <summary>One kit sprite by its manifest name.</summary>
    [Serializable]
    public sealed class KitSprite
    {
        /// <summary>The sprite's name (its file name without the extension).</summary>
        public string name;

        /// <summary>The sprite.</summary>
        public Sprite sprite;
    }

    /// <summary>Plates, tabs and headings (BigShoulders Bold).</summary>
    public TMP_FontAsset labelFont;

    /// <summary>The phosphor readouts (GeistMono Bold).</summary>
    public TMP_FontAsset readoutFont;

    /// <summary>The morning paper's masthead (UnifrakturMaguntia).</summary>
    public TMP_FontAsset mastheadFont;

    /// <summary>Body lines on kit surfaces: bubbles, slips, notes (Outfit Regular).</summary>
    public TMP_FontAsset bodyFont;

    /// <summary>Bold body lines (Outfit Bold).</summary>
    public TMP_FontAsset bodyBoldFont;

    /// <summary>A label's ink on a dark face (bone #EEE5D0).</summary>
    public Color inkOnDark = new Color(0.933f, 0.898f, 0.816f, 1f);

    /// <summary>A label's ink on a light face (ink #2B1C24).</summary>
    public Color inkOnLight = new Color(0.169f, 0.11f, 0.141f, 1f);

    /// <summary>An alerting line's ink on a light face (Quit game; oxblood #8A2F3B).</summary>
    public Color inkAlert = new Color(0.541f, 0.184f, 0.231f, 1f);

    /// <summary>Signal red (#C23A2E): a large error or warning word on a light face (the Title's).</summary>
    public Color signalRed = new Color(0.761f, 0.227f, 0.18f, 1f);

    /// <summary>The phosphor readouts' ink (#CCFFD1).</summary>
    public Color phosphorInk = new Color(0.8f, 1f, 0.82f, 1f);

    /// <summary>The empty room round every sprite for its shadow and halo, in sprite pixels (the manifest's pad).</summary>
    public int spritePad = 28;

    /// <summary>Sprite pixels per canvas unit on the office overlay (the kit is exported at twice its design size: 2 draws it at its design size at 1080p).</summary>
    [Min(0.1f)] public float overlayScale = 2f;

    /// <summary>Sprite pixels per canvas unit on the PC desktop (1 desktop unit is 0.78 px at 1080p, so a smaller value keeps the chrome's ink as thick as the office's).</summary>
    [Min(0.1f)] public float desktopScale = 1.6f;

    /// <summary>The type scale (run 7, Saleh: "the text doesn't match the scale"): every kit text's size from its role and its component's height (KitTypeScale), the one place to tune it; a role missing here takes KitTypeScale.Defaults (the builders add it).</summary>
    public List<TextStyle> typeScale = new List<TextStyle>();

    /// <summary>The letter spacing of the kit's labels (TMP em/100: the sheets' tracking).</summary>
    public float labelTracking = 3f;

    /// <summary>Every kit sprite (refreshed from the manifest by the builders).</summary>
    public List<KitSprite> sprites = new List<KitSprite>();

    /// <summary>Sprites by name (built on first use).</summary>
    [NonSerialized] private Dictionary<string, Sprite> _byName;

    /// <summary>The sprite called <paramref name="spriteName"/>, or null.</summary>
    public Sprite Get(string spriteName)
    {
        if (_byName == null)
        {
            _byName = new Dictionary<string, Sprite>();
            foreach (KitSprite s in sprites)
                if (s != null && !string.IsNullOrEmpty(s.name) && s.sprite != null)
                    _byName[s.name] = s.sprite;
        }
        return spriteName != null && _byName.TryGetValue(spriteName, out Sprite sprite) ? sprite : null;
    }

    /// <summary>A stateful piece's sprite in <paramref name="state"/> (UiKitNames.Of), or null.</summary>
    public Sprite Get(string piece, KitState state) => Get(UiKitNames.Of(piece, state));

    /// <summary>A role's style: its entry in <see cref="typeScale"/>, else its default.</summary>
    public TextStyle Style(KitText kind)
    {
        foreach (TextStyle s in typeScale)
            if (s != null && s.kind == kind)
                return s;
        foreach ((KitText k, float share, float min, float max) in KitTypeScale.Defaults)
            if (k == kind)
                return new TextStyle { kind = k, share = share, min = min, max = max };
        return new TextStyle { kind = kind };
    }

    /// <summary>A role's size for a component <paramref name="height"/> units tall (KitTypeScale.Size), never under <paramref name="floor"/> (a canvas's reading floor).</summary>
    public float TextSize(KitText kind, float height, float floor = 0f)
    {
        TextStyle s = Style(kind);
        return KitTypeScale.Size(s.share, Mathf.Max(s.min, floor), Mathf.Max(s.max, floor), height);
    }

    /// <summary>A role's face: the readout face for readouts, the masthead's for mastheads, the body face for the reading roles, tooltips and fields, else the label face.</summary>
    public TMP_FontAsset Face(KitText kind)
    {
        switch (kind)
        {
            case KitText.Readout:
                return readoutFont;
            case KitText.Masthead:
                return mastheadFont;
            case KitText.Body:
            case KitText.BodySmall:
            case KitText.BodyLarge:
            case KitText.Tooltip:
            case KitText.Field:
                return bodyFont;
            default:
                return labelFont;
        }
    }

    /// <summary>A label's ink on <paramref name="sprite"/> (UiKitNames.DarkFace: bone on a dark face, ink on a light one).</summary>
    public Color InkOn(string sprite) => UiKitNames.DarkFace(sprite) ? inkOnDark : inkOnLight;

    /// <summary>
    /// Shows <paramref name="piece"/> on <paramref name="face"/>: a stateful
    /// piece at rest with its other states on <paramref name="control"/>'s
    /// sprite swap (when given), else the sprite of that name. Null-safe; a
    /// piece the kit lacks leaves the face as it is. The control gets its game
    /// feel (UiJuice.On): every kit control, whether a builder skins it
    /// (SceneUiKit.Skin) or a view builds it at run time, comes through here.
    /// </summary>
    public void Show(Image face, string piece, Selectable control = null)
    {
        if (face == null)
            return;
        Sprite rest = Get(piece, KitState.Rest) ?? Get(piece);
        if (rest == null)
            return;
        face.sprite = rest;
        if (control == null)
            return;
        Sprite hover = Get(piece, KitState.Hover);
        control.spriteState = new SpriteState
        {
            highlightedSprite = hover,
            selectedSprite = null,
            pressedSprite = Get(piece, KitState.Pressed) ?? hover,
            disabledSprite = Get(piece, KitState.Locked)
        };
        UiJuice.On(control);
    }

    /// <summary>Drops the cached lookup (the builders refresh the sprite list from the manifest).</summary>
    public void ClearCache() => _byName = null;

    /// <summary>Drops the cached lookup when the list changes.</summary>
    private void OnValidate() => _byName = null;

    /// <summary>Drops the cached lookup after a reload.</summary>
    private void OnEnable() => _byName = null;
}
