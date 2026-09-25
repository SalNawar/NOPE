using UnityEngine;

/// <summary>
/// The traveller in the booth: the layered figure, shown from presentation
/// until the decision (GameManager.SetTravellerAtDesk), empty otherwise. Its
/// anchor (the figure's shoulders) is the one point the traveller wheel's ring
/// and the reply bubble centre on; its hit zone (a child outside the figure's
/// sorting group) opens the wheel.
/// </summary>
public sealed class TravellerView : MonoBehaviour
{
    /// <summary>The layered figure (Figure child: a SortingGroup with one renderer per LookLayer).</summary>
    [SerializeField] private LookSpriteStack figure;

    /// <summary>Where the wheel and the bubble centre (the traveller's shoulders).</summary>
    [SerializeField] private Transform anchor;

    /// <summary>Where the wheel and the bubble centre.</summary>
    public Transform Anchor => anchor;

    private void Awake() => Clear();

    /// <summary>Shows the traveller's look (presentation); a null look or art shows nobody.</summary>
    public void Show(TravellerLook look, CharacterArt art)
    {
        if (figure != null)
            figure.Show(look, art);
    }

    /// <summary>A premade's picture changes to what they say (the expression of their last line).</summary>
    public void SetExpression(string expression)
    {
        if (figure != null)
            figure.SetExpression(expression);
    }

    /// <summary>Hides the traveller (the decision, or no traveller yet).</summary>
    public void Clear()
    {
        if (figure != null)
            figure.Clear();
    }
}
