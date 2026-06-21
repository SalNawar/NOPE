using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimize / maximize / close behaviour for a desktop window. Attach to the
/// window root and wire the title-bar buttons. Close and Minimize both hide the
/// window (an icon re-opens it); Maximize toggles between the authored size and
/// (near) full-canvas. Pairs with <see cref="DraggableWindow"/> for dragging.
/// </summary>
public sealed class OSWindowChrome : MonoBehaviour
{
    /// <summary>The panel that is shown/hidden/resized (defaults to this object).</summary>
    [SerializeField] private RectTransform window;

    /// <summary>Hides the window (stow to taskbar / icon).</summary>
    [SerializeField] private Button minimizeButton;

    /// <summary>Toggles maximized / restored size.</summary>
    [SerializeField] private Button maximizeButton;

    /// <summary>Closes (hides) the window.</summary>
    [SerializeField] private Button closeButton;

    private Vector2 _restoreSize;
    private Vector2 _restoreMin;
    private Vector2 _restoreMax;
    private Vector2 _restorePos;
    private bool _maximized;

    private void Awake()
    {
        if (window == null)
            window = transform as RectTransform;

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(Minimize);
        if (maximizeButton != null)
            maximizeButton.onClick.AddListener(ToggleMaximize);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    /// <summary>Shows the window and brings it to the front.</summary>
    public void Open()
    {
        if (window == null)
            return;

        window.gameObject.SetActive(true);
        window.SetAsLastSibling();
    }

    /// <summary>Hides the window.</summary>
    public void Close()
    {
        if (window != null)
            window.gameObject.SetActive(false);
    }

    /// <summary>Hides the window (taskbar stow); same visual result as Close.</summary>
    public void Minimize() => Close();

    /// <summary>Toggles between the authored size and near-full-canvas.</summary>
    public void ToggleMaximize()
    {
        if (window == null)
            return;

        if (!_maximized)
        {
            _restoreMin = window.anchorMin;
            _restoreMax = window.anchorMax;
            _restoreSize = window.sizeDelta;
            _restorePos = window.anchoredPosition;

            window.anchorMin = new Vector2(0.05f, 0.08f);
            window.anchorMax = new Vector2(0.95f, 0.96f);
            window.offsetMin = Vector2.zero;
            window.offsetMax = Vector2.zero;
            _maximized = true;
        }
        else
        {
            window.anchorMin = _restoreMin;
            window.anchorMax = _restoreMax;
            window.sizeDelta = _restoreSize;
            window.anchoredPosition = _restorePos;
            _maximized = false;
        }

        window.SetAsLastSibling();
    }
}
