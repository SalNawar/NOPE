using TMPro;
using UnityEngine;

/// <summary>
/// The office case HUD (piece 10 X12), top centre of the office overlay:
/// the claim tag (the PC's claim banner text: the traveller's name, role and
/// claim, always readable) and, under it, the office compare strip
/// (CompareController draws it). It shows while a traveller is at the desk
/// and the PC frame is closed (BoothRules.CaseHudVisible; the PC shows its own
/// banner and bar then).
/// </summary>
public sealed class OfficeCaseHud : MonoBehaviour
{
    /// <summary>The HUD's root (the claim tag and the compare strip), shown while visible.</summary>
    [SerializeField] private GameObject root;

    /// <summary>The claim tag, shown while it has a claim.</summary>
    [SerializeField] private GameObject claimRoot;

    /// <summary>The claim tag's text.</summary>
    [SerializeField] private TMP_Text claimText;

    private void Awake()
    {
        SetClaim(string.Empty);
        SetVisible(false);
    }

    /// <summary>The claim tag's text (the formatted claim banner); an empty claim hides the tag.</summary>
    public void SetClaim(string claim)
    {
        if (claimText != null)
            claimText.text = claim ?? string.Empty;
        if (claimRoot != null)
            claimRoot.SetActive(!string.IsNullOrEmpty(claim));
    }

    /// <summary>Shows or hides the HUD (BoothCoordinator: BoothRules.CaseHudVisible).</summary>
    public void SetVisible(bool visible)
    {
        if (root != null && root.activeSelf != visible)
            root.SetActive(visible);
    }
}
