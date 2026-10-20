using UnityEngine;

/// <summary>
/// The painted hall's light from the one day cycle (docs/HALL_LIGHTING.md;
/// BUG-25): each painted layer (NOPE/Hall Deep Layout) gets the four states'
/// weights (HallBakedCycle, read off the rig's HallDayCycle knobs, so the
/// painting is night exactly when the lamps are) and the floor shadows' ray
/// in its property block; every scheduled fixture's lit share with its wall
/// glow and floor pool (the night's real light pools) and the night knobs
/// (tint, pool colour and strength, diffuser glow) are shader globals
/// (_Hall*), so anything drawn with the Deep Layout material (the hall
/// slots' sprites, HallSlotsLink) takes the same night as the painting
/// without copying them; and the art's daylight (the hall's 3D sun) is posed
/// and coloured for the hour without real-time shadows (BUG-14b: no sun
/// shadow inside the cubicle). Nothing gates on the layers' material: the
/// art's own material reference went with the four-state shader (BUG-73h).
/// The blocks and globals are written only when a value changed (BUG-65h:
/// the 58 blocks were rewritten every frame); the daylight is written every
/// frame, after the art presentation's own write (DefaultExecutionOrder 200 and
/// LateUpdate: the presentation writes in Update), so the art's second clock
/// has no visible effect (its deletion is an art request, docs/pending/LIGHT.md).
/// </summary>
[ExecuteAlways, DefaultExecutionOrder(200)]
public sealed class HallBakedLighting : MonoBehaviour
{
    /// <summary>The hall's lighting rig: its hour and knobs drive the states.</summary>
    [SerializeField] HallLightingRig rig;
    /// <summary>The painted hall whose layers get the state weights.</summary>
    [SerializeField] AnimeHallPresentation art;

    /// <summary>The most fixtures the painted layers take (the shader's arrays).</summary>
    private const int MaxFixtures = 16;

    /// <summary>The smallest change that rewrites the layers (half of one 8-bit step: never visible).</summary>
    private const float Step = 0.002f;

    MaterialPropertyBlock properties;
    HallLight[] fixtures;
    readonly float[] fixtureLevels = new float[MaxFixtures];
    readonly Vector4[] fixtureCenters = new Vector4[MaxFixtures];
    readonly Vector4[] fixturePools = new Vector4[MaxFixtures];
    int fixtureCount;
    bool shapesKnown;

    /// <summary>What the layers were last given (to write only on a change).</summary>
    Vector4 writtenWeights = new Vector4(-1f, -1f, -1f, -1f);
    readonly float[] writtenLevels = new float[MaxFixtures];
    float writtenAmount = -1f, writtenMotion = -1f;
    Color writtenNight, writtenPool;
    float writtenStrength = -1f, writtenGain = -1f;

    // Globals (HallDeepLayout.shader declares them outside its Properties, so no material value hides them).
    static readonly int FixtureLevelsId = Shader.PropertyToID("_HallFixtureLevels");
    static readonly int FixtureCountId = Shader.PropertyToID("_HallFixtureCount");
    static readonly int FixtureCentersId = Shader.PropertyToID("_HallFixtureCenters");
    static readonly int FixturePoolsId = Shader.PropertyToID("_HallFixturePools");
    static readonly int NightTintId = Shader.PropertyToID("_HallNightTint");
    static readonly int PoolColourId = Shader.PropertyToID("_HallPoolColour");
    static readonly int PoolStrengthId = Shader.PropertyToID("_HallPoolStrength");
    static readonly int EmissionGainId = Shader.PropertyToID("_HallEmissionGain");
    // Per layer (the painting's block, which HallSlotsLink copies onto the slots).
    static readonly int StateWeightsId = Shader.PropertyToID("_StateWeights");
    static readonly int ShadowRayId = Shader.PropertyToID("_HallShadowRay");
    static readonly int LightingAmountId = Shader.PropertyToID("_LightingAmount");
    static readonly int CloudMotionId = Shader.PropertyToID("_CloudMotion");
    static readonly int PaletteAmountId = Shader.PropertyToID("_PaletteAmount");

    /// <summary>A ceiling fixture's wall glow radii (uv; the four-state bake's).</summary>
    static readonly Vector2 CeilingGlow = new Vector2(.075f, .17f);
    /// <summary>A gallery lamp's wall glow radii (uv).</summary>
    static readonly Vector2 MountedGlow = new Vector2(.045f, .20f);

    void OnEnable() { shapesKnown = false; fixtures = null; writtenAmount = -1f; }

    void LateUpdate() => Apply();

    /// <summary>The daylight's direction for the states' shares (also the floor shadows' ray).</summary>
    public static Vector3 DaylightDirection(HallBakedCycle.Weights4 weights) =>
        new Vector3(1, -.55f, .65f) * weights.x + new Vector3(1, -1.5f, .08f) * weights.y + new Vector3(1, -.45f, -.65f) * weights.z + new Vector3(1, -.8f, .15f) * weights.w;

    static bool Differs(float a, float b) => Mathf.Abs(a - b) > Step;

    static Color Blend(HallBakedCycle.Weights4 w, Color a, Color b, Color c, Color d) => a * w.x + b * w.y + c * w.z + d * w.w;

    /// <summary>Writes the states to every painted layer and the fixtures and night knobs as globals when one changed, and poses the daylight.</summary>
    public void Apply()
    {
        if (rig == null || art == null || rig.Settings == null) return;
        HallLightingSO settings = rig.Settings;
        var weights = HallBakedCycle.Weights(rig.Hour, settings.Cycle);
        var direction = DaylightDirection(weights);
        if (fixtures == null || !Application.isPlaying)
        {
            // The rig's ceiling fixtures and the gallery lamps drawn among the art's layers: every fixture lights.
            var found = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Concat(
                rig.GetComponentsInChildren<HallLight>(true), art.GetComponentsInChildren<HallLight>(true))));
            fixtures = System.Array.FindAll(found, l => l.kind == HallLightKind.Fixture);
            System.Array.Sort(fixtures, (a, b) => string.CompareOrdinal(a.name, b.name));
            fixtureCount = System.Math.Min(fixtures.Length, MaxFixtures);
            shapesKnown = false;
        }

        bool changed = !shapesKnown || !Application.isPlaying || !settings.lightingOn;
        for (int i = 0; i < fixtureCount; i++)
        {
            fixtureLevels[i] = fixtures[i] != null && fixtures[i].gameObject.activeInHierarchy ? fixtures[i].Lit : 0;
            changed |= Differs(fixtureLevels[i], writtenLevels[i]);
        }
        var stateWeights = new Vector4(weights.x, weights.y, weights.z, weights.w);
        float amount = settings.lightingOn ? art.lightingAmount : 0;
        float motion = MotionPreference.Reduced ? 0 : 1;
        changed |= Differs(stateWeights.x, writtenWeights.x) || Differs(stateWeights.y, writtenWeights.y) || Differs(stateWeights.z, writtenWeights.z)
                || Differs(stateWeights.w, writtenWeights.w) || amount != writtenAmount || motion != writtenMotion
                || settings.hallNightTint != writtenNight || settings.fixtureColour != writtenPool
                || settings.hallPoolStrength != writtenStrength || settings.hallEmissionGain != writtenGain;

        if (changed)
        {
            WriteGlobals(settings);
            WriteLayers(stateWeights, direction, amount, motion);
        }

        if (art.daylight != null)
        {
            art.daylight.color = Blend(weights, new Color(1, .88f, .7f), new Color(1, .97f, .91f), new Color(1, .66f, .4f), new Color(.47f, .58f, .87f));
            art.daylight.intensity = weights.x * .58f + weights.y * .7f + weights.z * .38f + weights.w * .12f;
            art.daylight.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            // In play only: the art scene keeps its authored value if it is saved from the editor.
            if (Application.isPlaying && art.daylight.shadows != LightShadows.None)
                art.daylight.shadows = LightShadows.None;
        }
    }

    /// <summary>
    /// The fixtures (each one's lit share, place, wall glow and floor pool)
    /// and the night knobs, as globals: the painting's layers and the slots
    /// drawn with its material read the same values. The fixtures' places are
    /// measured on the first layer with a sprite (the layers share one
    /// registered canvas), once per gather (in edit mode every time).
    /// </summary>
    void WriteGlobals(HallLightingSO settings)
    {
        if (!shapesKnown)
            foreach (var layer in art.layers)
                if (layer.renderer != null && layer.renderer.sprite != null)
                {
                    Shapes(layer.renderer, layer.renderer.sprite);
                    break;
                }
        Shader.SetGlobalFloatArray(FixtureLevelsId, fixtureLevels);
        Shader.SetGlobalFloat(FixtureCountId, fixtureCount);
        Shader.SetGlobalVectorArray(FixtureCentersId, fixtureCenters);
        Shader.SetGlobalVectorArray(FixturePoolsId, fixturePools);
        // Alpha 1 marks the knobs as published (until then the shader keeps its built-in night, the knob's default).
        Color night = settings.hallNightTint;
        Shader.SetGlobalColor(NightTintId, new Color(night.r, night.g, night.b, 1f));
        Shader.SetGlobalColor(PoolColourId, settings.fixtureColour);
        Shader.SetGlobalFloat(PoolStrengthId, settings.hallPoolStrength);
        Shader.SetGlobalFloat(EmissionGainId, settings.hallEmissionGain);
        shapesKnown = true;
        writtenNight = settings.hallNightTint;
        writtenPool = settings.fixtureColour;
        writtenStrength = settings.hallPoolStrength;
        writtenGain = settings.hallEmissionGain;
        System.Array.Copy(fixtureLevels, writtenLevels, MaxFixtures);
    }

    /// <summary>Writes every painted layer's block: the states, the floor shadows' ray, the lighting amount and the cloud motion.</summary>
    void WriteLayers(Vector4 stateWeights, Vector3 direction, float amount, float motion)
    {
        properties ??= new MaterialPropertyBlock();
        var ray = new Vector4(direction.x / -direction.y, direction.z / -direction.y, 0, 0);
        foreach (var layer in art.layers)
        {
            if (layer.renderer == null) continue;
            layer.renderer.GetPropertyBlock(properties);
            properties.SetVector(StateWeightsId, stateWeights);
            properties.SetVector(ShadowRayId, ray);
            properties.SetFloat(LightingAmountId, amount);
            properties.SetFloat(CloudMotionId, motion);
            properties.SetFloat(PaletteAmountId, 1);
            layer.renderer.SetPropertyBlock(properties);
        }
        writtenWeights = stateWeights;
        writtenAmount = amount;
        writtenMotion = motion;
    }

    /// <summary>
    /// Each fixture's place on the painting (uv) with its wall glow's radii,
    /// and its floor pool: a gallery lamp's authored one
    /// (HallMountedFixture.floorPool), a ceiling fixture's on the floor below
    /// it (the four-state bake's placement). The layers share one registered canvas.
    /// </summary>
    void Shapes(SpriteRenderer layer, Sprite sprite)
    {
        for (int i = 0; i < fixtureCount; i++)
        {
            if (fixtures[i] == null) continue;
            var local = layer.transform.InverseTransformPoint(fixtures[i].transform.position);
            float u = (local.x * sprite.pixelsPerUnit + sprite.pivot.x) / sprite.rect.width;
            float v = (local.y * sprite.pixelsPerUnit + sprite.pivot.y) / sprite.rect.height;
            var mounted = fixtures[i].GetComponent<HallMountedFixture>();
            Vector2 glow = mounted != null ? MountedGlow : CeilingGlow;
            fixtureCenters[i] = new Vector4(u, v, glow.x, glow.y);
            fixturePools[i] = mounted != null ? mounted.floorPool : new Vector4(u, .2f + (.95f - v) * 1.45f, .11f, .17f);
        }
    }
}
