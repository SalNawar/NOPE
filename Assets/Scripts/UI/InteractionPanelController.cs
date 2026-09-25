using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One action the player can issue to the traveller ("Request Passport").</summary>
public struct InteractionAction
{
    /// <summary>Button label.</summary>
    public string label;

    /// <summary>Shown in the panel's centre slot when it has one (the wheel's "&lt; Back"); otherwise listed like any action.</summary>
    public bool centre;

    /// <summary>Optional: drawn in a square at the button's left, the label starting past it (the traveller wheel's kind icons); null keeps a plain label.</summary>
    public Sprite icon;

    /// <summary>Invoked when the player issues the action.</summary>
    public Action execute;
}

/// <summary>
/// A list of the interview's current choices: the traveller wheel's ring
/// (laid out by RadialLayoutGroup), or a plain vertical list (the hybrid
/// scene's intercom). Actions are the current interview node's choices
/// (requests, questions, dialog replies), supplied per step by the
/// investigation controller; the panel only renders buttons.
/// </summary>
public sealed class InteractionPanelController : MonoBehaviour
{
    /// <summary>Container the action buttons are spawned under.</summary>
    [SerializeField] private Transform actionsRoot;

    /// <summary>Disabled template button cloned per action.</summary>
    [SerializeField] private Button actionButtonTemplate;

    /// <summary>Optional: where centre actions go (the traveller wheel); none keeps every action in the list.</summary>
    [SerializeField] private Transform centreSlot;

    /// <summary>An action icon's side (reference px; a layout value, like RadialLayoutGroup's sizes).</summary>
    [SerializeField] private float iconSize = 28f;

    /// <summary>The gap between the button's left edge, an action icon and the label (reference px).</summary>
    [SerializeField] private float iconPadding = 8f;

    private readonly List<GameObject> _spawned = new();

    private void Awake()
    {
        if (actionButtonTemplate != null)
            actionButtonTemplate.gameObject.SetActive(false);
    }

    /// <summary>Replaces the visible actions with the given list.</summary>
    public void SetActions(IReadOnlyList<InteractionAction> actions)
    {
        Clear();

        if (actions == null || actionsRoot == null || actionButtonTemplate == null)
            return;

        foreach (InteractionAction action in actions)
        {
            Transform parent = action.centre && centreSlot != null ? centreSlot : actionsRoot;
            Button btn = Instantiate(actionButtonTemplate, parent);
            btn.gameObject.SetActive(true);
            _spawned.Add(btn.gameObject);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = action.label;

            if (action.icon != null)
                AddIcon(btn.transform, label, action.icon);

            Action execute = action.execute;
            btn.onClick.AddListener(() => execute?.Invoke());
        }
    }

    /// <summary>
    /// Puts <paramref name="icon"/> in a square at the button's left edge,
    /// vertically centred, and moves the label's left edge past it. Created on
    /// each spawned button at runtime (the template has no icon), so no builder,
    /// prefab or scene change is needed; the button and its icon are destroyed
    /// together.
    /// </summary>
    private void AddIcon(Transform button, TMP_Text label, Sprite icon)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(button, false);
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(iconPadding, 0f);
        rt.sizeDelta = new Vector2(iconSize, iconSize);

        Image image = go.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;

        if (label != null)
        {
            RectTransform lr = label.rectTransform;
            lr.offsetMin = new Vector2(Mathf.Max(lr.offsetMin.x, iconPadding * 2f + iconSize), lr.offsetMin.y);
        }
    }

    /// <summary>
    /// Removes all spawned action buttons. Each is deactivated first: Destroy
    /// waits for the end of the frame, and a layout group must never count a
    /// dying button with the new ones.
    /// </summary>
    public void Clear()
    {
        foreach (GameObject go in _spawned)
        {
            if (go == null)
                continue;

            go.SetActive(false);
            Destroy(go);
        }
        _spawned.Clear();
    }
}
