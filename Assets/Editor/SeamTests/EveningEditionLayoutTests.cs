using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The evening edition's page as EveningEditionView lays it out (FLOW,
/// BUG-56a). qa-endings-saves (2026-11-07) saw the Welfare Office's and the
/// world report's editions (no picture) with their two buttons grown to 224 px
/// squares: the button row was flexible, so it shared the page's spare height
/// with the story. The page is built in edit mode (Build, by reflection; Start
/// does not run) and laid out at once. NeedsEditor: it makes Unity objects.
/// </summary>
public class EveningEditionLayoutTests
{
    private static RectTransform BuildPage(GameObject host, bool picture)
    {
        var view = host.AddComponent<EveningEditionView>();
        EveningEditionPage page = EveningEdition.Compose("The Temporal Times", "Edition of day 4", "The Welfare Office", "The End",
                                                         "While you were at the desk, an inspector let himself in.", null, null, null, 3);
        Sprite sprite = picture ? Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f) : null;
        typeof(EveningEditionView).GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] { page, sprite });
        var pageRect = (RectTransform)host.transform.Find("Page");
        LayoutRebuilder.ForceRebuildLayoutImmediate(pageRect);
        return pageRect;
    }

    [TestCase(false, Description = "the Welfare Office and the world report: no picture")]
    [TestCase(true, Description = "Fired and Bankrupt: a picture")]
    [Category("NeedsEditor")]
    public void TheButtons_KeepTheirPlateHeight_AndTheStoryTakesTheSpareHeight(bool picture)
    {
        var host = new GameObject("EveningEditionLayoutTest");
        try
        {
            RectTransform page = BuildPage(host, picture);
            foreach (string name in new[] { "TitleButton", "QuitButton" })
            {
                var button = (RectTransform)page.Find("Buttons/" + name);
                Assert.IsNotNull(button, name);
                Assert.AreEqual(64f, button.rect.height, 0.5f, $"{name}'s plate is 64 px high");
                Assert.AreEqual(260f, button.rect.width, 0.5f, $"{name}'s plate is 260 px wide");
            }
            var story = (RectTransform)page.Find("Story");
            Assert.GreaterOrEqual(story.rect.height, picture ? 190f : 400f, "the story takes the page's spare height");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test, Category("NeedsEditor")]
    public void TheFirstButton_ReadsClose_NotTheTaskbarsStart()
    {
        var host = new GameObject("EveningEditionLabelTest");
        try
        {
            RectTransform page = BuildPage(host, false);
            TMP_Text label = page.Find("Buttons/TitleButton/Label").GetComponent<TMP_Text>();
            Assert.AreEqual(UiText.Get("window.close"), label.text);
            Assert.AreNotEqual(UiText.Get("taskbar.start"), label.text, "an ending page's button never promises a start");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }
}
