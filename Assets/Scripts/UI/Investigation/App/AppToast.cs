using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's toast (the PC redesign WN5): a strip on the desktop
/// above the windows, over the compare dock's edge, naming what arrived
/// ("Displacement Certificate scanned") with an Open button, gone after its
/// time or once Open is clicked; a hint (a step done at the desk: "Use the
/// traveller wheel") shows without Open. It lives on the desktop, not in the app, so
/// it shows while the app is closed or minimised too.
/// </summary>
public sealed class AppToast : MonoBehaviour
{
    /// <summary>The toast's line.</summary>
    [SerializeField] private TMP_Text text;

    /// <summary>Open: the toast's action.</summary>
    [SerializeField] private Button openButton;

    private Action _open;
    private float _until;
    private bool _wired;

    /// <summary>Shows <paramref name="line"/> for <paramref name="seconds"/> (unscaled); Open runs <paramref name="open"/> (null: a hint, no Open).</summary>
    public void Show(string line, float seconds, Action open)
    {
        Wire();
        if (text != null)
            text.text = line;
        if (openButton != null)
            openButton.gameObject.SetActive(open != null);
        _open = open;
        _until = Time.unscaledTime + seconds;
        gameObject.SetActive(true);
    }

    /// <summary>Hides the toast.</summary>
    public void Hide()
    {
        _open = null;
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Time.unscaledTime >= _until)
            Hide();
    }

    /// <summary>Wires Open once (the toast is shown before it ever ran).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (openButton != null)
            openButton.onClick.AddListener(() =>
            {
                Action open = _open;
                Hide();
                open?.Invoke();
            });
    }
}
