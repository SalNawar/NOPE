using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's morning paper (run 7; the UI kit's newspaper,
/// docs/ui-kit/newspaper.png): "The Temporal Times" front page over the
/// office before the shift, built from the day's data by DayFlowUIController
/// through MorningPaper: the issue line, the blackletter masthead between
/// its weather and edition boxes, the dateline (the day, today's date, the
/// edition), the key story (the headline in condensed capitals, its deck)
/// over three columns of dummy type, the small story in the right column
/// under its title, the START SHIFT plate on the paper's foot, and the
/// Bureau memo clipped over the paper's lower right corner, under its small
/// story and past its edge (a manila slip,
/// a brass clip on its top, tilted: "BUREAU MEMO · DESK 3", the day's
/// bulletin's title and body; Saleh 2026-10-07: the clerk's work news is the
/// Bureau's, never the world's paper).
/// Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The front page's size (overlay units): landscape, as the reference broadsheet.</summary>
    private static readonly Vector2 MorningPaperSize = new Vector2(1240f, 800f);

    /// <summary>The START SHIFT plate on the paper's foot (overlay units).</summary>
    private static readonly Vector2 MorningPaperPlate = new Vector2(360f, 80f);

    /// <summary>The Bureau memo slip (overlay units), its centre from the paper's lower right corner, and its tilt in degrees.</summary>
    private static readonly Vector2 BureauMemoSize = new Vector2(470f, 330f), BureauMemoAt = new Vector2(-120f, 110f);

    /// <summary>The memo's tilt (degrees; a slip clipped on by hand).</summary>
    private const float BureauMemoTilt = -2.5f;

    /// <summary>The paper's texts DayFlowUIController writes.</summary>
    private struct PaperTexts
    {
        /// <summary>The dateline's day, its date, the key story's headline and deck, the small story's title and lines, the Bureau memo's title and body.</summary>
        public TMP_Text Title, Date, Headline, Deck, StoryTitle, Story, MemoTitle, MemoBody;

        /// <summary>The right column's dummy type shown on a day with no small story.</summary>
        public GameObject Filler;

        /// <summary>The Bureau memo slip (shown on a day with a bulletin).</summary>
        public GameObject Memo;
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
        // Each line by the type scale: its role, and the height of its band on the page.
        TMP_Text Line(string name, string key, KitText kind, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Color colour, FontStyles style = FontStyles.Normal)
        {
            TMP_Text t = Text(paper, name, key != null ? null : string.Empty, 22, align, aMin, aMax, colour, ThemeRoleId.Newsletter, key, style);
            t.raycastTarget = false;
            if (_kit != null)
                SceneUiKit.SkinText(t, _kit, kind, (aMax.y - aMin.y) * MorningPaperSize.y, colour);
            return t;
        }

        void Rule(string name, float y, float x0 = 0.03f, float x1 = 0.97f, float thick = 2f)
        {
            Transform r = Panel(paper, name, new Vector2(x0, y), new Vector2(x1, y), Vector2.zero, new Vector2(0f, thick), ink, ThemeRoleId.NewsletterBorder);
            r.GetComponent<Image>().raycastTarget = false;
            SceneUiKit.Tag(r.GetComponent<Image>(), ThemeRoleId.NewsletterBorder, ThemePart.Kit);
        }

        // The issue line, the masthead, its boxes and the double rule.
        Line("Edition", "paper.edition", KitText.Pill, TextAlignmentOptions.MidlineRight, new Vector2(0.62f, 0.935f), new Vector2(0.97f, 0.975f), ink);
        Line("Price", "paper.price", KitText.Pill, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.935f), new Vector2(0.38f, 0.975f), ink);
        Line("Masthead", "briefing.masthead", KitText.Masthead, TextAlignmentOptions.Center, new Vector2(0.18f, 0.80f), new Vector2(0.82f, 0.94f), ink);
        Rule("RuleTop", 0.795f);
        Rule("RuleTop2", 0.785f, thick: 1f);

        // The dateline.
        TMP_Text title = Line("TitleText", null, KitText.Pill, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.745f), new Vector2(0.38f, 0.78f), ink);
        TMP_Text date = Line("DateText", null, KitText.Pill, TextAlignmentOptions.Center, new Vector2(0.38f, 0.745f), new Vector2(0.62f, 0.78f), ink);
        Rule("RuleDate", 0.74f, thick: 1f);

        // The key story: headline, deck.
        TMP_Text headline = Line("Headline", null, KitText.Headline, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.47f), new Vector2(0.64f, 0.69f), ink);
        TMP_Text deck = Line("Deck", null, KitText.BodyLarge, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.33f), new Vector2(0.64f, 0.465f), ink, FontStyles.Italic);
        Rule("RuleStory", 0.32f, 0.03f, 0.64f, 1f);
        DummyColumns(paper, "Lead", new Vector2(0.03f, 0.13f), new Vector2(0.64f, 0.31f), 3, ink);

        // The small story in the right column.
        Transform column = Panel(paper, "ColumnRule", new Vector2(0.665f, 0.13f), new Vector2(0.665f, 0.73f), Vector2.zero, new Vector2(1f, 0f), ink, ThemeRoleId.NewsletterBorder);
        column.GetComponent<Image>().raycastTarget = false;
        SceneUiKit.Tag(column.GetComponent<Image>(), ThemeRoleId.NewsletterBorder, ThemePart.Kit);
        TMP_Text storyTitle = Line("StoryTitle", null, KitText.PanelHeading, TextAlignmentOptions.TopLeft, new Vector2(0.69f, 0.645f), new Vector2(0.97f, 0.725f), ink);
        TMP_Text story = Line("Story", null, KitText.Body, TextAlignmentOptions.TopLeft, new Vector2(0.69f, 0.36f), new Vector2(0.97f, 0.64f), ink);
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

        Transform memo = BuildBureauMemo(panel, scale, ink, red, out TMP_Text memoTitle, out TMP_Text memoBody);

        texts = new PaperTexts { Title = title, Date = date, Headline = headline, Deck = deck, StoryTitle = storyTitle, Story = story, Filler = filler.gameObject,
                                 Memo = memo.gameObject, MemoTitle = memoTitle, MemoBody = memoBody };
        panel.gameObject.SetActive(false);
        return panel;
    }

    /// <summary>
    /// The Bureau memo (see the class summary): a manila slip over the
    /// paper's lower right corner, over its side column's dummy type and
    /// past its edge, tilted, a brass clip on its top; its header
    /// ("briefing.bulletinHeader", "BUREAU MEMO · DESK 3") in the alert ink,
    /// the bulletin's title as its heading and the rest as typed body text,
    /// both written by DayFlowUIController. Returns the slip.
    /// </summary>
    private static Transform BuildBureauMemo(Transform panel, float scale, Color ink, Color red, out TMP_Text title, out TMP_Text body)
    {
        Transform memo = Panel(panel, "BureauMemo", new Vector2(1f, 0f), new Vector2(1f, 0f), BureauMemoAt, BureauMemoSize, Paper, ThemeRoleId.Newsletter);
        memo.localRotation = Quaternion.Euler(0f, 0f, BureauMemoTilt);
        memo.GetComponent<Image>().raycastTarget = false;
        KitSkin(memo, "panel_manila", scale);

        TMP_Text MemoLine(string name, string key, KitText kind, Vector2 aMin, Vector2 aMax, Color colour, TextAlignmentOptions align)
        {
            TMP_Text t = Text(memo, name, key != null ? null : string.Empty, 22, align, aMin, aMax, colour, ThemeRoleId.Newsletter, key);
            t.raycastTarget = false;
            if (_kit != null)
                SceneUiKit.SkinText(t, _kit, kind, (aMax.y - aMin.y) * BureauMemoSize.y, colour);
            return t;
        }

        MemoLine("Header", "briefing.bulletinHeader", KitText.Pill, new Vector2(0.07f, 0.8f), new Vector2(0.93f, 0.9f), red, TextAlignmentOptions.MidlineLeft);
        title = MemoLine("Title", null, KitText.ListTitle, new Vector2(0.07f, 0.64f), new Vector2(0.93f, 0.79f), ink, TextAlignmentOptions.TopLeft);
        title.fontStyle |= FontStyles.Bold;
        body = MemoLine("Body", null, KitText.BodySmall, new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.63f), ink, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Ellipsis;

        // The brass clip holding it on.
        Transform clip = Panel(memo, "Clip", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(96f, 30f), Color.white, ThemeRoleId.Newsletter);
        clip.GetComponent<Image>().raycastTarget = false;
        KitSkin(clip, "pill_brass", scale);
        memo.gameObject.SetActive(false);
        return memo;
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
