using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>Where a gate check measures (FIX_PLAN §4: EditMode checks, the play harness, the built player).</summary>
public enum GateKind
{
    /// <summary>In edit mode, from assets, scenes and code (runs inside DemoGate).</summary>
    Static,
    /// <summary>Read from a play run's results (the editor harness or a UTF play test).</summary>
    Play,
    /// <summary>Read from a built player's results ([BP]: flicker, SSAO, load time, GC).</summary>
    BuiltPlayer,
}

/// <summary>
/// Registers a static method as a gate check (P0-B QA-RIG "GateCheck
/// registration"). The method is parameterless and returns a
/// <see cref="GateResult"/> or an IEnumerable of them. Each track adds its
/// checks in its own Assets/Editor/QA/Checks/&lt;Track&gt;Checks.cs; DemoGate
/// finds them all, runs them at every merge and before every demo, and
/// refuses on any red one.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GateCheckAttribute : Attribute
{
    /// <summary>The check's id ("track.area.what"), unique.</summary>
    public string Id { get; }

    /// <summary>The owning track (a red result names it).</summary>
    public string Owner { get; }

    /// <summary>Where it measures.</summary>
    public GateKind Kind { get; }

    /// <summary>True: only the demo gate runs it (a merge gate skips it), for criteria that hold only at the end (a cleanup list empty, every Phase B approval in).</summary>
    public bool DemoOnly { get; set; }

    /// <summary>A check <paramref name="id"/> owned by <paramref name="owner"/>, measuring as <paramref name="kind"/>.</summary>
    public GateCheckAttribute(string id, string owner, GateKind kind = GateKind.Static)
    {
        Id = id;
        Owner = owner;
        Kind = kind;
    }
}

/// <summary>One gate check's outcome.</summary>
public sealed class GateResult
{
    /// <summary>The check's id.</summary>
    public string Id { get; set; }

    /// <summary>The owning track.</summary>
    public string Owner { get; set; }

    /// <summary>True when it held.</summary>
    public bool Pass { get; set; }

    /// <summary>The measured number(s).</summary>
    public string Measured { get; set; } = string.Empty;

    /// <summary>What was checked, and what failed.</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>A passing result.</summary>
    public static GateResult Ok(string id, string owner, string measured, string detail = "") =>
        new GateResult { Id = id, Owner = owner, Pass = true, Measured = measured, Detail = detail };

    /// <summary>A failing result.</summary>
    public static GateResult Fail(string id, string owner, string measured, string detail) =>
        new GateResult { Id = id, Owner = owner, Pass = false, Measured = measured, Detail = detail };

    /// <summary>A result that passes when <paramref name="pass"/>.</summary>
    public static GateResult Of(string id, string owner, bool pass, string measured, string detail) =>
        new GateResult { Id = id, Owner = owner, Pass = pass, Measured = measured, Detail = detail };

    /// <summary>One line for a report.</summary>
    public override string ToString() => $"{(Pass ? "PASS" : "FAIL")} {Id} [{Owner}] {Measured}{(string.IsNullOrEmpty(Detail) ? string.Empty : " - " + Detail)}";
}

/// <summary>A registered check: its attribute and its method.</summary>
public sealed class RegisteredCheck
{
    /// <summary>The registration.</summary>
    public GateCheckAttribute Info { get; }

    /// <summary>The check's method.</summary>
    public MethodInfo Method { get; }

    /// <summary>A registered check.</summary>
    public RegisteredCheck(GateCheckAttribute info, MethodInfo method)
    {
        Info = info;
        Method = method;
    }

    /// <summary>
    /// Runs the check. An exception, a null or no result becomes a failing
    /// result under the check's id, so a broken check can never pass.
    /// Results without an id or owner get the registration's.
    /// </summary>
    public List<GateResult> Run()
    {
        var results = new List<GateResult>();
        try
        {
            object value = Method.Invoke(null, null);
            if (value is GateResult one)
                results.Add(one);
            else if (value is IEnumerable<GateResult> many)
                results.AddRange(many.Where(r => r != null));
        }
        catch (Exception e)
        {
            Exception inner = e is TargetInvocationException t && t.InnerException != null ? t.InnerException : e;
            results.Add(GateResult.Fail(Info.Id, Info.Owner, "threw", $"{inner.GetType().Name}: {inner.Message}"));
            return results;
        }
        if (results.Count == 0)
            results.Add(GateResult.Fail(Info.Id, Info.Owner, "no result", "The check returned nothing."));
        foreach (GateResult r in results)
        {
            if (string.IsNullOrEmpty(r.Id))
                r.Id = Info.Id;
            if (string.IsNullOrEmpty(r.Owner))
                r.Owner = Info.Owner;
        }
        return results;
    }
}

/// <summary>Finds every <see cref="GateCheckAttribute"/> method in the loaded assemblies.</summary>
public static class GateChecks
{
    /// <summary>Every registered check, by id; a method with the wrong shape is reported as a failing check by <see cref="Problems"/>.</summary>
    public static List<RegisteredCheck> All(IEnumerable<Assembly> assemblies = null)
    {
        var found = new List<RegisteredCheck>();
        foreach (Assembly a in assemblies ?? AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = a.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
            foreach (Type t in types)
                foreach (MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    GateCheckAttribute info = m.GetCustomAttribute<GateCheckAttribute>();
                    if (info != null)
                        found.Add(new RegisteredCheck(info, m));
                }
        }
        return found.OrderBy(c => c.Info.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>Registration faults, as failing results: a duplicate id, a method with parameters or the wrong return type, a missing owner.</summary>
    public static List<GateResult> Problems(IEnumerable<RegisteredCheck> checks)
    {
        var problems = new List<GateResult>();
        foreach (IGrouping<string, RegisteredCheck> dup in checks.GroupBy(c => c.Info.Id).Where(g => g.Count() > 1))
            problems.Add(GateResult.Fail(dup.Key, dup.First().Info.Owner, "duplicate id", "Registered by " + string.Join(", ", dup.Select(c => c.Method.DeclaringType?.Name + "." + c.Method.Name))));
        foreach (RegisteredCheck c in checks)
        {
            if (c.Method.GetParameters().Length != 0)
                problems.Add(GateResult.Fail(c.Info.Id, c.Info.Owner, "bad check", $"{c.Method.Name} takes parameters."));
            if (c.Method.ReturnType != typeof(GateResult) && !typeof(IEnumerable<GateResult>).IsAssignableFrom(c.Method.ReturnType))
                problems.Add(GateResult.Fail(c.Info.Id, c.Info.Owner, "bad check", $"{c.Method.Name} returns {c.Method.ReturnType.Name}, not GateResult."));
            if (string.IsNullOrWhiteSpace(c.Info.Owner))
                problems.Add(GateResult.Fail(c.Info.Id, "QA-RIG", "bad check", $"{c.Method.Name} names no owner."));
        }
        return problems;
    }
}
