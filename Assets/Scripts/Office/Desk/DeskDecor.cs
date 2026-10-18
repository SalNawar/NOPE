using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The art's small props the player may move (Saleh's 1018a playtest: "you
/// should be able to orgnize your desk by moving stuff around ... the big
/// machience and upgrades the player cant move but small decorations he can";
/// the desk layout spec §3; "use existing for now"). Each decoration of
/// DeskLayoutSettings.decor (the art's parts named with its prefixes) is
/// gathered under one root at run time (the art scene is never edited) and
/// stands in one of the decor zones, clear of the other decorations: where
/// the player last put it (saved per player in PlayerPrefs,
/// "TimeDesk.DeskDecor", outside the run save), else as near as it can to
/// the art's place inside its own zone (DeskArrangement.TrySettle). Dragged
/// (its DecorHandle: the prop's own click box when it has one, so its click
/// reaction still plays, else a box of its own on the Interactable layer),
/// it lifts DeskLayoutSettings.decorLift off the desk and follows the pointer
/// over it; let go, it settles on the nearest free spot of a zone, on a
/// DeskLayoutSettings.decorStep grid, or goes back where it was when no zone
/// has room. The machines, the stamp rack and the upgrades' objects have
/// fixed bays and never move. Added and run by the office binder.
/// </summary>
public sealed class DeskDecor : MonoBehaviour
{
    /// <summary>The PlayerPrefs key of the saved places (DeskArrangement.FormatPlaces).</summary>
    private const string Key = "TimeDesk.DeskDecor";

    /// <summary>One decoration: its id, its root (its parts' parent, at its footprint's centre on its base), its footprint and its zone.</summary>
    private sealed class Item
    {
        public string Id;
        public Transform Root;
        public Vector2 Size;
        public float Base;
        public Vector3 DragFrom;
        public bool Handled;
    }

    private readonly List<Item> _items = new List<Item>();
    private DeskFrame _frame;
    private DeskLayoutSettings _layout;
    private DeskRect[] _zones = System.Array.Empty<DeskRect>();

    /// <summary>
    /// Gathers each decoration of <paramref name="layout"/> from the art's
    /// <paramref name="art"/> renderers (every part named "prefix__…") under
    /// a root and stands it in a zone (where the player put it, else nearest
    /// the art's place in its own zone). True when anything was moved (the
    /// office binder resolves its anchors again).
    /// </summary>
    public bool Arrange(DeskFrame frame, DeskLayoutSettings layout, IReadOnlyList<Renderer> art)
    {
        _frame = frame;
        _layout = layout;
        _items.Clear();
        if (layout == null || layout.decor == null || art == null)
            return false;
        _zones = new DeskRect[layout.decorZones.Length];
        for (int i = 0; i < _zones.Length; i++)
            _zones[i] = layout.decorZones[i].Rect;
        Dictionary<string, (float x, float y)> saved = DeskArrangement.ParsePlaces(PlayerPrefs.GetString(PlayerPrefKeys.For(Key), string.Empty));
        var taken = new List<DeskRect>();
        bool moved = false;
        foreach (DeskDecorItem decor in layout.decor)
        {
            Item item = Gather(decor, art);
            if (item == null)
                continue;
            _items.Add(item);
            Vector2 at = _frame.Local(item.Root.position);
            bool known = saved.TryGetValue(item.Id, out (float x, float y) place);
            Vector2 want = known ? new Vector2(place.x, place.y) : at;
            DeskRect[] zones = known || decor.zone < 0 || decor.zone >= _zones.Length ? _zones : new[] { _zones[decor.zone] };
            if (!Settle(item, want, zones, taken, out Vector2 spot) && !Settle(item, want, _zones, taken, out spot))
                spot = at; // no zone has room: it stays where the art put it
            taken.Add(new DeskRect(spot.x, spot.y, item.Size.x, item.Size.y));
            moved |= (spot - at).sqrMagnitude > 1e-8f;
            item.Root.position = _frame.Point(spot) + Vector3.up * (item.Base - _frame.Origin.y);
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

    /// <summary>A drag began on decoration <paramref name="index"/>: it lifts where it is.</summary>
    internal void Lift(int index)
    {
        Item item = _items[index];
        item.DragFrom = item.Root.position;
        item.Root.position += Vector3.up * _layout.decorLift;
    }

    /// <summary>Decoration <paramref name="index"/> follows the pointer's <paramref name="ray"/> over the desk, its centre under it, lifted.</summary>
    internal void Follow(int index, Ray ray)
    {
        Item item = _items[index];
        var desk = new Plane(Vector3.up, new Vector3(0f, item.Base, 0f));
        if (desk.Raycast(ray, out float d))
            item.Root.position = ray.GetPoint(d) + Vector3.up * _layout.decorLift;
    }

    /// <summary>Decoration <paramref name="index"/> let go: it settles on the nearest free spot of a zone (else back where the drag began) and every place is saved.</summary>
    internal void Drop(int index)
    {
        Item item = _items[index];
        var taken = new List<DeskRect>();
        foreach (Item other in _items)
            if (other != item)
            {
                Vector2 o = _frame.Local(other.Root.position);
                taken.Add(new DeskRect(o.x, o.y, other.Size.x, other.Size.y));
            }
        item.Root.position = Settle(item, _frame.Local(item.Root.position), _zones, taken, out Vector2 spot)
            ? _frame.Point(spot) + Vector3.up * (item.Base - _frame.Origin.y)
            : item.DragFrom;
        Save();
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

    private bool Settle(Item item, Vector2 want, IReadOnlyList<DeskRect> zones, IReadOnlyList<DeskRect> taken, out Vector2 spot)
    {
        bool found = DeskArrangement.TrySettle(item.Size.x, item.Size.y, want.x, want.y, zones, taken, _layout.decorStep, out float x, out float y);
        spot = new Vector2(x, y);
        return found;
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
        // The footprint in the desk's frame (a world box seen along the frame's axes).
        float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector2 c = _frame.Local(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, b.min.y, (i & 2) == 0 ? b.min.z : b.max.z));
            x0 = Mathf.Min(x0, c.x); x1 = Mathf.Max(x1, c.x); y0 = Mathf.Min(y0, c.y); y1 = Mathf.Max(y1, c.y);
        }
        Transform root = new GameObject("Decor_" + decor.id).transform;
        root.SetParent(parts[0].parent, false);
        root.SetPositionAndRotation(_frame.Point(new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f)) + Vector3.up * (b.min.y - _frame.Origin.y), _frame.Facing(0f));
        foreach (Transform part in parts)
            if (!parts.Exists(p => p != part && part.IsChildOf(p)))
                part.SetParent(root, true);
        return new Item { Id = decor.id, Root = root, Size = new Vector2(x1 - x0, y1 - y0), Base = b.min.y };
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
