"""A small C# lexer and structural parser for the audit tools.

Lexer (`lex`): produces tokens with 1-based line numbers. Comments and
whitespace are dropped; preprocessor lines (#if/#region/...) are recorded but
not tokenised (both branches of an #if are lexed, which is fine for this
codebase). Token kinds:
  id      identifiers and contextual keywords (var, get, set, when, nameof...)
  kw      reserved C# keywords
  number  numeric literals (hex/binary, digit separators, exponents, suffixes)
  string  "..." / @"..." / $"..." / $@"..." / @$"..." as ONE token (text kept);
          interpolated strings carry `holes`: a token list per {expression}
  char    character literals
  punct   punctuation; multi-char operators (=>, ??, ?., ??=, &&, ||, ==, !=,
          <=, >=, ++, --, +=, -=, ..., <<, ::) are single tokens. `>>` is two
          `>` tokens so nested generics close cleanly.

Parser (`parse`): brace matching plus declaration heuristics, not a full C#
grammar. Finds namespaces, types (class/struct/interface/enum/record/
delegate, nested; partial parts are merged later by full name in
`parse_project`), and members (methods, constructors, finalizers, operators,
properties/indexers/events with accessor bodies, expression-bodied members,
fields, constants, event fields, enum members) with line spans, modifiers,
attributes and signature text. Local functions and lambdas belong to the
enclosing member's body.
"""
from __future__ import annotations

import re
from dataclasses import dataclass, field

KEYWORDS = set("""
abstract as base bool break byte case catch char checked class const continue
decimal default delegate do double else enum event explicit extern false
finally fixed float for foreach goto if implicit in int interface internal is
lock long namespace new null object operator out override params private
protected public readonly ref return sbyte sealed short sizeof stackalloc
static string struct switch this throw true try typeof uint ulong unchecked
unsafe ushort using virtual void volatile while
""".split())

MODIFIERS = {"public", "private", "protected", "internal", "static", "readonly",
             "const", "abstract", "virtual", "override", "sealed", "partial",
             "async", "extern", "unsafe", "volatile", "new", "fixed", "ref"}

TYPE_KEYWORDS = ("class", "struct", "interface", "enum", "record")

_PUNCT3 = ("??=", "<<=", "...")
_PUNCT2 = ("=>", "==", "!=", "<=", ">=", "&&", "||", "??", "?.", "++", "--",
           "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<", "::", "->", "..")

_MASTER = re.compile(r"""
  (?P<ws>[ \t\f\v]+|\n)
| (?P<lc>//[^\n]*)
| (?P<bc>/\*.*?\*/)
| (?P<pp>\#[^\n]*)
| (?P<istr>\$@"|@\$"|\$")
| (?P<vstr>@"(?:[^"]|"")*")
| (?P<str>"(?:[^"\\\n]|\\.)*")
| (?P<chr>'(?:[^'\\\n]|\\.)+')
| (?P<num>0[xX][0-9a-fA-F_]+[uUlL]*|0[bB][01_]+[uUlL]*
        |\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d[\d_]*)?[fFdDmMuUlL]{0,2}
        |\.\d[\d_]*(?:[eE][+-]?\d[\d_]*)?[fFdDmM]?)
| (?P<id>@?[^\W\d]\w*)
| (?P<p3>\?\?=|<<=|\.\.\.)
| (?P<p2>=>|==|!=|<=|>=|&&|\|\||\?\?|\?\.|\+\+|--|\+=|-=|\*=|/=|%=|&=|\|=|\^=|<<|::|->|\.\.)
| (?P<p1>[{}()\[\];,.:+\-*/%&|^!~=<>?\\])
""", re.S | re.X)


class LexError(Exception):
    pass


class Token:
    __slots__ = ("kind", "text", "line", "end_line", "holes")

    def __init__(self, kind, text, line, end_line=None, holes=None):
        self.kind = kind
        self.text = text
        self.line = line
        self.end_line = line if end_line is None else end_line
        self.holes = holes

    def __repr__(self):
        return f"Token({self.kind},{self.text!r},{self.line})"


@dataclass
class LexResult:
    tokens: list
    code_lines: set          # lines holding a token or a preprocessor directive
    comment_lines: set       # lines touched by a comment
    preproc_lines: set
    n_lines: int


def _scan_interp(text, i, verbatim, line, holes_out):
    """Scans an interpolated string body starting just after the opening quote.
    Returns (index after closing quote, line at that point). Hole expressions
    are lexed recursively into `holes_out`."""
    n = len(text)
    while i < n:
        c = text[i]
        if c == '"':
            if verbatim and i + 1 < n and text[i + 1] == '"':
                i += 2
                continue
            return i + 1, line
        if c == "\\" and not verbatim:
            i += 2
            continue
        if c == "\n":
            line += 1
            i += 1
            continue
        if c == "{":
            if i + 1 < n and text[i + 1] == "{":
                i += 2
                continue
            start, start_line = i + 1, line
            i, line, expr_end = _scan_hole(text, i + 1, line)
            holes_out.append(_lex_range(text, start, expr_end, start_line).tokens)
            i += 1  # past '}'
            continue
        if c == "}" and i + 1 < n and text[i + 1] == "}":
            i += 2
            continue
        i += 1
    raise LexError(f"unterminated interpolated string at line {line}")


def _scan_hole(text, i, line):
    """Scans an interpolation hole from just after '{'. Returns
    (index of closing '}', line, end index of the expression part)."""
    depth = 0
    expr_end = None
    n = len(text)
    while i < n:
        c = text[i]
        if c == "\n":
            line += 1
        elif c in "([{":
            depth += 1
        elif c in ")]":
            depth -= 1
        elif c == "}":
            if depth == 0:
                return i, line, (expr_end if expr_end is not None else i)
            depth -= 1
        elif c == ":" and depth == 0 and expr_end is None:
            if text.startswith("::", i):
                i += 2
                continue
            expr_end = i  # format specifier follows; skip to '}'
            j = text.index("}", i)
            line += text.count("\n", i, j)
            return j, line, expr_end
        elif c in "$@\"'":
            m = re.compile(r'\$@"|@\$"|\$"').match(text, i)
            if m:
                sub = []
                i, line = _scan_interp(text, m.end(), "@" in m.group(), line, sub)
                continue
            m = re.compile(r'@"(?:[^"]|"")*"|"(?:[^"\\\n]|\\.)*"|\'(?:[^\'\\\n]|\\.)+\'').match(text, i)
            if m:
                line += m.group().count("\n")
                i = m.end()
                continue
        i += 1
    raise LexError(f"unterminated interpolation hole at line {line}")


def _lex_range(text, pos, end, line):
    tokens = []
    code, comments, pp = set(), set(), set()
    master = _MASTER
    while pos < end:
        m = master.match(text, pos, end)
        if not m:
            raise LexError(f"cannot lex {text[pos:pos + 20]!r} at line {line}")
        kind = m.lastgroup
        s = m.group()
        if kind == "ws":
            if s == "\n":
                line += 1
            pos = m.end()
            continue
        if kind == "lc":
            comments.add(line)
        elif kind == "bc":
            nl = s.count("\n")
            comments.update(range(line, line + nl + 1))
            line += nl
        elif kind == "pp":
            pp.add(line)
            code.add(line)
        elif kind == "istr":
            holes = []
            j, end_line = _scan_interp(text, m.end(), "@" in s, line, holes)
            tokens.append(Token("string", text[pos:j], line, end_line, holes))
            code.update(range(line, end_line + 1))
            line = end_line
            pos = j
            continue
        elif kind in ("vstr", "str"):
            nl = s.count("\n")
            tokens.append(Token("string", s, line, line + nl))
            code.update(range(line, line + nl + 1))
            line += nl
        elif kind == "chr":
            tokens.append(Token("char", s, line))
            code.add(line)
        elif kind == "num":
            tokens.append(Token("number", s, line))
            code.add(line)
        elif kind == "id":
            tokens.append(Token("kw" if s in KEYWORDS else "id", s, line))
            code.add(line)
        else:
            tokens.append(Token("punct", s, line))
            code.add(line)
        pos = m.end()
    return LexResult(tokens, code, comments, pp, line)


def lex(text: str) -> LexResult:
    """Lexes a whole file (LF line endings expected)."""
    r = _lex_range(text, 0, len(text), 1)
    r.n_lines = text.count("\n") + (0 if text.endswith("\n") or not text else 1)
    return r


def flatten(tokens):
    """Yields tokens including the tokens inside interpolation holes."""
    for t in tokens:
        yield t
        if t.holes:
            for h in t.holes:
                yield from flatten(h)


def match_brackets(tokens):
    """Index of the matching bracket for every (, [, { and ), ], } token."""
    match = {}
    stack = []
    pairs = {")": "(", "]": "[", "}": "{"}
    for i, t in enumerate(tokens):
        if t.kind != "punct":
            continue
        x = t.text
        if x in "([{" and len(x) == 1:
            stack.append(i)
        elif x in pairs and len(x) == 1:
            if not stack or tokens[stack[-1]].text != pairs[x]:
                raise LexError(f"unbalanced {x!r} at line {t.line}")
            j = stack.pop()
            match[i] = j
            match[j] = i
    if stack:
        raise LexError(f"unclosed {tokens[stack[-1]].text!r} at line {tokens[stack[-1]].line}")
    return match


# ---------------------------------------------------------------- structure

@dataclass
class Attribute:
    name: str       # last dotted segment without an `Attribute` suffix
    args: str       # argument text (joined tokens), '' when none
    target: str     # `field`, `assembly`, ... or ''
    lo: int         # token range of the whole [...] section
    hi: int


@dataclass
class Accessor:
    kind: str       # get | set | init | add | remove
    line: int
    end_line: int
    body: tuple | None   # (lo, hi) token range of the body contents, None = auto
    expression: bool = False
    modifiers: list = field(default_factory=list)


@dataclass
class Member:
    kind: str       # method ctor dtor operator conversion property indexer event
    #                 field const event_field enum_member
    name: str
    file: str
    modifiers: list
    attributes: list
    type_text: str  # return / field / property type ('' for ctors)
    params: str     # parameter list text including parentheses ('' if none)
    signature: str
    line: int       # first header token (attributes excluded)
    end_line: int
    name_line: int
    lo: int         # token range of the declaration (header..end), attributes excluded
    hi: int
    body: tuple | None = None        # (lo, hi) method body contents or expression
    expression_bodied: bool = False
    accessors: list = field(default_factory=list)
    init: tuple | None = None        # (lo, hi) initializer tokens (fields, properties, enum members)
    parent: object = None

    @property
    def access(self) -> str:
        m = self.modifiers
        if "public" in m:
            return "public"
        if "protected" in m and "internal" in m:
            return "protected internal"
        if "private" in m and "protected" in m:
            return "private protected"
        for a in ("protected", "internal", "private"):
            if a in m:
                return a
        return ""  # default: private in classes/structs, public in interfaces/enums

    def effective_access(self) -> str:
        a = self.access
        if a:
            return a
        if self.parent is not None and self.parent.kind in ("interface", "enum"):
            return "public"
        if self.kind == "enum_member":
            return "public"
        return "private"


@dataclass
class TypePart:
    file: str
    line: int
    end_line: int
    lo: int
    hi: int
    body: tuple | None    # (lo, hi) body contents token range


@dataclass
class TypeDecl:
    kind: str       # class struct interface enum record record struct delegate
    name: str
    type_params: list
    namespace: str
    outer: object         # TypeDecl or None
    modifiers: list
    attributes: list
    bases: list
    parts: list = field(default_factory=list)
    members: list = field(default_factory=list)
    nested: list = field(default_factory=list)

    @property
    def simple_name(self) -> str:
        return self.name

    @property
    def display_name(self) -> str:
        n = self.name + (f"<{','.join(self.type_params)}>" if self.type_params else "")
        prefix = self.outer.display_name + "." if self.outer else (self.namespace + "." if self.namespace else "")
        return prefix + n

    @property
    def key(self) -> str:
        n = self.name + (f"`{len(self.type_params)}" if self.type_params else "")
        prefix = self.outer.key + "+" if self.outer else (self.namespace + "." if self.namespace else "")
        return prefix + n

    @property
    def files(self) -> list:
        return sorted({p.file for p in self.parts})


@dataclass
class FileParse:
    path: str
    tokens: list
    lexinfo: LexResult
    match: dict
    types: list                     # every TypeDecl part created in this file (flat)
    members: list                   # every Member in this file
    usings: list                    # (lo, hi) token ranges of using directives
    attr_ranges: list               # (lo, hi) token ranges of declaration attributes
    namespaces: list                # names declared
    using_names: list               # namespaces imported


_SPACE_BEFORE_NOT = {")", "]", ">", ",", ";", ".", "?.", "(", "[", "?", "::"}
_SPACE_AFTER_NOT = {"(", "[", "<", ".", "?.", "::", "~", "!"}


def join_tokens(tokens) -> str:
    """Readable single-line text for a token run."""
    out = []
    prev = None
    for t in tokens:
        x = t.text
        if prev is not None:
            space = True
            if x in _SPACE_BEFORE_NOT or prev.text in _SPACE_AFTER_NOT:
                space = False
            if x == "<" and prev.kind in ("id", "kw"):
                space = False
            if x == "(" and prev.kind in ("kw",) and prev.text in ("if", "for", "foreach", "while", "switch", "catch", "using", "lock", "return"):
                space = True
            if x == "?" and prev.kind in ("id", "kw") and prev.text not in ("return",):
                space = False
            if prev.text == ">" and x not in ("(", ")", ",", ">", "[", "]", ";", ".", "?"):
                space = True
            if prev.text == "," or prev.text in ("=>", "=", "==", "&&", "||", "??", ":"):
                space = True
            if x in ("=>", "=", "==", "&&", "||", "??") or (x == ":" and prev.text != "::"):
                space = True
            if space:
                out.append(" ")
        out.append(x)
        prev = t
    return "".join(out)


class _Parser:
    def __init__(self, path, lexinfo):
        self.path = path
        self.t = lexinfo.tokens
        self.lexinfo = lexinfo
        self.m = match_brackets(self.t)
        self.types = []
        self.members = []
        self.usings = []
        self.attr_ranges = []
        self.namespaces = []
        self.using_names = []

    # helpers
    def tx(self, i):
        return self.t[i].text if 0 <= i < len(self.t) else ""

    def skip_to_semicolon(self, j, end):
        """From j, skips balanced groups until the ';' at depth 0; returns its index."""
        t = self.t
        while j < end:
            x = t[j].text
            if t[j].kind == "punct":
                if x in ("(", "[", "{"):
                    j = self.m[j] + 1
                    continue
                if x == ";":
                    return j
                if x == "}":   # malformed; stop at container end
                    return j - 1
            j += 1
        return end - 1

    def parse_attrs(self, i):
        """Parses consecutive [...] sections at i. Returns (attrs, next index)."""
        attrs = []
        t = self.t
        while i < len(t) and t[i].text == "[" and t[i].kind == "punct":
            j = self.m[i]
            self.attr_ranges.append((i, j + 1))
            k = i + 1
            target = ""
            if k + 1 < j and t[k].kind in ("id", "kw") and t[k + 1].text == ":":
                target = t[k].text
                k += 2
            while k < j:
                # one attribute: dotted name [ (args) ]
                name_parts = []
                while k < j and t[k].text not in ("(", ","):
                    if t[k].kind in ("id", "kw"):
                        name_parts.append(t[k].text)
                    k += 1
                args = ""
                if k < j and t[k].text == "(":
                    close = self.m[k]
                    args = join_tokens(t[k + 1:close])
                    k = close + 1
                name = name_parts[-1] if name_parts else "?"
                if name.endswith("Attribute") and len(name) > 9:
                    name = name[:-9]
                attrs.append(Attribute(name, args, target, i, j + 1))
                while k < j and t[k].text != ",":
                    k += 1
                k += 1
            i = j + 1
        return attrs, i

    def generic_close(self, q):
        """If t[q] is a '<' that opens generic type arguments (follows an
        identifier and encloses only type-ish tokens), returns the index of
        the matching '>', else None."""
        t = self.t
        if q == 0 or t[q - 1].kind != "id":
            return None
        depth = 0
        r = q
        while r < len(t):
            x = t[r].text
            if x == "<":
                depth += 1
            elif x == ">":
                depth -= 1
                if depth == 0:
                    return r
            elif t[r].kind in ("id", "kw") or x in (",", ".", "?", "[", "]", "(", ")", "::"):
                pass
            else:
                return None
            r += 1
        return None

    def header_end(self, i, end):
        """Scans a declaration header; returns index of the first depth-0
        '{', ';', '=>' or '=' (or end)."""
        t = self.t
        j = i
        while j < end:
            tok = t[j]
            if tok.kind == "punct":
                x = tok.text
                if x in ("(", "["):
                    j = self.m[j] + 1
                    continue
                if x in ("{", ";", "=>", "=", "}"):
                    return j
            j += 1
        return end

    # containers
    def parse_container(self, i, end, ns, outer):
        t = self.t
        while i < end:
            tok = t[i]
            if tok.text == ";" and tok.kind == "punct":
                i += 1
                continue
            attrs, i = self.parse_attrs(i)
            if i >= end:
                break
            h = i
            j = self.header_end(i, end)
            head = t[h:j]
            texts = [x.text for x in head]
            stop = t[j].text if j < end else ""
            # using directive / extern alias
            if texts and texts[0] in ("using", "extern") and outer is None and "(" not in texts:
                k = self.skip_to_semicolon(j, end)
                self.usings.append((h, k + 1))
                if texts[0] == "using":
                    names = [x for x in texts[1:] if x not in ("static", ".")]
                    if "=" not in texts and stop != "=":
                        self.using_names.append(".".join(names))
                i = k + 1
                continue
            if "namespace" in texts:
                k = texts.index("namespace")
                name = "".join(texts[k + 1:])
                full = ".".join([n for n in ns + [name] if n])
                self.namespaces.append(full)
                if stop == "{":
                    close = self.m[j]
                    self.parse_container(j + 1, close, ns + [name], None)
                    i = close + 1
                else:  # file-scoped namespace
                    self.parse_container(j + 1, end, ns + [name], None)
                    i = end
                continue
            kind_idx = self._type_keyword(head)
            if kind_idx is not None:
                i = self.parse_type(h, j, end, kind_idx, attrs, ns, outer)
                continue
            if outer is None:
                # stray tokens at namespace level (e.g. global attributes)
                i = (self.skip_to_semicolon(j, end) + 1) if stop != "{" else self.m[j] + 1
                continue
            i = self.parse_member(h, j, end, attrs, outer)
        return i

    def _type_keyword(self, head):
        for k, tok in enumerate(head):
            x = tok.text
            if x == "(" or x == "where":
                return None
            if x in ("class", "struct", "interface", "enum") and tok.kind == "kw":
                if k + 1 < len(head) and head[k + 1].kind == "id":
                    return k
            if x == "record" and tok.kind == "id" and k + 1 < len(head) and (
                    head[k + 1].kind == "id" or head[k + 1].text in ("class", "struct")):
                if all(p.text in MODIFIERS or p.text == "new" for p in head[:k]):
                    return k
            if x == "delegate" and tok.kind == "kw":
                return k
        return None

    def parse_type(self, h, j, end, k, attrs, ns, outer):
        t = self.t
        head = t[h:j]
        kind = head[k].text
        n = k + 1
        if kind == "record" and head[n].text in ("class", "struct"):
            kind = "record struct" if head[n].text == "struct" else "record"
            n += 1
        mods = [x.text for x in head[:k] if x.text in MODIFIERS]
        if kind == "delegate":
            # delegate RetType Name<T>(params);
            p = next((q for q in range(k + 1, len(head)) if head[q].text == "("), len(head))
            name_idx = p - 1
            if head[name_idx].text == ">":
                depth = 0
                for q in range(name_idx, k, -1):
                    if head[q].text == ">":
                        depth += 1
                    elif head[q].text == "<":
                        depth -= 1
                        if depth == 0:
                            name_idx = q - 1
                            break
            name = head[name_idx].text
            tparams = []
            bases = []
        else:
            name = head[n].text
            tparams = []
            q = n + 1
            if q < len(head) and head[q].text == "<":
                depth = 0
                while q < len(head):
                    x = head[q].text
                    if x == "<":
                        depth += 1
                    elif x == ">":
                        depth -= 1
                        if depth == 0:
                            break
                    elif head[q].kind == "id" and depth == 1:
                        tparams.append(x)
                    q += 1
                q += 1
            if q < len(head) and head[q].text == "(":   # record primary constructor
                q = self.m[h + q] - h + 1
            bases = []
            if q < len(head) and head[q].text == ":":
                cur = []
                depth = 0
                q += 1
                while q < len(head):
                    x = head[q].text
                    if x == "where" and depth == 0:
                        break
                    if x == "<":
                        depth += 1
                    elif x == ">":
                        depth -= 1
                    if x == "," and depth == 0:
                        bases.append(join_tokens(cur))
                        cur = []
                    elif x == "(":   # record base arguments
                        q = self.m[h + q] - h + 1
                        continue
                    else:
                        cur.append(head[q])
                    q += 1
                if cur:
                    bases.append(join_tokens(cur))
        td = TypeDecl(kind=kind, name=name, type_params=tparams,
                      namespace=".".join(x for x in ns if x), outer=outer,
                      modifiers=mods, attributes=attrs, bases=bases)
        stop = self.tx(j)
        if stop == "{" and kind != "delegate":
            close = self.m[j]
            part = TypePart(self.path, t[h].line, t[close].line, h, close + 1, (j + 1, close))
            td.parts.append(part)
            self.types.append(td)
            if outer is not None:
                outer.nested.append(td)
            if kind == "enum":
                self.parse_enum(j + 1, close, td)
            else:
                self.parse_container(j + 1, close, ns, td)
            return close + 1
        k2 = self.skip_to_semicolon(j, end)
        part = TypePart(self.path, t[h].line, t[k2].line, h, k2 + 1, None)
        td.parts.append(part)
        self.types.append(td)
        if outer is not None:
            outer.nested.append(td)
        return k2 + 1

    def parse_enum(self, i, end, td):
        t = self.t
        while i < end:
            attrs, i = self.parse_attrs(i)
            if i >= end:
                break
            j = i
            while j < end and t[j].text != ",":
                if t[j].text in ("(", "[", "{"):
                    j = self.m[j]
                j += 1
            if t[i].kind == "id":
                init = None
                if i + 1 < j and t[i + 1].text == "=":
                    init = (i + 2, j)
                mem = Member("enum_member", t[i].text, self.path, [], attrs, "", "",
                             join_tokens(t[i:j]), t[i].line, t[j - 1].line, t[i].line,
                             i, j, init=init, parent=td)
                td.members.append(mem)
                self.members.append(mem)
            i = j + 1

    def _params_paren(self, h, j):
        """Index of the parameter-list '(' in header [h, j) or None."""
        t = self.t
        q = h
        while q < j:
            if t[q].text == "(":
                prev = t[q - 1] if q > h else None
                prev2 = t[q - 2] if q - 1 > h else None
                if prev is not None and (prev.kind == "id" or prev.text == ">" or prev.text == "operator"
                                         or (prev2 is not None and prev2.text == "operator")):
                    return q
                q = self.m[q] + 1
                continue
            if t[q].text == "[":
                q = self.m[q] + 1
                continue
            q += 1
        return None

    def parse_member(self, h, j, end, attrs, outer):
        t = self.t
        stop = self.tx(j)
        head = t[h:j]
        texts = [x.text for x in head]
        mods = []
        q = 0
        while q < len(head) and head[q].text in MODIFIERS and head[q].kind in ("kw", "id"):
            # `partial`/`async`/`record` are contextual; accept only in leading position
            mods.append(head[q].text)
            q += 1
        is_event = q < len(head) and head[q].text == "event"
        if is_event:
            mods.append("event")
            q += 1
        rest_lo = h + q
        pp = self._params_paren(rest_lo, j)
        is_indexer = False
        if pp is None:
            for r in range(rest_lo, j):
                if t[r].text == "this" and r + 1 < j and t[r + 1].text == "[":
                    is_indexer = True
                    break
        member_kind = None
        name = ""
        type_text = ""
        params = ""
        name_line = t[h].line
        if pp is not None:
            params = join_tokens(t[pp:self.m[pp] + 1])
            name_idx = pp - 1
            if t[name_idx].text == ">":   # generic method
                depth = 0
                for r in range(name_idx, rest_lo - 1, -1):
                    if t[r].text == ">":
                        depth += 1
                    elif t[r].text == "<":
                        depth -= 1
                        if depth == 0:
                            name_idx = r - 1
                            break
            if "operator" in texts:
                oi = h + texts.index("operator")
                if any(x in ("implicit", "explicit") for x in texts):
                    member_kind = "conversion"
                    name = "operator " + join_tokens(t[oi + 1:pp])
                    type_text = join_tokens(t[oi + 1:pp])
                else:
                    member_kind = "operator"
                    name = "operator " + join_tokens(t[oi + 1:pp])
                    type_text = join_tokens(t[rest_lo:oi])
                name_line = t[oi].line
            elif t[rest_lo].text == "~":
                member_kind = "dtor"
                name = "~" + t[name_idx].text
                name_line = t[name_idx].line
            else:
                name = t[name_idx].text
                name_line = t[name_idx].line
                # explicit interface implementation: I.Name
                lo_name = name_idx
                while lo_name - 2 >= rest_lo and t[lo_name - 1].text == "." and t[lo_name - 2].kind == "id":
                    lo_name -= 2
                type_text = join_tokens(t[rest_lo:lo_name])
                if lo_name != name_idx:
                    name = join_tokens(t[lo_name:name_idx + 1])
                if not type_text and name == outer.name:
                    member_kind = "ctor"
                else:
                    member_kind = "method"
        elif is_indexer:
            member_kind = "indexer"
            r = next(r for r in range(rest_lo, j) if t[r].text == "this")
            name = "this[]"
            type_text = join_tokens(t[rest_lo:r])
            params = join_tokens(t[r + 1:self.m[r + 1] + 1])
            name_line = t[r].line
        else:
            # field / property / event: type then name(s)
            if stop in ("{", "=>"):
                member_kind = "event" if is_event else "property"
                name_idx = j - 1
                lo_name = name_idx
                while lo_name - 2 >= rest_lo and t[lo_name - 1].text == "." and t[lo_name - 2].kind == "id":
                    lo_name -= 2
                name = join_tokens(t[lo_name:name_idx + 1]) if name_idx >= rest_lo else "?"
                name_line = t[name_idx].line if name_idx >= rest_lo else t[h].line
                type_text = join_tokens(t[rest_lo:lo_name])
            else:
                member_kind = "event_field" if is_event else ("const" if "const" in mods else "field")
        sig_hi = j
        if member_kind in ("field", "const", "event_field"):
            return self._parse_fields(h, j, end, attrs, outer, mods, rest_lo, member_kind)

        mem = Member(member_kind, name, self.path, mods, attrs, type_text, params, "",
                     t[h].line, t[h].line, name_line, h, h, parent=outer)
        mem.signature = join_tokens(t[h:sig_hi]) if sig_hi > h else name
        if stop == "{":
            close = self.m[j]
            if member_kind in ("property", "indexer", "event"):
                mem.accessors = self.parse_accessors(j + 1, close)
                last = close
                # property initializer: `{ get; set; } = value;`
                if close + 1 < end and t[close + 1].text == "=":
                    k = self.skip_to_semicolon(close + 2, end)
                    mem.init = (close + 2, k)
                    last = k
            else:
                mem.body = (j + 1, close)
                last = close
            mem.end_line = t[last].line
            mem.lo, mem.hi = h, last + 1
        elif stop == "=>":
            k = self.skip_to_semicolon(j + 1, end)
            mem.expression_bodied = True
            if member_kind in ("property", "indexer"):
                mem.accessors = [Accessor("get", t[j].line, t[k].line, (j + 1, k), expression=True)]
            else:
                mem.body = (j + 1, k)
            mem.end_line = t[k].line
            mem.lo, mem.hi = h, k + 1
            last = k
        else:  # ';' abstract / interface / extern / partial declaration
            k = self.skip_to_semicolon(j, end) if stop != ";" else j
            mem.end_line = t[k].line
            mem.lo, mem.hi = h, k + 1
            last = k
        outer.members.append(mem)
        self.members.append(mem)
        return last + 1

    def _parse_fields(self, h, j, end, attrs, outer, mods, rest_lo, member_kind):
        """Field declaration(s) `Type a = x, b;` starting with header [h, j)."""
        t = self.t
        k = self.skip_to_semicolon(j, end)
        # declarators: split [rest_lo, k) at depth-0 commas that follow a
        # declarator name (commas inside generic type args are ignored by
        # tracking '<' '>' only in the type part).
        # type part ends at the first declarator name: the identifier followed
        # by '=', ',' or ';' outside angle brackets.
        q = rest_lo
        angle = 0
        first_name = None
        while q < k:
            x = t[q].text
            if x == "<":
                angle += 1
            elif x == ">":
                angle -= 1
            elif x in ("(", "["):
                q = self.m[q] + 1
                continue
            elif angle == 0 and t[q].kind == "id" and t[q + 1].text in ("=", ",", ";"):
                first_name = q
                break
            q += 1
        if first_name is None:
            first_name = k - 1
        type_text = join_tokens(t[rest_lo:first_name])
        decls = []   # (name_idx, init_lo, init_hi)
        q = first_name
        while q < k:
            name_idx = q
            q += 1
            init = None
            if q < k and t[q].text == "=":
                lo = q + 1
                while q < k and t[q].text != ",":
                    if t[q].text in ("(", "[", "{"):
                        q = self.m[q]
                    elif t[q].text == "<":
                        q = self.generic_close(q) or q
                    q += 1
                init = (lo, q)
            decls.append((name_idx, init))
            if q < k and t[q].text == ",":
                q += 1
                continue
            break
        for idx, (name_idx, init) in enumerate(decls):
            sig = " ".join(x for x in (" ".join(mods), type_text, t[name_idx].text) if x)
            mem = Member(member_kind, t[name_idx].text, self.path, list(mods), attrs, type_text, "", sig,
                         t[h].line, t[k].line, t[name_idx].line, h, k + 1, init=init, parent=outer)
            outer.members.append(mem)
            self.members.append(mem)
        return k + 1

    def parse_accessors(self, i, end):
        t = self.t
        out = []
        while i < end:
            attrs, i = self.parse_attrs(i)
            mods = []
            while i < end and t[i].text in MODIFIERS:
                mods.append(t[i].text)
                i += 1
            if i >= end:
                break
            kind = t[i].text
            line = t[i].line
            i += 1
            if i < end and t[i].text == "{":
                close = self.m[i]
                out.append(Accessor(kind, line, t[close].line, (i + 1, close), False, mods))
                i = close + 1
            elif i < end and t[i].text == "=>":
                k = self.skip_to_semicolon(i + 1, end)
                out.append(Accessor(kind, line, t[k].line, (i + 1, k), True, mods))
                i = k + 1
            elif i < end and t[i].text == ";":
                out.append(Accessor(kind, line, line, None, False, mods))
                i += 1
            else:
                i += 1
        return out


def parse(path: str, text: str) -> FileParse:
    lexinfo = lex(text)
    p = _Parser(path, lexinfo)
    p.parse_container(0, len(p.t), [], None)
    return FileParse(path, p.t, lexinfo, p.m, p.types, p.members, p.usings,
                     p.attr_ranges, p.namespaces, p.using_names)


@dataclass
class Project:
    files: dict            # path -> FileParse
    types: dict            # key -> TypeDecl (partials merged)
    order: list            # type keys in deterministic order


def parse_project(sources) -> Project:
    """Parses every SourceFile and merges partial types by full name.

    Merged TypeDecl objects keep all parts (file + line span); members from all
    parts are concatenated in file order. Member.parent points at the merged
    TypeDecl.
    """
    files = {}
    merged = {}
    for sf in sorted(sources, key=lambda s: s.path):
        fp = parse(sf.path, sf.read())
        files[sf.path] = fp
        for td in fp.types:
            key = td.key
            if key in merged:
                tgt = merged[key]
                tgt.parts.extend(td.parts)
                tgt.members.extend(td.members)
                tgt.nested.extend(td.nested)
                for a in td.attributes:
                    tgt.attributes.append(a)
                for m in td.modifiers:
                    if m not in tgt.modifiers:
                        tgt.modifiers.append(m)
                for b in td.bases:
                    if b not in tgt.bases:
                        tgt.bases.append(b)
                _redirect(td, tgt)
            else:
                merged[key] = td
    for td in merged.values():
        td.nested = _dedupe(td.nested, merged)
        if td.outer is not None:
            td.outer = merged.get(td.outer.key, td.outer)
    order = sorted(merged)
    return Project(files, merged, order)


def _dedupe(nested, merged):
    seen = {}
    for n in nested:
        seen.setdefault(n.key, merged.get(n.key, n))
    return [seen[k] for k in sorted(seen)]


def _redirect(src, tgt):
    for m in src.members:
        m.parent = tgt
    for n in src.nested:
        n.outer = tgt


# ---------------------------------------------------------------- methods

@dataclass
class Unit:
    """A method-like unit with a body: method/ctor/operator body, an accessor
    body, or an expression-bodied member. `lines` = header line .. last line."""
    name: str          # Type.Member(params) or Type.Prop.get
    member: Member
    type: TypeDecl
    file: str
    line: int          # first line (member header, or accessor keyword)
    end_line: int
    name_line: int     # where Roslyn reports it (member name / accessor keyword)
    body: tuple        # (lo, hi) token range
    accessor: str = ""

    @property
    def lines(self) -> int:
        return self.end_line - self.line + 1


def units_of(project: Project):
    """All method-like units, deterministic order."""
    out = []
    for key in project.order:
        td = project.types[key]
        for m in td.members:
            base = f"{td.display_name}.{m.name}"
            if m.body is not None and m.kind in ("method", "ctor", "dtor", "operator", "conversion"):
                out.append(Unit(base + _short_params(m.params), m, td, m.file, m.line, m.end_line,
                                m.name_line, m.body))
            for acc in m.accessors:
                if acc.body is None:
                    continue
                if acc.expression and m.expression_bodied:
                    # Roslyn reports expression-bodied getters at the expression
                    line, nl = m.line, project.files[m.file].tokens[acc.body[0]].line
                else:
                    line, nl = acc.line, acc.line
                out.append(Unit(f"{base}.{acc.kind}", m, td, m.file, line, acc.end_line, nl,
                                acc.body, acc.kind))
    out.sort(key=lambda u: (u.file, u.line, u.name))
    return out


def _short_params(params: str) -> str:
    """`(int a, string b = "x")` -> `(int, string)`."""
    if not params:
        return "()"
    inner = params[1:-1].strip()
    if not inner:
        return "()"
    parts, depth, cur = [], 0, ""
    for ch in inner:
        if ch in "<([":
            depth += 1
        elif ch in ">)]":
            depth -= 1
        if ch == "," and depth == 0:
            parts.append(cur)
            cur = ""
        else:
            cur += ch
    parts.append(cur)
    types = []
    for p in parts:
        p = p.split("=")[0].strip()
        bits = p.rsplit(" ", 1)
        types.append(bits[0] if len(bits) == 2 else p)
    return "(" + ", ".join(types) + ")"


# ---------------------------------------------------------------- self-test

def self_check(project: Project) -> list:
    """Sanity checks: returns a list of problem strings (empty = fine)."""
    problems = []
    for path, fp in sorted(project.files.items()):
        opens = sum(1 for x in fp.tokens if x.kind == "punct" and x.text == "{")
        closes = sum(1 for x in fp.tokens if x.kind == "punct" and x.text == "}")
        if opens != closes:
            problems.append(f"{path}: braces {opens} open vs {closes} close")
    return problems


_SELFTEST = r'''
#region Header
using System; // "not a string
/* it's a ' block "comment" */
namespace N.M {
  [Serializable, Obsolete("x")]
  public sealed partial class A<T> : Base<T>, IFoo where T : class {
    [SerializeField, Range(0, 1)] private float speed = .5f, other;
    public const long Big = 0x1F_FFUL + 1_000 + (long)1e-3f;
    static readonly Dictionary<string, List<int>> Map = new Dictionary<string, List<int>>();
    public int P { get; private set; } = 3;
    public int Q => x?.y ?? 0;
    public A() : base(1) { }
    ~A() { }
    public static A<T> operator +(A<T> a, A<T> b) => a;
    public string S(int i) {
      var s = $"a {i} {{lit}} {i:0.00} {(i > 0 ? "y" : "z")} {i,-5}" + @"C:\p ""q""" + $@"{i}
line";
      char c = '\'', d = '\\', e = '"';
      int L(int k) { return k > 1 ? k : 1; }
      return s + c;
    }
    enum E { X = 1, [Obsolete] Y, Z }
  }
}
'''


def _selftest():
    fp = parse("selftest.cs", _SELFTEST)
    toks = fp.tokens
    strings = [t for t in toks if t.kind == "string"]
    assert strings[0].text.startswith('"x"'), strings[0]
    interp = strings[1]
    assert interp.text.startswith('$"a {i}') and interp.text.endswith('{i,-5}"'), interp.text
    assert [" ".join(x.text for x in h) for h in interp.holes] == ["i", "i", "( i > 0 ? \"y\" : \"z\" )", "i , - 5"], interp.holes
    assert strings[2].text == r'@"C:\p ""q"""', strings[2].text
    assert strings[3].text.startswith('$@"{i}') and strings[3].end_line == strings[3].line + 1
    chars = [t.text for t in toks if t.kind == "char"]
    assert chars == [r"'\''", r"'\\'", "'\"'"], chars
    nums = [t.text for t in toks if t.kind == "number"]
    assert nums[:4] == ["0", "1", ".5f", "0x1F_FFUL"] and "1e-3f" in nums and "1_000" in nums, nums
    assert sum(1 for t in toks if t.text == ">") >= 4          # >> split for generics
    assert any(t.text == "?." for t in toks) and any(t.text == "??" for t in toks)
    last = toks[-1]
    assert last.text == "}" and last.line == _SELFTEST.count("\n"), (last, _SELFTEST.count("\n"))
    types = {t.name: t for t in fp.types}
    a = types["A"]
    assert a.type_params == ["T"] and a.bases == ["Base<T>", "IFoo"], (a.type_params, a.bases)
    assert a.namespace == "N.M" and {x.name for x in a.attributes} == {"Serializable", "Obsolete"}
    kinds = {(m.kind, m.name) for m in a.members}
    for k in [("field", "speed"), ("field", "other"), ("const", "Big"), ("field", "Map"), ("property", "P"),
              ("property", "Q"), ("ctor", "A"), ("dtor", "~A"), ("operator", "operator +"), ("method", "S")]:
        assert k in kinds, (k, sorted(kinds))
    speed = next(m for m in a.members if m.name == "speed")
    assert {x.name for x in speed.attributes} == {"SerializeField", "Range"} and speed.init is not None
    s = next(m for m in a.members if m.name == "S")
    assert (s.line, s.end_line) == (16, 22), (s.line, s.end_line)     # local function stays inside S
    e = types["E"]
    assert [m.name for m in e.members] == ["X", "Y", "Z"]
    assert self_check(Project({"selftest.cs": fp}, {}, [])) == []
    return "cslex self-test passed"


def _main(argv=None):
    import argparse
    import sys
    from pathlib import Path
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import sources
    ap = argparse.ArgumentParser(description="cslex self-test and project sanity check")
    ap.add_argument("--repo", type=Path, default=Path(__file__).resolve().parent.parent.parent)
    a = ap.parse_args(argv)
    print(_selftest())
    srcs = sources.load_sources(a.repo)
    proj = parse_project(srcs)
    problems = self_check(proj)
    decl = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|protected|internal|static|sealed|abstract|"
                      r"partial|readonly|unsafe|new)\s+)*(?:class|struct|interface|enum|record|delegate)\s+\w+", re.M)
    grep = sum(len(decl.findall(s.read())) for s in srcs)
    parts = sum(len(t.parts) for t in proj.types.values())
    print(f"{len(srcs)} files, {len(proj.types)} types ({parts} parts; line-regex finds {grep} declarations), "
          f"{len(units_of(proj))} method units, brace problems: {len(problems)}")
    for p in problems:
        print("  " + p)
    return 1 if problems or grep != parts else 0


if __name__ == "__main__":
    raise SystemExit(_main())
