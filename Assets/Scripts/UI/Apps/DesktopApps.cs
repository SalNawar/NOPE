using System;
using UnityEngine;

/// <summary>
/// The desktop's apps by id (the PC spec's DK1: investigation, internet,
/// mail, citizen_account, notes, settings), on the desktop canvas. The Start
/// menu's app entries call <see cref="OpenApp"/> today; phase 17's desktop
/// icons call it too, and Mail's News link opens "internet" through it. An id
/// without a window here (Investigation until phase 16) opens nothing.
/// </summary>
public sealed class DesktopApps : MonoBehaviour
{
    /// <summary>An app id and its window.</summary>
    [Serializable]
    private struct App
    {
        /// <summary>The app's id (DK1).</summary>
        public string id;

        /// <summary>The window the app opens.</summary>
        public DesktopWindow window;
    }

    /// <summary>The apps the desktop has a window for (the builder lists them).</summary>
    [SerializeField] private App[] apps;

    /// <summary>The Start menu's shell (opening an app closes the menu).</summary>
    [SerializeField] private DesktopShell shell;

    /// <summary>Opens the app's window (a minimised one restores), raised and focused, and closes the Start menu; an unknown id is logged and opens nothing.</summary>
    public void OpenApp(string id)
    {
        if (shell != null)
            shell.CloseStartMenu();

        DesktopWindow window = Window(id);
        if (window != null)
            window.Open();
        else
            Debug.LogWarning($"[DesktopApps] No window for the app '{id}' on this desktop.", this);
    }

    /// <summary>The app's window, or null.</summary>
    private DesktopWindow Window(string id)
    {
        if (apps == null || string.IsNullOrEmpty(id))
            return null;
        foreach (App app in apps)
            if (app.id == id)
                return app.window;
        return null;
    }
}
