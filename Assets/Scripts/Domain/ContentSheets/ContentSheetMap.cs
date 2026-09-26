using System.Linq;
using static ColumnSpec;
using static SheetSpec;

/// <summary>
/// The content spreadsheet's map of Assets/Data/World/world_source.json: one sheet per
/// table, one column per field, in the JSON's own order, with each column's type and
/// rules as data. The engine (<see cref="ContentSheets"/>) reads nothing else, so a new
/// section of the source is a new entry here:
/// <list type="bullet">
/// <item>a new field: a column where the JSON orders it (<c>Text("kinds")</c>, <c>.Omit()</c> when the JSON leaves it out at its default, <c>.Ref("sheet")</c> when it names another sheet's row);</item>
/// <item>a new list of objects: <c>Rows("sheetName", "jsonKey", Key("id", "childHeader"), columns...)</c> where the JSON orders it (Key only when rows have child sheets or are referred to);</item>
/// <item>a list of plain values: <c>List("jsonKey")</c> for ids ("a|b" in one cell) or <c>Values("sheetName", "jsonKey", Text("text"))</c> for sentences;</item>
/// <item>a nested object: dotted column paths ("culture.language"), or <c>Single</c> under a one-row sheet.</item>
/// </list>
/// The round-trip test (ContentSheetMapTests) fails, naming the JSON path, until every key
/// of the source is mapped. Sheet names are at most 31 characters (Excel's limit).
/// </summary>
public static class ContentSheetMap
{
    /// <summary>The whole map, rooted at the document.</summary>
    public static readonly SheetSpec World =
        Single("world", "",
            Int("travellerAgeMin").Note("youngest ordinary traveller"),
            Int("travellerAgeMax").Note("oldest ordinary traveller"),
            Looks(),
            Content(),
            Rows("eras", "eras", Key("id", "era"),
                Text("id").Required(),
                Text("displayName"),
                Int("order"),
                Bool("future").Omit().Note("the office's own time (at most one era)"),
                Values("eraSmallTalk", "smallTalk", Text("text")).Note("small talk any traveller of the era may say")),
            Countries(),
            Places(),
            Rows("rules", "rules", Key("asset"),
                Text("asset").Required().Note("the rule asset's name"),
                Text("type"),
                Text("country").Ref("countries"),
                Text("era").Ref("eras"),
                Text("description")).Note("travel rules a day can switch on"),
            Days(),
            Interview(),
            Questions(),
            Dialogs(),
            Premades(),
            History(),
            Ui(),
            Translation());

    private static SheetSpec Looks() =>
        Single("looks", "looks",
            Rows("faceBands", "faceBands",
                Int("minAge"),
                List("faces")),
            Int("greyFromAge"),
            Text("wholeFigureLabel"),
            Rows("confusable", "confusable",
                Text("a").Ref("places"),
                Text("b").Ref("places"),
                Text("slot"),
                Text("gender"),
                Text("why").Note("a note for reviewers")).Note("two places whose items in a slot look alike"));

    private static SheetSpec Content() =>
        Single("content", "content",
            Text("library"),
            Text("blueprint"),
            Text("dayPlanFolder"),
            Rows("contentAttributes", "attributes", Key("id"),
                Text("id").Required(),
                Text("asset")),
            Values("contentArchetypes", "archetypes", Text("asset")),
            Values("contentBooks", "books", Text("asset"))).Note("the authored assets the world is wired into (asset paths)");

    private static SheetSpec Countries() =>
        Rows("countries", "countries", Key("id", "country"),
            new SheetField[]
            {
                Text("id").Required(),
                Text("displayName"),
                Rows("countryBaselines", "baselines",
                    Text("attribute").Ref("contentAttributes"),
                    Float("score")),
                Nums("looks.skin").Note("skin tone weights 1..5"),
                Rows("countryHair", "looks.hair",
                    Text("colour"),
                    Num("weight")),
            }.Concat(Culture("culture.", "country")).ToArray());

    private static SheetSpec Places() =>
        Rows("places", "places", Key("{country}_{era}", "place"),
            Text("country").Required().Ref("countries"),
            Text("era").Required().Ref("eras"),
            Text("displayName"),
            Text("moment").Note("research context only (not generated)"),
            Int("year"),
            Text("tongue").Ref("tongues"),
            Rows("placeFacts", "facts",
                Text("category"),
                Text("value")),
            List("maleNames"),
            List("femaleNames"),
            Values("placeSmallTalk", "smallTalk", Text("text")).OmitEmpty(),
            Keyed("wardrobeSets", "wardrobe", Text("gender").OneOf("m", "f"),
                Text("signature").Note("the slot whose item is the dress signature"),
                Keyed("wardrobe", "", Text("slot").OneOf("outfit", "hair", "facialHair", "headwear", "accessory"),
                    Text("label").Required(),
                    Bool("wig").Omit(),
                    Bool("back").Omit(),
                    Bool("leakable").Omit(),
                    List("covers").Omit().Note("slots this item hides"),
                    Text("artNation").Omit().Note("files the art under another nation (blank: the place's own)")).Note("one worn item per row")),
            Nums("looks.skin").Omit().Note("overrides the country's skin weights"),
            Rows("placeHair", "looks.hair",
                Text("colour"),
                Num("weight")).OmitEmpty().Note("overrides the country's hair weights"))
            .Note("a place is a country in an era; child sheets name it {country}_{era}");

    private static SheetSpec Days() =>
        Rows("days", "days", Key("day", "day"),
            Text("asset").Required(),
            Int("day").Required(),
            Int("queue"),
            Int("tells"),
            List("channels"),
            Rows("dayEras", "eras",
                Text("era").Ref("eras"),
                Num("weight")),
            List("countries").Ref("countries"),
            List("rules").Ref("rules"),
            List("premades").Ref("premades").Note("premades that may roll this day"),
            Rows("dayForced", "forced",
                Int("slot"),
                Text("premade").Ref("premades"),
                Text("blueprint")),
            Float("premadeChance"));

    private static SheetSpec Interview() =>
        Single("interview", "interview",
            Text("deskName"),
            Text("opener"),
            Text("openerLegendary"),
            Text("claim"),
            Text("honorificMale"),
            Text("honorificFemale"),
            Text("honorificUnknown"),
            Text("requestLabel"),
            Text("requestPrompt"),
            Text("requestReply"),
            Text("askLabel"),
            Text("backLabel"),
            Text("smallTalkLabel"),
            Text("smallTalkPrompt"),
            Rows("interviewRequests", "requests", Key("id"),
                Text("id").Required(),
                Text("label"),
                Text("prompt"),
                Text("reply")).Note("spoken requests: the wheel entry, the desk's prompt and the traveller's reply"),
            Int("menuCapacity"),
            Int("maxLineChars"),
            Text("lookLabel")).Note("the interview's wording");

    private static SheetSpec Questions() =>
        Rows("questions", "questions", Key("id", "question"),
            Text("id").Required(),
            Text("category"),
            Text("label"),
            Text("prompt"),
            Text("answer"),
            Int("fromDay"),
            Text("announce"),
            GateConditions("questionConditions"),
            Rows("questionOverrides", "overrides",
                Text("era").Ref("eras"),
                Text("prompt"),
                Text("answer")).Note("the wording in one era"));

    private static SheetSpec Dialogs() =>
        Rows("dialogs", "dialogs", Key("id", "dialog"),
            Text("id").Required(),
            Text("label").Note("the wheel entry"),
            Bool("repeatable").Omit(),
            GateConditions("dialogConditions"),
            Rows("dialogNodes", "nodes", Key("id", "node"),
                Text("id").Required(),
                Lines("dialogLines"),
                Rows("dialogChoices", "choices", Key("id", "choice"),
                    Text("id").Required(),
                    Text("label"),
                    Lines("choiceLines"),
                    Text("next").Note("the node it leads to (blank: the dialog ends)"),
                    Text("effect")))).Note("narrative dialogs: nodes, their lines and the player's choices");

    private static SheetSpec Premades() =>
        Rows("premades", "premades", Key("id", "premade"),
            Text("id").Required(),
            Text("name"),
            Text("gender"),
            Text("place").Ref("places"),
            Text("truePlace").Ref("places").Note("where they really come from (blank: honest)"),
            Text("birthDate"),
            Text("archetype"),
            Text("intro"),
            Text("recordNote"),
            Text("dialog").Ref("dialogs"),
            Bool("repeatable"),
            Rows("premadeImpacts", "impacts",
                Text("attribute").Ref("contentAttributes"),
                Num("onCorrect"),
                Num("onWrong"),
                Bool("skipNationScore").Omit())).Note("premade characters: real people and written stories");

    private static SheetSpec History() =>
        Single("history", "history",
            Text("lines.leaderGained"),
            Text("lines.leaderLost"),
            Text("lines.carry"),
            Rows("historyRules", "rules", Key("id", "rule"),
                Text("id").Required(),
                Text("name"),
                Text("news"),
                Rows("historyConditions", "conditions",
                    Text("type").Required(),
                    Text("key").Omit(),
                    Text("nation").Omit().Ref("countries"),
                    Text("place").Omit().Ref("places"),
                    Text("attribute").Omit().Ref("contentAttributes"),
                    Num("threshold").Omit()),
                Rows("historyEdits", "edits",
                    Text("place").Ref("places"),
                    Text("category"),
                    Text("value"))).Note("history rules: when their conditions pass at night they rewrite a place's fact"));

    private static SheetSpec Ui() =>
        Single("ui", "ui",
            Text("readingLanguage"),
            Int("glossPercent"),
            Float("labelMinScale"),
            Float("contrast.text"),
            Float("contrast.largeText"),
            Float("contrast.glyph"),
            Float("contrast.hint"),
            Float("contrast.outline"),
            Float("contrast.minTextFillAlpha"),
            Text("latinFallbackFont"),
            Rows("uiPalette", "paletteMap",
                Text("role"),
                Text("fill"),
                Num("alpha"),
                Text("ink"),
                Text("textClass")),
            Single("uiNeutral", "neutral", new SheetField[] { Text("id") }.Concat(Culture("", "neutral")).ToArray()),
            Rows("uiStrings", "strings", Key("key"),
                Text("key").Required(),
                Text("text"),
                Text("gloss"),
                Text("tier")),
            Rows("uiLanguages", "languages", Key("language", "language"),
                Text("language").Required(),
                Bool("rtl"),
                Rows("uiLanguageEntries", "entries",
                    Text("key").Ref("uiStrings"),
                    Text("text"))));

    private static SheetSpec Translation() =>
        Single("translation", "translation",
            Int("fromDay"),
            Text("announce"),
            Float("flip.startDelay"),
            Float("flip.letterInterval"),
            Float("flip.letterSeconds"),
            Int("flip.scrambleSteps"),
            Float("flip.rowStagger"),
            Text("fallbackGlyphs"),
            Rows("scripts", "scripts", Key("id", "script"),
                Text("id").Required(),
                Bool("rightToLeft"),
                Fonts("scriptFonts", "fonts")),
            Rows("packs", "packs", Key("id"),
                Text("id").Required(),
                Text("displayName"),
                Int("writtenCost"),
                Int("spokenCost")),
            Rows("tongues", "tongues", Key("id"),
                Text("id").Required(),
                Text("displayName"),
                Text("script").Ref("scripts"),
                Text("pack").Ref("packs"),
                Text("glyphs")));

    // ---- shared shapes ----

    /// <summary>A culture block (a country's "culture" or ui.neutral): the same columns and child sheets under a path prefix.</summary>
    private static SheetField[] Culture(string prefix, string sheetPrefix) => new SheetField[]
    {
        Text(prefix + "displayName"),
        Text(prefix + "language"),
        Bool(prefix + "runtimeFont"),
        Fonts(sheetPrefix + "Fonts", prefix + "fonts"),
        Text(prefix + "headingAdd"),
        Text(prefix + "buttonAdd"),
        Bool(prefix + "stripItalic"),
        Rows(sheetPrefix + "Seeds", prefix + "seeds",
            Text("name"),
            Text("hex")),
        Rows(sheetPrefix + "Overrides", prefix + "overrides",
            Text("role"),
            Text("fill"),
            Text("ink")),
        Rows(sheetPrefix + "Art", prefix + "art",
            Text("name"),
            Text("hex")),
        Text(prefix + "wallpaper"),
    };

    private static SheetSpec Fonts(string name, string path) =>
        Rows(name, path,
            Text("file"),
            Int("face"),
            Text("family"),
            Text("style")).Note("font candidates, tried in order");

    /// <summary>The gate conditions of a question or a dialog (every field written).</summary>
    private static SheetSpec GateConditions(string name) =>
        Rows(name, "conditions",
            Text("type").Required(),
            Text("key"),
            Num("threshold"),
            Text("place").Omit().Ref("places"),
            Text("attribute").Omit().Ref("contentAttributes"),
            Text("nation").Omit().Ref("countries")).Note("all must pass");

    /// <summary>Dialog lines: a node's or a choice's.</summary>
    private static SheetSpec Lines(string name) =>
        Rows(name, "lines",
            Text("id"),
            Text("speaker"),
            Text("expression").Omit(),
            Text("text"));
}
