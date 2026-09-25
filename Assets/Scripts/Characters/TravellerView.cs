using UnityEngine;

/// <summary>
/// The traveller in the booth: shown from presentation until the decision
/// (GameManager.SetTravellerAtDesk), hidden otherwise. Its anchor (the
/// traveller's chest) is the one point the traveller wheel's ring and the
/// reply bubble centre on. Piece 4's layered figure extends this component.
/// </summary>
public sealed class TravellerView : MonoBehaviour
{
    /// <summary>What shows while the traveller is at the desk (the placeholder sprite now; piece 4's layers).</summary>
    [SerializeField] private Renderer[] figure;

    /// <summary>Where the wheel and the bubble centre (the traveller's chest).</summary>
    [SerializeField] private Transform anchor;

    /// <summary>Where the wheel and the bubble centre.</summary>
    public Transform Anchor => anchor;

    private void Awake() => Clear();

    /// <summary>Shows the traveller (presentation).</summary>
    public void Show() => SetFigureVisible(true);

    /// <summary>Hides the traveller (the decision, or no traveller yet).</summary>
    public void Clear() => SetFigureVisible(false);

    private void SetFigureVisible(bool visible)
    {
        if (figure == null)
            return;

        foreach (Renderer part in figure)
            if (part != null)
                part.enabled = visible;
    }
}
