using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>What a light in the anime hall stands for, which says how the day-night cycle drives it (HallLightingRig). Serialized: append only.</summary>
public enum HallLightKind
{
    /// <summary>The whole hall's light (a global Light2D): the day's colour and intensity.</summary>
    Global = 0,

    /// <summary>A sun shaft through a window (an additive Light2D): turns and lengthens with the sun, none at night.</summary>
    Window = 1,

    /// <summary>An interior ceiling fixture: switches on at dusk in its order (staggered), off at dawn.</summary>
    Fixture = 2,

    /// <summary>A screen (the Departure Board, the vending machine, a small display): always on, fainter by day.</summary>
    Screen = 3,

    /// <summary>A lit sign (a department door's panel, an alarm lamp): always on, fainter by day.</summary>
    Sign = 4,

    /// <summary>The light around a portal ring (its order is the portal's number): on while the ring is open, off while it is closed.</summary>
    Portal = 5,

    /// <summary>The desk lamp (a 3D light on the preserved desk): always on, fainter by day.</summary>
    DeskLamp = 6,

    /// <summary>The PC screen's glow on the desk (a 3D light): always on, fainter by day.</summary>
    DeskScreen = 7,

    /// <summary>The sky seen through the windows (a global Light2D on the HallSky sorting layer): the day's sky colour and brightness; no interior light reaches it.</summary>
    Sky = 8,
}

/// <summary>
/// One light of the anime hall (docs/HALL_LIGHTING.md): its kind and its
/// strength at full share, which HallLightingRig scales by the time of day
/// (so the Light2D's or Light's own intensity is written by the rig: tune this
/// one). The screens', signs' and desk lights' colour is their light's own;
/// the rig colours the global and sky lights, the shafts, the fixtures and the
/// portal lights. Added by Tools > TimeDesk > Add Anime Hall Hooks beside a Light2D
/// (the hall) or a Light (the preserved 3D desk).
/// </summary>
[DisallowMultipleComponent]
public sealed class HallLight : MonoBehaviour
{
    /// <summary>What the light stands for.</summary>
    public HallLightKind kind;

    /// <summary>The light's intensity at full share (the rig writes the light's own intensity from it).</summary>
    [Min(0f)] public float intensity = 1f;

    /// <summary>A fixture's place in the dusk's switching order (0 first); a portal light's portal number (1 to 5).</summary>
    public int order;

    /// <summary>The hall's 2D light beside it (null for a desk light).</summary>
    public Light2D Light2D { get; private set; }

    /// <summary>The desk's 3D light beside it (null for a hall light).</summary>
    public Light Light3D { get; private set; }

    /// <summary>Seconds since a fixture started to switch on (negative while it is off); the rig keeps it.</summary>
    internal float SinceOn = -1f;

    /// <summary>A portal light's current share (0 to 1), eased towards its ring's state by the rig.</summary>
    internal float Share;

    /// <summary>A fixture's lit share this frame as the rig set it (its schedule level from `fixtureOffShare` to 1, times the strike flicker; 0 while the rig's lighting is off): the painted diffusers glow by it (HallMountedFixture, HallBakedLighting).</summary>
    internal float Lit;

    /// <summary>Finds the light beside it (the rig calls it once when it gathers its lights).</summary>
    internal void Bind()
    {
        Light2D = GetComponent<Light2D>();
        Light3D = GetComponent<Light>();
    }
}
