"""Token-level scans used by metrics.py: literals (magic numbers, strings),
per-frame reachability with Find/GetComponent and allocation sites, static
mutable state and singleton access. Each function documents its rule and its
limits; none of them resolves types semantically."""
from __future__ import annotations

import bisect
import re

import cslex

# ------------------------------------------------------------------ helpers

PRIMITIVES = {"bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint",
              "long", "ulong", "short", "ushort", "object", "string", "void"}
WELL_KNOWN_STRUCTS = {"Vector2", "Vector3", "Vector4", "Vector2Int", "Vector3Int", "Quaternion",
                      "Color", "Color32", "Rect", "RectInt", "Bounds", "BoundsInt", "Ray", "Ray2D",
                      "Matrix4x4", "KeyValuePair", "ValueTuple", "Keyframe", "LayerMask",
                      "RaycastHit", "RaycastHit2D", "TimeSpan", "DateTime", "CancellationToken",
                      "Plane", "GradientColorKey", "GradientAlphaKey", "Nullable", "Span",
                      "ReadOnlySpan", "Guid", "Resolution", "Touch", "RangeInt", "Hash128",
                      "RenderParams", "Scene", "ContactPoint", "Pose", "Rect", "SphericalHarmonicsL2"}
PER_FRAME_NAMES = {"Update", "LateUpdate", "FixedUpdate", "OnGUI"}
PER_FRAME_EVENTS = {("EditorApplication", "update"), ("Canvas", "willRenderCanvases"),
                    ("Application", "onBeforeRender"), ("Camera", "onPreCull"),
                    ("Camera", "onPreRender"), ("Camera", "onPostRender")}
GET_COMPONENT = {"GetComponent", "GetComponents", "GetComponentInChildren", "GetComponentInParent",
                 "GetComponentsInChildren", "GetComponentsInParent"}
FIND_OBJECT = {"FindObjectOfType", "FindObjectsOfType", "FindFirstObjectByType",
               "FindAnyObjectByType", "FindObjectsByType"}
FIND_TAG = {"FindWithTag", "FindGameObjectWithTag", "FindGameObjectsWithTag"}
LINQ = {"Select", "SelectMany", "Where", "Any", "All", "First", "FirstOrDefault", "Last",
        "LastOrDefault", "Single", "SingleOrDefault", "Count", "Sum", "Min", "Max", "Average",
        "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "ToList", "ToArray",
        "ToDictionary", "ToHashSet", "Distinct", "Concat", "Zip", "Skip", "Take", "SkipWhile",
        "TakeWhile", "GroupBy", "Aggregate", "SequenceEqual", "Cast", "OfType", "Except",
        "Intersect", "Union", "DefaultIfEmpty", "ElementAt", "ElementAtOrDefault"}
NOT_LINQ_RECEIVERS = {"Mathf", "Math", "string", "String", "Enum", "Array", "Path", "Task"}
MUTABLE_COLLECTION = re.compile(r"\b(List|Dictionary|HashSet|Queue|Stack|SortedDictionary|SortedList|"
                                r"SortedSet|LinkedList|StringBuilder|ConcurrentDictionary|ConcurrentQueue)\b|\[\]")
ALLOWED_NUMBERS = (0.0, 1.0, -1.0, 2.0, 0.5, 100.0)
LOG_METHODS = {"Log", "LogWarning", "LogError", "LogException", "LogFormat", "LogWarningFormat",
               "LogErrorFormat", "LogAssertion", "Assert", "AssertFormat"}
_UNARY_PREV = {"(", "[", "{", ",", "=", "==", "!=", "<", ">", "<=", ">=", "+", "-", "*", "/", "%",
               "?", ":", "&&", "||", "??", "=>", "return", "case", "in", "+=", "-=", "*=", "/=", ";"}


def walk(tokens, lo, hi, owner=None):
    """Yields (tokens, i, owner_index) over tokens[lo:hi] and, recursively, the
    tokens inside interpolation holes (owner_index = index of the enclosing
    top-level string token)."""
    for i in range(lo, hi):
        t = tokens[i]
        yield tokens, i, (i if owner is None else owner)
        if t.holes:
            for h in t.holes:
                yield from walk(h, 0, len(h), i if owner is None else owner)


def number_value(text: str):
    s = text.replace("_", "").lower()
    try:
        if s.startswith("0x"):
            return float(int(s.rstrip("ul"), 16))
        if s.startswith("0b"):
            return float(int(s[2:].rstrip("ul"), 2))
        return float(s.rstrip("fdmul"))
    except ValueError:
        return None


def is_unary_minus(tokens, i) -> bool:
    """tokens[i] is a number; True when it is preceded by a unary minus."""
    if i == 0 or tokens[i - 1].text != "-":
        return False
    if i == 1:
        return True
    p = tokens[i - 2]
    return p.text in _UNARY_PREV or (p.kind == "kw" and p.text not in ("this", "base", "true", "false", "null"))


def classify_question(tokens, i) -> str:
    """For a '?' token: 'ternary', 'nullable' (type marker) or 'conditional' (?[ )."""
    nxt = tokens[i + 1] if i + 1 < len(tokens) else None
    if nxt is None:
        return "nullable"
    if nxt.text in (">", ",", ")", "]", ";", "=", "{", "=>"):
        return "nullable"
    if nxt.text == "[":
        return "nullable" if i + 2 < len(tokens) and tokens[i + 2].text == "]" else "conditional"
    if nxt.kind == "id" and i + 2 < len(tokens) and tokens[i + 2].text in ("=", ";", ",", ")", "=>", "{", "in"):
        prev = tokens[i - 1] if i > 0 else None
        if prev is not None and (prev.kind in ("id", "kw") or prev.text in (">", "]")):
            return "nullable"
    return "ternary"


def switch_arms(tokens, match) -> set:
    """Indices of '=>' tokens that are switch-expression arms (`x switch { A => .. }`)."""
    arms = set()
    for s, t in enumerate(tokens):
        if t.text == "switch" and t.kind == "kw" and s + 1 < len(tokens) and tokens[s + 1].text == "{":
            close = match[s + 1]
            j = s + 2
            while j < close:
                x = tokens[j].text
                if x in ("(", "[", "{") and tokens[j].kind == "punct":
                    j = match[j] + 1
                    continue
                if x == "=>":
                    arms.add(j)
                j += 1
    return arms


def _generic_skip(tokens, q):
    """If tokens[q] is '<' opening type args, index after the matching '>'; else q."""
    if q >= len(tokens) or tokens[q].text != "<":
        return q
    depth = 0
    r = q
    while r < len(tokens):
        x = tokens[r].text
        if x == "<":
            depth += 1
        elif x == ">":
            depth -= 1
            if depth == 0:
                return r + 1
        elif not (tokens[r].kind in ("id", "kw") or x in (",", ".", "?", "[", "]", "(", ")")):
            return q
        r += 1
    return q


def is_call(tokens, i) -> bool:
    """tokens[i] (identifier) is followed by `(` or by generic args then `(`."""
    q = _generic_skip(tokens, i + 1)
    return q < len(tokens) and tokens[q].text == "("


def receiver(tokens, i) -> str | None:
    """For `X . name` returns 'X' (the identifier/keyword before the dot)."""
    if i >= 2 and tokens[i - 1].text in (".", "?.") and tokens[i - 2].kind in ("id", "kw"):
        return tokens[i - 2].text
    if i >= 2 and tokens[i - 1].text in (".", "?.") and tokens[i - 2].text == ")":
        return "(...)"
    return None


# ---------------------------------------------------------- complexity

def complexity(tokens, lo, hi, arms: set) -> tuple[int, int]:
    """Approximate cyclomatic complexity of a body, two rules:
    spec rule  = 1 + if + case (not default) + for + foreach + while + catch
                 + && + || + ?? + ternary ? + switch-expression arms (the `_`
                 discard arm excluded) + `when` clauses;
    roslyn-calibrated = 1 + if + case + `default:` + for + foreach + while
                 + && + || + ?? + ternary ? + ?. + ?[  (what CA1502 counts:
                 case labels incl. default and conditional access count;
                 catch, when, switch-expression arms and ??= do not).
    Lambdas and local functions inside the body are included (as in Roslyn)."""
    spec = cal = 1
    for toks, i, owner in walk(tokens, lo, hi):
        t = toks[i]
        x = t.text
        if t.kind == "kw":
            if x in ("if", "for", "foreach", "while"):
                spec += 1
                cal += 1
            elif x == "case":
                spec += 1
                cal += 1
            elif x == "catch":
                spec += 1
            elif x == "default" and i + 1 < len(toks) and toks[i + 1].text == ":":
                cal += 1
        elif t.kind == "punct":
            if x in ("&&", "||", "??"):
                spec += 1
                cal += 1
            elif x == "?.":
                cal += 1
            elif x == "?":
                kind = classify_question(toks, i)
                if kind == "ternary":
                    spec += 1
                    cal += 1
                elif kind == "conditional":
                    cal += 1
            elif x == "=>" and toks is tokens and i in arms:
                if not (i > 0 and toks[i - 1].text == "_"):
                    spec += 1
        elif t.kind == "id" and x == "when":
            p = toks[i - 1].text if i > 0 else ""
            n = toks[i + 1].text if i + 1 < len(toks) else ""
            if p not in (".", ",", "(") and n not in ("=", ";", ",", ")", "."):
                spec += 1
    return spec, cal


# -------------------------------------------------------------- literals

class LiteralScanner:
    """Classifies numeric and string literals of one file.

    Number buckets: attribute (inside a declaration attribute), const (const
    field or local const), enum (enum member value), serialized_default (the
    initializer of a serialized field of a Unity object / [Serializable]
    type), field_initializer (initializer of any other field or property:
    a named value), allowed (value in {0, 1, -1, 2, 0.5, 100}), magic (the
    rest; `array_size` marks `new T[n]` sizes, still magic).
    String buckets: attribute, const, empty, log (argument of Debug.Log*/
    Debug.Assert*/Assert.*/exception constructors), serialized_default,
    field_initializer, literal (the rest). nameof() yields no string token.
    Interpolated strings count as one literal; numbers inside their holes are
    classified like any other number."""

    def __init__(self, model, path):
        self.model = model
        self.path = path
        fp = model.files[path]
        self.fp = fp
        self.tokens = fp.tokens
        self.attr = sorted(fp.attr_ranges)
        self.attr_lo = [a[0] for a in self.attr]
        self.log_flags = self._log_context()
        self._serialized = {}

    def _in_attr(self, i):
        k = bisect.bisect_right(self.attr_lo, i) - 1
        return k >= 0 and self.attr[k][0] <= i < self.attr[k][1]

    def _log_context(self):
        t = self.tokens
        flags = {}
        stack = []
        for i, tok in enumerate(t):
            if tok.kind == "punct" and tok.text == "(":
                is_log = False
                if i >= 1 and t[i - 1].kind == "id":
                    name = t[i - 1].text
                    rcv = receiver(t, i - 1)
                    if rcv == "Debug" and name in LOG_METHODS:
                        is_log = True
                    elif rcv == "Assert":
                        is_log = True
                    elif name.endswith("Exception"):
                        k = i - 1
                        while k >= 2 and t[k - 1].text == "." and t[k - 2].kind == "id":
                            k -= 2
                        if k >= 1 and t[k - 1].text == "new":
                            is_log = True
                stack.append(is_log or (stack[-1] if stack else False))
            elif tok.kind == "punct" and tok.text == ")":
                if stack:
                    stack.pop()
            elif tok.kind == "string" and stack and stack[-1]:
                flags[i] = True
        return flags

    def _statement_has_const(self, i):
        t = self.tokens
        k = i - 1
        while k >= 0 and t[k].text not in (";", "{", "}"):
            if t[k].text == "const" and t[k].kind == "kw":
                return True
            k -= 1
        return False

    def _field_bucket(self, member, i):
        if member.kind not in ("field", "property") or member.init is None:
            return None
        lo, hi = member.init
        if not lo <= i < hi:
            return None
        td = member.parent
        key = id(td)
        if key not in self._serialized:
            ser = set()
            if self.model.is_serializable_type(td):
                ser = {id(m) for m in self.model.serialized_fields(td)}
            self._serialized[key] = ser
        return "serialized_default" if id(member) in self._serialized[key] else "field_initializer"

    def context(self, i):
        """Bucket for a literal whose top-level (owner) index is i, or None
        when it is an ordinary use."""
        if self._in_attr(i):
            return "attribute"
        member = self.model.member_at(self.path, i)
        if member is None:
            return "outside_member"
        if member.kind == "const" or self._statement_has_const(i):
            return "const"
        if member.kind == "enum_member":
            return "enum"
        return self._field_bucket(member, i)

    def numbers(self):
        out = []
        for toks, i, owner in walk(self.tokens, 0, len(self.tokens)):
            t = toks[i]
            if t.kind != "number":
                continue
            bucket = self.context(owner)
            value = number_value(t.text)
            neg = is_unary_minus(toks, i)
            if bucket is None:
                v = -value if (neg and value is not None) else value
                bucket = "allowed" if v in ALLOWED_NUMBERS else "magic"
            size = False
            if bucket == "magic" and i >= 1 and toks[i - 1].text == "[":
                k = i - 2
                while k >= 0 and (toks[k].kind in ("id", "kw") and toks[k].text != "new" or toks[k].text in (".", "<", ">", ",")):
                    k -= 1
                size = k >= 0 and toks[k].text == "new"
            member = self.model.member_at(self.path, owner)
            out.append({"line": t.line, "text": ("-" if neg else "") + t.text, "bucket": bucket,
                        "array_size": size, "member": _member_name(member)})
        return out

    def strings(self):
        out = []
        for i, t in enumerate(self.tokens):
            if t.kind != "string":
                continue
            bucket = self.context(i)
            if bucket is None:
                body = t.text.lstrip("$@")
                if body == '""':
                    bucket = "empty"
                elif self.log_flags.get(i):
                    bucket = "log"
                else:
                    bucket = "literal"
            member = self.model.member_at(self.path, i)
            out.append({"line": t.line, "text": t.text, "bucket": bucket, "member": _member_name(member)})
        return out


def _member_name(member) -> str:
    if member is None:
        return ""
    return f"{member.parent.display_name}.{member.name}"


# ------------------------------------------------------------ per-frame

def type_callees(model, td):
    """{unit: set(units)} for the units of one (merged) type: calls resolved by
    simple name to methods of the same type (all overloads), bare identifier
    uses of the type's properties resolve to their accessors."""
    units = [u for u in model.units if u.type is td]
    methods, props = {}, {}
    for u in units:
        key = u.member.name.rsplit(".", 1)[-1]
        if u.accessor:
            props.setdefault(key, []).append(u)
        else:
            methods.setdefault(key, []).append(u)
    edges = {}
    for u in units:
        toks = model.files[u.file].tokens
        out = []
        seen = set()
        for tt, i, _ in walk(toks, u.body[0], u.body[1]):
            t = tt[i]
            if t.kind != "id":
                continue
            if i >= 1 and tt[i - 1].text in (".", "?."):
                if not (i >= 2 and tt[i - 2].text in ("this", "base")):
                    continue
            name = t.text
            targets = []
            if name in methods and is_call(tt, i):
                targets = methods[name]
            elif name in props and not is_call(tt, i):
                targets = props[name]
            for v in targets:
                if id(v) not in seen and v is not u:
                    seen.add(id(v))
                    out.append(v)
        edges[id(u)] = out
    return units, edges


def per_frame_roots(model):
    """Runtime roots: Update/LateUpdate/FixedUpdate/OnGUI methods of types in
    Assets/Scripts (runtime), plus method groups registered with `+=` on
    per-frame callbacks (EditorApplication.update, Canvas.willRenderCanvases,
    RenderPipelineManager.*, Application.onBeforeRender, Camera.onPre*)."""
    roots = []
    notes = []
    for u in model.units:
        if model.by_path[u.file].role != "runtime" or u.accessor:
            continue
        if u.member.kind == "method" and u.member.name in PER_FRAME_NAMES:
            roots.append((u, u.member.name))
    for path, fp in sorted(model.files.items()):
        if model.by_path[path].role != "runtime":
            continue
        t = fp.tokens
        for i in range(2, len(t) - 1):
            if t[i].text != "+=":
                continue
            ev, rc = t[i - 1].text, (t[i - 3].text if i >= 3 and t[i - 2].text == "." else "")
            if not ((rc, ev) in PER_FRAME_EVENTS or rc == "RenderPipelineManager"):
                continue
            member = model.member_at(path, i)
            if t[i + 1].kind == "id" and i + 2 < len(t) and t[i + 2].text == ";" and member is not None:
                for u in model.units:
                    if u.type is member.parent and u.member.name == t[i + 1].text and not u.accessor:
                        roots.append((u, f"{rc}.{ev}"))
            else:
                notes.append(f"{path}:{t[i].line} registers a non-method-group on {rc}.{ev}")
    roots.sort(key=lambda r: (r[0].file, r[0].line, r[0].name))
    return roots, notes


def reachable(model, roots):
    """BFS within each root's type. Returns {id(unit): (unit, root_name, chain, all_roots)}."""
    cache = {}
    reached = {}
    for root, why in roots:
        td = root.type
        if id(td) not in cache:
            cache[id(td)] = type_callees(model, td)
        _, edges = cache[id(td)]
        queue = [(root, [root.name])]
        seen = {id(root)}
        while queue:
            u, chain = queue.pop(0)
            rec = reached.get(id(u))
            if rec is None:
                reached[id(u)] = [u, root.name, chain, {root.name}]
            else:
                rec[3].add(root.name)
            for v in edges.get(id(u), []):
                if id(v) not in seen:
                    seen.add(id(v))
                    queue.append((v, chain + [v.name]))
    return reached


def find_sites(model, u):
    """Find/GetComponent/Camera.main/Resources.Load calls in a unit body."""
    out = []
    toks = model.files[u.file].tokens
    our_types = model.types_by_simple
    for tt, i, _ in walk(toks, u.body[0], u.body[1]):
        t = tt[i]
        if t.kind != "id":
            continue
        name = t.text
        kind = None
        if name in GET_COMPONENT and is_call(tt, i):
            kind = "GetComponent*"
            q = _generic_skip(tt, i + 1)
            if q + 2 < len(tt) and tt[q].text == "(" and tt[q + 1].text == "true" and tt[q + 2].text == ")":
                kind = "GetComponent*(includeInactive)"
        elif name == "TryGetComponent" and is_call(tt, i):
            kind = "TryGetComponent (non-allocating)"
        elif name in FIND_OBJECT and is_call(tt, i):
            kind = "FindObject*"
        elif name in FIND_TAG and is_call(tt, i):
            kind = "GameObject.FindWithTag*"
        elif name == "Find" and is_call(tt, i):
            rc = receiver(tt, i)
            if rc == "GameObject":
                kind = "GameObject.Find"
            elif rc == "transform" or (rc or "").endswith(("Transform", "transform")):
                kind = "Transform.Find"
            elif rc is not None and rc not in our_types:
                kind = f"{rc}.Find (Transform.Find or List.Find)"
        elif name == "main" and receiver(tt, i) == "Camera":
            kind = "Camera.main"
        elif name.startswith("Load") and receiver(tt, i) == "Resources" and is_call(tt, i):
            kind = "Resources.Load"
        if kind:
            out.append((t.line, kind, name))
    return out


def alloc_sites(model, u, arms_by_file):
    """Allocation-prone constructs in a unit body. Limits: boxing and params
    arrays are not detected; Contains()/Reverse() are skipped (List methods
    share the names); receiver types are not resolved."""
    out = []
    fp = model.files[u.file]
    toks = fp.tokens
    linq_file = "System.Linq" in fp.using_names
    arms = arms_by_file[u.file]
    structs = WELL_KNOWN_STRUCTS | model.struct_names
    for tt, i, _ in walk(toks, u.body[0], u.body[1]):
        t = tt[i]
        x = t.text
        kind = None
        if t.kind == "kw" and x == "new":
            nxt = tt[i + 1] if i + 1 < len(tt) else None
            if nxt is None:
                continue
            if nxt.text == "(":
                kind = "new() target-typed"
            elif nxt.text == "[":
                kind = "new[] array"
            elif nxt.text == "{":
                kind = "anonymous type"
            else:
                q = i + 1
                simple = None
                while q < len(tt) and (tt[q].kind in ("id", "kw") or tt[q].text in (".", "::")):
                    if tt[q].kind in ("id", "kw"):
                        simple = tt[q].text
                    q += 1
                q = _generic_skip(tt, q)
                while q < len(tt) and tt[q].text == "?":
                    q += 1
                if q < len(tt) and tt[q].text == "[":
                    kind = f"new {simple}[] array"
                elif simple in structs or simple in PRIMITIVES - {"object", "string"}:
                    kind = None
                else:
                    kind = f"new {simple}"
        elif t.kind == "string" and x.startswith(("$", "@$")):
            kind = "string interpolation"
        elif t.kind == "punct" and x == "+":
            prev = tt[i - 1] if i else None
            nxt = tt[i + 1] if i + 1 < len(tt) else None
            if (prev is not None and prev.kind == "string") or (nxt is not None and nxt.kind == "string"):
                if not (prev is not None and prev.kind == "string" and nxt is not None and nxt.kind == "string"):
                    kind = "string concatenation"
        elif t.kind == "punct" and x == "+=":
            if i + 1 < len(tt) and tt[i + 1].kind == "string":
                kind = "string concatenation"
        elif t.kind == "id" and x in ("Format", "Join", "Concat") and receiver(tt, i) in ("string", "String"):
            kind = f"string.{x}"
        elif t.kind == "id" and x == "ToString" and receiver(tt, i) is not None and is_call(tt, i):
            kind = "ToString()"
        elif t.kind == "punct" and x == "=>":
            if not (tt is toks and i in arms):
                kind = "lambda"
        elif t.kind == "kw" and x == "delegate" and i + 1 < len(tt) and tt[i + 1].text in ("(", "{"):
            kind = "anonymous method"
        elif t.kind == "id" and x in LINQ and is_call(tt, i):
            rc = receiver(tt, i)
            if rc is not None and rc not in NOT_LINQ_RECEIVERS:
                if x in ("ToArray", "ToList"):
                    kind = f"{x}() (allocates)"
                elif linq_file:
                    kind = f"LINQ .{x}()"
        elif t.kind == "kw" and x == "foreach" and linq_file and tt is toks and i + 1 < len(tt) and tt[i + 1].text == "(":
            close = fp.match.get(i + 1)
            if close:
                k = i + 2
                while k < close and tt[k].text != "in":
                    k += 1
                for q in range(k + 1, close):
                    if tt[q].kind == "id" and tt[q].text in LINQ and receiver(tt, q) is not None and is_call(tt, q):
                        kind = "foreach over LINQ"
                        break
        if kind:
            out.append((t.line, kind))
    return out


# ------------------------------------------------------- statics, singletons

def static_state(model):
    """Static mutable state: static non-const non-readonly fields; static
    readonly fields holding a mutable collection/array/StringBuilder; static
    auto-properties with a set/init accessor; static events."""
    out = []
    for td in model.types:
        for m in td.members:
            mods = set(m.modifiers)
            if "static" not in mods:
                continue
            kind = None
            if m.kind == "field" and "readonly" not in mods:
                kind = "static field"
            elif m.kind == "field" and MUTABLE_COLLECTION.search(m.type_text or ""):
                kind = "static readonly mutable collection"
            elif m.kind == "property" and any(a.kind in ("set", "init") and a.body is None for a in m.accessors):
                kind = "static auto-property with setter"
            elif m.kind in ("event_field", "event"):
                kind = "static event"
            if kind:
                sf = model.by_path[m.file]
                out.append({"file": m.file, "line": m.name_line, "type": td.display_name, "member": m.name,
                            "declared_type": m.type_text, "kind": kind, "access": m.effective_access(),
                            "assembly": sf.assembly, "role": sf.role})
    out.sort(key=lambda r: (r["file"], r["line"], r["member"]))
    return out


def singletons(model):
    """`Type.Instance` / `Type.HasInstance` sites (any identifier before the
    dot), `Type.Current` sites for types that declare a static Current, the
    types declaring such static accessors, and FindObject*-style searches in
    runtime code grouped by type argument."""
    definers = {}
    for td in model.types:
        for m in td.members:
            if m.name in ("Instance", "HasInstance", "Current", "instance") and "static" in m.modifiers:
                definers.setdefault(td.name, []).append({"member": m.name, "kind": m.kind,
                                                         "file": m.file, "line": m.name_line})
    current_types = {k for k, v in definers.items() if any(d["member"] == "Current" for d in v)}
    sites = []
    searches = []
    for path, fp in sorted(model.files.items()):
        role = model.by_path[path].role
        for tt, i, owner in walk(fp.tokens, 0, len(fp.tokens)):
            t = tt[i]
            if t.kind != "id":
                continue
            if t.text in ("Instance", "HasInstance", "Current") and i >= 2 and tt[i - 1].text == "." \
                    and tt[i - 2].kind == "id":
                typ = tt[i - 2].text
                if t.text == "Current" and typ not in current_types:
                    continue
                member = model.member_at(path, owner)
                sites.append({"type": typ, "accessor": t.text, "file": path, "line": t.line, "role": role,
                              "in_member": _member_name(member)})
            elif t.text in FIND_OBJECT and role == "runtime" and is_call(tt, i):
                arg = "?"
                if i + 1 < len(tt) and tt[i + 1].text == "<":
                    q = _generic_skip(tt, i + 1)
                    arg = cslex.join_tokens(tt[i + 2:q - 1])
                elif i + 4 < len(tt) and tt[i + 1].text == "(" and tt[i + 2].text == "typeof":
                    arg = tt[i + 4].text
                searches.append({"type": arg, "call": t.text, "file": path, "line": t.line})
    return definers, sites, searches
