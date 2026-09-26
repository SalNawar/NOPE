using UnityEngine;

/// <summary>
/// A PC page kind's form (redesign phase 5, PC spec FO3, FO9, §6.2): an
/// authored asset in Assets/Data/Forms (Form_RecordExtract TC-901,
/// Form_Register TC-911, Form_Memo TC-950, Form_Statement TC-960, ...), its
/// own form number and title, printed English words (titles, section heads,
/// column heads, captions, fine print: forms are diegetic and never follow the
/// UI language), and blocks that flow: the page is as tall as its rows. A
/// view fills the page's slots (FormData) and draws it with a FormView at its
/// pane's width.
/// </summary>
[CreateAssetMenu(fileName = "Form_", menuName = "TimeDesk/Form (PC page kind)", order = 13)]
public sealed class FormSpecSO : ScriptableObject
{
    /// <summary>The page's form: its number, its title and its blocks (a page kind flows: fixedPage off).</summary>
    public FormSpec form = new FormSpec { fixedPage = false };

    /// <summary>The data a page of this kind starts from: <paramref name="agency"/>'s name and programme over the form's number and title.</summary>
    public FormData Page(AgencyContent agency) => FormData.Page(form, agency != null ? agency.name : null, agency != null ? agency.programme : null);
}
