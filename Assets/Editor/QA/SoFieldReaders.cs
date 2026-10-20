using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// The dead-config check (BUG-73a: "a test that every SO field has a
/// reader"). Finds every serialized field of every ScriptableObject class in
/// the C# sources, and of every [Serializable] class or struct an SO holds
/// (MotionKnobs inside MotionTuning, say): public, or [SerializeField] /
/// [SerializeReference]; not static, const, readonly or [NonSerialized].
/// Then it looks for a reader: a member access ".name" anywhere, or the bare
/// name inside its own class's files (or a subclass's), never a declaration,
/// a string, a comment or a plain assignment. A field nothing reads is a knob
/// that does nothing (BUG-73: two paperSlideSeconds, stale MotionKnobs).
/// Pure text work, so it runs outside Unity (SoFieldReadersTests); a member
/// name another type shares counts as read, so the check can miss a dead
/// field but never flags a live one.
/// </summary>
public static class SoFieldReaders
{
    /// <summary>One serialized field of a ScriptableObject class (or of a serializable type an SO holds).</summary>
    public sealed class Field
    {
        /// <summary>The declaring class or struct.</summary>
        public string Class;

        /// <summary>The field's name.</summary>
        public string Name;

        /// <summary>The file it is declared in.</summary>
        public string File;

        /// <summary>"Class.Name", the allowlist's key.</summary>
        public string Key => Class + "." + Name;

        /// <summary>The key and the file.</summary>
        public override string ToString() => $"{Key} ({File})";
    }

    /// <summary>What the scan found.</summary>
    public sealed class Scan
    {
        /// <summary>The ScriptableObject classes found.</summary>
        public readonly SortedSet<string> Classes = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>The [Serializable] classes and structs the SOs hold (directly or through each other).</summary>
        public readonly SortedSet<string> HeldTypes = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>Every serialized field found.</summary>
        public readonly List<Field> Fields = new List<Field>();

        /// <summary>The fields nothing reads.</summary>
        public readonly List<Field> Unread = new List<Field>();
    }

    private static readonly HashSet<string> Modifiers = new HashSet<string>(StringComparer.Ordinal)
        { "public", "private", "protected", "internal", "static", "readonly", "const", "new", "volatile", "unsafe", "extern", "fixed" };

    /// <summary>A type declaration: its name, base list and the brace its body opens with.</summary>
    private sealed class TypeDecl
    {
        /// <summary>The type's name.</summary>
        public string Name;

        /// <summary>The file declaring it.</summary>
        public string File;

        /// <summary>The index of its body's opening brace.</summary>
        public int Open;

        /// <summary>Its base types' simple names.</summary>
        public string[] Bases;

        /// <summary>True when it is marked [Serializable].</summary>
        public bool Serializable;
    }

    /// <summary>Scans <paramref name="sources"/> (file path → C# text) for ScriptableObject fields and their readers.</summary>
    public static Scan Run(IDictionary<string, string> sources)
    {
        var cleaned = sources.ToDictionary(p => p.Key, p => Clean(p.Value));
        var scan = new Scan();
        List<TypeDecl> types = cleaned.SelectMany(f => Declarations(f.Key, f.Value)).ToList();

        // ScriptableObject classes, through base classes declared in the sources.
        var bases = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (TypeDecl t in types)
        {
            if (!bases.TryGetValue(t.Name, out HashSet<string> set))
                bases[t.Name] = set = new HashSet<string>(StringComparer.Ordinal);
            set.UnionWith(t.Bases);
        }
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (KeyValuePair<string, HashSet<string>> c in bases)
                if (!scan.Classes.Contains(c.Key) && c.Value.Any(b => b == "ScriptableObject" || scan.Classes.Contains(b)))
                    grew |= scan.Classes.Add(c.Key);
        }
        var serializable = new HashSet<string>(types.Where(t => t.Serializable).Select(t => t.Name), StringComparer.Ordinal);

        // Their serialized fields, then those of the serializable types they hold, transitively. A held type is found
        // by its simple name: in the holder's own file first (a nested type), else only when one declaration has it.
        var declarations = new HashSet<(string file, int index)>();
        var scanned = new HashSet<string>(scan.Classes, StringComparer.Ordinal);
        var seenDecls = new HashSet<TypeDecl>();
        var queue = new Queue<TypeDecl>(types.Where(t => scan.Classes.Contains(t.Name)));
        while (queue.Count > 0)
        {
            TypeDecl t = queue.Dequeue();
            if (!seenDecls.Add(t))
                continue;
            foreach ((string name, int index, string[] typeWords) in FieldsInBody(cleaned[t.File], t.Open))
            {
                scan.Fields.Add(new Field { Class = t.Name, Name = name, File = t.File });
                declarations.Add((t.File, index));
                foreach (string word in typeWords.Select(w => w.Split('.').Last()).Where(serializable.Contains))
                {
                    List<TypeDecl> candidates = types.Where(d => d.Serializable && d.Name == word).ToList();
                    List<TypeDecl> local = candidates.Where(d => d.File == t.File).ToList();
                    List<TypeDecl> held = local.Count > 0 ? local : candidates.Count == 1 ? candidates : new List<TypeDecl>();
                    foreach (TypeDecl h in held)
                    {
                        scanned.Add(h.Name);
                        scan.HeldTypes.Add(h.Name);
                        queue.Enqueue(h);
                    }
                }
            }
        }

        // The files where a field can be used bare (unqualified): those declaring its type or a type derived from it.
        var typeFiles = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (TypeDecl t in types)
            foreach (string cls in SelfAndBases(t.Name, bases, scanned))
            {
                if (!typeFiles.TryGetValue(cls, out HashSet<string> files))
                    typeFiles[cls] = files = new HashSet<string>(StringComparer.Ordinal);
                files.Add(t.File);
            }

        // A reader: a member access ".name" anywhere, or the bare name in its type's files; never a declaration,
        // a string, a comment or a plain assignment.
        ILookup<string, Field> byName = scan.Fields.ToLookup(f => f.Name, StringComparer.Ordinal);
        var read = new HashSet<Field>();
        foreach (KeyValuePair<string, string> file in cleaned)
            foreach (Match m in Identifier.Matches(file.Value))
            {
                string id = m.Value.TrimStart('@');
                if (!byName.Contains(id) || declarations.Contains((file.Key, m.Index)) || IsPlainWrite(file.Value, m.Index + m.Length))
                    continue;
                bool member = IsMemberAccess(file.Value, m.Index);
                foreach (Field f in byName[id])
                    if (member || typeFiles.TryGetValue(f.Class, out HashSet<string> files) && files.Contains(file.Key))
                        read.Add(f);
            }
        foreach (Field f in scan.Fields)
            if (!read.Contains(f))
                scan.Unread.Add(f);
        return scan;
    }

    /// <summary>Reads every .cs file under <paramref name="assetsFolder"/> except tests (a test reading a knob does not make it live).</summary>
    public static Dictionary<string, string> Sources(string assetsFolder)
    {
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(assetsFolder, "*.cs", SearchOption.AllDirectories))
        {
            string rel = path.Replace('\\', '/');
            if (rel.Contains("/Tests/") || rel.Contains("/SeamTests/") || rel.EndsWith("Tests.cs", StringComparison.Ordinal) || Path.GetFileName(rel).StartsWith("_", StringComparison.Ordinal))
                continue;
            int assets = rel.IndexOf("Assets/", StringComparison.Ordinal);
            sources[assets >= 0 ? rel.Substring(assets) : rel] = File.ReadAllText(path);
        }
        return sources;
    }

    /// <summary>The allowlist's keys ("Class.field" per line; '#' starts a comment).</summary>
    public static HashSet<string> ParseAllowlist(string text)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in (text ?? string.Empty).Split('\n'))
        {
            string key = line.Split('#')[0].Trim();
            if (key.Length > 0)
                keys.Add(key);
        }
        return keys;
    }

    private static readonly Regex TypeDeclaration = new Regex(
        @"\b(?:class|struct)\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>{]*>)?\s*(?::\s*(?<bases>[^{;]+?))?\s*(?:where\b[^{;]*)?\{", RegexOptions.Compiled);

    private static readonly Regex Identifier = new Regex(@"@?\b[A-Za-z_]\w*\b", RegexOptions.Compiled);

    /// <summary>Every class and struct declared in <paramref name="text"/>, with its bases and whether it is [Serializable].</summary>
    private static IEnumerable<TypeDecl> Declarations(string file, string text)
    {
        foreach (Match m in TypeDeclaration.Matches(text))
        {
            string[] bases = m.Groups["bases"].Success
                ? m.Groups["bases"].Value.Split(',').Select(b => Regex.Replace(b, @"<.*$", string.Empty).Split('.').Last().Trim()).Where(b => b.Length > 0).ToArray()
                : Array.Empty<string>();
            // The attributes and modifiers before the declaration: back to the previous statement or brace.
            int from = Math.Max(text.LastIndexOfAny(new[] { ';', '{', '}' }, Math.Max(0, m.Index - 1)), 0);
            string head = text.Substring(from, m.Index - from);
            yield return new TypeDecl
            {
                Name = m.Groups["name"].Value,
                File = file,
                Open = m.Index + m.Length - 1,
                Bases = bases,
                Serializable = Regex.IsMatch(head, @"\[\s*(?:System\.)?Serializable\b"),
            };
        }
    }

    /// <summary><paramref name="cls"/> and every one of its base types among <paramref name="scanned"/> (a subclass's file may use their fields bare).</summary>
    private static IEnumerable<string> SelfAndBases(string cls, Dictionary<string, HashSet<string>> bases, ICollection<string> scanned)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var todo = new Stack<string>();
        todo.Push(cls);
        while (todo.Count > 0)
        {
            string c = todo.Pop();
            if (!seen.Add(c))
                continue;
            if (scanned.Contains(c))
                yield return c;
            if (bases.TryGetValue(c, out HashSet<string> parents))
                foreach (string p in parents)
                    todo.Push(p);
        }
    }

    /// <summary>True when the identifier at <paramref name="index"/> follows a '.' (whitespace allowed): "x.name", "x?.name".</summary>
    private static bool IsMemberAccess(string text, int index)
    {
        int i = index - 1;
        while (i >= 0 && char.IsWhiteSpace(text[i]))
            i--;
        return i >= 0 && text[i] == '.';
    }

    /// <summary>The serialized fields declared at the top level of the type body opening at <paramref name="open"/>: each name, its index and its type's words.</summary>
    private static IEnumerable<(string name, int index, string[] typeWords)> FieldsInBody(string text, int open)
    {
        int depth = 0;
        int start = open + 1;
        bool initializer = false;
        for (int i = open; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{')
            {
                depth++;
                if (depth == 2 && !initializer)
                {
                    string head = text.Substring(start, i - start);
                    initializer = HasAssignment(head);
                    if (!initializer)
                    {
                        i = Skip(text, i);
                        depth--;
                        start = i + 1;
                    }
                }
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    yield break;
                if (depth == 1 && !initializer)
                    start = i + 1;
            }
            else if (c == ';' && depth == 1)
            {
                foreach ((string, int, string[]) f in ParseFields(text, start, i))
                    yield return f;
                start = i + 1;
                initializer = false;
            }
        }
    }

    /// <summary>The index of the brace closing the one at <paramref name="open"/>.</summary>
    private static int Skip(string text, int open)
    {
        int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}' && --depth == 0) return i;
        }
        return text.Length - 1;
    }

    private static readonly Regex Assignment = new Regex(@"(?<![=!<>+\-*/%&|^])=(?![=>])", RegexOptions.Compiled);

    private static bool HasAssignment(string s) => Assignment.IsMatch(s);

    /// <summary>
    /// The fields declared by the statement text[start..end) when it declares
    /// serialized fields ("public float a = 1f, b = 2f;" declares two), each
    /// with its name's index and the type's words.
    /// </summary>
    private static IEnumerable<(string, int, string[])> ParseFields(string text, int start, int end)
    {
        string statement = text.Substring(start, end - start);
        // Attributes, collected and blanked (indices stay).
        var attributes = new StringBuilder();
        string body = Regex.Replace(statement, @"\[[^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*\]", m =>
        {
            attributes.Append(m.Value).Append(' ');
            return new string(' ', m.Length);
        });
        if (body.Contains("=>") || Regex.IsMatch(body, @"\b(event|delegate|using|operator|class|struct|enum|interface|abstract|override|virtual)\b"))
            yield break;
        List<(string part, int offset)> parts = SplitDeclarators(body);
        string first = parts[0].part;
        Match assign = Assignment.Match(first);
        string declaration = assign.Success ? first.Substring(0, assign.Index) : first;
        if (declaration.Contains("("))
            yield break;
        Match name = Regex.Match(declaration, @"(?<name>@?[A-Za-z_]\w*)\s*$");
        if (!name.Success)
            yield break;
        string[] words = Regex.Matches(declaration.Substring(0, name.Index), @"[A-Za-z_][\w.]*").Cast<Match>().Select(m => m.Value).ToArray();
        int k = 0;
        var modifiers = new HashSet<string>(StringComparer.Ordinal);
        while (k < words.Length && Modifiers.Contains(words[k]))
            modifiers.Add(words[k++]);
        if (k >= words.Length)
            yield break; // no type: not a field
        if (modifiers.Contains("static") || modifiers.Contains("const") || modifiers.Contains("readonly"))
            yield break;
        string attrs = attributes.ToString();
        if (Regex.IsMatch(attrs, @"\bNonSerialized\b"))
            yield break;
        if (!modifiers.Contains("public") && !Regex.IsMatch(attrs, @"\bSerialize(Field|Reference)\b"))
            yield break;
        string[] typeWords = words.Skip(k).ToArray();
        yield return (name.Groups["name"].Value.TrimStart('@'), start + name.Groups["name"].Index, typeWords);
        // Further declarators: ", name [= value]".
        foreach ((string part, int offset) in parts.Skip(1))
        {
            Match more = Regex.Match(part, @"^\s*(?<name>@?[A-Za-z_]\w*)\s*(?:=|$)");
            if (!more.Success)
                yield break;
            yield return (more.Groups["name"].Value.TrimStart('@'), start + offset + more.Groups["name"].Index, typeWords);
        }
    }

    /// <summary>
    /// <paramref name="body"/> split at its top-level commas (outside (), [],
    /// {} and, before the first '=' of a part, a generic's &lt;&gt;), with each
    /// part's offset.
    /// </summary>
    private static List<(string, int)> SplitDeclarators(string body)
    {
        var parts = new List<(string, int)>();
        int depth = 0, angle = 0, from = 0;
        bool assigned = false;
        for (int i = 0; i < body.Length; i++)
        {
            char c = body[i];
            if (c == '(' || c == '[' || c == '{') depth++;
            else if (c == ')' || c == ']' || c == '}') depth--;
            else if (!assigned && c == '<') angle++;
            else if (!assigned && c == '>' && angle > 0) angle--;
            else if (c == '=' && depth == 0 && angle == 0 && (i + 1 >= body.Length || body[i + 1] != '=') && (i == 0 || "=!<>+-*/%&|^".IndexOf(body[i - 1]) < 0)) assigned = true;
            else if (c == ',' && depth == 0 && angle == 0)
            {
                parts.Add((body.Substring(from, i - from), from));
                from = i + 1;
                assigned = false;
            }
        }
        parts.Add((body.Substring(from), from));
        return parts;
    }

    /// <summary>True when the identifier ending at <paramref name="after"/> is only assigned ("x = ...", not "==", "=>" or a compound assignment).</summary>
    private static bool IsPlainWrite(string text, int after)
    {
        int i = after;
        while (i < text.Length && (text[i] == ' ' || text[i] == '\t' || text[i] == '\r' || text[i] == '\n'))
            i++;
        return i < text.Length && text[i] == '=' && (i + 1 >= text.Length || (text[i + 1] != '=' && text[i + 1] != '>'));
    }

    /// <summary>
    /// The source with comments, preprocessor lines and string and char
    /// literals blanked to spaces (indices and lines kept), except the code
    /// inside an interpolated string's holes, which stays.
    /// </summary>
    public static string Clean(string s)
    {
        s = Regex.Replace(s, @"^[ \t]*#[^\r\n]*", m => new string(' ', m.Length), RegexOptions.Multiline);
        var o = new StringBuilder(s);
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') Blank(o, i++);
            }
            else if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                Blank(o, i++); Blank(o, i++);
                while (i < s.Length && !(s[i] == '*' && i + 1 < s.Length && s[i + 1] == '/')) Blank(o, i++);
                if (i < s.Length) { Blank(o, i++); Blank(o, i++); }
            }
            else if (c == '\'')
            {
                Blank(o, i++);
                while (i < s.Length && s[i] != '\'') { if (s[i] == '\\') Blank(o, i++); if (i < s.Length) Blank(o, i++); }
                if (i < s.Length) Blank(o, i++);
            }
            else if (c == '"' || ((c == '$' || c == '@') && i + 1 < s.Length && (s[i + 1] == '"' || ((s[i + 1] == '$' || s[i + 1] == '@') && i + 2 < s.Length && s[i + 2] == '"'))))
            {
                bool interpolated = false, verbatim = false;
                while (s[i] != '"')
                {
                    interpolated |= s[i] == '$';
                    verbatim |= s[i] == '@';
                    Blank(o, i++);
                }
                Blank(o, i++); // opening quote
                while (i < s.Length)
                {
                    if (!verbatim && s[i] == '\\') { Blank(o, i++); if (i < s.Length) Blank(o, i++); continue; }
                    if (s[i] == '"')
                    {
                        if (verbatim && i + 1 < s.Length && s[i + 1] == '"') { Blank(o, i++); Blank(o, i++); continue; }
                        Blank(o, i++);
                        break;
                    }
                    if (interpolated && s[i] == '{')
                    {
                        if (i + 1 < s.Length && s[i + 1] == '{') { Blank(o, i++); Blank(o, i++); continue; }
                        // Keep the hole's code (up to its closing brace; nested strings in holes are rare and blanked roughly).
                        int depth = 0;
                        while (i < s.Length)
                        {
                            if (s[i] == '{') depth++;
                            else if (s[i] == '}' && --depth == 0) { i++; break; }
                            i++;
                        }
                        continue;
                    }
                    if (s[i] == '\n' && !verbatim)
                        break; // an unterminated regular string: stop at the line's end
                    Blank(o, i++);
                }
            }
            else
            {
                i++;
            }
        }
        return o.ToString();
    }

    private static void Blank(StringBuilder o, int i)
    {
        if (i < o.Length && o[i] != '\n' && o[i] != '\r')
            o[i] = ' ';
    }
}
