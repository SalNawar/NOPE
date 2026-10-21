using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// What the desk home map adds to the art's desk at run time (Saleh,
/// 2026-10-20, DESK APPROVED: "The traveller is behind glass with a DEAL
/// TRAY that carries papers in and out over the counter"; the art scene is
/// never edited): the deal tray on its home right above the mat (a brass
/// plate with its dark slot, in the art's own materials, until the desk
/// set's model comes: an art request, DESK-PLAN.md) and, only when the art
/// office has no phone to serve as the intercom (the Intercom anchor), a
/// stand-in intercom on the intercom's home left of the mat (a small console
/// with a speaker grille and a talk button; a click asks the traveller where
/// the player is, TravellerWheel.Open; the guide's "intercom"). No shelves
/// and no risers: the homes are on the desk top, and a home whose object has
/// not come shows nothing (DESK-HOMES designs those). Built once by the
/// office binder.
/// </summary>
public static class DeskFurniture
{
    /// <summary>The stand-in intercom's console (metres across, high at the back, deep), its grille and its talk button.</summary>
    private static readonly Vector3 ConsoleSize = new Vector3(0.12f, 0.045f, 0.08f), GrilleSize = new Vector3(0.07f, 0.004f, 0.04f);
    private const float ButtonDiameter = 0.024f, ButtonHeight = 0.008f;

    /// <summary>How high the stand-in intercom's click box reaches (metres): the console and its button with room for the pointer.</summary>
    private const float ClickHeight = 0.07f;

    /// <summary>The deal tray plate's thickness and its slot's share of the plate's depth (the slot runs across, toward the traveller).</summary>
    private const float TrayThickness = 0.004f, SlotShare = 0.45f;

    /// <summary>
    /// Builds the deal tray and, when <paramref name="artIntercom"/> is false,
    /// the stand-in intercom under <paramref name="parent"/> on their homes of
    /// <paramref name="layout"/> in <paramref name="frame"/>, each part in
    /// <paramref name="material"/>'s pick for its art material name; the
    /// stand-in's click calls <paramref name="openWheel"/>. Returns the
    /// stand-in intercom (null when the art's serves, or without a layout).
    /// </summary>
    public static Transform Build(Transform parent, DeskFrame frame, DeskLayoutSettings layout, Func<string, Material> material, UnityAction openWheel, bool artIntercom)
    {
        if (layout == null)
            return null;
        Transform old = parent.Find("DeskFurniture");
        if (old != null)
            UnityEngine.Object.Destroy(old.gameObject);
        Transform root = new GameObject("DeskFurniture").transform;
        root.SetParent(parent, false);

        DealTray(root, frame, layout.HomeOf(DeskHomeMap.DealTray), material);
        return artIntercom ? null : Intercom(root, frame, layout.Bay(DeskHomeMap.Intercom), material, openWheel);
    }

    /// <summary>The deal tray on <paramref name="home"/>: a brass plate flush on the desk with a dark slot along its traveller's side, nothing to click.</summary>
    private static void DealTray(Transform root, DeskFrame frame, DeskHome home, Func<string, Material> material)
    {
        if (home == null)
            return;
        Transform tray = new GameObject("DealTray").transform;
        tray.SetParent(root, false);
        frame.Place(tray, new DeskBay(home), 0f);
        Transform plate = Part(tray, "Plate", PrimitiveType.Cube, material("DeskClean_Brass"));
        plate.localPosition = new Vector3(0f, TrayThickness / 2f, 0f);
        plate.localScale = new Vector3(home.width, TrayThickness, home.depth);
        Transform slot = Part(tray, "Slot", PrimitiveType.Cube, material("DeskClean_Dark"));
        slot.localPosition = new Vector3(0f, TrayThickness + 0.0005f, home.depth * (1f - SlotShare) / 2f);
        slot.localScale = new Vector3(home.width * 0.9f, 0.001f, home.depth * SlotShare);
        DeskLayout.Track(DeskHomeMap.DealTray, tray);
    }

    /// <summary>The stand-in intercom on <paramref name="bay"/>: the console, its grille and its talk button, under one click box on the Interactable layer.</summary>
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
        DeskLayout.Track(DeskHomeMap.Intercom, intercom);
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
