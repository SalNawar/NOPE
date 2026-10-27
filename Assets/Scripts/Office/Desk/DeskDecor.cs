using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The art's small props the player may move (Saleh's 1018a playtest: "you
/// should be able to orgnize your desk by moving stuff around ... the big
/// machience and upgrades the player cant move but small decorations he can";
/// 2026-10-20: "we will cut all but decor objects later will go on clear
/// places like restory"). Each decoration of DeskLayoutSettings.decor (the
/// art's parts named with its prefixes) is gathered under one root at run
/// time (the art scene is never edited) and stands where the player last put
/// it (saved per player in PlayerPrefs, "TimeDesk.DeskDecor", outside the
/// run save), else in the decor home, clear of the others
/// (DeskArrangement.TrySettle). Dragged by its DecorHandle (the prop's own
/// click box when it has one, else a box of its own on the Interactable
/// layer), it is the desk's one hold (HoldRegistry.Desk), keeps the point it
/// was taken by under the pointer (its grab offset), lifts
/// DeskLayoutSettings.decorLift over whatever lies under it (DeskLayout's
/// obstacles) and casts a shadow where it will rest; let go anywhere free
/// off the mat (on the desk, clear of the mat, every home, the fixed art and
/// the other decorations) it rests right there, nudged
/// DeskLayoutSettings.decorNudge at most (BUG-23), else it goes back where
/// the drag began, as it does when the hold is cancelled. Each decoration is
/// an obstacle where it rests. The machines, the stamp rack and the
/// upgrades' objects have fixed homes and never move. Added and run by the
/// office binder.
/// </summary>
public sealed class DeskDecor : MonoBehaviour
{
    /// <summary>The PlayerPrefs key of the saved places (DeskArrangement.FormatPlaces).</summary>
    private const string Key = "TimeDesk.DeskDecor";

    /// <summary>The grid a let-go decoration is nudged on (metres).</summary>
    private const float NudgeStep = 0.005f;

    /// <summary>The shadow's darkness under a carried decoration.</summary>
    private static readonly Color ShadowColour = new Color(0f, 0f, 0f, 0.35f);

    /// <summary>One decoration: its id, its root (its parts' parent, at its footprint's centre on its base), its footprint, where a drag took it from and by which point.</summary>
    private sealed class Item : IHoldable
    {
        public DeskDecor Owner;
        public string Id;
        public Transform Root;
        public Vector2 Size;
        public float Base, Height;
        public Vector3 DragFrom, Grab;
        public bool Handled, Held;

        /// <summary>The hold is cancelled (Esc, focus lost, another hold): back where the drag began.</summary>
        public void CancelHold() => Owner.Cancel(this);
    }

    private readonly List<Item> _items = new List<Item>();
    private DeskFrame _frame;
    private DeskLayoutSettings _layout;
    private Transform _shadow;

    /// <summary>
    /// Gathers each decoration of <paramref name="layout"/> from the art's
    /// <paramref name="art"/> renderers (every part named "prefix__…") under
    /// a root and stands it where the player put it (still a free spot), else
    /// in the decor home. True when anything was moved (the office binder
    /// resolves its anchors again).
    /// </summary>
    public bool Arrange(DeskFrame frame, DeskLayoutSettings layout, IReadOnlyList<Renderer> art)
    {
        _frame = frame;
        _layout = layout;
        _items.Clear();
        if (layout == null || layout.decor == null || art == null)
            return false;
        DeskHome zone = layout.HomeOf(DeskHomeMap.Decor);
        Dictionary<string, (float x, float y)> saved = DeskArrangement.ParsePlaces(PlayerPrefs.GetString(PlayerPrefKeys.For(Key), string.Empty));
        bool moved = false;
        foreach (DeskDecorItem decor in layout.decor)
        {
            Item item = Gather(decor, art);
            if (item == null)
                continue;
            Vector2 at = _frame.Local(item.Root.position);
            Vector2 spot = at;
            bool placed = saved.TryGetValue(item.Id, out (float x, float y) place) && Free(item, new Vector2(place.x, place.y), out spot);
            if (!placed && zone != null)
            {
                placed = DeskArrangement.TrySettle(item.Size.x, item.Size.y, at.x, at.y, new[] { zone.Footprint }, Rests(null), layout.decorStep, out float x, out float y);
                spot = new Vector2(x, y);
            }
            if (!placed)
                spot = at; // no room anywhere: it stays where the art put it
            _items.Add(item);
            moved |= (spot - at).sqrMagnitude > 1e-8f;
            Rest(item, spot);
        }
        return moved;
    }

    /// <summary>
    /// Makes every decoration draggable once the props are bound (the office
    /// binder): a decoration holding one of <paramref name="propBoxes"/>' art
    /// (its click box placed over a part of it) takes that box along and drags
    /// by it; any other gets a box of its own over its parts.
    /// </summary>
    public void MakeHandles(IEnumerable<KeyValuePair<Transform, Collider>> propBoxes)
    {
        foreach (KeyValuePair<Transform, Collider> prop in propBoxes)
        {
            Item item = _items.Find(i => prop.Key != null && prop.Key.IsChildOf(i.Root));
            if (item == null || prop.Value == null || item.Handled)
                continue;
            prop.Value.transform.SetParent(item.Root, true);
            AddHandle(prop.Value.gameObject, item);
        }
        foreach (Item item in _items)
        {
            if (item.Handled)
                continue;
            var box = new GameObject("DecorHandle").AddComponent<BoxCollider>();
            box.gameObject.layer = OfficeLayers.InteractableLayer;
            box.transform.SetParent(item.Root, false);
            Bounds b = BoundsOf(item.Root);
            box.center = item.Root.InverseTransformPoint(b.center);
            box.size = item.Root.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z));
            AddHandle(box.gameObject, item);
        }
    }

    private void AddHandle(GameObject host, Item item)
    {
        item.Handled = true;
        host.AddComponent<DecorHandle>().Init(this, _items.IndexOf(item));
    }

    /// <summary>A drag began on decoration <paramref name="index"/> by the pointer's <paramref name="ray"/>: it becomes the desk's hold, keeps the point it was taken by under the pointer and lifts.</summary>
    internal void Lift(int index, Ray ray)
    {
        Item item = _items[index];
        item.DragFrom = item.Root.position;
        item.Grab = Hit(item, ray, out Vector3 hit) ? hit - item.Root.position : Vector3.zero;
        item.Grab.y = 0f;
        item.Held = true;
        DeskLayout.Unregister(ObstacleId(item));
        HoldRegistry.Desk.Begin(item);
        Follow(index, ray);
    }

    /// <summary>The held decoration <paramref name="index"/> follows the pointer's <paramref name="ray"/> over the desk (its grab point under it, kept on the desk), lifted over what lies under it, its shadow where it would rest.</summary>
    internal void Follow(int index, Ray ray)
    {
        Item item = _items[index];
        if (!item.Held || !Hit(item, ray, out Vector3 hit))
            return;
        Vector2 at = _frame.Local(hit - item.Grab);
        DeskRect desk = _layout.deskTop.Rect;
        (float x, float y) = new DeskRect(desk.CentreX, desk.CentreY, Mathf.Max(0f, desk.Width - item.Size.x), Mathf.Max(0f, desk.Height - item.Size.y)).Clamp(at.x, at.y);
        at = new Vector2(x, y);
        float under = DeskLayout.Obstacles.HeightAt(at.x, at.y);
        item.Root.position = _frame.Point(at) + Vector3.up * (item.Base - _frame.Origin.y + under + _layout.decorLift);
        ShowShadow(item, Free(item, at, out Vector2 spot) ? spot : (Vector2?)null);
    }

    /// <summary>Decoration <paramref name="index"/> let go: it rests where it was let go (nudged a few centimetres at most), else back where the drag began; every place is saved.</summary>
    internal void Drop(int index)
    {
        Item item = _items[index];
        if (!item.Held)
            return;
        item.Held = false;
        HoldRegistry.Desk.End(item);
        if (Free(item, _frame.Local(item.Root.position), out Vector2 spot))
            Rest(item, spot);
        else
            Back(item);
        Save();
    }

    /// <summary>The hold on <paramref name="item"/> is cancelled: back where the drag began.</summary>
    private void Cancel(Item item)
    {
        if (!item.Held)
            return;
        item.Held = false;
        Back(item);
    }

    private void Back(Item item)
    {
        item.Root.position = item.DragFrom;
        ShowShadow(item, null);
        Register(item, _frame.Local(item.DragFrom));
    }

    /// <summary><paramref name="item"/> stands at <paramref name="spot"/> on the desk, an obstacle there.</summary>
    private void Rest(Item item, Vector2 spot)
    {
        item.Root.position = _frame.Point(spot) + Vector3.up * (item.Base - _frame.Origin.y);
        ShowShadow(item, null);
        Register(item, spot);
    }

    private void Register(Item item, Vector2 at) => DeskLayout.Register(ObstacleId(item), new DeskRect(at.x, at.y, item.Size.x, item.Size.y), item.Height);

    private static string ObstacleId(Item item) => "decor:" + item.Id;

    /// <summary>The free spot nearest <paramref name="want"/> for <paramref name="item"/> (DeskArrangement.TrySettleFree within decorNudge).</summary>
    private bool Free(Item item, Vector2 want, out Vector2 spot)
    {
        bool found = DeskArrangement.TrySettleFree(item.Size.x, item.Size.y, want.x, want.y, _layout.deskTop.Rect, Blocked(item), _layout.decorNudge, NudgeStep,
                                                   out float x, out float y);
        spot = new Vector2(x, y);
        return found;
    }

    /// <summary>What a decoration may not rest on: the mat, every home but the decor home, the fixed art and the other decorations.</summary>
    private List<DeskRect> Blocked(Item item)
    {
        var blocked = new List<DeskRect> { DeskLayout.Bound ? DeskLayout.Mat.Rect : _layout.mat.Rect };
        foreach (DeskHome home in _layout.homes ?? System.Array.Empty<DeskHome>())
            if (home != null && home.show != HomeShow.Decor)
                blocked.Add(home.Footprint);
        foreach (DeskHome art in _layout.fixedArt ?? System.Array.Empty<DeskHome>())
            if (art != null)
                blocked.Add(art.Footprint);
        blocked.AddRange(Rests(item));
        return blocked;
    }

    /// <summary>The other decorations' footprints where they stand now (all of them with no <paramref name="except"/>; a held one where its drag began).</summary>
    private List<DeskRect> Rests(Item except)
    {
        var rests = new List<DeskRect>();
        foreach (Item other in _items)
            if (other != except)
            {
                Vector2 o = _frame.Local(other.Held ? other.DragFrom : other.Root.position);
                rests.Add(new DeskRect(o.x, o.y, other.Size.x, other.Size.y));
            }
        return rests;
    }

    /// <summary>Where <paramref name="ray"/> meets the desk's top under <paramref name="item"/>.</summary>
    private static bool Hit(Item item, Ray ray, out Vector3 hit)
    {
        var desk = new Plane(Vector3.up, new Vector3(0f, item.Base, 0f));
        hit = default;
        if (!desk.Raycast(ray, out float d))
            return false;
        hit = ray.GetPoint(d);
        return true;
    }

    /// <summary>The carried decoration's shadow on the desk at <paramref name="spot"/> (where it would rest), or none.</summary>
    private void ShowShadow(Item item, Vector2? spot)
    {
        if (spot == null)
        {
            if (_shadow != null)
                _shadow.gameObject.SetActive(false);
            return;
        }
        if (_shadow == null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "DecorShadow";
            Destroy(quad.GetComponent<Collider>());
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                quad.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = ShadowColour };
            _shadow = quad.transform;
            _shadow.SetParent(transform, false);
        }
        _shadow.gameObject.SetActive(true);
        _shadow.SetPositionAndRotation(_frame.Point(spot.Value) + Vector3.up * (item.Base - _frame.Origin.y + 0.001f),
                                       _frame.Facing(0f) * Quaternion.Euler(90f, 0f, 0f));
        _shadow.localScale = new Vector3(item.Size.x, item.Size.y, 1f);
    }

    /// <summary>Every decoration's place, saved per player.</summary>
    private void Save()
    {
        var places = new List<KeyValuePair<string, (float x, float y)>>();
        foreach (Item item in _items)
        {
            Vector2 at = _frame.Local(item.Root.position);
            places.Add(new KeyValuePair<string, (float x, float y)>(item.Id, (at.x, at.y)));
        }
        PlayerPrefs.SetString(PlayerPrefKeys.For(Key), DeskArrangement.FormatPlaces(places));
        PlayerPrefs.Save();
    }

    /// <summary>The art's parts of <paramref name="decor"/> gathered under a new root at their footprint's centre on their base, facing the chair; null when the art has none.</summary>
    private Item Gather(DeskDecorItem decor, IReadOnlyList<Renderer> art)
    {
        var parts = new List<Transform>();
        foreach (Renderer r in art)
        {
            if (r == null)
                continue;
            foreach (string prefix in decor.parts ?? System.Array.Empty<string>())
                if (!string.IsNullOrEmpty(prefix) && r.name.StartsWith(prefix + "__", System.StringComparison.Ordinal) && !parts.Contains(r.transform))
                    parts.Add(r.transform);
        }
        if (parts.Count == 0)
            return null;
        Bounds b = default;
        bool any = false;
        foreach (Transform part in parts)
            foreach (Renderer r in part.GetComponents<Renderer>())
            {
                if (!any)
                    b = r.bounds;
                else
                    b.Encapsulate(r.bounds);
                any = true;
            }
        DeskRect footprint = _frame.Footprint(b);
        Transform root = new GameObject("Decor_" + decor.id).transform;
        root.SetParent(parts[0].parent, false);
        root.SetPositionAndRotation(_frame.Point(new Vector2(footprint.CentreX, footprint.CentreY)) + Vector3.up * (b.min.y - _frame.Origin.y), _frame.Facing(0f));
        foreach (Transform part in parts)
            if (!parts.Exists(p => p != part && part.IsChildOf(p)))
                part.SetParent(root, true);
        return new Item { Owner = this, Id = decor.id, Root = root, Size = new Vector2(footprint.Width, footprint.Height), Base = b.min.y, Height = b.size.y };
    }

    private static Bounds BoundsOf(Transform root)
    {
        Renderer[] parts = root.GetComponentsInChildren<Renderer>(false);
        if (parts.Length == 0)
            return new Bounds(root.position, Vector3.one * 0.05f);
        Bounds b = parts[0].bounds;
        foreach (Renderer r in parts)
            b.Encapsulate(r.bounds);
        return b;
    }
}
