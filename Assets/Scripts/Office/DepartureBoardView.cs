using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// The hall's Departure Board (the portals spec v3 BD1-BD5): the day's portal
/// rows drawn by the gameplay layer on the art's blank display (a world-space
/// TextMeshPro on the Gameplay sorting layer, auto-sized so every row is the
/// same size, in DeskConfigSO's board ink with the state words in its state
/// ink), fitted to the display's rect inset by the config's share on each side
/// and re-fitted when the display moves (the art may pan); and its click box
/// on the Interactable layer over the display, whose reaction's tooltip lists
/// the same portals in UI text with each ring's name (PortalText). The rows
/// and the tooltip read the shift's day-start PortalDay (GameManager.Portals),
/// the one the rings and the Portals app read. The office binder binds it to
/// the DepartureBoard anchor (an Anchor_DepartureBoard marker over the
/// display, else the display layer's opaque rect); without one (the 3D room)
/// it stays hidden.
/// </summary>
public sealed class DepartureBoardView : MonoBehaviour
{
    /// <summary>The rows' text.</summary>
    [SerializeField] private TextMeshPro rows;

    /// <summary>The click box over the display (its Clickable and DeskReaction are on the same object).</summary>
    [SerializeField] private BoxCollider box;

    /// <summary>The click box's reaction (its tooltip shows the portals' lines).</summary>
    [SerializeField] private DeskReaction reaction;

    /// <summary>Where the tooltip hangs (moved to the display's bottom centre: the board is high in the view, so the tooltip opens under it).</summary>
    [SerializeField] private Transform tooltipPoint;

    /// <summary>The board's inks and inset.</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The shift, whose day-start portals the board prints.</summary>
    [SerializeField] private GameManager game;

    /// <summary>How far in front of the display the rows sit (metres), so they never fight it for depth.</summary>
    private const float Lift = 0.002f;

    /// <summary>The click box's depth (metres).</summary>
    private const float BoxDepth = 0.05f;

    private Transform _frame;
    private Rect _rect;
    private Bounds _fitted;
    private PortalDay _shown;
    private string _tooltip = string.Empty;

    /// <summary>Binds the board to the display: <paramref name="rect"/> in <paramref name="frame"/>'s space (OfficeAnchors.TryLocalRect); the click box outlines <paramref name="outline"/> on hover.</summary>
    public void Bind(Transform frame, Rect rect, Renderer[] outline)
    {
        _frame = frame;
        _rect = rect;
        _fitted = default;
        _shown = null;
        if (box != null && box.TryGetComponent(out Clickable click))
            click.SetOutline(outline);
        if (reaction != null)
        {
            reaction.SetReadout(() => _tooltip);
            reaction.SetTooltipPoint(tooltipPoint);
        }
        gameObject.SetActive(true);
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (_frame == null || config == null)
            return;

        PortalDay day = game != null ? game.Portals : PortalDay.None;
        if (!ReferenceEquals(day, _shown))
            Write(day);

        Bounds display = OfficeAnchors.WorldBounds(_frame, _rect);
        if (display == _fitted)
            return;
        _fitted = display;
        Fit(display);
    }

    /// <summary>The rows and the tooltip's text from <paramref name="day"/>.</summary>
    private void Write(PortalDay day)
    {
        _shown = day;
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        string state = ColorUtility.ToHtmlStringRGBA(config.hallBoardStateInk);
        var text = new StringBuilder();
        foreach (BoardRow row in PortalText.BoardRows(day, p => PortalsWindow.EraName(library, p), p => PortalsWindow.PlaceName(library, p), UiText.Get))
        {
            if (text.Length > 0)
                text.Append('\n');
            text.Append(row.Text);
            if (row.State != null)
                text.Append("  <color=#").Append(state).Append('>').Append(row.State).Append("</color>");
        }
        if (rows != null)
        {
            rows.color = config.hallBoardInk;
            rows.text = text.ToString();
        }
        _tooltip = string.Join("\n", PortalText.TooltipLines(day, p => PortalsWindow.EraName(library, p), p => PortalsWindow.PlaceName(library, p), UiText.Get));
    }

    /// <summary>The rows inside the display (inset), the click box over it and the tooltip point at its bottom.</summary>
    private void Fit(Bounds display)
    {
        Quaternion facing = _frame.rotation;
        Vector3 toward = facing * Vector3.back * Lift;
        if (rows != null)
        {
            rows.transform.SetPositionAndRotation(display.center + toward, facing);
            float keep = 1f - 2f * config.hallBoardInset;
            ((RectTransform)rows.transform).sizeDelta = new Vector2(display.size.x * keep, display.size.y * keep);
        }
        if (box != null)
        {
            box.transform.SetPositionAndRotation(display.center, facing);
            box.center = Vector3.zero;
            box.size = new Vector3(display.size.x, display.size.y, BoxDepth);
        }
        if (tooltipPoint != null)
            tooltipPoint.position = new Vector3(display.center.x, display.min.y, display.center.z);
    }
}
