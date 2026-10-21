using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The hall's windows look out on the r17 city (CITY-R17). Added at runtime to the hall's
/// <see cref="HallCityExterior"/> by <see cref="CityR17.SetHall"/>, so HALL-FX's files are untouched: every window
/// and panel renderer drawing <c>NOPE/Hall Living City</c> gets a material on <c>NOPE/City R17 Window</c> that keeps
/// its apertures (<c>_Masks</c>, <c>_Region</c>) and samples the city's render texture at the sprite's uv (each
/// window group mapped to its own plate rect: the config's boxes), and the old painted flyers
/// (<see cref="HallCityExterior.Lane.vehicle"/>) are hidden. Disabling or destroying it puts everything back.
/// </summary>
[DisallowMultipleComponent]
public sealed class CityR17Windows : MonoBehaviour
{
    private const string LivingCityShader = "NOPE/Hall Living City";
    private static readonly int MasksId = Shader.PropertyToID("_Masks");
    private static readonly int RegionId = Shader.PropertyToID("_Region");
    private static readonly int LeftBoxId = Shader.PropertyToID("_LeftBox");
    private static readonly int LeftPlateId = Shader.PropertyToID("_LeftPlate");
    private static readonly int FrontBoxId = Shader.PropertyToID("_FrontBox");
    private static readonly int FrontPlateId = Shader.PropertyToID("_FrontPlate");
    private static readonly int HourTintId = Shader.PropertyToID("_HourTint");

    private struct Swap
    {
        public Renderer Renderer;
        public Material Original, City;
    }

    private readonly List<Swap> _swaps = new List<Swap>();
    private readonly List<SpriteRenderer> _flyers = new List<SpriteRenderer>();
    private HallCityExterior _exterior;
    private CityR17ConfigSO _config;
    private bool _applied;

    /// <summary>True while any swapped window is on screen (the city then needs drawing).</summary>
    public bool Visible
    {
        get
        {
            if (!_applied || !isActiveAndEnabled)
                return false;
            for (int i = 0; i < _swaps.Count; i++)
                if (_swaps[i].Renderer != null && _swaps[i].Renderer.isVisible)
                    return true;
            return false;
        }
    }

    /// <summary>How many renderers show the city (0: the hall had no living-city windows).</summary>
    public int Count => _swaps.Count;

    /// <summary>Installs (or finds) the windows on an exterior.</summary>
    public static CityR17Windows Install(HallCityExterior exterior, CityR17ConfigSO config)
    {
        if (exterior == null || config == null || !config.windowCity || config.windowShader == null)
            return null;
        CityR17Windows w = exterior.GetComponent<CityR17Windows>();
        if (w == null)
            w = exterior.gameObject.AddComponent<CityR17Windows>();
        w._exterior = exterior;
        w._config = config;
        w.Apply();
        return w;
    }

    private void OnEnable()
    {
        if (_exterior != null && _config != null)
            Apply();
    }

    private void OnDisable() => Restore();

    private void OnDestroy() => Restore();

    private void Apply()
    {
        if (_applied || _exterior == null)
            return;
        _swaps.Clear();
        var seen = new HashSet<Renderer>();
        if (_exterior.window != null)
            Consider(_exterior.window, seen);
        if (_exterior.panelRenderers != null)
            foreach (SpriteRenderer r in _exterior.panelRenderers)
                Consider(r, seen);
        _flyers.Clear();
        if (_exterior.lanes != null)
            foreach (HallCityExterior.Lane lane in _exterior.lanes)
                if (lane != null && lane.vehicle != null && lane.vehicle.enabled)
                {
                    lane.vehicle.enabled = false;
                    _flyers.Add(lane.vehicle);
                }
        _applied = true;
    }

    private void Consider(Renderer r, HashSet<Renderer> seen)
    {
        if (r == null || !seen.Add(r))
            return;
        Material original = r.sharedMaterial;
        if (original == null || original.shader == null || original.shader.name != LivingCityShader)
            return;
        var city = new Material(_config.windowShader) { name = original.name + " (city r17)", hideFlags = HideFlags.DontSave };
        if (original.HasProperty(MasksId))
            city.SetTexture(MasksId, original.GetTexture(MasksId));
        if (original.HasProperty(RegionId))
            city.SetFloat(RegionId, original.GetFloat(RegionId));
        city.SetVector(LeftBoxId, _config.leftWindowsBox);
        city.SetVector(LeftPlateId, _config.leftWindowsPlate);
        city.SetVector(FrontBoxId, _config.frontWindowsBox);
        city.SetVector(FrontPlateId, _config.frontWindowsPlate);
        city.SetFloat(HourTintId, _config.windowHourTint);
        city.renderQueue = original.renderQueue;
        r.sharedMaterial = city;
        _swaps.Add(new Swap { Renderer = r, Original = original, City = city });
    }

    private void Restore()
    {
        if (!_applied)
            return;
        foreach (Swap s in _swaps)
        {
            if (s.Renderer != null && s.Renderer.sharedMaterial == s.City)
                s.Renderer.sharedMaterial = s.Original;
            if (s.City != null)
                Destroy(s.City);
        }
        _swaps.Clear();
        foreach (SpriteRenderer f in _flyers)
            if (f != null)
                f.enabled = true;
        _flyers.Clear();
        _applied = false;
    }
}
