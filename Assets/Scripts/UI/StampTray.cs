using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The stamp tray (piece 10 X13): the verdict at the desk. While a traveller
/// is at the desk (BoothRules.StampTrayAllowed), a click on the desk stamp
/// opens a small tray above it with Accept (left, tick) and Deny (right,
/// cross), labelled like the PC's buttons; a choice raises Decided (the
/// investigation decides the case as the PC's buttons do) and closes it. It is
/// modal like the traveller wheel (the same host pattern): this host is always
/// active; its Catcher child, a full-screen click-to-close area holding the
/// panel, shows only while open; the panel follows the stamp through the
/// office camera (OverlayProjection) plus DeskConfigSO.stampTrayOffset.
/// Escape (not the one that opened it in the same frame) or a click outside
/// the panel closes it.
/// </summary>
public sealed class StampTray : MonoBehaviour, IPointerClickHandler
{
    /// <summary>The full-screen transparent click catcher, active only while open; the panel sits under it.</summary>
    [SerializeField] private GameObject catcher;

    /// <summary>The tray's panel (anchors and pivot (0.5, 0.5)), placed over the stamp.</summary>
    [SerializeField] private RectTransform panel;

    /// <summary>Accept (left, tick).</summary>
    [SerializeField] private Button acceptButton;

    /// <summary>Deny (right, cross).</summary>
    [SerializeField] private Button denyButton;

    /// <summary>The desk tuning (the tray's offset above the stamp).</summary>
    [SerializeField] private DeskConfigSO config;

    private bool _canOpen;
    private Camera _camera;
    private RectTransform _canvasRect;
    private Transform _follow;
    private int _openedFrame;

    /// <summary>True while the tray is open.</summary>
    public bool IsOpen => catcher != null && catcher.activeSelf;

    /// <summary>Raised when the tray opens or closes.</summary>
    public event Action OpenChanged;

    /// <summary>Raised when a verdict is chosen: true for Accept.</summary>
    public event Action<bool> Decided;

    /// <summary>Wires the choices, caches the overlay canvas's rect and closes the tray (never this host).</summary>
    private void Awake()
    {
        _canvasRect = OverlayProjection.CanvasRectOf(this);
        if (acceptButton != null)
            acceptButton.onClick.AddListener(() => Choose(true));
        if (denyButton != null)
            denyButton.onClick.AddListener(() => Choose(false));
        if (catcher != null)
            catcher.SetActive(false);
    }

    /// <summary>Only while open: follows the stamp, and Escape closes.</summary>
    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        Place();
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && _openedFrame < Time.frameCount)
            Close();
    }

    /// <summary>The office camera the tray is placed through (the office binder's).</summary>
    public void SetCamera(Camera office) => _camera = office;

    /// <summary>What the tray sits above (the stamp's click box; the office binder's).</summary>
    public void SetFollow(Transform follow) => _follow = follow;

    /// <summary>Opens the tray over the stamp, unless it may not open or is open (the stamp's persistent call).</summary>
    public void Open()
    {
        if (!_canOpen || IsOpen || catcher == null)
            return;

        _openedFrame = Time.frameCount;
        catcher.SetActive(true);
        Place();
        OpenChanged?.Invoke();
    }

    /// <summary>Closes the tray.</summary>
    public void Close()
    {
        if (!IsOpen)
            return;

        catcher.SetActive(false);
        OpenChanged?.Invoke();
    }

    /// <summary>Whether the tray may be open (BoothRules.StampTrayAllowed); false closes it.</summary>
    public void SetCanOpen(bool can)
    {
        _canOpen = can;
        if (!can)
            Close();
    }

    /// <summary>A click on the catcher itself (outside the panel) closes the tray.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerPressRaycast.gameObject == catcher)
            Close();
    }

    /// <summary>A verdict: the tray closes, then Decided.</summary>
    private void Choose(bool accepted)
    {
        if (!IsOpen)
            return;

        Close();
        Decided?.Invoke(accepted);
    }

    /// <summary>Puts the panel over the stamp plus the offset (with no stamp or camera it stays where it is).</summary>
    private void Place()
    {
        if (panel != null && _follow != null && config != null)
            OverlayProjection.TryPlace(panel, _canvasRect, _camera, _follow.position, config.stampTrayOffset);
    }
}
