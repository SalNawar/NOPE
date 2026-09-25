using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// The traveller wheel: the interview's choices on an elliptical ring around
/// the traveller (the ring's InteractionPanelController lays them out with
/// RadialLayoutGroup; "&lt; Back" sits in the centre slot), opened by clicking
/// the traveller or the desk intercom while BoothCoordinator allows it, and
/// the traveller's replies in a speech bubble beside them. This host is always
/// active (so it wakes at load); its Catcher child, a full-screen
/// click-to-close area holding the ring, is shown only while the wheel is
/// open. Escape or a click outside the ring closes it.
/// </summary>
public sealed class TravellerWheel : MonoBehaviour, IPointerClickHandler
{
    /// <summary>The full-screen transparent click catcher, active only while open; the ring sits under it.</summary>
    [SerializeField] private GameObject catcher;

    /// <summary>The ring's root (anchors and pivot (0.5, 0.5)), placed over the traveller.</summary>
    [SerializeField] private RectTransform ring;

    /// <summary>The ring's layout.</summary>
    [SerializeField] private RadialLayoutGroup layout;

    /// <summary>The ring's centre slot ("&lt; Back").</summary>
    [SerializeField] private RectTransform centreSlot;

    /// <summary>The traveller: its anchor places the ring and the reply bubble.</summary>
    [SerializeField] private TravellerView traveller;

    /// <summary>The speech bubble (an overlay callout).</summary>
    [SerializeField] private OverlayCallout bubble;

    /// <summary>The wheel's layout and bubble knobs.</summary>
    [SerializeField] private DeskConfigSO config;

    private bool _canOpen;
    private Camera _camera;
    private RectTransform _canvasRect;

    /// <summary>True while the wheel is open.</summary>
    public bool IsOpen => catcher != null && catcher.activeSelf;

    /// <summary>Raised when the wheel opens or closes.</summary>
    public event Action OpenChanged;

    /// <summary>
    /// Runs at load (the host is active): applies the knobs to the ring and its
    /// centre, sizes the ring to the box around every item (so the projection
    /// keeps it on screen), caches the camera and the overlay canvas's rect
    /// (the per-frame placement looks nothing up) and closes the wheel (never
    /// deactivating this host).
    /// </summary>
    private void Awake()
    {
        if (config != null)
        {
            if (layout != null)
            {
                layout.Radii = config.wheelRadii;
                layout.ItemSize = config.wheelItemSize;
            }

            if (ring != null)
            {
                (float width, float height) = RadialLayout.Extent(config.wheelRadii.x, config.wheelRadii.y, config.wheelItemSize.x, config.wheelItemSize.y);
                ring.sizeDelta = new Vector2(width, height);
            }

            if (centreSlot != null)
                centreSlot.sizeDelta = config.wheelCentreSize;
        }

        _camera = Camera.main;
        _canvasRect = OverlayProjection.CanvasRectOf(this);
        if (catcher != null)
            catcher.SetActive(false);
    }

    /// <summary>Only while open: follows the traveller; Escape closes.</summary>
    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        Place();
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            Close();
    }

    /// <summary>Opens the wheel over the traveller, unless it may not open or is open (the traveller hit zone's and the desk intercom's persistent call).</summary>
    public void Open()
    {
        if (!_canOpen || IsOpen || catcher == null)
            return;

        catcher.SetActive(true);
        Place();
        OpenChanged?.Invoke();
    }

    /// <summary>Closes the ring (a reply bubble stays until its time is up).</summary>
    public void Close()
    {
        if (!IsOpen)
            return;

        catcher.SetActive(false);
        OpenChanged?.Invoke();
    }

    /// <summary>Whether the wheel may be open (BoothRules.WheelAllowed); false closes it and hides the reply bubble (the view left the booth or the traveller left).</summary>
    public void SetCanOpen(bool can)
    {
        _canOpen = can;
        if (can)
            return;

        Close();
        if (bubble != null)
            bubble.Hide();
    }

    /// <summary>Shows the traveller's reply beside them (the caller has applied DisplayText); a blank reply hides the bubble.</summary>
    public void Say(string text)
    {
        if (bubble != null && config != null)
            bubble.Show(text, traveller != null ? traveller.Anchor : null, config.bubbleOffset, config.bubbleSeconds);
    }

    /// <summary>A click on the catcher (outside the ring's buttons) closes the wheel.</summary>
    public void OnPointerClick(PointerEventData eventData) => Close();

    /// <summary>Centres the ring on the traveller's anchor (with no traveller or anchor it stays centred on the screen).</summary>
    private void Place()
    {
        if (ring != null && traveller != null && traveller.Anchor != null)
            OverlayProjection.TryPlace(ring, _canvasRect, _camera, traveller.Anchor.position, Vector2.zero);
    }
}
