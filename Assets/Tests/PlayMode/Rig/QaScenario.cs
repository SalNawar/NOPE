#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.IO;
using UnityEngine;

/// <summary>
/// A scripted QA play (tools/qa/scenarios/*.json), run the same way in the
/// editor (the job runner's harness or a UTF play test) and in a QA player
/// build (BuiltPlayerCapture): a name, an owning track, the run seed and the
/// steps. docs/pending/QA-RIG.md lists every op.
/// </summary>
[Serializable]
public sealed class QaScenario
{
    /// <summary>The scenario's name: the prefix of every assertion id it writes.</summary>
    public string name = "scenario";

    /// <summary>The track that owns its assertions (a red result names it).</summary>
    public string owner = "QA-RIG";

    /// <summary>The run seed set on RunConfig before New Run (the golden seed by default; 0 leaves the config's).</summary>
    public int seed = 12345;

    /// <summary>The steps, in order.</summary>
    public QaStep[] steps = Array.Empty<QaStep>();

    /// <summary>Reads a scenario from a JSON file.</summary>
    public static QaScenario Load(string path) => Parse(File.ReadAllText(path));

    /// <summary>Reads a scenario from JSON text (JsonUtility; unknown keys are ignored, missing ones keep their defaults).</summary>
    public static QaScenario Parse(string json)
    {
        QaScenario s = JsonUtility.FromJson<QaScenario>(json);
        if (s == null)
            throw new FormatException("Not a QA scenario.");
        s.steps = s.steps ?? Array.Empty<QaStep>();
        return s;
    }
}

/// <summary>
/// One scenario step: the op and its arguments (each op reads the fields it
/// needs). Screen positions (<see cref="x"/>, <see cref="y"/>,
/// <see cref="x2"/>, <see cref="y2"/>) are fractions of the screen, origin
/// bottom-left, so a step means the same place at every aspect.
/// </summary>
[Serializable]
public sealed class QaStep
{
    /// <summary>The op (see docs/pending/QA-RIG.md).</summary>
    public string op = string.Empty;

    /// <summary>A name: a sequence's or still's name, a cheat's name, a scene's name.</summary>
    public string name = string.Empty;

    /// <summary>The first target: "Type.field", "Type.Method" or an object's path or name.</summary>
    public string target = string.Empty;

    /// <summary>The second target (a drag's drop object).</summary>
    public string target2 = string.Empty;

    /// <summary>A drag path: Plain, Fast, Slow, Wobble, Diag, Flick, Edge, Off or Onto (P- prefix allowed).</summary>
    public string path = string.Empty;

    /// <summary>A key (the Input System's Key names: Q, A, Tab, Escape, Space, F1 ...).</summary>
    public string key = string.Empty;

    /// <summary>Free text: a cheat's text, a pet's name, a note.</summary>
    public string text = string.Empty;

    /// <summary>An integer: a day, frames, a count, a byte budget.</summary>
    public int n;

    /// <summary>A width in pixels (res).</summary>
    public int w;

    /// <summary>A height in pixels (res).</summary>
    public int h;

    /// <summary>A number: seconds, an hour, an amount.</summary>
    public float f;

    /// <summary>A screen fraction (0..1), from the left.</summary>
    public float x;

    /// <summary>A screen fraction (0..1), from the bottom.</summary>
    public float y;

    /// <summary>The second point's x fraction.</summary>
    public float x2;

    /// <summary>The second point's y fraction.</summary>
    public float y2;

    /// <summary>
    /// A press op (newRun, startShift, sign, button, click on a target) that
    /// may reach a covered or off-screen target directly: true invokes the
    /// button or sends the click event and only notes it. False (the
    /// default): the press is still delivered so the run goes on, but the
    /// run's <c>presses</c> assertion fails and names it (BUG-53: a control
    /// hidden behind an overlay must not pass; BUG-68's input-blocking class).
    /// </summary>
    public bool direct;
}

/// <summary>One step's outcome in a run report.</summary>
[Serializable]
public sealed class QaStepResult
{
    /// <summary>The step's index.</summary>
    public int i;

    /// <summary>The step's op.</summary>
    public string op;

    /// <summary>False when it threw or timed out (the run stops there).</summary>
    public bool ok;

    /// <summary>Wall-clock seconds the step took.</summary>
    public float seconds;

    /// <summary>What happened (an exception's message, a fallback taken).</summary>
    public string note;
}

/// <summary>One measured check of a run: the unit DemoGate reads (as a <see cref="QaGateResult"/>).</summary>
[Serializable]
public sealed class QaAssertion
{
    /// <summary>The id: "&lt;scenario&gt;.&lt;check&gt;[.&lt;name&gt;]".</summary>
    public string id;

    /// <summary>The track a red result belongs to.</summary>
    public string owner;

    /// <summary>True when the criterion held.</summary>
    public bool pass;

    /// <summary>The measured number(s), as text.</summary>
    public string measured;

    /// <summary>What was checked and what failed.</summary>
    public string detail;
}

/// <summary>A whole run's report (run.json in the run's folder).</summary>
[Serializable]
public sealed class QaRunReport
{
    /// <summary>The scenario's name.</summary>
    public string scenario;

    /// <summary>The scenario's owner.</summary>
    public string owner;

    /// <summary>True when every step ran and every assertion passed.</summary>
    public bool ok;

    /// <summary>True in a player build (false in the editor).</summary>
    public bool player;

    /// <summary>The Unity version.</summary>
    public string unity;

    /// <summary>The quality level the run played at (name).</summary>
    public string quality;

    /// <summary>When it started (local, ISO).</summary>
    public string started;

    /// <summary>When it finished (local, ISO).</summary>
    public string finished;

    /// <summary>Errors and exceptions logged during the run.</summary>
    public int errors;

    /// <summary>Warnings logged during the run.</summary>
    public int warnings;

    /// <summary>The camera logger's summary line.</summary>
    public string camera;

    /// <summary>Every step's outcome.</summary>
    public QaStepResult[] steps = Array.Empty<QaStepResult>();

    /// <summary>Every assertion.</summary>
    public QaAssertion[] assertions = Array.Empty<QaAssertion>();
}

/// <summary>
/// A gate result file (Library/QA/results/&lt;id&gt;.json): one measured check,
/// written by a run (imported from its run.json), by tools/qa/seqtools.py
/// (flicker, flash, latency on captured frames) or by an Editor check, and
/// read by DemoGate. Stamped with the commit it measured, so a stale result
/// does not count.
/// </summary>
[Serializable]
public sealed class QaGateResult
{
    /// <summary>The check's id.</summary>
    public string id;

    /// <summary>The owning track.</summary>
    public string owner;

    /// <summary>True when the criterion held.</summary>
    public bool pass;

    /// <summary>The measured number(s).</summary>
    public string measured;

    /// <summary>What was checked and what failed.</summary>
    public string detail;

    /// <summary>The commit measured (git HEAD), or empty when unknown.</summary>
    public string commit;

    /// <summary>True when the tree had uncommitted changes to Assets, Packages or ProjectSettings when it was measured (or the QA player was built): such a result is not a measurement of <see cref="commit"/> and counts as stale.</summary>
    public bool dirty;

    /// <summary>When it was written (local, ISO).</summary>
    public string written;

    /// <summary>Where it came from (a run folder, a sequence folder).</summary>
    public string source;

    /// <summary>True when measured on a built player ([BP]).</summary>
    public bool builtPlayer;
}
#endif
