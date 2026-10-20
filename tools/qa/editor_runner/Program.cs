// QA-RIG's offline runner for the pure Editor-side tests (Assembly-CSharp-Editor: Assets/Editor/QA/Tests and the seam tests).
// The house runner (NOPE-tools/runner) loads only TimeDeskEditMode.dll; this one loads Assembly-CSharp-Editor.dll from the
// compile check's output, runs [Test]/[TestCase] methods of the classes whose name contains the filter, and skips the
// "NeedsEditor" category (those create Unity objects or read imported assets: Unity's EditMode suite runs them).
// Usage: dotnet run --project tools/qa/editor_runner -c Release -- <compile-check Temp\Bin\Debug> [classFilter]
// Set QA_PROJECT to the project folder for the tests that scan the project (SO readers, quality parity).
using System.Reflection;
using System.Runtime.Loader;

var bin = args[0];
var filter = args.Length > 1 ? args[1] : "";
var unityManaged = @"C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Data\Managed";
var testDll = Directory.GetFiles(bin, "Assembly-CSharp-Editor.dll", SearchOption.AllDirectories).First();
var testDir = Path.GetDirectoryName(testDll);
var dirs = new[] { testDir }.Concat(Directory.GetDirectories(bin)).Append(Path.Combine(unityManaged, "UnityEngine")).Append(unityManaged).ToArray();
AssemblyLoadContext.Default.Resolving += (ctx, name) =>
{
    if (name.Name is "netstandard" or "mscorlib" || name.Name.StartsWith("System")) return null;
    foreach (var d in dirs)
    {
        var p = Path.Combine(d, name.Name + ".dll");
        if (File.Exists(p)) return ctx.LoadFromAssemblyPath(p);
    }
    return null;
};
var nunitDll = Directory.GetFiles(bin, "nunit.framework.dll", SearchOption.AllDirectories).First();
var nunit = AssemblyLoadContext.Default.LoadFromAssemblyPath(nunitDll);
var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(testDll);
var testAttr = nunit.GetType("NUnit.Framework.TestAttribute");
var caseAttr = nunit.GetType("NUnit.Framework.TestCaseAttribute");
var setupAttr = nunit.GetType("NUnit.Framework.SetUpAttribute");
var categoryAttr = nunit.GetType("NUnit.Framework.CategoryAttribute");
var ignoreException = nunit.GetType("NUnit.Framework.IgnoreException");
int pass = 0, fail = 0, skipped = 0, ignored = 0;
Type[] types;
try { types = asm.GetTypes(); }
catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
foreach (var t in types.Where(t => t.IsClass && !t.IsAbstract && t.Name.Contains(filter)))
foreach (var m in t.GetMethods())
{
    var cases = m.GetCustomAttributes(caseAttr, true);
    if (m.GetCustomAttributes(testAttr, true).Length == 0 && cases.Length == 0) continue;
    var categories = m.GetCustomAttributes(categoryAttr, true).Select(c => (string)categoryAttr.GetProperty("Name").GetValue(c));
    if (categories.Contains("NeedsEditor")) { skipped++; continue; }
    var name = $"{t.Name}.{m.Name}";
    var argSets = cases.Length > 0
        ? cases.Select(c => (object[])caseAttr.GetProperty("Arguments").GetValue(c)).ToList()
        : new List<object[]> { null };
    foreach (var a in argSets)
    {
        var label = a == null ? name : $"{name}({string.Join(", ", a)})";
        try
        {
            var inst = Activator.CreateInstance(t);
            foreach (var s in t.GetMethods().Where(x => x.GetCustomAttributes(setupAttr, true).Length > 0)) s.Invoke(inst, null);
            var ps = m.GetParameters();
            object[] callArgs = a == null ? null : a.Select((v, i) => v == null ? null : Convert.ChangeType(v, ps[i].ParameterType)).ToArray();
            m.Invoke(inst, callArgs);
            pass++;
        }
        catch (Exception ex)
        {
            var e = ex; while (e.InnerException != null) e = e.InnerException;
            if (ignoreException.IsInstanceOfType(e)) { ignored++; Console.WriteLine($"IGNORED {label} :: {e.Message}"); continue; }
            fail++;
            Console.WriteLine($"FAIL {label} :: {e.GetType().Name}: {e.Message}");
        }
    }
}
Console.WriteLine($"passed {pass}, failed {fail}, ignored {ignored}, skipped (NeedsEditor) {skipped}");
return fail == 0 ? 0 : 1;
