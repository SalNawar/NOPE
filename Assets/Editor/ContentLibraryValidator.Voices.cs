using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The validator's voices check (the personalities spec's C3, C4, §9.2): the
/// library's cast (Personalities.Problems) and the voice lines
/// (VoiceChecks.Problems, the rules Generate World runs before writing), fed
/// from the library's assets. The longest-line limit lives in the source
/// only, so Generate World alone checks the lines' lengths. Each
/// personality's coverage is an info line, never a warning (C4).
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports each problem of the cast and the voice lines; logs the warnings and the coverage; returns how many issues.</summary>
    private static int CheckVoices(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message)
        {
            Debug.LogError($"[ContentLibraryValidator] {message} ('{lib.name}'; run Tools > TimeDesk > Generate World)", lib);
            issues++;
        }

        foreach (string problem in Personalities.Problems(lib.Personalities))
            Error(problem);

        InterviewLines lines = lib.Interview ?? new InterviewLines();
        List<KindForms> kindForms = KindForms(lib, out _, out _);
        var input = new VoiceCheckInput
        {
            Voices = lines.voices ?? new VoiceBook(),
            KindSmallTalk = lines.kindSmallTalk ?? new List<VoiceLine>(),
            Weights = lines.smallTalkWeights,
            Cast = lib.Personalities,
            Premades = lib.Legendaries.Where(l => l != null).Select(l => l.id).ToList(),
            Eras = lib.Eras.Where(e => e != null).Select(e => e.id).ToList(),
            Questions = lib.Questions.Where(q => q != null && q.question != null).Select(q => q.question.id).ToList(),
            SpokenRequests = (lines.requests ?? new List<InterviewRequest>()).Where(r => r != null).Select(r => r.id).ToList(),
            Requests = kindForms.SelectMany(k => k.Askable ?? new AskableForm[0]).Select(f => FormRequests.IdOf(f.AskGroup, f.FormNumber)).Distinct().ToList(),
            KindsInPlay = kindForms.Select(k => k.Kind).Distinct().ToList(),
            FactValues = lib.Profiles.Where(p => p != null).SelectMany(p => p.facts).Where(f => f != null && !string.IsNullOrWhiteSpace(f.value)).Select(f => f.value).Distinct().ToList(),
            TransponderModels = lib.Agency.transponders.Where(t => t != null).Select(t => t.model).ToList(),
            Employers = lib.Agency.employers.Where(e => e != null).Select(e => e.name).ToList(),
            DefaultReactions = lines.reactions ?? new List<VoiceLine>(),
            DefaultSlips = lines.slips ?? new List<VoiceLine>(),
            SlipChances = lib.DayPlans.Where(p => p != null).Select(p => (p.DayNumber, p.SlipChance)).ToList()
        };
        VoiceCheckResult result = VoiceChecks.Problems(input);
        foreach (string error in result.Errors)
            Error(error);
        foreach (string warning in result.Warnings)
        {
            Debug.LogWarning($"[ContentLibraryValidator] {warning} ('{lib.name}')", lib);
            issues++;
        }
        foreach (string info in result.Info)
            Debug.Log($"[ContentLibraryValidator] {info}", lib);
        return issues;
    }
}
