using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A screen-space edge tab under the painted desk (desk v3; Saleh's rule:
/// everything diegetic, actions are in-world desk objects, never UI buttons;
/// his review of the day-1 still: "So many visual problems"): in the office
/// view (the reading view included) it draws nothing and takes no clicks, its
/// key still working through OfficeControls; with the PC open or the city
/// view on it shows again, as the way back its owner gives it (the PC tab's
/// DESK, the city's). Added by the office binder to the CITY, PC and STAMPS
/// tabs when the painted desk is installed (OfficeSceneBinder.HideScreenChrome);
/// the tabs stay their owners' (CAM, STAMP). A frame allocates nothing.
/// </summary>
public sealed class PaintedChrome : MonoBehaviour
{
    private Graphic[] _graphics = System.Array.Empty<Graphic>();
    private OfficeViewController _view;
    private CityView _city;
    private bool _shown = true, _bound;

    /// <summary>Hides the tab's graphics (every Graphic under it) in the office view of <paramref name="view"/>, showing them with the PC open or <paramref name="city"/> on.</summary>
    public void Bind(OfficeViewController view, CityView city)
    {
        _view = view;
        _city = city;
        _graphics = GetComponentsInChildren<Graphic>(true);
        _bound = true;
        Apply(Shown());
    }

    /// <summary>True when the tab shows: the PC open, or the city view on.</summary>
    private bool Shown() => (_view != null && _view.Current == OfficeView.MonitorFocus) || (_city != null && _city.IsOn);

    private void LateUpdate()
    {
        if (!_bound)
            return;
        bool shown = Shown();
        if (shown != _shown)
            Apply(shown);
    }

    private void Apply(bool shown)
    {
        _shown = shown;
        foreach (Graphic g in _graphics)
            if (g != null)
                g.enabled = shown;
    }
}
