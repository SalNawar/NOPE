"""Loads the source set once and offers the helpers the inventory and the
metrics share: parsed project, method units, Unity base-class resolution,
serialized-field detection, identifier-token counts per file and
token -> enclosing-member lookup."""
from __future__ import annotations

import bisect
from collections import Counter
from pathlib import Path

import cslex
import sources

# Unity base classes -> the family they belong to. Anything deriving from one
# of these (directly or through our own types) is a Unity object whose public
# and [SerializeField] fields are serialized.
UNITY_BASES = {
    "MonoBehaviour": "MonoBehaviour", "UIBehaviour": "MonoBehaviour",
    "Graphic": "MonoBehaviour", "MaskableGraphic": "MonoBehaviour",
    "Selectable": "MonoBehaviour", "LayoutGroup": "MonoBehaviour",
    "BaseMeshEffect": "MonoBehaviour", "Shadow": "MonoBehaviour", "Outline": "MonoBehaviour",
    "ScriptableObject": "ScriptableObject", "StateMachineBehaviour": "ScriptableObject",
    "Editor": "Editor", "EditorWindow": "EditorWindow", "ScriptableWizard": "EditorWindow",
}

UNITY_MESSAGES = {
    "Awake", "Start", "Update", "LateUpdate", "FixedUpdate", "OnEnable", "OnDisable",
    "OnDestroy", "OnValidate", "Reset", "OnGUI", "OnDrawGizmos", "OnDrawGizmosSelected",
    "OnApplicationQuit", "OnApplicationPause", "OnApplicationFocus",
    "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay", "OnTriggerEnter2D", "OnTriggerExit2D",
    "OnTriggerStay2D", "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay",
    "OnCollisionEnter2D", "OnCollisionExit2D", "OnCollisionStay2D",
    "OnMouseDown", "OnMouseUp", "OnMouseEnter", "OnMouseExit", "OnMouseOver", "OnMouseDrag",
    "OnMouseUpAsButton", "OnBecameVisible", "OnBecameInvisible",
    "OnRectTransformDimensionsChange", "OnTransformParentChanged", "OnTransformChildrenChanged",
    "OnBeforeSerialize", "OnAfterDeserialize", "OnPreprocessBuild", "OnPostprocessBuild",
    "OnPostprocessAllAssets", "OnPreprocessTexture", "OnPostprocessTexture", "OnPreprocessModel",
    "OnPostprocessModel", "OnPreprocessAsset", "OnWillRenderObject", "OnRenderObject",
    "OnPreRender", "OnPostRender", "OnRenderImage", "OnCanvasGroupChanged",
    "OnDidApplyAnimationProperties", "OnInspectorGUI", "OnSceneGUI", "OnParticleCollision",
    "OnAnimatorMove", "OnAnimatorIK", "OnJointBreak", "OnControllerColliderHit",
    "OnServerInitialized", "OnConnectedToServer", "OnLevelWasLoaded", "OnAudioFilterRead",
    "CreateInspectorGUI", "OnEnterPlayMode", "OnSelectionChange", "OnFocus", "OnLostFocus",
    "OnHierarchyChange", "OnProjectChange", "OnInspectorUpdate",
}

# Attributes that make a member or type an entry point Unity/NUnit call by reflection.
ENTRY_ATTRIBUTES = {"Test", "UnityTest", "TestCase", "TestCaseSource", "SetUp", "TearDown",
                    "OneTimeSetUp", "OneTimeTearDown", "MenuItem", "ContextMenu",
                    "ContextMenuItem", "InitializeOnLoadMethod", "RuntimeInitializeOnLoadMethod",
                    "DidReloadScripts", "PostProcessBuild", "PostProcessScene", "OnOpenAsset",
                    "InitializeOnEnterPlayMode", "TestFixture", "InitializeOnLoad",
                    "CustomEditor", "CustomPropertyDrawer", "CreateAssetMenu"}


def base_name(text: str) -> str:
    """`UnityEngine.MonoBehaviour` / `Foo<T>` -> simple name."""
    t = text.split("<", 1)[0].strip()
    return t.rsplit(".", 1)[-1]


class Model:
    def __init__(self, repo: Path):
        self.repo = Path(repo).resolve()
        self.sources = sources.load_sources(self.repo)
        self.by_path = {s.path: s for s in self.sources}
        self.project = cslex.parse_project(self.sources)
        self.files = self.project.files
        self.types = [self.project.types[k] for k in self.project.order]
        self.units = cslex.units_of(self.project)
        self.head = sources.git_head(self.repo)
        self.types_by_simple = {}
        for td in self.types:
            self.types_by_simple.setdefault(td.name, []).append(td)
        self.struct_names = {td.name for td in self.types if td.kind in ("struct", "record struct", "enum")}
        self.ident_counts = {}
        for path, fp in self.files.items():
            self.ident_counts[path] = Counter(t.text for t in cslex.flatten(fp.tokens) if t.kind == "id")
        self._member_index = {}
        for path, fp in self.files.items():
            spans = sorted((m.lo, m.hi, i) for i, m in enumerate(fp.members))
            self._member_index[path] = ([s[0] for s in spans], spans, fp.members)
        self._unity_cache = {}

    # ---- types
    def primary_file(self, td) -> str:
        return min(p.file for p in td.parts)

    def assembly(self, td) -> str:
        return self.by_path[self.primary_file(td)].assembly

    def owner(self, td) -> str:
        owners = {self.by_path[p.file].owner for p in td.parts}
        return "codex-light-touch" if "codex-light-touch" in owners else "ours"

    def role(self, td) -> str:
        return self.by_path[self.primary_file(td)].role

    def unity_family(self, td, depth=0) -> str | None:
        """MonoBehaviour / ScriptableObject / Editor / EditorWindow when the type
        derives from one (directly or via our own classes), else None."""
        if td.key in self._unity_cache:
            return self._unity_cache[td.key]
        fam = None
        if td.kind == "class" and td.bases and depth < 20:
            b = base_name(td.bases[0])
            if b in UNITY_BASES:
                fam = UNITY_BASES[b]
            else:
                for cand in self.types_by_simple.get(b, []):
                    if cand is not td:
                        fam = self.unity_family(cand, depth + 1)
                        if fam:
                            break
        self._unity_cache[td.key] = fam
        return fam

    def is_serializable_type(self, td) -> bool:
        return bool(self.unity_family(td)) or any(a.name == "Serializable" for a in td.attributes)

    def serialized_fields(self, td) -> list:
        """Fields Unity serializes: public or [SerializeField] instance fields
        (not const/static/readonly, not [NonSerialized]); plus auto-properties
        with [field: SerializeField]."""
        out = []
        for m in td.members:
            names = {a.name for a in m.attributes}
            if m.kind == "field":
                if {"static", "readonly", "const"} & set(m.modifiers) or "NonSerialized" in names:
                    continue
                if "SerializeField" in names or m.effective_access() == "public":
                    out.append(m)
            elif m.kind == "property" and any(a.name == "SerializeField" and a.target == "field" for a in m.attributes):
                out.append(m)
        return out

    # ---- tokens
    def member_at(self, path: str, idx: int):
        """The member whose declaration token range holds token `idx` (members
        are disjoint; fields declared together share one range)."""
        los, spans, members = self._member_index[path]
        k = bisect.bisect_right(los, idx) - 1
        if k >= 0 and spans[k][0] <= idx < spans[k][1]:
            return members[spans[k][2]]
        return None

    def code_lines_in(self, path: str, lo_line: int, hi_line: int) -> int:
        cl = self.files[path].lexinfo.code_lines
        return sum(1 for ln in range(lo_line, hi_line + 1) if ln in cl)


def roslyn_symbol(u) -> str:
    """Name Roslyn's CA1502 uses for a unit (method name, .ctor, get_X ...)."""
    m = u.member
    if u.accessor:
        prop = "Item" if m.kind == "indexer" else m.name.rsplit(".", 1)[-1]
        return f"{u.accessor}_{prop}"
    if m.kind == "ctor":
        return ".cctor" if "static" in m.modifiers else ".ctor"
    if m.kind == "dtor":
        return "Finalize"
    if m.kind == "conversion":
        return "op_Explicit" if "explicit" in m.signature.split("operator")[0] else "op_Implicit"
    if m.kind == "operator":
        return OPERATOR_NAMES.get(m.name[len("operator "):].strip(), m.name)
    return m.name.rsplit(".", 1)[-1]


OPERATOR_NAMES = {"+": "op_Addition", "-": "op_Subtraction", "*": "op_Multiply", "/": "op_Division",
                  "%": "op_Modulus", "==": "op_Equality", "!=": "op_Inequality", "<": "op_LessThan",
                  ">": "op_GreaterThan", "<=": "op_LessThanOrEqual", ">=": "op_GreaterThanOrEqual",
                  "!": "op_LogicalNot", "true": "op_True", "false": "op_False", "&": "op_BitwiseAnd",
                  "|": "op_BitwiseOr", "^": "op_ExclusiveOr", "~": "op_OnesComplement",
                  "++": "op_Increment", "--": "op_Decrement", "<<": "op_LeftShift"}
