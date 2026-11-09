using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The cheat menu's screen layer (RUN-SAFE): the badge a cheated run shows
/// at the screen's bottom left, "CHEATS ON" with every run switch and waiting
/// next-generation switch that is on, named loudly (DevToolsState.StickyLine;
/// BUG-27a: a sticky switch is never silent), and the blocker that takes
/// every click while the cheat panel is open, so nothing behind it (the desk,
/// the PC, Home, the Title) is clicked through it (BUG-68a; the panel itself
/// is IMGUI and draws above it). It is uGUI, so a closed panel costs no
/// OnGUI pass (BUG-65b), and the badge's line is rebuilt only when what it
/// shows changes (DevToolsState.StickyKey), so it allocates nothing per
/// frame. DebugPanelController adds it to the persistent RunManager object.
/// </summary>
public sealed class CheatBadge : MonoBehaviour
{
    /// <summary>The canvas sorting order: above the game's screens, under the IMGUI panel (IMGUI always draws last).</summary>
    private const int SortingOrder = 31000;

    /// <summary>The blocker: a clear full-screen graphic that takes every click while the panel is open.</summary>
    private GameObject _blocker;

    /// <summary>The badge's line.</summary>
    private TMP_Text _badge;

    /// <summary>The key the badge's line was built for (DevToolsState.StickyKey); a new key rebuilds it.</summary>
    private int _shownKey = int.MinValue;

    /// <summary>Builds the canvas: the blocker (off) and the badge.</summary>
    private void Awake()
    {
        var root = new GameObject("CheatLayer", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _blocker = new GameObject("InputBlocker", typeof(RectTransform), typeof(Image));
        _blocker.transform.SetParent(root.transform, false);
        var blockRect = (RectTransform)_blocker.transform;
        blockRect.anchorMin = Vector2.zero;
        blockRect.anchorMax = Vector2.one;
        blockRect.offsetMin = blockRect.offsetMax = Vector2.zero;
        var block = _blocker.GetComponent<Image>();
        block.color = Color.clear;
        block.raycastTarget = true;
        _blocker.SetActive(false);

        var label = new GameObject("Badge", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        var rect = (RectTransform)label.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(12f, 8f);
        rect.sizeDelta = new Vector2(1400f, 30f);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.fontSize = 18f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.color = new Color(1f, 0.82f, 0.35f, 0.85f);
        text.raycastTarget = false;
        text.text = string.Empty;
        _badge = text;
    }

    /// <summary>Takes every click (<paramref name="on"/>: the panel is open) or lets them through.</summary>
    public void SetBlocking(bool on)
    {
        if (_blocker != null && _blocker.activeSelf != on)
            _blocker.SetActive(on);
    }

    /// <summary>Rebuilds the badge's line when the run's cheated flag or a sticky switch changed.</summary>
    private void LateUpdate()
    {
        bool cheated = RunManager.HasInstance && RunManager.Instance.World != null && RunManager.Instance.World.cheated;
        int key = DevToolsState.StickyKey(cheated);
        if (key == _shownKey)
            return;
        _shownKey = key;
        _badge.text = DevToolsState.StickyLine(cheated);
    }
}
