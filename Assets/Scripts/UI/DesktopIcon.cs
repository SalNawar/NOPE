using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A desktop icon that opens its window when clicked. Optionally gated behind an
/// upgrade: when <see cref="requiredUpgradeId"/> is set and the player has not
/// unlocked it (WorldState.HasUpgrade), the icon is non-interactable and dimmed.
/// </summary>
public sealed class DesktopIcon : MonoBehaviour
{
    /// <summary>Window this icon opens / focuses.</summary>
    [SerializeField] private OSWindowChrome targetWindow;

    /// <summary>Button on the icon (defaults to this object's Button).</summary>
    [SerializeField] private Button button;

    /// <summary>Upgrade id required to enable this icon; empty = always available.</summary>
    [SerializeField] private string requiredUpgradeId;

    /// <summary>Dims the icon while it is locked.</summary>
    [SerializeField] private CanvasGroup canvasGroup;

    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(Open);

        RefreshLock();
    }

    /// <summary>Re-evaluates the unlock state and updates interactability/dimming.</summary>
    public void RefreshLock()
    {
        bool unlocked =
            string.IsNullOrEmpty(requiredUpgradeId) ||
            (RunManager.HasInstance &&
             RunManager.Instance.World != null &&
             RunManager.Instance.World.HasUpgrade(requiredUpgradeId));

        if (button != null)
            button.interactable = unlocked;

        if (canvasGroup != null)
            canvasGroup.alpha = unlocked ? 1f : 0.4f;
    }

    /// <summary>Opens the target window.</summary>
    public void Open()
    {
        if (targetWindow != null)
            targetWindow.Open();
    }
}
