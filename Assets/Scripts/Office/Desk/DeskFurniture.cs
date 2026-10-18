using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// What the desk's plan adds to the art's desk at run time (the desk layout
/// spec §2; plan v2; the art scene is never edited): the two low back shelves
/// (DeskLayoutSettings.commsShelf, hallShelf: risers shelfHeight high in the
/// desk's wood, so what stands on them stands clear of the papers' plane) and
/// the intercom on the counter strip's right (DeskLayoutSettings.intercom: a
/// small console with a speaker grille and a talk button; a click asks the
/// traveller where the player is, TravellerWheel.Open, with no trip to the
/// office view; the guide's "intercom"). Built once by the office binder,
/// from boxes in the art's own materials (named, e.g. "DeskClean_Wood"; the
/// desk's when the art has none of that name) until the art has its own
/// shelves and intercom. The later machines' bays are kept empty: nothing is
/// built for them.
/// </summary>
public static class DeskFurniture
{
    /// <summary>The intercom's console (metres across, high at the back, deep), its grille and its talk button.</summary>
    private static readonly Vector3 ConsoleSize = new Vector3(0.12f, 0.045f, 0.08f), GrilleSize = new Vector3(0.07f, 0.004f, 0.04f);
    private const float ButtonDiameter = 0.024f, ButtonHeight = 0.008f;

    /// <summary>How high the intercom's click box reaches (metres): the console and its button with room for the pointer.</summary>
    private const float ClickHeight = 0.07f;

    /// <summary>
    /// Builds the shelves and the intercom under <paramref name="parent"/> on
    /// their bays of <paramref name="layout"/> in <paramref name="frame"/>,
    /// each part in <paramref name="material"/>'s pick for its art material
    /// name; the intercom's click calls <paramref name="openWheel"/>. Returns
    /// the intercom (null without a layout).
    /// </summary>
    public static Transform Build(Transform parent, DeskFrame frame, DeskLayoutSettings layout, Func<string, Material> material, UnityAction openWheel)
    {
        if (layout == null)
            return null;
        Transform old = parent.Find("DeskFurniture");
        if (old != null)
            UnityEngine.Object.Destroy(old.gameObject);
        Transform root = new GameObject("DeskFurniture").transform;
        root.SetParent(parent, false);

        Material wood = material("DeskClean_Wood");
        Shelf(root, "CommsShelf", frame, layout.commsShelf, layout.shelfHeight, wood);
        Shelf(root, "HallShelf", frame, layout.hallShelf, layout.shelfHeight, wood);
        return Intercom(root, frame, layout.intercom, material, openWheel);
    }

    /// <summary>A shelf: a riser on <paramref name="bay"/>, <paramref name="height"/> high, nothing to click.</summary>
    private static void Shelf(Transform root, string name, DeskFrame frame, DeskBay bay, float height, Material wood)
    {
        if (bay == null || height <= 0f)
            return;
        Transform shelf = Part(root, name, PrimitiveType.Cube, wood);
        shelf.SetPositionAndRotation(frame.Point(bay.at, height / 2f), frame.Facing(bay.yaw));
        shelf.localScale = new Vector3(bay.size.x, height, bay.size.y);
    }

    /// <summary>The intercom on <paramref name="bay"/>: the console, its grille and its talk button, under one click box on the Interactable layer.</summary>
    private static Transform Intercom(Transform root, DeskFrame frame, DeskBay bay, Func<string, Material> material, UnityAction openWheel)
    {
        if (bay == null)
            return null;
        Transform intercom = new GameObject("Intercom").transform;
        intercom.SetParent(root, false);
        frame.Place(intercom, bay, 0f);

        Transform console = Part(intercom, "Console", PrimitiveType.Cube, material("DeskClean_PhoneBody"));
        console.localPosition = new Vector3(0f, ConsoleSize.y / 2f, 0f);
        console.localScale = ConsoleSize;
        Transform grille = Part(intercom, "Grille", PrimitiveType.Cube, material("DeskClean_Dark"));
        grille.localPosition = new Vector3(-ConsoleSize.x * 0.15f, ConsoleSize.y + GrilleSize.y / 2f, 0f);
        grille.localScale = GrilleSize;
        Transform button = Part(intercom, "TalkButton", PrimitiveType.Cylinder, material("DeskClean_Orange"));
        button.localPosition = new Vector3(ConsoleSize.x * 0.3f, ConsoleSize.y + ButtonHeight / 2f, 0f);
        button.localScale = new Vector3(ButtonDiameter, ButtonHeight / 2f, ButtonDiameter); // a cylinder primitive is 2 high

        intercom.gameObject.layer = OfficeLayers.InteractableLayer;
        var box = intercom.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, ClickHeight / 2f, 0f);
        box.size = new Vector3(ConsoleSize.x, ClickHeight, ConsoleSize.z);
        var click = intercom.gameObject.AddComponent<Clickable>();
        click.SetOutline(intercom.GetComponentsInChildren<Renderer>(false));
        if (openWheel != null)
            click.onClick.AddListener(openWheel);
        return intercom;
    }

    /// <summary>A primitive part in <paramref name="material"/> (its own collider removed: the parent's click box, or none, takes the pointer).</summary>
    private static Transform Part(Transform parent, string name, PrimitiveType shape, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        UnityEngine.Object.Destroy(part.GetComponent<Collider>());
        if (material != null)
            part.GetComponent<Renderer>().sharedMaterial = material;
        part.transform.SetParent(parent, false);
        return part.transform;
    }
}
