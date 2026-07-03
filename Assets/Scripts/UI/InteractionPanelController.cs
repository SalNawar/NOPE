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

    /// <summary>Invoked when the player issues the action.</summary>
    public Action execute;
}

/// <summary>
/// The intercom: a vertical list of actions the player can issue to the
/// traveller. Actions are provided per case by the investigation controller;
/// the panel only renders buttons (future actions — interrogation questions,
/// photo capture — plug in as more list entries).
/// </summary>
public sealed class InteractionPanelController : MonoBehaviour
{
    /// <summary>Container the action buttons are spawned under.</summary>
    [SerializeField] private Transform actionsRoot;

    /// <summary>Disabled template button cloned per action.</summary>
    [SerializeField] private Button actionButtonTemplate;

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
            Button btn = Instantiate(actionButtonTemplate, actionsRoot);
            btn.gameObject.SetActive(true);
            _spawned.Add(btn.gameObject);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = action.label;

            Action execute = action.execute;
            btn.onClick.AddListener(() => execute?.Invoke());
        }
    }

    /// <summary>Removes all spawned action buttons.</summary>
    public void Clear()
    {
        foreach (GameObject go in _spawned)
            if (go != null)
                Destroy(go);
        _spawned.Clear();
    }
}
