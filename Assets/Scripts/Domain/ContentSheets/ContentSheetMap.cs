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
            Single("agency", "agency",
                Text("name").Note("the agency's printed name"),
                Text("programme"),
                Text("firstDate").Note("the desk calendar's date on day 1"),
                Int("displaced.foundWithinDays").Note("a displaced person was found 1 to this many days before today"),
                Int("displaced.validDaysMin").Note("the fewest days after today a Displacement Certificate is valid"),
                Int("displaced.validDaysMax").Note("the most days after today a Displacement Certificate is valid"),
                Text("clerk.citizenId").Note("the clerk's own Citizen Account (the Citizen Account app)"),
                Text("clerk.name"),
                Text("clerk.born"),
                Text("clerk.lineage"),
                Text("clerk.employment"),
                Text("clerk.note"),
                Int("clerk.startDebt").Note("the clerk's own debt at the start of a run, in cr"),
                Num("clerk.garnishShare").Note("the share of each shift's pay that goes to the clerk's debt (0.25 = 25%)"),
                Text("clerk.reliefEmployer").Note("the clerk's own Debt Relief Labour Contract on the bankrupt ending"),
                Text("clerk.reliefWorksite"),
                Int("clerk.reliefWage").Note("the contract's day wage in cr; its term is the debt at this wage"),
                Single("agencyAccounts", "accounts",
                    Int("validDaysMin").Note("the fewest days after today a citizen's honest paper is valid"),
                    Int("validDaysMax").Note("the most days after today a citizen's honest paper is valid"),
                    Int("tripsWithinDays").Note("a past trip on an account left 1 to this many days ago"),
                    Rows("agencyStatuses", "statuses",
                        Text("status").Required().OneOf("Premium", "Standard", "Eligible"),
                        Int("debtMin"),
                        Int("debtMax"),
                        Int("tripsMin"),
                        Int("tripsMax")).Note("each account status's debt and past trips")).Note("the ranges a 2150 citizen's Citizen Account is drawn from"),
                Rows("agencyTransponders", "transponders",
                    Text("id").Required(),
                    Text("transponderClass").OneOf("Premium", "Economy"),
                    Text("model"),
                    Text("prefix").Note("the serial's prefix (HP gives HP-40718)"),
                    Num("weight")).Note("the transponder models citizens travel on, weighted per class")),
            Rows("eras", "eras", Key("id", "era"),
                Text("id").Required(),
                Text("displayName"),
                Int("order"),
                Bool("future").Omit().Note("the office's own time (at most one era)"),
                Values("eraSmallTalk", "smallTalk", Text("text")).Note("small talk any traveller of the era may say")),
            Countries(),
            Places(),
            Present(),
            Rows("rules", "rules", Key("asset"),
                Text("asset").Required().Note("the rule asset's name"),
                Text("type"),
                Text("country").Omit().Ref("countries").Note("a closure's country (blank: none)"),
                Text("era").Omit().Ref("eras").Note("a closure's era (blank: none)"),
                Text("description")).Note("travel rules a day can switch on: closures and standing procedures"),
            Days(),
            Interview(),
            Questions(),
            Dialogs(),
            Premades(),
            History(),
            Values("newsDebt", "news.debt", Text("text")).Note("the morning paper's debt lines: one a day, in a shuffled order per run"),
            Pc(),
            Ui(),
            Translation());

    private static SheetSpec Looks() =>
        Single("looks", "looks",
            Rows("faceBands", "faceBands",
                Int("minAge"),
                List("faces")),
            Int("greyFromAge"),
            Text("wholeFigureLabel"),
            Num("costumeErrors.otherPlace").Note("weight of a costume error that is another of today's places' item"),
            Num("costumeErrors.presentClothes").Note("weight of a costume error that is the present's whole look (2150 clothes)"),
            Num("costumeErrors.presentAccessory").Note("weight of a costume error that is one 2150 accessory"),
            Rows("confusable", "confusable",
                Text("a").Ref("places"),
                Text("b").Ref("places"),
                Text("slot"),
                Text("gender"),
                Text("why").Note("a note for reviewers")).Note("two places whose items in a slot look alike"));

    private static SheetSpec Content() =>
        Single("content", "content",
            Text("library"),
            Rows("contentBlueprints", "blueprints",
                Text("kind").Required().OneOf("RichTourist", "PoorTourist", "Labourer", "Displaced"),
                Text("asset")).Note("each traveller kind's case blueprint"),
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
            Text("moment").Note("the place's moment in a sentence (Chronopedia's article)"),
            Int("year"),
            Text("tongue").Ref("tongues"),
            Rows("placeFacts", "facts",
                Text("category"),
                Text("value")),
            List("maleNames"),
            List("femaleNames"),
            Values("placeSmallTalk", "smallTalk", Text("text")).OmitEmpty(),
            Wardrobe("wardrobe", "wardrobe"),
            Nums("looks.skin").Omit().Note("overrides the country's skin weights"),
            Rows("placeHair", "looks.hair",
                Text("colour"),
                Num("weight")).OmitEmpty().Note("overrides the country's hair weights"))
            .Note("a place is a country in an era; child sheets name it {country}_{era}");

    /// <summary>The neutral present (the present while no nation leads): its name, year and facts; its clothes (its Culture fact is derived from them, and a 2150 citizen who forgot their costume wears them whole) and its 2150 accessory kit (costume errors).</summary>
    private static SheetSpec Present() =>
        Single("present", "present",
            Text("displayName"),
            Int("year"),
            Rows("presentFacts", "facts",
                Text("category"),
                Text("value")),
            Wardrobe("presentWardrobe", "wardrobe").Note("the present's clothes, worn whole by a 2150 citizen who forgot their costume"),
            Rows("presentKit", "kit",
                Text("gender").Required().OneOf("m", "f"),
                Text("label").Required(),
                Text("variant").Required().Note("the art key token: accessory_{gender}_neutral_future_{variant}"))
                .Note("the present's 2150 accessory kit, one item per row: one can slip onto a right costume (a costume error)"))
            .Note("the present, 2150, while no nation leads: never a destination; every book lists its row");

    /// <summary>A wardrobe at <paramref name="path"/>: per gender its signature slot and one worn item per slot (a place's, or the present's).</summary>
    private static SheetSpec Wardrobe(string name, string path) =>
        Keyed(name + "Sets", path, Text("gender").OneOf("m", "f"),
            Text("signature").Note("the slot whose item is the dress signature"),
            Keyed(name, "", Text("slot").OneOf("outfit", "hair", "facialHair", "headwear", "accessory"),
                Text("label").Required(),
                Bool("wig").Omit(),
                Bool("back").Omit(),
                Bool("leakable").Omit(),
                List("covers").Omit().Note("slots this item hides"),
                Text("artNation").Omit().Note("files the art under another nation (blank: the place's own)")).Note("one worn item per row"));

    private static SheetSpec Days() =>
        Rows("days", "days", Key("day", "day"),
            Text("asset").Required(),
            Int("day").Required(),
            Int("queue"),
            Int("tells"),
            List("channels"),
            Rows("dayKinds", "kinds",
                Text("kind").Required().OneOf("RichTourist", "PoorTourist", "Labourer", "Displaced"),
                Num("weight")).Note("the day's traveller mix: each kind's weight"),
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
            Float("premadeChance"),
            Float("costumeErrorChance").Note("chance per 2150 citizen of a costume error (0 before the dress rule's first day)"));

    private static SheetSpec Interview() =>
        Single("interview", "interview",
            Text("deskName"),
            Text("opener"),
            Text("openerLegendary"),
            Rows("claims", "claims",
                Text("kind").Required().OneOf("RichTourist", "PoorTourist", "Labourer", "Displaced"),
                Text("text")).Note("the claim per traveller kind ({place}): one row per kind a blueprint makes"),
            Text("honorificMale"),
            Text("honorificFemale"),
            Text("honorificUnknown"),
            Text("requestLabel"),
            Text("papersLabel").Note("the hub entry that opens the papers menu"),
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
            Text("lines.dominant").Note("an attribute becomes dominant in a place: {attribute} and {place}"),
            Text("lines.panic").Note("an accepted costume error caused a panic: {place} and {value} (the wrong item)"),
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

    /// <summary>The PC block: the Internet's sites, the Static sites' authored pages, the Lineage Archive's people and relations, and Mail's authored messages.</summary>
    private static SheetSpec Pc() =>
        Single("pc", "pc",
            Rows("pcSites", "sites", Key("id", "site"),
                Text("id").Required(),
                Text("kind").OneOf("News", "History", "Ancestry", "Static").Note("the page builder that serves it"),
                Text("name"),
                Text("domain").Note("chronet://{domain}: lower-case letters, digits, dots and dashes"),
                Text("glyph").Note("the start-page tile's art key"),
                Text("blurb"),
                Int("fromDay").Note("the first day it is listed")).Note("the Internet's sites"),
            Rows("pcPages", "pages", Key("{site}/{path}", "page"),
                Text("site").Required().Ref("pcSites"),
                Text("path").Note("blank: the site's front page"),
                Text("title"),
                Rows("pcPageBlocks", "blocks",
                    Text("kind").OneOf("Headline", "Heading", "Paragraph", "Note", "Link", "Box"),
                    Text("text"),
                    Text("address").Omit().Note("a Link's site name, domain or chronet:// address"),
                    List("lines").Omit().Note("a Box's lines"))).Note("a Static site's authored pages"),
            Bool("ancestry.includePremades").Note("the premades who are who they claim get cards"),
            Rows("pcPeople", "ancestry.people", Key("id", "person"),
                Text("id").Required(),
                Text("name"),
                Text("born"),
                Text("died"),
                Text("place").Ref("places"),
                Text("note")).Note("the Lineage Archive's people of the past (no traveller's name)"),
            Rows("pcRelations", "ancestry.relations",
                Text("person").Required().Note("a person's or a premade's id: the card it shows on"),
                Text("kind"),
                Text("other").Required().Note("the other card's id")).Note("one direction per row"),
            Rows("pcMail", "mail", Key("id", "mail"),
                Text("id").Required(),
                Int("fromDay").Note("the day it arrives (its date)"),
                Int("untilDay").Note("the last day it is in the inbox; 0 keeps it for the run"),
                Text("flag").Note("a story flag it waits for (blank: none)"),
                Text("from"),
                Text("subject"),
                Values("pcMailBody", "body", Text("text")).Note("its paragraphs")).Note("Mail's authored messages"));

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
            List("keyWords.slots").Note("slots whose value stays English in untranslated speech"),
            List("keyWords.words").Note("words that stay English in untranslated speech"),
            Bool("keyWords.digits"),
            Text("fallbackGlyphs"),
            Rows("scripts", "scripts", Key("id", "script"),
                Text("id").Required(),
                Bool("rightToLeft"),
                Fonts("scriptFonts", "fonts")),
            Rows("packs", "packs", Key("id"),
                Text("id").Required(),
                Text("displayName"),
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
