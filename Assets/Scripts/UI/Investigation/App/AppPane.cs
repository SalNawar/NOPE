using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One pane of the Investigation app (the PC redesign AP2, AP3, AP5, AP8):
/// its tab strip (one tab per source, in TabOrder.Default, each with a badge),
/// its header (the active view's chips: a click shows that item) and its
/// content (the active tab's view; between travellers a case source shows the
/// no-case state, "Waiting for the next traveller", instead). A tab is shown
/// only by the player's click or the app's own rules (a new case shows
/// Documents, AP8); a view never switches the tab. The views are IAppView
/// components, so a view drawn by the forms engine drops in for today's.
/// Phase 18 adds the second pane and the reordering; the keys' focus ring
/// walks its tabs and chips (AppPane.Keys, phase 20).
/// </summary>
public sealed partial class AppPane : MonoBehaviour
{
    /// <summary>The tabs' buttons, in TabOrder.Default.</summary>
    [SerializeField] private Button[] tabButtons = new Button[0];

    /// <summary>Each tab's active look (drawn over the button while it is the active tab), in TabOrder.Default.</summary>
    [SerializeField] private GameObject[] tabActive = new GameObject[0];

    /// <summary>Each tab's badge, in TabOrder.Default.</summary>
    [SerializeField] private GameObject[] tabBadges = new GameObject[0];

    /// <summary>The views, one per tab (their Tab says which).</summary>
    [SerializeField] private AppView[] views = new AppView[0];

    /// <summary>The header's chip row.</summary>
    [SerializeField] private RectTransform chipStrip;

    /// <summary>A chip (inactive), cloned per item of the active view.</summary>
    [SerializeField] private Button chipTemplate;

    /// <summary>The no-case state over the content (a case source's tab between travellers).</summary>
    [SerializeField] private GameObject noCase;

    /// <summary>The chosen chip's tint (pressed).</summary>
    [SerializeField] private Color chosenTint = new Color(0.72f, 0.72f, 0.72f, 1f);

    /// <summary>An unavailable item's chip tint (dimmed; it still shows why when clicked).</summary>
    [SerializeField] private Color unavailableTint = new Color(1f, 1f, 1f, 0.55f);

    private readonly Dictionary<AppTab, IAppView> _views = new Dictionary<AppTab, IAppView>();
    private readonly List<Button> _chips = new List<Button>();
    private AppTab _active = AppTab.Documents;
    private bool _caseOn;
    private bool _ready;

    /// <summary>Raised when a tab is shown (the app clears its badge).</summary>
    public event Action<AppTab> Shown;

    /// <summary>The active tab.</summary>
    public AppTab ActiveTab => _active;

    /// <summary>The tab's view, or null when the pane has none.</summary>
    public IAppView View(AppTab tab)
    {
        Init();
        return _views.TryGetValue(tab, out IAppView view) ? view : null;
    }

    /// <summary>True when the pane hosts a view for the tab (read from the wired views, before the pane first shows).</summary>
    public bool Hosts(AppTab tab)
    {
        foreach (AppView view in views)
            if (view != null && view.Tab == tab)
                return true;
        return false;
    }

    /// <summary>Shows the tab: its view (or the no-case state), its chips, its button pressed.</summary>
    public void Show(AppTab tab)
    {
        Init();
        _active = tab;
        Apply();
        Shown?.Invoke(tab);
    }

    /// <summary>A traveller is at the desk (a case source's view shows) or not (it shows the no-case state).</summary>
    public void SetCase(bool on)
    {
        Init();
        _caseOn = on;
        Apply();
    }

    /// <summary>Shows or hides the tab's badge.</summary>
    public void SetBadge(AppTab tab, bool on)
    {
        int i = IndexOf(tab);
        if (i >= 0 && i < tabBadges.Length && tabBadges[i] != null && tabBadges[i].activeSelf != on)
            tabBadges[i].SetActive(on);
    }

    /// <summary>Maps the views, wires the tabs and the views' chips (once; the pane may be driven while its window is still closed).</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;

        foreach (AppView view in views)
            if (view != null)
            {
                _views[view.Tab] = view;
                view.ChipsChanged += () => ChipsChanged(view);
            }

        for (int i = 0; i < tabButtons.Length && i < TabOrder.Default.Count; i++)
        {
            AppTab tab = TabOrder.Default[i];
            if (tabButtons[i] != null)
                tabButtons[i].onClick.AddListener(() => Show(tab));
        }

        if (chipTemplate != null)
            chipTemplate.gameObject.SetActive(false);
    }

    /// <summary>A view's chips changed: redrawn when it is the active one.</summary>
    private void ChipsChanged(IAppView view)
    {
        if (view.Tab == _active)
            DrawChips();
    }

    /// <summary>Which view shows, the no-case state, the tabs' looks and the chips.</summary>
    private void Apply()
    {
        bool blocked = !_caseOn && TabOrder.IsCaseSource(_active);
        foreach (KeyValuePair<AppTab, IAppView> pair in _views)
            pair.Value.SetVisible(pair.Key == _active && !blocked);
        if (noCase != null && noCase.activeSelf != blocked)
            noCase.SetActive(blocked);

        for (int i = 0; i < tabActive.Length && i < TabOrder.Default.Count; i++)
            if (tabActive[i] != null && tabActive[i].activeSelf != (TabOrder.Default[i] == _active))
                tabActive[i].SetActive(TabOrder.Default[i] == _active);
        DrawChips();
    }

    /// <summary>The active view's chips (none while the no-case state shows), the chosen one pressed, an unavailable one dimmed.</summary>
    private void DrawChips()
    {
        foreach (Button chip in _chips)
            if (chip != null)
                Destroy(chip.gameObject);
        _chips.Clear();

        if (chipTemplate == null || chipStrip == null || (!_caseOn && TabOrder.IsCaseSource(_active)) || !_views.TryGetValue(_active, out IAppView view))
            return;

        IReadOnlyList<AppChip> items = view.Chips;
        for (int i = 0; i < items.Count; i++)
        {
            Button chip = Instantiate(chipTemplate, chipStrip);
            chip.gameObject.name = "Chip_" + i;
            chip.gameObject.SetActive(true);
            TMP_Text label = chip.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = items[i].Label;
            ColorBlock colours = chip.colors;
            colours.normalColor = i == view.Selected ? chosenTint : items[i].Available ? Color.white : unavailableTint;
            colours.selectedColor = colours.normalColor;
            chip.colors = colours;
            int index = i;
            chip.onClick.AddListener(() => view.Select(index));
            _chips.Add(chip);
        }
    }

    /// <summary>The tab's place in the strip (TabOrder.Default), or -1.</summary>
    private static int IndexOf(AppTab tab)
    {
        for (int i = 0; i < TabOrder.Default.Count; i++)
            if (TabOrder.Default[i] == tab)
                return i;
        return -1;
    }
}
