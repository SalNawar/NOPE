using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for the title scene added in Alpha Phase 5: a title panel (Continue /
/// New Run) and an ending panel (shown instead, when WorldState.endingId is
/// set, displaying the reached EndingSO and offering New Run; on the Debt
/// Relief ending, the clerk's own papers beside it, redesign phase 13). The
/// world panel (the endings spec E0) lists the world's outcomes over the END
/// OF DEMO card: on the run's last day instead of the ending panel, and after
/// a failure behind the ending panel's "The world you leave behind" button.
/// The adoption panel (the Home pet spec PS1) comes between New Run and the
/// office: a dog or a cat, each a choice card of the UI kit (the chosen one
/// framed), and a name typed and checked (PetNames.Check: refused names say
/// why, ui.strings "adopt.problem.*", and the field turns to the kit's error
/// field until the name is edited). The title block shows the kit's logo
/// (the one picture with its words baked in). The panels are optional; if unwired,
/// TitleSceneController degrades to loading the office scene directly so the
/// run stays playable.
/// </summary>
public sealed class TitleUIController : MonoBehaviour
{
    [Header("The UI kit")]
    /// <summary>The cel UI kit (Assets/Data/UI/UiKit_Default.asset): the name field's error and focus faces.</summary>
    [SerializeField] private UiKitSO kit;

    [Header("Title Panel")]
    /// <summary>Root panel shown when the run has not ended.</summary>
    [SerializeField] private GameObject titlePanel;

    /// <summary>Resumes the saved run (hidden if there is no save).</summary>
    [SerializeField] private Button continueButton;

    /// <summary>Starts a brand-new run.</summary>
    [SerializeField] private Button newRunButton;

    [Header("Adoption Panel (the Home pet spec PS1)")]
    /// <summary>Root of the adoption panel, shown after New Run.</summary>
    [SerializeField] private GameObject adoptPanel;

    /// <summary>The panel's title.</summary>
    [SerializeField] private TMP_Text adoptTitleText;

    /// <summary>The panel's line under the title.</summary>
    [SerializeField] private TMP_Text adoptBodyText;

    /// <summary>Chooses the dog (a kit choice card; the chosen card is the one that takes no clicks, its locked face the card's selected one).</summary>
    [SerializeField] private Button adoptDogButton;

    /// <summary>Chooses the cat (a kit choice card, as the dog's).</summary>
    [SerializeField] private Button adoptCatButton;

    /// <summary>The name the player types.</summary>
    [SerializeField] private TMP_InputField adoptNameInput;

    /// <summary>Why the name is refused ("" while it is fine).</summary>
    [SerializeField] private TMP_Text adoptProblemText;

    /// <summary>Adopts and starts the run.</summary>
    [SerializeField] private Button adoptButton;

    /// <summary>Goes back to the title.</summary>
    [SerializeField] private Button adoptBackButton;

    [Header("Ending Panel")]
    /// <summary>Root panel shown when WorldState.endingId is set.</summary>
    [SerializeField] private GameObject endingPanel;

    /// <summary>Reached ending's display name.</summary>
    [SerializeField] private TMP_Text endingTitleText;

    /// <summary>The reached ending's picture, full screen behind the ending panel (EndingSO.picture; hidden when it has none).</summary>
    [SerializeField] private Image endingPicture;

    /// <summary>Reached ending's flavor text.</summary>
    [SerializeField] private TMP_Text endingBodyText;

    /// <summary>Clears the ended run and starts a new one.</summary>
    [SerializeField] private Button endingNewRunButton;

    /// <summary>Opens the world panel as "the world you leave behind" (a failure's ending; hidden without it).</summary>
    [SerializeField] private Button endingWorldButton;

    [Header("World Panel: the world's outcomes and END OF DEMO (the endings spec E0)")]
    /// <summary>Root panel of the world's outcomes: the run's last day, or a failure's "world you leave behind".</summary>
    [SerializeField] private GameObject worldPanel;

    /// <summary>The page's heading: the last day's ending's name, or "The world you leave behind".</summary>
    [SerializeField] private TMP_Text worldTitleText;

    /// <summary>The world's outcomes, one factor a line (its question, then its answer in bold).</summary>
    [SerializeField] private TMP_Text worldOutcomesText;

    /// <summary>The END OF DEMO card, set apart under the outcomes (EndingSO.closingCard of the run's last day); hidden when blank.</summary>
    [SerializeField] private TMP_Text worldCardText;

    /// <summary>Clears the ended run and starts a new one.</summary>
    [SerializeField] private Button worldNewRunButton;

    [Header("Ending Panel: the clerk's papers (redesign phase 13)")]
    /// <summary>The Debt Relief ending's papers, beside the ending panel: the clerk's own Labour Contract and account (hidden for every other ending).</summary>
    [SerializeField] private GameObject clerkPapers;

    /// <summary>The clerk's Labour Contract (TC-520): its title over its rows.</summary>
    [SerializeField] private TMP_Text clerkContractText;

    /// <summary>The clerk's Record Extract (TC-901), now Frozen: its title and holder line over its rows.</summary>
    [SerializeField] private TMP_Text clerkAccountText;

    /// <summary>True if the title panel is wired.</summary>
    public bool HasTitlePanel => titlePanel != null;

    /// <summary>True if the ending panel is wired.</summary>
    public bool HasEndingPanel => endingPanel != null;

    /// <summary>True if the world panel is wired.</summary>
    public bool HasWorldPanel => worldPanel != null;

    /// <summary>True if the adoption panel, its name field and its adopt button are wired.</summary>
    public bool HasAdoptPanel => adoptPanel != null && adoptNameInput != null && adoptButton != null;

    /// <summary>The kind the adoption panel has chosen.</summary>
    private PetKind _adoptKind;

    /// <summary>
    /// Hides both panels until a Show* call activates one. The panels are
    /// optional, so each is tested with Unity's == (audit R4-010): an
    /// unassigned serialized field is Unity's fake null in the Editor, which
    /// ?. does not see, and SetActive on it would throw.
    /// </summary>
    private void Awake()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
        if (worldPanel != null) worldPanel.SetActive(false);
        if (adoptPanel != null) adoptPanel.SetActive(false);
        if (endingPicture != null) endingPicture.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the adoption panel over the others (the Home pet spec PS1): the
    /// dog chosen first, with its suggested name in the field
    /// (<paramref name="words"/>); choosing a kind draws it and swaps a
    /// suggested name for the other's; Adopt checks the name (PetNames.Check
    /// at home.pet.nameMaxLength: a refusal says why and adopts nothing) and
    /// calls <paramref name="onAdopt"/> with the kind and the cleaned name;
    /// Back calls <paramref name="onBack"/>.
    /// </summary>
    public void ShowAdopt(PetContent words, Action<PetKind, string> onAdopt, Action onBack)
    {
        if (!HasAdoptPanel)
            return;

        words ??= new PetContent();
        if (titlePanel != null) titlePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
        if (worldPanel != null) worldPanel.SetActive(false);
        if (endingPicture != null) endingPicture.gameObject.SetActive(false);
        adoptPanel.SetActive(true);

        if (adoptTitleText != null)
            adoptTitleText.text = UiText.Get("adopt.title");
        if (adoptBodyText != null)
            adoptBodyText.text = UiText.Get("adopt.body");
        SetLabel(adoptDogButton, UiText.Get("adopt.dog"));
        SetLabel(adoptCatButton, UiText.Get("adopt.cat"));
        SetLabel(adoptButton, UiText.Get("adopt.go"));
        SetLabel(adoptBackButton, UiText.Get("adopt.back"));
        if (adoptNameInput.placeholder is TMP_Text placeholder)
            placeholder.text = UiText.Get("adopt.placeholder");
        adoptNameInput.characterLimit = Math.Max(1, words.nameMaxLength) + 8;
        adoptNameInput.text = string.Empty;
        if (adoptProblemText != null)
            adoptProblemText.text = string.Empty;
        ShowNameRefused(false);
        adoptNameInput.onValueChanged.RemoveAllListeners();
        adoptNameInput.onValueChanged.AddListener(_ => ShowNameRefused(false));

        void Choose(PetKind kind)
        {
            string before = words.Kind(_adoptKind)?.suggestedName ?? string.Empty;
            _adoptKind = kind;
            if (string.IsNullOrWhiteSpace(adoptNameInput.text) || adoptNameInput.text == before)
                adoptNameInput.text = words.Kind(kind)?.suggestedName ?? string.Empty;
            if (adoptDogButton != null) adoptDogButton.interactable = kind != PetKind.Dog;
            if (adoptCatButton != null) adoptCatButton.interactable = kind != PetKind.Cat;
        }

        _adoptKind = PetKind.Dog;
        adoptNameInput.text = words.Kind(PetKind.Dog)?.suggestedName ?? string.Empty;
        Choose(PetKind.Dog);
        Wire(adoptDogButton, () => Choose(PetKind.Dog));
        Wire(adoptCatButton, () => Choose(PetKind.Cat));
        Wire(adoptBackButton, onBack);
        Wire(adoptButton, () =>
        {
            PetNameProblem problem = PetNames.Check(adoptNameInput.text, words.nameMaxLength);
            if (problem != PetNameProblem.None)
            {
                if (adoptProblemText != null)
                    adoptProblemText.text = UiText.Format("adopt.problem." + problem, words.nameMaxLength);
                ShowNameRefused(true);
                return;
            }
            adoptPanel.SetActive(false);
            onAdopt?.Invoke(_adoptKind, PetNames.Clean(adoptNameInput.text));
        });
    }

    /// <summary>The name field's face: the kit's error field while a refused name stands (<paramref name="refused"/>), else its rest face with the focus face while typing; its reason clears with the error.</summary>
    private void ShowNameRefused(bool refused)
    {
        if (kit == null || adoptNameInput == null || !(adoptNameInput.targetGraphic is Image face))
            return;
        Sprite error = kit.Get("field_error"), focus = kit.Get("field_focus"), rest = kit.Get("field_rest");
        if (rest == null)
            return;
        face.sprite = refused && error != null ? error : rest;
        SpriteState states = adoptNameInput.spriteState;
        states.selectedSprite = refused && error != null ? error : focus;
        states.highlightedSprite = refused && error != null ? error : null;
        adoptNameInput.spriteState = states;
        if (!refused && adoptProblemText != null)
            adoptProblemText.text = string.Empty;
    }

    /// <summary>Sets a button's label, when it has one.</summary>
    private static void SetLabel(Button button, string text)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (label != null)
            label.text = text;
    }

    /// <summary>
    /// Shows the title panel. The Continue button is hidden entirely if no
    /// save exists.
    /// </summary>
    public void ShowTitle(bool hasSave, Action onContinue, Action onNewRun)
    {
        if (titlePanel == null)
            return;

        if (adoptPanel != null) adoptPanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
        if (worldPanel != null) worldPanel.SetActive(false);
        titlePanel.SetActive(true);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSave);
            continueButton.onClick.RemoveAllListeners();

            if (hasSave)
                continueButton.onClick.AddListener(() => onContinue?.Invoke());
        }

        if (newRunButton != null)
        {
            newRunButton.onClick.RemoveAllListeners();
            newRunButton.onClick.AddListener(() => onNewRun?.Invoke());
        }
    }

    /// <summary>
    /// Shows the ending panel for the reached EndingSO (may be null if the id
    /// has no matching content yet — falls back to a generic message): its
    /// title, its body and, set apart under it, its closing card when it has
    /// one; its world button, labelled <paramref name="worldLabel"/>, runs
    /// <paramref name="onWorld"/> (hidden when null: "The world you leave behind");
    /// <paramref name="fill"/> puts the run's words into the body (the pet's
    /// name and kind into the Welfare Office's ending; PetContent.Fill).
    /// </summary>
    public void ShowEnding(EndingSO ending, Action onNewRun, string worldLabel = null, Action onWorld = null, Func<string, string> fill = null)
    {
        if (endingPanel == null)
            return;

        if (adoptPanel != null) adoptPanel.SetActive(false);
        if (titlePanel != null) titlePanel.SetActive(false);
        if (worldPanel != null) worldPanel.SetActive(false);
        endingPanel.SetActive(true);

        if (endingTitleText != null)
            endingTitleText.text = ending != null && !string.IsNullOrEmpty(ending.displayName)
                ? ending.displayName
                : "The End";

        if (endingBodyText != null)
        {
            string body = ending == null ? string.Empty
                : string.IsNullOrWhiteSpace(ending.closingCard) ? ending.bodyText
                : $"{ending.bodyText}\n\n{ending.closingCard}";
            endingBodyText.text = fill != null ? fill(body) : body;
        }

        if (endingPicture != null)
        {
            Sprite picture = ending != null ? ending.picture : null;
            endingPicture.sprite = picture;
            endingPicture.gameObject.SetActive(picture != null);
        }

        Wire(endingNewRunButton, onNewRun);
        if (endingWorldButton != null)
        {
            endingWorldButton.gameObject.SetActive(onWorld != null);
            Wire(endingWorldButton, onWorld);
            TMP_Text label = endingWorldButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null && !string.IsNullOrEmpty(worldLabel))
                label.text = worldLabel;
        }
    }

    /// <summary>
    /// Shows the world panel: <paramref name="heading"/>, the world's outcomes
    /// (WorldFactors.Lines: each question, then its answer in bold; never a
    /// score or a rank), and <paramref name="card"/> set apart under them (the
    /// END OF DEMO card; hidden when blank), over the title's background.
    /// </summary>
    public void ShowWorld(string heading, IReadOnlyList<OutcomeLine> outcomes, string card, Action onNewRun)
    {
        if (worldPanel == null)
            return;

        if (titlePanel != null) titlePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
        if (endingPicture != null) endingPicture.gameObject.SetActive(false);
        worldPanel.SetActive(true);

        if (worldTitleText != null)
            worldTitleText.text = heading ?? string.Empty;

        if (worldOutcomesText != null)
            worldOutcomesText.text = Outcomes(outcomes);

        if (worldCardText != null)
        {
            worldCardText.text = card ?? string.Empty;
            worldCardText.gameObject.SetActive(!string.IsNullOrWhiteSpace(card));
        }

        Wire(worldNewRunButton, onNewRun);
    }

    /// <summary>Points a button at <paramref name="action"/> alone.</summary>
    private static void Wire(Button button, Action action)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action?.Invoke());
    }

    /// <summary>The outcomes' text: each factor's question, then its answer in bold on the next line, a blank line between factors.</summary>
    private static string Outcomes(IReadOnlyList<OutcomeLine> outcomes)
    {
        var sb = new StringBuilder();
        foreach (OutcomeLine line in outcomes ?? Array.Empty<OutcomeLine>())
        {
            if (sb.Length > 0)
                sb.AppendLine().AppendLine();
            sb.Append(line.Question).AppendLine().Append("<b>").Append(line.Answer).Append("</b>");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Shows the clerk's papers beside the ending panel (the Debt Relief ending,
    /// redesign phase 13: the Labour Contract on one side, the account, now
    /// Frozen, on the other), each a title over its rows under their group
    /// headings. Drawn with today's widgets, the one place the forms engine
    /// replaces with TC-520 and TC-901. A null contract or account hides them.
    /// </summary>
    public void ShowClerkPapers(string contractTitle, IReadOnlyList<AccountRow> contract, string accountTitle, IReadOnlyList<AccountRow> account)
    {
        if (clerkPapers == null)
            return;

        bool show = contract != null && account != null;
        clerkPapers.SetActive(show);
        if (!show)
            return;

        if (clerkContractText != null)
            clerkContractText.text = Paper(contractTitle, contract);
        if (clerkAccountText != null)
            clerkAccountText.text = Paper(accountTitle, account);
    }

    /// <summary>A paper's text: its title in bold, then each row's label and value (the value indented to its column, so a long one wraps there), a bold heading where the group changes.</summary>
    private static string Paper(string title, IReadOnlyList<AccountRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<b>").Append(title).Append("</b>\n");
        string group = null;
        foreach (AccountRow row in rows)
        {
            if (row.Group != group && !string.IsNullOrEmpty(row.Group))
                sb.Append("\n<b>").Append(row.Group).Append("</b>\n");
            group = row.Group;
            sb.Append(row.Label).Append("<indent=42%>").Append(row.Value).Append("</indent>\n");
        }
        return sb.ToString();
    }
}
