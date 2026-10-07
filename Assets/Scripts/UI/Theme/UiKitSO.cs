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

    /// <summary>The phosphor readouts' ink (#CCFFD1).</summary>
    public Color phosphorInk = new Color(0.8f, 1f, 0.82f, 1f);

    /// <summary>The red of the paper's and the slip's headings (signal red #C23A2E).</summary>
    public Color signalRed = new Color(0.761f, 0.227f, 0.18f, 1f);

    /// <summary>The empty room round every sprite for its shadow and halo, in sprite pixels (the manifest's pad).</summary>
    public int spritePad = 28;

    /// <summary>Sprite pixels per canvas unit on the office overlay (the kit is exported at twice its design size: 2 draws it at its design size at 1080p).</summary>
    [Min(0.1f)] public float overlayScale = 2f;

    /// <summary>Sprite pixels per canvas unit on the PC desktop (1 desktop unit is 0.78 px at 1080p, so a smaller value keeps the chrome's ink as thick as the office's).</summary>
    [Min(0.1f)] public float desktopScale = 1.6f;

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
