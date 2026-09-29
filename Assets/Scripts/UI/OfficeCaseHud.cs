using UnityEngine;

/// <summary>
/// The office case HUD (piece 10 X12), top centre of the office overlay: the
/// office compare strip (CompareController draws it). It prints no claim: the
/// traveller says it (the personalities spec's B1-B2; the claim tag went and
/// the strip took its place). It shows while a traveller is at the desk and
/// the PC frame is closed (BoothRules.CaseHudVisible; the PC shows its own
/// bar then).
/// </summary>
public sealed class OfficeCaseHud : MonoBehaviour
{
    /// <summary>The HUD's root (the compare strip), shown while visible.</summary>
    [SerializeField] private GameObject root;

    private void Awake()
    {
        SetVisible(false);
    }

    /// <summary>Shows or hides the HUD (BoothCoordinator: BoothRules.CaseHudVisible).</summary>
    public void SetVisible(bool visible)
    {
        if (root != null && root.activeSelf != visible)
            root.SetActive(visible);
    }
}
