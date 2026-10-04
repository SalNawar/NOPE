using System;
using System.Collections.Generic;

/// <summary>
/// One voice row (world_source.json interview.voices.{list}[], and the kinds'
/// small talk, interview.kindSmallTalk): a line one personality or one premade
/// says in one slot (a default row names neither), for the kinds it names (none:
/// any) and the claimed era it names (blank: any), under the slot's key (the
/// personalities spec's §4.1-4.2).
/// </summary>
[Serializable]
public sealed class VoiceLine
{
    /// <summary>The personality that says it (Personality.id); blank for a premade's row or a default.</summary>
    public string personality = string.Empty;

    /// <summary>The premade that says it (LegendarySO.id); blank for a personality's row or a default.</summary>
    public string premade = string.Empty;

    /// <summary>The kinds it is for; empty: any kind. Serialized TravellerKind values (append only).</summary>
    public List<TravellerKind> kinds = new List<TravellerKind>();

    /// <summary>The claimed era it is for (EraSO.id); blank: any era.</summary>
    public string era = string.Empty;

    /// <summary>
    /// The slot's key: a missing form's or a hand-over's request (a form number
    /// or a group id; a hand-over's may be blank: any), a spoken request's id,
    /// an answer's question id; blank for a claim or small talk.
    /// </summary>
    public string key = string.Empty;

    /// <summary>A missing form's variant (Honest: the kind never needs it; Missing: they left it out).</summary>
    public MissingFormVariant variant;

    /// <summary>A reaction's verdict (the stamp: Accepted or Denied; the personalities spec's R2).</summary>
    public ReactionVerdict verdict;

    /// <summary>A reaction's intent (Honest or Lying; R2).</summary>
    public ReactionIntent intent;

    /// <summary>A reaction's fault reason (Faults.Reasons: forged, smuggled, closed, panic...); blank: any.</summary>
    public string reason = string.Empty;

    /// <summary>A slip's lie kind (a LieKind's name); blank: any (the personalities spec's T10). A reply to a question about a difference may name one too.</summary>
    public string lie = string.Empty;

    /// <summary>A reply to the desk's question about a logged difference: how the traveller answers (Confrontations; wave 5, lesson 3). Its row may also name a fault reason and a lie kind.</summary>
    public ConfrontOutcome outcome;

    /// <summary>The line ("interview.voices.{list}.{voice}.{n}"), with the slot's tokens.</summary>
    public LineText line = new LineText();

    /// <summary>A reaction's second line ("{line id}.then"); blank text: none (R1).</summary>
    public LineText then = new LineText();
}

/// <summary>The personalities' and premades' rows, one list per voice slot (world_source.json interview.voices; the defaults stay where they were, InterviewLines).</summary>
[Serializable]
public sealed class VoiceBook
{
    /// <summary>The claim ({place} required).</summary>
    public List<VoiceLine> claims = new List<VoiceLine>();

    /// <summary>The reply as a paper is handed over ({document}, {place}).</summary>
    public List<VoiceLine> handOver = new List<VoiceLine>();

    /// <summary>A refusal of a form the traveller does not carry, by request and variant ({document}, {place}).</summary>
    public List<VoiceLine> missingForms = new List<VoiceLine>();

    /// <summary>The reply to a spoken request ("Step closer"), by its id ({place}).</summary>
    public List<VoiceLine> spoken = new List<VoiceLine>();

    /// <summary>An answer, by question id ({value} required, {place}).</summary>
    public List<VoiceLine> answers = new List<VoiceLine>();

    /// <summary>The personality's (or a premade's) small talk ({place}).</summary>
    public List<VoiceLine> smallTalk = new List<VoiceLine>();

    /// <summary>The reaction to the stamp, by verdict, intent and optionally a fault reason: a line and an optional then line ({place}; the personalities spec's R1-R3).</summary>
    public List<VoiceLine> reactions = new List<VoiceLine>();

    /// <summary>A liar's slip after small talk, optionally by lie kind ({place}; T9-T11).</summary>
    public List<VoiceLine> slips = new List<VoiceLine>();

    /// <summary>The answer to the desk's waiver pad, by reply (the row's key: a WaiverPadReply's name; {place}; the endings and strandings spec §7.3).</summary>
    public List<VoiceLine> waiverPad = new List<VoiceLine>();

    /// <summary>The reply to the desk's question about a logged difference, by outcome and optionally a fault reason or a lie kind ({value}, {other}, {place}; wave 5, lesson 3).</summary>
    public List<VoiceLine> confront = new List<VoiceLine>();
}

/// <summary>How small talk picks its source (world_source.json interview.smallTalkWeights; the personalities spec's V5): the personality's lines, the home's, the kind's.</summary>
[Serializable]
public sealed class SmallTalkWeights
{
    /// <summary>The personality's own lines.</summary>
    public float personality = 1f;

    /// <summary>The home's lines (a displaced person's claimed place, else its era; a 2150 citizen's present, else the Future era).</summary>
    public float home = 1f;

    /// <summary>The kind's lines (interview.kindSmallTalk).</summary>
    public float kind = 1f;
}

/// <summary>Who speaks (the personalities spec's PS5): the traveller's personality, or the premade they are, and the dialog seed every line pick is a value of.</summary>
public sealed class Voice
{
    /// <summary>A voice of <paramref name="personality"/> (blank: none) or <paramref name="premade"/> (a premade speaks only its own rows), picking with <paramref name="seed"/>.</summary>
    public Voice(string personality, string premade, int seed)
    {
        Personality = personality ?? string.Empty;
        Premade = premade ?? string.Empty;
        Seed = seed;
    }

    /// <summary>The personality's id (CaseInstance.personality); blank for a premade or an empty cast.</summary>
    public string Personality { get; }

    /// <summary>The premade's id; blank for a generated traveller.</summary>
    public string Premade { get; }

    /// <summary>The dialog seed (CaseInstance.dialogSeed).</summary>
    public int Seed { get; }

    /// <summary>No voice: every line is the default.</summary>
    public static readonly Voice None = new Voice(null, null, 0);
}

/// <summary>
/// What a line may depend on before the stamp (the personalities spec's T2):
/// the kind as presented and the claimed era, nothing else. A field added
/// here would let the hidden truth choose a line (VoicesTests pins the fields).
/// </summary>
public sealed class VoiceContext
{
    /// <summary>The kind the traveller presents as.</summary>
    public readonly TravellerKind kind;

    /// <summary>The claimed era's id.</summary>
    public readonly string eraId;

    /// <summary>A context of <paramref name="kind"/> and <paramref name="eraId"/>.</summary>
    public VoiceContext(TravellerKind kind, string eraId)
    {
        this.kind = kind;
        this.eraId = eraId;
    }
}

/// <summary>The slot keys a line's pick is a value of (Seeds.OfKey; the personalities spec's §4.3 step 5): one per verb, so a line added to one slot never moves another's pick.</summary>
public static class VoiceKeys
{
    /// <summary>The claim.</summary>
    public const string Claim = "claim";

    /// <summary>The source small talk draws on.</summary>
    public const string SmallTalkSource = "smalltalk:source";

    /// <summary>The small-talk line within its source.</summary>
    public const string SmallTalk = "smalltalk";

    /// <summary>A hand-over of <paramref name="request"/>.</summary>
    public static string HandOver(string request) => "handover:" + request;

    /// <summary>A refusal of <paramref name="request"/> for <paramref name="variant"/>.</summary>
    public static string Missing(string request, MissingFormVariant variant) => $"missing:{request}:{variant}";

    /// <summary>The reply to the spoken request <paramref name="id"/>.</summary>
    public static string Spoken(string id) => "spoken:" + id;

    /// <summary>The answer to question <paramref name="questionId"/>.</summary>
    public static string Answer(string questionId) => "answer:" + questionId;

    /// <summary>A liar's slip.</summary>
    public const string Slip = "slip";

    /// <summary>The reaction to <paramref name="verdict"/> with <paramref name="intent"/>.</summary>
    public static string Reaction(ReactionVerdict verdict, ReactionIntent intent) => $"reaction:{verdict}:{intent}";

    /// <summary>The answer to the waiver pad with <paramref name="reply"/>.</summary>
    public static string WaiverPad(WaiverPadReply reply) => "waiverpad:" + reply;

    /// <summary>The reply to the question about a difference in <paramref name="category"/> (the line's pick).</summary>
    public static string Confront(ClueCategory category) => "confront:" + category;

    /// <summary>Whether a liar cracks over a difference in <paramref name="category"/> (Confrontations.Outcome: a value, never a draw).</summary>
    public static string ConfrontRoll(ClueCategory category) => "confront:roll:" + category;
}

/// <summary>
/// The voice resolver (the personalities spec's V1-V5, §4.3): for one slot and
/// one traveller, a premade's own rows first; else the rows of the traveller's
/// personality whose kinds and era match their context (ContextMatch: named
/// kinds 2, a named era 1) and whose keys equal the verb's (a named optional
/// key, a hand-over's request, 4), the most specific tier as the pool; an empty
/// pool falls back to the slot's defaults (InterviewLines); one line of the
/// pool as a value of the dialog seed and the slot key (Pick), never a draw.
/// Small talk picks its source first (the personality's, the home's, the
/// kind's) by interview.smallTalkWeights, then a line, both as values. Lines
/// come back as templates; InterviewScript fills them. Pure.
/// </summary>
public static class Voices
{
    /// <summary>What a named optional key adds to a row's score (a hand-over's request; phase V3 a reason, V4 a lie kind).</summary>
    public const int NamedKeyScore = 4;

    /// <summary>The index of one of <paramref name="count"/> lines: (uint)Mix(seed, OfKey(slotKey)) % count; -1 for none.</summary>
    public static int Pick(int seed, string slotKey, int count) =>
        count <= 0 ? -1 : (int)((uint)Seeds.Mix(seed, Seeds.OfKey(slotKey)) % (uint)count);

    /// <summary>
    /// The voice's best tier of <paramref name="rows"/>: the rows naming its
    /// premade (for a premade) or its personality (for anyone else), whose
    /// context matches and whose keys match (<paramref name="key"/>: the extra
    /// score of a row, ContextMatch.NoMatch when its keys do not match); the
    /// rows of the highest summed score, in list order. Empty for no voice.
    /// </summary>
    public static List<VoiceLine> Pool(IReadOnlyList<VoiceLine> rows, Voice voice, VoiceContext context, Func<VoiceLine, int> key)
    {
        var pool = new List<VoiceLine>();
        if (voice == null || rows == null)
            return pool;
        bool premade = !string.IsNullOrEmpty(voice.Premade);
        if (!premade && string.IsNullOrEmpty(voice.Personality))
            return pool;

        foreach (VoiceLine row in rows)
            if (row != null && (premade ? row.premade == voice.Premade : string.IsNullOrEmpty(row.premade) && row.personality == voice.Personality))
                Add(pool, row, context, key);
        return Best(pool, context, key);
    }

    /// <summary>The traveller's claim: the voice's, else their kind's (interview.claims); null when the kind has none (the caller says the place alone).</summary>
    public static LineText Claim(InterviewLines lines, Voice voice, VoiceContext context) =>
        Of(Pool(Book(lines).claims, voice, context, _ => 0), voice, VoiceKeys.Claim) ?? Interview.ClaimLine(lines, context.kind);

    /// <summary>The traveller's reply as they hand <paramref name="request"/> over: the voice's (a row naming the request before a blank one), else interview.requestReply.</summary>
    public static LineText HandOver(InterviewLines lines, Voice voice, VoiceContext context, string request) =>
        Of(Pool(Book(lines).handOver, voice, context, r => string.IsNullOrEmpty(r.key) ? 0 : r.key == request ? NamedKeyScore : ContextMatch.NoMatch), voice, VoiceKeys.HandOver(request))
        ?? lines?.requestReply;

    /// <summary>
    /// The traveller's refusal of <paramref name="request"/>, a form they do
    /// not carry, for <paramref name="variant"/>: the voice's row, else the
    /// kind's default (interview.missingFormReplies). A Missing variant with no
    /// line of either for the request (a form the kind never carries: only a
    /// form they should carry was left out) says the Honest line the same way.
    /// Null when none is authored.
    /// </summary>
    public static LineText Missing(InterviewLines lines, Voice voice, VoiceContext context, string request, MissingFormVariant variant)
    {
        LineText line = MissingOf(lines, voice, context, request, variant);
        return line != null || variant == MissingFormVariant.Honest ? line : MissingOf(lines, voice, context, request, MissingFormVariant.Honest);
    }

    /// <summary>The traveller's reply to the spoken <paramref name="request"/>: the voice's, else the request's own reply.</summary>
    public static LineText Spoken(InterviewLines lines, Voice voice, VoiceContext context, InterviewRequest request) =>
        request == null ? null
        : Of(Pool(Book(lines).spoken, voice, context, r => r.key == request.id ? 0 : ContextMatch.NoMatch), voice, VoiceKeys.Spoken(request.id)) ?? request.reply;

    /// <summary>The traveller's answer template for <paramref name="question"/>: the voice's, else the question's override for the kind and era, else its answer (InterviewQuestion.AnswerFor).</summary>
    public static LineText Answer(InterviewLines lines, Voice voice, VoiceContext context, InterviewQuestion question) =>
        question == null ? null
        : Of(Pool(Book(lines).answers, voice, context, r => r.key == question.id ? 0 : ContextMatch.NoMatch), voice, VoiceKeys.Answer(question.id))
          ?? question.AnswerFor(context.eraId, context.kind);

    /// <summary>
    /// The home's small talk (the personalities spec's V5): a displaced
    /// person's claimed place's lines, else its era's; a 2150 citizen's
    /// present's lines, else the Future era's (the present is their home,
    /// never the destination). Empty when neither has one.
    /// </summary>
    public static IReadOnlyList<LineText> Home(TravellerKind kind, IReadOnlyList<LineText> claimedPlace, IReadOnlyList<LineText> claimedEra,
                                               IReadOnlyList<LineText> presentPlace, IReadOnlyList<LineText> futureEra)
    {
        bool citizen = TravellerKinds.IsCitizen(kind);
        IReadOnlyList<LineText> first = citizen ? presentPlace : claimedPlace, second = citizen ? futureEra : claimedEra;
        return first != null && first.Count > 0 ? first : second ?? Array.Empty<LineText>();
    }

    /// <summary>
    /// The traveller's small talk (the personalities spec's V5): a premade's
    /// own lines when it has any (its only source); otherwise a source picked
    /// by interview.smallTalkWeights among those with a line (the
    /// personality's best tier, <paramref name="home"/>, the kind's best tier
    /// of interview.kindSmallTalk; a zero weight is never picked) as a value
    /// of the seed and "smalltalk:source", then a line of it as a value of
    /// "smalltalk". Null when no source has a line.
    /// </summary>
    public static LineText SmallTalk(InterviewLines lines, Voice voice, VoiceContext context, IReadOnlyList<LineText> home)
    {
        voice = voice ?? Voice.None;
        VoiceBook book = Book(lines);
        if (!string.IsNullOrEmpty(voice.Premade))
        {
            LineText own = Of(Pool(book.smallTalk, voice, context, _ => 0), voice, VoiceKeys.SmallTalk);
            if (own != null)
                return own;
        }

        SmallTalkWeights weights = lines?.smallTalkWeights ?? new SmallTalkWeights();
        var sources = new List<(float weight, IReadOnlyList<LineText> lines)>
        {
            (weights.personality, Lines(string.IsNullOrEmpty(voice.Premade) ? Pool(book.smallTalk, voice, context, _ => 0) : null)),
            (weights.home, Present(home)),
            (weights.kind, Lines(Defaults(lines?.kindSmallTalk, context, _ => 0)))
        };
        sources.RemoveAll(s => s.lines.Count == 0);
        double u = (uint)Seeds.Mix(voice.Seed, Seeds.OfKey(VoiceKeys.SmallTalkSource)) / 4294967296.0;
        (float weight, IReadOnlyList<LineText> lines) source = WeightedRandom.Pick(sources, s => s.weight, new FixedRandom((float)u));
        return source.lines != null ? source.lines[Pick(voice.Seed, VoiceKeys.SmallTalk, source.lines.Count)] : null;
    }

    /// <summary>
    /// The traveller's reaction to the stamp (the personalities spec's R1-R3,
    /// §6): the voice's rows of <paramref name="verdict"/> and
    /// <paramref name="intent"/> (a row naming <paramref name="reason"/>, the
    /// case's fault reason, scores 4; a row naming another reason never
    /// matches), else the defaults (interview.reactions) the same way; one row
    /// as a value of "reaction:{verdict}:{intent}", its line and then line.
    /// Null when neither has one.
    /// </summary>
    public static VoiceLine Reaction(InterviewLines lines, Voice voice, VoiceContext context, ReactionVerdict verdict, ReactionIntent intent, string reason)
    {
        int Key(VoiceLine r) => r.verdict != verdict || r.intent != intent ? ContextMatch.NoMatch
                              : string.IsNullOrEmpty(r.reason) ? 0 : r.reason == reason ? NamedKeyScore : ContextMatch.NoMatch;
        return Row(Pool(Book(lines).reactions, voice, context, Key), Defaults(lines?.reactions, context, Key), voice, VoiceKeys.Reaction(verdict, intent));
    }

    /// <summary>
    /// A liar's slip (the personalities spec's T10): the voice's slip rows (a
    /// row naming the traveller's <paramref name="lie"/> kind scores 4; one
    /// naming another never matches), else the defaults (interview.slips) the
    /// same way; one line as a value of "slip". Null when neither has one.
    /// </summary>
    public static LineText Slip(InterviewLines lines, Voice voice, VoiceContext context, LieKind? lie)
    {
        string name = lie.HasValue ? lie.Value.ToString() : string.Empty;
        int Key(VoiceLine r) => string.IsNullOrEmpty(r.lie) ? 0 : r.lie == name ? NamedKeyScore : ContextMatch.NoMatch;
        return Row(Pool(Book(lines).slips, voice, context, Key), Defaults(lines?.slips, context, Key), voice, VoiceKeys.Slip)?.line;
    }

    /// <summary>
    /// The traveller's answer to the desk's waiver pad (the endings and
    /// strandings spec §7.3): the voice's waiverPad rows keyed by
    /// <paramref name="reply"/>'s name, else the defaults
    /// (interview.waiverPad.replies) the same way; one line as a value of
    /// "waiverpad:{reply}". Null when neither has one.
    /// </summary>
    public static LineText WaiverPad(InterviewLines lines, Voice voice, VoiceContext context, WaiverPadReply reply)
    {
        string name = reply.ToString();
        int Key(VoiceLine r) => r.key == name ? 0 : ContextMatch.NoMatch;
        return Row(Pool(Book(lines).waiverPad, voice, context, Key), Defaults(lines?.waiverPad?.replies, context, Key), voice, VoiceKeys.WaiverPad(reply))?.line;
    }

    /// <summary>
    /// The traveller's reply to the desk's question about a logged difference
    /// in <paramref name="category"/> (wave 5, lesson 3): the voice's confront
    /// rows of <paramref name="outcome"/> (a row naming the case's fault
    /// <paramref name="reason"/> or the traveller's <paramref name="lie"/> kind
    /// scores 4 for each; a row naming another never matches), else the
    /// defaults (interview.confront.replies) the same way; one row as a value
    /// of "confront:{category}". Null when neither has one.
    /// </summary>
    public static LineText Confront(InterviewLines lines, Voice voice, VoiceContext context, ConfrontOutcome outcome, string reason, LieKind? lie, ClueCategory category)
    {
        string lieName = lie.HasValue ? lie.Value.ToString() : string.Empty;
        int Key(VoiceLine r)
        {
            if (r.outcome != outcome)
                return ContextMatch.NoMatch;
            int score = 0;
            if (!string.IsNullOrEmpty(r.reason))
            {
                if (r.reason != reason)
                    return ContextMatch.NoMatch;
                score += NamedKeyScore;
            }
            if (!string.IsNullOrEmpty(r.lie))
            {
                if (r.lie != lieName)
                    return ContextMatch.NoMatch;
                score += NamedKeyScore;
            }
            return score;
        }
        return Row(Pool(Book(lines).confront, voice, context, Key), Defaults(lines?.confront?.replies, context, Key), voice, VoiceKeys.Confront(category))?.line;
    }

    /// <summary>One row of the voice's pool, else of the defaults' pool, as a value of <paramref name="slotKey"/>; null when both are empty.</summary>
    private static VoiceLine Row(List<VoiceLine> pool, List<VoiceLine> defaults, Voice voice, string slotKey)
    {
        List<VoiceLine> from = pool.Count > 0 ? pool : defaults;
        return from.Count == 0 ? null : from[Pick(voice != null ? voice.Seed : 0, slotKey, from.Count)];
    }

    /// <summary>The best tier of default rows (a row names no voice, or its voice is ignored): the kinds' small talk.</summary>
    private static List<VoiceLine> Defaults(IReadOnlyList<VoiceLine> rows, VoiceContext context, Func<VoiceLine, int> key)
    {
        var pool = new List<VoiceLine>();
        foreach (VoiceLine row in rows ?? Array.Empty<VoiceLine>())
            if (row != null)
                Add(pool, row, context, key);
        return Best(pool, context, key);
    }

    private static LineText MissingOf(InterviewLines lines, Voice voice, VoiceContext context, string request, MissingFormVariant variant) =>
        Of(Pool(Book(lines).missingForms, voice, context, r => r.key == request && r.variant == variant ? 0 : ContextMatch.NoMatch), voice, VoiceKeys.Missing(request, variant))
        ?? Interview.MissingFormReply(lines, context.kind, request, variant);

    /// <summary>Adds <paramref name="row"/> to <paramref name="pool"/> when its context and keys match.</summary>
    private static void Add(List<VoiceLine> pool, VoiceLine row, VoiceContext context, Func<VoiceLine, int> key)
    {
        if (Score(row, context, key) != ContextMatch.NoMatch)
            pool.Add(row);
    }

    /// <summary>The rows of the highest score, in order.</summary>
    private static List<VoiceLine> Best(List<VoiceLine> matches, VoiceContext context, Func<VoiceLine, int> key)
    {
        int best = ContextMatch.NoMatch;
        foreach (VoiceLine row in matches)
            best = Math.Max(best, Score(row, context, key));
        matches.RemoveAll(r => Score(r, context, key) != best);
        return matches;
    }

    /// <summary>A row's score: its context's (ContextMatch) plus its keys' (NoMatch when either does not match).</summary>
    private static int Score(VoiceLine row, VoiceContext context, Func<VoiceLine, int> key)
    {
        int keys = key != null ? key(row) : 0;
        int match = ContextMatch.Score(row.kinds, row.era, context.kind, context.eraId);
        return keys == ContextMatch.NoMatch || match == ContextMatch.NoMatch ? ContextMatch.NoMatch : match + keys;
    }

    /// <summary>One line of <paramref name="pool"/> by the voice's seed and <paramref name="slotKey"/>; null for an empty pool.</summary>
    private static LineText Of(List<VoiceLine> pool, Voice voice, string slotKey) =>
        pool.Count == 0 ? null : pool[Pick(voice != null ? voice.Seed : 0, slotKey, pool.Count)].line;

    private static IReadOnlyList<LineText> Lines(List<VoiceLine> rows)
    {
        var lines = new List<LineText>();
        foreach (VoiceLine row in rows ?? new List<VoiceLine>())
            if (row.line != null && !string.IsNullOrEmpty(row.line.text))
                lines.Add(row.line);
        return lines;
    }

    private static IReadOnlyList<LineText> Present(IReadOnlyList<LineText> lines)
    {
        var present = new List<LineText>();
        foreach (LineText line in lines ?? Array.Empty<LineText>())
            if (line != null && !string.IsNullOrEmpty(line.text))
                present.Add(line);
        return present;
    }

    private static VoiceBook Book(InterviewLines lines) => lines?.voices ?? new VoiceBook();
}
