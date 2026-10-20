using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings' Reduce flashing row (GAME_FEEL_BIBLE G3, gap 2: "UI-PC builds
/// the setting"): an Off / On pair under Motion, the chosen one on the
/// kit's accent plate (as the Settings window paints its pairs); a choice is
/// kept at once (FlashPreference) and every flash follows it (FlashPolicy:
/// a static tint instead of a flash). Built by OfficeSceneUIBuilder's
/// Settings window.
/// </summary>
public sealed class FlashSettingRow : MonoBehaviour
{
    /// <summary>The kit pieces of the chosen option and of the other.</summary>
    private const string ChosenPlate = "miniplate_ox", PlainPlate = "miniplate_bone";

    /// <summary>Off: flashes as designed.</summary>
    [SerializeField] private Button offButton;

    /// <summary>On: every flash a static tint.</summary>
    [SerializeField] private Button onButton;

    /// <summary>The UI kit (the plates).</summary>
    [SerializeField] private UiKitSO kit;

    private void Awake()
    {
        if (offButton != null)
            offButton.onClick.AddListener(() => Choose(false));
        if (onButton != null)
            onButton.onClick.AddListener(() => Choose(true));
    }

    private void OnEnable() => Show();

    /// <summary>Keeps the choice and shows it.</summary>
    private void Choose(bool reduce)
    {
        FlashPreference.Reduce = reduce;
        Show();
    }

    /// <summary>The chosen option on the accent plate, the other plain.</summary>
    private void Show()
    {
        bool reduce = FlashPreference.Reduce;
        Paint(offButton, !reduce);
        Paint(onButton, reduce);
        UiJuice.Choose(offButton, !reduce, false);
        UiJuice.Choose(onButton, reduce, false);
    }

    /// <summary>One option's plate and ink.</summary>
    private void Paint(Button button, bool chosen)
    {
        if (button == null || kit == null || !(button.targetGraphic is Image face))
            return;
        string piece = chosen ? ChosenPlate : PlainPlate;
        kit.Show(face, piece, button);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.color = kit.InkOn(piece);
    }
}
