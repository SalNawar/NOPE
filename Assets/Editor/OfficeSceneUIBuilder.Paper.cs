using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's morning paper (run 7; the UI kit's newspaper,
/// docs/ui-kit/newspaper.png): "The Temporal Times" front page over the
/// office before the shift, built from the day's data by DayFlowUIController
/// through MorningPaper: the issue line, the blackletter masthead between
/// its weather and edition boxes, the dateline (the day, today's date, the
/// edition), the key story (the kicker, the headline in condensed capitals,
/// its deck) over three columns of dummy type, the small story in the right
/// column under its title, and the START SHIFT plate on the paper's foot.
/// Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The front page's size (overlay units): landscape, as the reference broadsheet.</summary>
    private static readonly Vector2 MorningPaperSize = new Vector2(1240f, 800f);

    /// <summary>The START SHIFT plate on the paper's foot (overlay units).</summary>
    private static readonly Vector2 MorningPaperPlate = new Vector2(360f, 80f);

    /// <summary>The paper's texts DayFlowUIController writes.</summary>
    private struct PaperTexts
    {
        /// <summary>The dateline's day, its date, the kicker, the key story's headline and deck, the small story's title and lines.</summary>
        public TMP_Text Title, Date, Kicker, Headline, Deck, StoryTitle, Story;

        /// <summary>The right column's dummy type shown on a day with no small story.</summary>
        public GameObject Filler;
    }

    /// <summary>The morning paper (see the class summary), rebuilt each run, inactive; returns its panel.</summary>
    private static Transform BuildMorningPaper(Transform overlay, out PaperTexts texts, out Button startShift)
    {
        DestroyChildIfPresent(overlay, "BriefingPanel");
        Transform panel = Panel(overlay, "BriefingPanel", Center, Center, new Vector2(0f, MorningPaperPlate.y / 4f), MorningPaperSize, Paper, ThemeRoleId.Newsletter);
        float scale = _kit != null ? _kit.overlayScale : 2f;
        KitSkin(panel, "panel_bone", scale);
        Transform paper = Panel(panel, "Paper", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        Color ink = _kit != null ? _kit.inkOnLight : Ink, red = _kit != null ? _kit.inkAlert : Color.red;
        TMP_Text Line(string name, string key, int size, int min, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Color colour, TMP_FontAsset face, bool upper,
                      FontStyles style = FontStyles.Normal, bool wrap = false)
        {
            TMP_Text t = Text(paper, name, key != null ? null : string.Empty, size, align, aMin, aMax, colour, ThemeRoleId.Newsletter, key, style);
            t.raycastTarget = false;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = min;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            if (_kit != null)
                SceneUiKit.SkinText(t, colour, face, upper);
            return t;
        }

        void Rule(string name, float y, float x0 = 0.03f, float x1 = 0.97f, float thick = 2f)
        {
            Transform r = Panel(paper, name, new Vector2(x0, y), new Vector2(x1, y), Vector2.zero, new Vector2(0f, thick), ink, ThemeRoleId.NewsletterBorder);
            r.GetComponent<Image>().raycastTarget = false;
            SceneUiKit.Tag(r.GetComponent<Image>(), ThemeRoleId.NewsletterBorder, ThemePart.Kit);
        }

        // The issue line, the masthead, its boxes and the double rule.
        Line("Edition", "paper.edition", 18, 14, TextAlignmentOptions.MidlineRight, new Vector2(0.62f, 0.935f), new Vector2(0.97f, 0.975f), ink, _kit?.labelFont, true);
        Line("Price", "paper.price", 18, 14, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.935f), new Vector2(0.38f, 0.975f), ink, _kit?.labelFont, true);
        Line("Masthead", "briefing.masthead", 92, 48, TextAlignmentOptions.Center, new Vector2(0.18f, 0.80f), new Vector2(0.82f, 0.94f), ink, _kit?.mastheadFont, false);
        Rule("RuleTop", 0.795f);
        Rule("RuleTop2", 0.785f, thick: 1f);

        // The dateline.
        TMP_Text title = Line("TitleText", null, 20, 14, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.745f), new Vector2(0.38f, 0.78f), ink, _kit?.labelFont, true);
        TMP_Text date = Line("DateText", null, 20, 14, TextAlignmentOptions.Center, new Vector2(0.38f, 0.745f), new Vector2(0.62f, 0.78f), ink, _kit?.labelFont, true);
        Rule("RuleDate", 0.74f, thick: 1f);

        // The key story: kicker, headline, deck.
        TMP_Text kicker = Line("Kicker", "briefing.bulletinHeader", 22, 20, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.69f), new Vector2(0.64f, 0.725f), red, _kit?.labelFont, true);
        TMP_Text headline = Line("Headline", null, 64, 30, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.47f), new Vector2(0.64f, 0.69f), ink, _kit?.labelFont, true, wrap: true);
        TMP_Text deck = Line("Deck", null, 25, 17, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.33f), new Vector2(0.64f, 0.465f), ink, _kit?.bodyFont, false, FontStyles.Italic, true);
        Rule("RuleStory", 0.32f, 0.03f, 0.64f, 1f);
        DummyColumns(paper, "Lead", new Vector2(0.03f, 0.13f), new Vector2(0.64f, 0.31f), 3, ink);

        // The small story in the right column.
        Transform column = Panel(paper, "ColumnRule", new Vector2(0.665f, 0.13f), new Vector2(0.665f, 0.73f), Vector2.zero, new Vector2(1f, 0f), ink, ThemeRoleId.NewsletterBorder);
        column.GetComponent<Image>().raycastTarget = false;
        SceneUiKit.Tag(column.GetComponent<Image>(), ThemeRoleId.NewsletterBorder, ThemePart.Kit);
        TMP_Text storyTitle = Line("StoryTitle", null, 30, 18, TextAlignmentOptions.TopLeft, new Vector2(0.69f, 0.645f), new Vector2(0.97f, 0.725f), ink, _kit?.labelFont, true, wrap: true);
        TMP_Text story = Line("Story", null, 20, 13, TextAlignmentOptions.TopLeft, new Vector2(0.69f, 0.36f), new Vector2(0.97f, 0.64f), ink, _kit?.bodyFont, false, wrap: true);
        DummyColumns(paper, "Side", new Vector2(0.69f, 0.13f), new Vector2(0.97f, 0.34f), 1, ink);
        Transform filler = Panel(paper, "StoryFiller", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        DummyColumns(filler, "Filler", new Vector2(0.69f, 0.37f), new Vector2(0.97f, 0.72f), 1, ink);

        // START SHIFT on the paper's foot.
        startShift = MakeButton(panel, "ActionButton", null, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), null, ThemeRoleId.NewsletterButton, "briefing.start");
        var plate = (RectTransform)startShift.transform;
        plate.pivot = new Vector2(0.5f, 0.5f);
        plate.anchoredPosition = new Vector2(0f, 14f);
        plate.sizeDelta = MorningPaperPlate;
        KitSkin(startShift, "plate_ox", scale);
        KitLabel(startShift, "plate_ox_rest");

        texts = new PaperTexts { Title = title, Date = date, Kicker = kicker, Headline = headline, Deck = deck, StoryTitle = storyTitle, Story = story, Filler = filler.gameObject };
        panel.gameObject.SetActive(false);
        return panel;
    }

    /// <summary>The paper's dummy type (its bars named <paramref name="name"/>, column and line): <paramref name="columns"/> columns of grey lines between the anchors (the reference's "rest dummy lines"; no words, so nothing to translate).</summary>
    private static void DummyColumns(Transform paper, string name, Vector2 aMin, Vector2 aMax, int columns, Color ink)
    {
        const int Lines = 9;
        var grey = new Color(ink.r, ink.g, ink.b, 0.28f);
        float width = (aMax.x - aMin.x) / columns, gutter = width * 0.06f;
        for (int c = 0; c < columns; c++)
            for (int l = 0; l < Lines; l++)
            {
                float y = aMax.y - (l + 0.5f) * (aMax.y - aMin.y) / Lines;
                float x0 = aMin.x + c * width + (c > 0 ? gutter : 0f), x1 = aMin.x + (c + 1) * width - (c < columns - 1 ? gutter : 0f);
                if (l == Lines - 1)
                    x1 = x0 + (x1 - x0) * 0.55f; // a paragraph's short last line
                Transform bar = Panel(paper, $"{name}{c}_{l}", new Vector2(x0, y), new Vector2(x1, y), Vector2.zero, new Vector2(0f, 4f), grey, ThemeRoleId.NewsletterBorder);
                bar.GetComponent<Image>().raycastTarget = false;
                SceneUiKit.Tag(bar.GetComponent<Image>(), ThemeRoleId.NewsletterBorder, ThemePart.Kit);
            }
    }
}
