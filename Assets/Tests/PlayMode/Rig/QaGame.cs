#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// The QA rig's view of the game: finding objects in the loaded scenes, their
/// screen points and rects (where the virtual mouse must go), the office's
/// state (loaded, today's briefing, a traveller at the desk) and the probes a
/// run asserts on every still (missing scripts, broken materials,
/// LiberationSans glyphs). Private fields are read by name, as the audits
/// did; a renamed field fails the step with its name, never silently.
/// </summary>
public static class QaGame
{
    /// <summary>The first component of type <typeparamref name="T"/> in the loaded scenes, inactive included.</summary>
    public static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

    /// <summary>A type of the game (Assembly-CSharp first, then every loaded assembly) by its name.</summary>
    public static Type TypeNamed(string name)
    {
        Type t = typeof(QaGame).Assembly.GetType(name);
        if (t != null)
            return t;
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = a.GetType(name);
            if (t != null)
                return t;
        }
        throw new ArgumentException($"No type named '{name}'.");
    }

    /// <summary>The first loaded component of the type named <paramref name="typeName"/> (inactive included).</summary>
    public static Component FindComponent(string typeName)
    {
        Type t = TypeNamed(typeName);
        Object found = Object.FindAnyObjectByType(t, FindObjectsInactive.Include);
        if (found == null)
            throw new InvalidOperationException($"No {typeName} is loaded.");
        return (Component)found;
    }

    /// <summary>The value of the instance field <paramref name="name"/> (any visibility, base classes included).</summary>
    public static object Field(object target, string name)
    {
        for (Type t = target.GetType(); t != null; t = t.BaseType)
        {
            FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
                return f.GetValue(target);
        }
        throw new MissingFieldException(target.GetType().Name, name);
    }

    /// <summary>
    /// Resolves "Type.member": a field or property of the first loaded
    /// component of that type (or of the type's static Instance, or a static
    /// member). Used by the button and call ops.
    /// </summary>
    public static object Member(string typeDotMember)
    {
        (Type type, string member) = Split(typeDotMember);
        object target = Instance(type);
        const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        for (Type t = type; t != null; t = t.BaseType)
        {
            FieldInfo f = t.GetField(member, any);
            if (f != null)
                return f.GetValue(f.IsStatic ? null : target ?? throw new InvalidOperationException($"No {type.Name} is loaded."));
            PropertyInfo p = t.GetProperty(member, any);
            if (p != null)
                return p.GetValue(p.GetMethod.IsStatic ? null : target ?? throw new InvalidOperationException($"No {type.Name} is loaded."));
        }
        throw new MissingMemberException(type.Name, member);
    }

    /// <summary>Calls the parameterless method "Type.Method" on the type's instance (or statically).</summary>
    public static void Call(string typeDotMethod)
    {
        (Type type, string member) = Split(typeDotMethod);
        MethodInfo m = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                           .FirstOrDefault(x => x.Name == member && x.GetParameters().Length == 0)
                       ?? throw new MissingMethodException(type.Name, member);
        object target = m.IsStatic ? null : Instance(type) ?? throw new InvalidOperationException($"No {type.Name} is loaded.");
        m.Invoke(target, null);
    }

    private static (Type, string) Split(string typeDotMember)
    {
        int dot = (typeDotMember ?? string.Empty).LastIndexOf('.');
        if (dot <= 0)
            throw new ArgumentException($"'{typeDotMember}' is not Type.member.");
        return (TypeNamed(typeDotMember.Substring(0, dot)), typeDotMember.Substring(dot + 1));
    }

    /// <summary>The type's static Instance property if it has one, else its first loaded object (components only).</summary>
    private static object Instance(Type type)
    {
        PropertyInfo instance = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instance != null)
            return instance.GetValue(null);
        return typeof(Object).IsAssignableFrom(type) ? Object.FindAnyObjectByType(type, FindObjectsInactive.Include) : null;
    }

    /// <summary>A loaded GameObject by its hierarchy path ("Office/Desk") or, failing that, its name (active ones first).</summary>
    public static GameObject GameObjectAt(string pathOrName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded)
                continue;
            foreach (GameObject root in s.GetRootGameObjects())
            {
                if (root.name == pathOrName)
                    return root;
                string prefix = root.name + "/";
                if (pathOrName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    Transform t = root.transform.Find(pathOrName.Substring(prefix.Length));
                    if (t != null)
                        return t.gameObject;
                }
            }
        }
        IEnumerable<Transform> all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        Transform named = all.Where(t => t.name == pathOrName).OrderByDescending(t => t.gameObject.activeInHierarchy).FirstOrDefault();
        return named != null ? named.gameObject : throw new InvalidOperationException($"No object '{pathOrName}' is loaded.");
    }

    /// <summary>The camera the office's pointer raycasts through (a PhysicsRaycaster's), else the main camera.</summary>
    public static Camera PointerCamera()
    {
        PhysicsRaycaster raycaster = Object.FindObjectsByType<PhysicsRaycaster>(FindObjectsInactive.Exclude)
                                           .FirstOrDefault(r => r.GetType() == typeof(PhysicsRaycaster) && r.isActiveAndEnabled);
        return raycaster != null ? raycaster.eventCamera : Camera.main;
    }

    /// <summary>
    /// Where on the screen <paramref name="go"/> is (pixels, origin
    /// bottom-left): a UI rect's centre through its canvas's camera, else its
    /// collider's (or renderers') centre through the pointer camera; null when
    /// behind the camera or off the screen.
    /// </summary>
    public static Vector2? ScreenPoint(GameObject go)
    {
        if (go.transform is RectTransform rt)
        {
            Canvas canvas = go.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[2]) * 0.5f);
            return OnScreen(p) ? p : (Vector2?)null;
        }
        Bounds? b = WorldBounds(go);
        Camera pointer = PointerCamera();
        if (b == null || pointer == null)
            return null;
        Vector3 s = pointer.WorldToScreenPoint(b.Value.center);
        return s.z > 0f && OnScreen(s) ? new Vector2(s.x, s.y) : (Vector2?)null;
    }

    /// <summary>The screen rect (pixels, origin bottom-left) <paramref name="go"/> covers: its UI rect, else its renderers' bounds projected; null when not in front of the camera.</summary>
    public static Rect? ScreenRect(GameObject go)
    {
        if (go == null || !go.activeInHierarchy)
            return null;
        if (go.transform is RectTransform rt)
        {
            Canvas canvas = go.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Enclose(corners.Select(c => (Vector3)RectTransformUtility.WorldToScreenPoint(cam, c)));
        }
        Bounds? b = WorldBounds(go);
        Camera pointer = PointerCamera();
        if (b == null || pointer == null)
            return null;
        Vector3 min = b.Value.min, max = b.Value.max;
        var points = new List<Vector3>();
        for (int i = 0; i < 8; i++)
        {
            Vector3 s = pointer.WorldToScreenPoint(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z));
            if (s.z <= 0f)
                return null;
            points.Add(s);
        }
        return Enclose(points);
    }

    private static Rect Enclose(IEnumerable<Vector3> points)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (Vector3 p in points)
        {
            x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y);
            x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    /// <summary>The world bounds of <paramref name="go"/>'s enabled colliders, else of its enabled renderers (text excluded); null when it has neither.</summary>
    public static Bounds? WorldBounds(GameObject go)
    {
        Bounds? b = null;
        foreach (Collider c in go.GetComponentsInChildren<Collider>(false).Where(c => c.enabled))
            b = Grow(b, c.bounds);
        if (b != null)
            return b;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && r.GetComponent<TMP_Text>() == null))
            b = Grow(b, r.bounds);
        return b;
    }

    private static Bounds Grow(Bounds? a, Bounds b)
    {
        if (a == null)
            return b;
        Bounds g = a.Value;
        g.Encapsulate(b);
        return g;
    }

    private static bool OnScreen(Vector2 p) => p.x >= 0f && p.y >= 0f && p.x <= Screen.width && p.y <= Screen.height;

    /// <summary>The topmost object the event system hits at <paramref name="screen"/> (what a click there would press), or null.</summary>
    public static GameObject TopAt(Vector2 screen)
    {
        if (EventSystem.current == null)
            return null;
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
        return hits.Count > 0 ? hits[0].gameObject : null;
    }

    /// <summary>True when the gameplay layer and the art office are both loaded and the GameManager is up.</summary>
    public static bool OfficeLoaded()
    {
        Scene g = SceneManager.GetSceneByName(OfficeScenes.GameplaySceneName);
        return g.IsValid() && g.isLoaded && OfficeScenes.ArtScene.IsValid() && Find<GameManager>() != null;
    }

    /// <summary>Today's morning paper (the briefing) when it shows, else null.</summary>
    public static GameObject Briefing()
    {
        DayFlowUIController flow = Find<DayFlowUIController>();
        return flow != null ? (GameObject)Field(flow, "briefingPanel") : null;
    }

    /// <summary>True when the briefing shows.</summary>
    public static bool BriefingShown() => Briefing() is GameObject b && b.activeInHierarchy;

    /// <summary>True when a traveller is at the desk with a case.</summary>
    public static bool TravellerAtDesk()
    {
        GameManager game = Find<GameManager>();
        InvestigationUIController ui = Find<InvestigationUIController>();
        return game != null && ui != null && Field(ui, "_currentCase") != null && (bool)Field(game, "_travellerAtDesk");
    }

    /// <summary>The run's current day, or 0 without a run.</summary>
    public static int Day() => RunManager.HasInstance && RunManager.Instance.World != null ? RunManager.Instance.World.day : 0;

    /// <summary>Waits (real time) until <paramref name="condition"/> holds; throws a TimeoutException naming <paramref name="what"/> after <paramref name="timeout"/> s.</summary>
    public static IEnumerator Until(Func<bool> condition, float timeout, string what)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end)
                throw new TimeoutException($"waited {timeout:0} s for {what}");
            yield return null;
        }
    }

    /// <summary>Waits <paramref name="n"/> frames.</summary>
    public static IEnumerator Frames(int n)
    {
        for (int i = 0; i < n; i++)
            yield return null;
    }

    /// <summary>Every loaded GameObject with a missing script, as "scene: path".</summary>
    public static List<string> MissingScripts()
    {
        var found = new List<string>();
        foreach (GameObject go in AllObjects())
            if (go.GetComponents<Component>().Any(c => c == null))
                found.Add(Where(go));
        return found;
    }

    /// <summary>Every enabled renderer drawing with no material or the error (magenta) shader, as "scene: path".</summary>
    public static List<string> BrokenMaterials()
    {
        var found = new List<string>();
        foreach (GameObject go in AllObjects())
            foreach (Renderer r in go.GetComponents<Renderer>())
                if (r.enabled && go.activeInHierarchy && !(r is ParticleSystemRenderer) && IsBroken(r))
                    found.Add(Where(go));
        return found;
    }

    /// <summary>True when <paramref name="r"/> has an empty material slot (drawn magenta) or a material on a missing, unsupported or error shader.</summary>
    public static bool IsBroken(Renderer r) =>
        r.sharedMaterials.Any(m => m == null || m.shader == null || !m.shader.isSupported || m.shader.name == "Hidden/InternalErrorShader");

    /// <summary>True when <paramref name="font"/> is a LiberationSans asset (the TMP default, or the runtime Latin fallback).</summary>
    public static bool IsLiberationSans(TMP_FontAsset font) => font != null && font.name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>
    /// Every visible text that draws at least one glyph from a LiberationSans
    /// font asset (the TMP default or the runtime fallback), as "path 'text'"
    /// (BUG-3/37: a label the culture's font could not draw).
    /// </summary>
    public static List<string> LiberationSansGlyphs()
    {
        var found = new List<string>();
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude))
        {
            if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text))
                continue;
            TMP_TextInfo info = t.textInfo;
            if (info == null)
                continue;
            for (int i = 0; i < info.characterCount && i < info.characterInfo.Length; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (c.isVisible && IsLiberationSans(c.fontAsset))
                {
                    found.Add($"{Where(t.gameObject)} '{Short(t.text)}'");
                    break;
                }
            }
        }
        return found;
    }

    private static string Short(string s) => s.Length > 40 ? s.Substring(0, 40) + "..." : s;

    private static IEnumerable<GameObject> AllObjects()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded)
                continue;
            foreach (GameObject root in s.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    yield return t.gameObject;
        }
    }

    /// <summary>"scene: root/child/..." for <paramref name="go"/>.</summary>
    public static string Where(GameObject go)
    {
        var names = new List<string>();
        for (Transform t = go.transform; t != null; t = t.parent)
            names.Add(t.name);
        names.Reverse();
        return go.scene.name + ": " + string.Join("/", names);
    }

    /// <summary>True when <paramref name="value"/> (a GameObject or a component) is active in the hierarchy (and enabled, for a behaviour).</summary>
    public static bool IsShown(object value)
    {
        switch (value)
        {
            case GameObject go:
                return go != null && go.activeInHierarchy;
            case Behaviour b:
                return b != null && b.isActiveAndEnabled;
            case Component c:
                return c != null && c.gameObject.activeInHierarchy;
            default:
                return false;
        }
    }

    /// <summary>The Button behind "Type.field" (a field holding a Button or a GameObject with one).</summary>
    public static Button ButtonAt(string typeDotField)
    {
        object v = Member(typeDotField);
        if (v is Button b)
            return b;
        if (v is Component c && c.GetComponent<Button>() is Button cb)
            return cb;
        if (v is GameObject g && g.GetComponent<Button>() is Button gb)
            return gb;
        throw new InvalidCastException($"{typeDotField} is not a Button.");
    }
}
#endif
