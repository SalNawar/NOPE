"""
Time Sorter (NOPE) - the eight upgrade desk props (track MC): supply crate, Auto-Feed tray, analysis beacon,
Interview Protocols card ring, BABEL translator rack, Translation Lens kit, Rolodex, RING SERVICE panel.

Run (background only; never open Blender's window, Saleh uses this PC):
    "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" -b --python upgrade_props_model.py -- <render_dir> [prop ...]
<prop> limits the run to some of: crate feeder beacon ring babel lens rolodex panel (default: all).
Re-runnable: each prop starts from an empty scene (its own reset) and overwrites its outputs.

Writes, per prop: Assets/Art/Office/Gameplay/Machines/<Name>.fbx (textures beside it, drawn by
machine_textures.py if missing; a .meta for every new FBX/PNG there), <Name>.blend beside <render_dir>,
<key>_tt00..03.png (turntable: front, viewer's right side, back, viewer's left side), <key>_use.png (in use)
and <key>_sheet.png (contact sheet, Pillow) in <render_dir>; verify_props.json (every FBX re-imported:
names, parents, pivots, bounds against the scene that exported it) beside <render_dir>.

Axes. Modelled in Blender metres, Z up, the prop's front (toward the chair and camera) = Blender +Y, the
viewer's left = Blender +X. Exported with machine_kit.export_fbx, Unity gets unity = (-x, z, -y): front =
local -Z, up = +Y, the viewer's right = +X. Rotations carry over as: Blender about X by a = Unity about X by a;
Blender about Y by a = Unity about Z by a; Blender about Z by a = Unity about Y by -a. Every moving part is
its own object with its origin on its pivot; rest rotations are zero except where noted (cards).

The contracts (Unity terms; "->" = child of):

SupplyCrate.fbx (0.24 x 0.13 x 0.18; origin bottom centre)
    Crate         the open box (floor + walls), root
    Flap_Left     -> Crate. The viewer's-left top flap (Unity -X side), pivot on its hinge (the left wall's top
                  edge), hinge along Unity Z; opens with local Z rotation + (0 = closed flat, +110 = open, +180 = flat out)
    Flap_Right    -> Crate. Mirror: pivot on the right wall's top edge, opens with local Z rotation -
    Flap_Front    -> Crate. Pivot on the front wall's top edge (hinge along X), opens with local X rotation -
    Flap_Back     -> Crate. Pivot on the back wall's top edge, opens with local X rotation +
                  (front/back lie 1 mm under left/right)
    Tape          -> Crate. Red tape (Crate_Tape) over the seam and 3 cm down front and back; pivot at its front
                  (bottom) end on the front face: peel by rotating it about local X (negative lifts it off the front face)
    Label         -> Crate. Shipping label (Crate_Label) on the front face
    ItemPoint     -> Crate. Empty: inside, centre, 4 cm up

ScannerFeeder.fbx (scanner coordinates: parent it to the scanner with zero offset; it sits on the viewer's
left of the scanner, Unity x -0.201..-0.36)
    Feeder        cream housing + slate input tray (rises outward to Unity x -0.36, y 0.115) + cheeks, root at (0,0,0)
    Rollers       -> Feeder. Two rubber rollers on one shaft; pivot on the shaft, spin about local Z (Unity)
    GuideFront    -> Feeder. Brass paper guide on the tray (front one), pivot its centre; slides along Unity Z
    GuideBack     -> Feeder. The back guide; same
    FeedLED       -> Feeder. Green lamp on the housing's front face (own material Feed_LED)
    FeederPlate   -> Feeder. Feeder_Plate decal on the housing's front face
    FeedPoint     -> Feeder. Empty at the rollers' nip

AnalysisBeacon.fbx (scanner coordinates; on a bracket on the viewer's right side of the scanner, near the back)
    Beacon        L-bracket on the side wall + slate base cylinder (top at y 0.064), root at (0,0,0)
    BeaconPod     -> Beacon. Pivot at the base top centre; pops by +0.03 along local Y
    Dome          -> BeaconPod. Amber, semi-transparent
    Reflector     -> BeaconPod. Chrome half-cylinder mirror, pivot on the vertical axis; spins about local Y
    BeaconLamp    -> BeaconPod. Emissive bulb (Beacon_Lamp, off at rest)
    BeaconPlate   -> Beacon. Beacon_Plate decal on the bracket's front apron

ProtocolRing.fbx (origin bottom centre of the base disc)
    ProtocolStand slate base disc (r 0.035) + brass post (0.13) + hook arm reaching forward (-Z Unity), root
    Ring          -> ProtocolStand. Brass ring (r 0.012), pivot on the hook point (its top); may sway about X
    Card_0..3     -> Ring. Laminated cards 0.085 x 0.055 x 0.0015, pivot = the card's hole on the ring.
                  A card flips by rotating about local X (+ swings its bottom forward and up, over the ring).
                  Rest local X rotations (fan): Card_0 +9, Card_1 +3, Card_2 -3, Card_3 -9 degrees; Card_0 frontmost.
                  Each card's face frame (local, before the rest rotation): face centre (0, -0.0217, -0.0008),
                  right = +X, up = +Y, outward normal = -Z (toward the camera); face 0.085 wide x 0.055 tall;
                  Protocol_Card UVs 0..1 (header band in the top 26%, the hole 5.8 mm below the top edge).
                  The back (+Z local) is plain card colour.

BabelRack.fbx (origin bottom centre)
    BabelRack     slate wedge 0.22 x 0.13 (0.085 back / 0.05 front), ink top deck, 4 dark bays, front grille
                  (merged), VU bezel and needle hub, root
    VUFace        -> BabelRack. Babel_VU quad on the front face
    VUGlass       -> BabelRack. The small glass over it (transparent)
    Needle        -> BabelRack. Red, pivot at its base (the dial's arc centre); swings about local Z:
                  +50 = viewer's left (low end), -50 = viewer's right (red zone), 0 = straight up
    BabelPlate    -> BabelRack. Babel_Plate decal on the front face
    Bay_0..3      -> BabelRack. Empties at the bay mouths (left to right as the viewer sees them: 0 = viewer's left)
    Cartridge_0..3 -> BabelRack. 0.045 x 0.065 x 0.014 boxes, pivot at the bottom centre, seated two-thirds in
                  their bay (bottom = Bay_n - 0.0433 in Y); slide along local -Y into the bay.
                  0 Near East (brass), 1 Mediterranean (slate), 2 East Asia (signal), 3 Northern Europe (lavender);
                  label decal on the front face
    CartridgeLED_0..3 -> Cartridge_n. Lamp on top, own material each (Babel_LED0..3, off at rest)

LensKit.fbx (origin bottom centre of the base)
    LensStand     oxblood base 0.11 x 0.07 + brass post 0.09 (back, viewer's left), root
    Loupe         -> LensStand. Word level: brass ring (r 0.022) on an arm; pivot = the swivel at the post top;
                  spins about local Y, tilts about local X
    LoupeGlass    -> Loupe. Pale cyan, semi-transparent (both sides)
    BarMagnifier  -> LensStand. Sentence level: 0.12 x 0.02 bar lens on two feet along the base's front edge;
                  pivot its bottom centre
    BarLine       -> BarMagnifier. Thin red emissive line along its top
    FresnelArm    -> LensStand. Object level: empty at the clamp on the post
    ArmBase       -> FresnelArm. The clamp collar and hinge knuckle
    ArmUpper      -> FresnelArm. Hinge bar + frame; pivot on the hinge (behind the post); unfolds with local X
                  rotation 0 (stowed upright) to -100 (folded forward over the desk, sheet facing down)
    FresnelSheet  -> ArmUpper. 0.15 x 0.11 semi-transparent sheet (LensKit_Fresnel), pivot its centre

Rolodex.fbx (origin bottom centre)
    Rolodex       oxblood base, two side wheels (r 0.05), brass axle (along X, 0.06 up), root
    Knob          -> Rolodex. Brass knob on the viewer's right wheel, pivot on the axle; turns about local X
    Drum          -> Rolodex. Pivot on the axle centre; rotates about local X
    Card_00..23   -> Drum. Index cards 0.076 x 0.048 standing radially, pivot on the axle, rest local X rotation
                  15 * k degrees (k = card number; + tips a card's top backward, away from the viewer).
                  Card_00 stands straight up facing the viewer and wears Rolodex_GoldCard; the rest Rolodex_Card

RingPanel.fbx (origin bottom centre)
    RingPanel     slate console 0.26 x 0.10 (0.07 back, 0.03 front) with a sloped face and the face bezel, root
    PanelFace     -> RingPanel. Relay_Face quad on the slope (modules 01..05 from the viewer's left)
    Lamp_1..5     -> RingPanel. Pilot lamps, pivot their centre, own material each (Panel_Lamp1..5, off at rest)
    Breaker_1..5  -> RingPanel. Toggle levers, pivot at the base, rotate about local X +/-25 (rest 0 = straight
                  out of the face)
    Sticker_1..5  -> RingPanel. Relay_Serviced decals (the game hides them until the ring is repaired)
"""
import json
import math
import os
import shutil
import subprocess
import sys
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import machine_kit as K  # noqa: E402
import props_kit as P  # noqa: E402

import bpy  # noqa: E402
from mathutils import Quaternion, Vector  # noqa: E402

REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(REPO, "Assets", "Art", "Office", "Gameplay", "Machines")
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
RENDERS = os.path.abspath(argv[0]) if argv else os.path.join(HERE, "renders")
ONLY = set(argv[1:])
os.makedirs(RENDERS, exist_ok=True)
WORK = os.path.dirname(RENDERS)   # .blend files and the re-import report go beside the renders, outside the repo

KRAFT, CREAM = "#C4A070", "#DCD0B4"           # the brief's two extra colours
SLATE, OXBLOOD, SIGNAL, BRASS = K.KIT["slate"], K.KIT["oxblood"], K.KIT["signal"], K.KIT["brass"]
MANILA, BONE, LAVENDER, INK, CARD = K.KIT["manila"], K.KIT["bone"], K.KIT["lavender"], K.KIT["ink"], K.KIT["card"]
CHROME = "#C9CCD4"                            # as the torch's chrome
GLASS = "#BFE6EA"                             # the lens kit's pale cyan (the brief's)
LED_GREEN, LED_AMBER, LED_RED = "#3CFF86", "#FFB23C", "#FF3A2A"

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
FRONT = (-X, Z, Y)        # a decal on a +Y face, read from the front (U = the viewer's right)
TOP = (-X, -Y, Z)         # a decal on a top face, read from the front

TEXTURES = ["Crate_Label.png", "Crate_Tape.png", "Feeder_Plate.png", "Beacon_Plate.png", "Protocol_Card.png",
            "Babel_Plate.png", "Babel_VU.png", "Cartridge_NearEast.png", "Cartridge_Mediterranean.png",
            "Cartridge_EastAsia.png", "Cartridge_NorthEurope.png", "Rolodex_Card.png", "Rolodex_GoldCard.png",
            "Relay_Face.png", "Relay_Serviced.png", "LensKit_Fresnel.png"]


def tex(name):
    return os.path.join(OUT, name)


def ensure_textures():
    missing = [n for n in TEXTURES if not os.path.exists(tex(n))]
    if not missing:
        return
    code = ("import sys, os; sys.path.insert(0, {h!r}); import machine_textures as T\n"
            "for n in {m!r}: T.TEXTURES[n](os.path.join(T.OUT, n)); print('texture', n)").format(h=HERE, m=missing)
    for exe in ("python", "py"):
        try:
            subprocess.run([exe, "-c", code], check=True, timeout=180)
            return
        except Exception as e:  # noqa: BLE001
            print("texture helper failed with", exe, e)


ensure_textures()

SC = COLL = STAGE = None


def begin(name):
    global SC, COLL, STAGE
    K.MATS.clear()
    K.MAT_INFO.clear()
    SC, COLL, STAGE = K.reset_scene(name)


def glow(ob, strength):
    """Set the emission of a lamp object's (emissive) material in the toon renders."""
    for m in ob.data.materials:
        em = next((n for n in m.node_tree.nodes if n.type == "EMISSION"), None)
        if em is not None:
            em.inputs["Strength"].default_value = strength


def light_up(matname, emit, strength):
    """Give a material an emissive look for the in-use shot (before toonify)."""
    m = K.MATS[matname]
    m["cel"]["emit"] = emit
    m["cel"]["emit_strength"] = strength


def stage_mats(*specs):
    for s in specs:
        K.material(*s)
    K.MAT_INFO[:] = [m for m in K.MAT_INFO if not m["name"].startswith("Stage_")]


def scanner_stand_in():
    """The cream scanner (body, lid) as a stand-in for the feeder / beacon in-use shots (not exported)."""
    stage_mats(("Stage_ScannerCream", CREAM, 0.5), ("Stage_ScannerLid", "#BDB29A", 0.5), ("Stage_ScannerGlass", "#2B3A40", 0.3))
    sc = K.Part("StageScanner")
    sc.prism_yz([(-0.160, 0.004), (0.160, 0.004), (0.160, 0.016), (0.136, 0.060), (-0.160, 0.060)], -0.200, 0.200, "Stage_ScannerCream")
    sc.box((0, -0.010, 0.070), (0.386, 0.300, 0.020), "Stage_ScannerLid")
    return sc.build(COLL, bevel=0.004, segments=2)


def snapshot(objs):
    bpy.context.view_layer.update()
    snap = {}
    for o in objs:
        if o.type == "MESH":
            lo, hi = K.world_bounds([o])
        else:
            lo = hi = o.matrix_world.translation.copy()
        snap[o.name] = dict(type=o.type, parent=o.parent.name if o.parent else None,
                            loc=list(o.matrix_world.translation), lo=list(lo), hi=list(hi),
                            rot_local_deg=[round(math.degrees(a), 3) for a in o.rotation_euler],
                            rot_world=list(o.matrix_world.to_quaternion()))
    return snap


def uv_ranges(ob):
    """Per textured material, the UV box of its faces (a decal should read 0..1)."""
    me = ob.data
    out = {}
    if not me.uv_layers:
        return out
    uvl = me.uv_layers.active.data
    for p in me.polygons:
        m = me.materials[p.material_index] if p.material_index < len(me.materials) else None
        if m is None or not m.get("cel") or not m["cel"].get("tex"):
            continue
        us = [uvl[i].uv[0] for i in p.loop_indices]
        vs = [uvl[i].uv[1] for i in p.loop_indices]
        r = out.setdefault(m.name, [9, 9, -9, -9])
        r[0], r[1], r[2], r[3] = min(r[0], *us), min(r[1], *vs), max(r[2], *us), max(r[3], *vs)
    return {k: [round(a, 3) for a in v] for k, v in out.items()}


RESULTS = {}


def fbx_models(path):
    """Every Model's local transform as written in the FBX, and as Unity reads it (it mirrors X:
    position (-x, y, z), Euler (x, -y, -z))."""
    import addon_utils
    import importlib
    mod = next(m for m in addon_utils.modules() if m.__name__.endswith("io_scene_fbx"))
    pf = importlib.import_module(mod.__name__ + ".parse_fbx")
    root, _ = pf.parse(path)
    out = {}
    for e in root.elems:
        if e.id.decode() != "Objects":
            continue
        for m in e.elems:
            if m.id.decode() != "Model":
                continue
            nm = m.props[1].split(b"\x00")[0].decode()
            props = {"Lcl Translation": [0.0, 0.0, 0.0], "Lcl Rotation": [0.0, 0.0, 0.0]}
            for p70 in m.elems:
                if p70.id.decode() == "Properties70":
                    for p in p70.elems:
                        k = p.props[0].decode()
                        if k in ("Lcl Translation", "Lcl Rotation", "Lcl Scaling"):
                            props[k] = [round(float(x), 5) for x in p.props[4:]]
            t, r = props["Lcl Translation"], props["Lcl Rotation"]
            out[nm] = dict(fbx=props, unity_local_pos=[round(-t[0], 5), t[1], t[2]],
                           unity_local_euler=[r[0], round(-r[1], 4) + 0.0, round(-r[2], 4) + 0.0])
    return out


def finish(key, fbx_name, root, objs, contract, use_shot, elev=26):
    """Unwrap, export, render (4-frame turntable + the in-use shot), save the .blend, re-import and check."""
    meshes = [o for o in objs if o.type == "MESH"]
    bpy.context.view_layer.update()
    pre = snapshot(objs)
    uvs = {o.name: uv_ranges(o) for o in meshes if uv_ranges(o)}
    tri = K.tris(meshes)
    unity = {o.name: dict(parent=o.parent.name if o.parent else None,
                          pivot_unity=[round(a, 4) for a in K.to_unity(o.matrix_world.translation)])
             for o in objs}
    lo, hi = K.world_bounds(meshes)
    report = dict(fbx=fbx_name, tris=tri, tris_total=sum(tri.values()),
                  size_blender_xyz=[round(a, 4) for a in (hi - lo)],
                  bounds_unity=[[round(a, 4) for a in K.to_unity(lo)], [round(a, 4) for a in K.to_unity(hi)]],
                  objects_unity=unity, decal_uvs=uvs, materials=list(K.MAT_INFO))
    os.makedirs(OUT, exist_ok=True)
    fbx = os.path.join(OUT, fbx_name)
    P.export_fbx_nested(objs, fbx, root)   # K.export_fbx + a fix for grandchildren (see props_kit)

    K.toonify()
    cam, cam_d = K.setup_stage(SC, COLL, STAGE)
    tgt = (lo + hi) / 2
    rad = (hi - lo).length / 2
    dist = 3.3 * rad + 0.05
    frames = K.turntable(SC, cam, cam_d, RENDERS, key, tuple(tgt), dist, elev, 50, steps=4)
    use_path = os.path.join(RENDERS, f"{key}_use.png")
    use_shot(cam, cam_d, use_path, tgt, dist)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, fbx_name.replace(".fbx", ".blend")))

    # ------------------------------------------------ verify: re-import the FBX into an empty scene
    bpy.ops.wm.read_factory_settings(use_empty=True)
    K.MATS.clear()
    bpy.ops.import_scene.fbx(filepath=fbx, axis_forward="-Z", axis_up="Y")
    bpy.context.view_layer.update()
    got = {o.name: o for o in bpy.context.scene.objects}
    post = snapshot(list(got.values()))
    problems = []
    for name, par in contract.items():
        if name not in got:
            problems.append(f"missing {name}")
            continue
        p = got[name].parent.name if got[name].parent else None
        if p != par:
            problems.append(f"{name}: parent {p}, expected {par}")
    extra = sorted(set(got) - set(contract))
    dev = 0.0
    root_name = next(n for n, par in contract.items() if par is None)
    rq0 = Quaternion(pre[root_name]["rot_world"]) if root_name in pre else Quaternion()
    rq1 = Quaternion(post[root_name]["rot_world"]) if root_name in post else Quaternion()
    for name, s in pre.items():
        if name not in post:
            continue
        for k in ("loc", "lo", "hi"):
            dev = max(dev, max(abs(a - b) for a, b in zip(s[k], post[name][k])))
        q0 = rq0.inverted() @ Quaternion(s["rot_world"])          # rotation relative to the root (the importer
        q1 = rq1.inverted() @ Quaternion(post[name]["rot_world"])  # puts its axis conversion on the root)
        rdev = math.degrees(q0.rotation_difference(q1).angle)
        rdev = min(rdev, 360.0 - rdev)                              # q and -q are the same rotation
        if rdev > 0.05:
            problems.append(f"{name}: world rotation differs by {rdev:.3f} deg on re-import")
    if dev > 2e-4:
        problems.append(f"geometry moved by {dev:.5f} m on re-import")
    imported = {n: dict(type=o.type, parent=o.parent.name if o.parent else None,
                        mats=[m.name for m in o.data.materials] if o.type == "MESH" else [],
                        uv_layers=len(o.data.uv_layers) if o.type == "MESH" else None)
                for n, o in got.items()}
    lcl = fbx_models(fbx)
    report.update(fbx_local_unity=lcl, frames=frames, use=use_path, reimport=dict(ok=not problems, problems=problems, extra=extra,
                                                              max_deviation_m=round(dev, 6), objects=imported))
    RESULTS[key] = report
    fbm = os.path.join(OUT, fbx_name.replace(".fbx", ".fbm"))
    if os.path.isdir(fbm):
        shutil.rmtree(fbm, ignore_errors=True)
    print("DONE", key, "tris", report["tris_total"], "ok" if not problems else problems)


def unwrap_parent(pairs, all_objs):
    """UVs for everything without hand-laid decal UVs, then parent (child, parent) keeping world poses."""
    K.unwrap([o for o in all_objs if o.type == "MESH"])
    for child, parent in pairs:
        P.parent_keep(child, parent)
    bpy.context.view_layer.update()


def shoot(cam, cam_d, path, loc, target, lens=50):
    K.render(SC, cam, cam_d, path, loc, target, lens=lens, res=(1200, 900))


# ================================================================ 1. SUPPLY CRATE
def build_crate():
    begin("SupplyCrate")
    K.material("Crate_Kraft", KRAFT, 0.85, note="cardboard: kraft #C4A070 (the brief's extra colour)")
    K.material("Crate_Tape", SIGNAL, 0.5, tex=tex("Crate_Tape.png"), note="red tape; Crate_Tape.png")
    K.material("Crate_Label", CARD, 0.6, tex=tex("Crate_Label.png"), note="shipping label; Crate_Label.png")
    W, D = 0.24, 0.18
    HX, HY = W / 2, D / 2
    T, FT = 0.003, 0.001
    XW_TOP, YW_TOP = 0.129, 0.128          # left/right walls a millimetre taller than front/back
    box = K.Part("Crate")
    box.box((0, 0, T / 2), (W, D, T), "Crate_Kraft")
    for s in (-1, 1):
        box.box((s * (HX - T / 2), 0, XW_TOP / 2), (T, D, XW_TOP), "Crate_Kraft")
        box.box((0, s * (HY - T / 2), YW_TOP / 2), (W - 2 * T, T, YW_TOP), "Crate_Kraft")
    crate = box.build(COLL, bevel=0.0007, segments=1, angle=30)

    flaps = {}
    lr_z, fb_z = XW_TOP + FT / 2, YW_TOP + FT / 2    # 0.1295 and 0.1285: front/back 1 mm under left/right
    for name, s in (("Flap_Left", 1), ("Flap_Right", -1)):
        f = K.Part(name, (s * HX, 0, lr_z))
        f.box((s * (HX - 0.0595), 0, lr_z), (0.119, D, FT), "Crate_Kraft")
        flaps[name] = f.build(COLL)
    for name, s in (("Flap_Front", 1), ("Flap_Back", -1)):
        f = K.Part(name, (0, s * HY, fb_z))
        f.box((0, s * (HY - 0.0425), fb_z), (W - 2 * T - 0.001, 0.085, FT), "Crate_Kraft")
        flaps[name] = f.build(COLL)

    TW = 0.045
    top_z = 0.1300 + 0.0003
    seg = 0.030 + 0.0003
    L = seg * 2 + (D + 0.0006)
    u1, u2 = seg / L, (seg + D + 0.0006) / L
    yf = HY + 0.0003
    tape = K.Part("Tape", (0, yf, 0.100))
    P.quad_uv(tape, (0, yf, 0.100 + seg / 2), (seg, TW), "Crate_Tape", (Z, X, Y), (0, 0), (u1, 1))
    P.quad_uv(tape, (0, 0, top_z), (D + 0.0006, TW), "Crate_Tape", (-Y, X, Z), (u1, 0), (u2, 1))
    P.quad_uv(tape, (0, -yf, 0.100 + seg / 2), (seg, TW), "Crate_Tape", (-Z, X, -Y), (u2, 0), (1, 1))
    tape = tape.build(COLL)
    label = K.Part("Label", (0, yf, 0.052)).quad((0, yf, 0.052), (0.105, 0.070), "Crate_Label", FRONT).build(COLL)
    item = P.empty(COLL, "ItemPoint", (0, 0, 0.04))

    kids = list(flaps.values()) + [tape, label, item]
    objs = [crate] + kids
    unwrap_parent([(k, crate) for k in kids], objs)
    contract = {"Crate": None, **{k: "Crate" for k in ("Flap_Left", "Flap_Right", "Flap_Front", "Flap_Back", "Tape", "Label", "ItemPoint")}}

    def use(cam, cam_d, path, tgt, dist):
        flaps["Flap_Left"].rotation_euler.y = math.radians(118)
        flaps["Flap_Right"].rotation_euler.y = math.radians(-112)
        flaps["Flap_Front"].rotation_euler.x = math.radians(-125)
        flaps["Flap_Back"].rotation_euler.x = math.radians(105)
        tape.hide_render = True
        stage_mats(("Stage_Gadget", SLATE, 0.45), ("Stage_GadgetTrim", BRASS, 0.4), ("Stage_GadgetLamp", LED_GREEN, 0.3, 1.0, LED_GREEN, 3.0))
        g = K.Part("StageItem", (0, 0, 0.17))
        g.box((0, 0, 0.17), (0.09, 0.06, 0.045), "Stage_Gadget")
        g.box((0, 0.031, 0.175), (0.06, 0.002, 0.012), "Stage_GadgetTrim")
        g.box((0.03, 0.031, 0.160), (0.008, 0.003, 0.008), "Stage_GadgetLamp")
        g.build(COLL, bevel=0.003, segments=2)
        K.toonify()
        shoot(cam, cam_d, path, (0.36, 0.56, 0.52), (0.0, 0.0, 0.10), lens=45)

    finish("supply_crate", "SupplyCrate.fbx", crate, objs, contract, use)


# ================================================================ 2. AUTO-FEED TRAY (scanner coordinates)
def build_feeder():
    begin("ScannerFeeder")
    K.material("Feed_Cream", CREAM, 0.5, note="housing: the scanner's cream #DCD0B4 (the brief's extra)")
    K.material("Feed_Slate", SLATE, 0.45, note="tray and cheeks: kit slate")
    K.material("Feed_Rubber", INK, 0.85, note="rollers, roller well, feet: kit ink")
    K.material("Feed_Brass", BRASS, 0.4, metal=0.3, note="guides, shaft: kit brass")
    K.material("Feed_LED", LED_GREEN, 0.3, emit=LED_GREEN, emit_strength=2.0, note="feed lamp (Unlit; the game lights/dims it)")
    K.material("Feed_Plate", SLATE, 0.5, tex=tex("Feeder_Plate.png"), note="plate; Feeder_Plate.png")
    HX0, HX1, HY0, HY1, HZ0, HZ1 = 0.201, 0.262, -0.150, 0.130, 0.004, 0.072
    hc = ((HX0 + HX1) / 2, (HY0 + HY1) / 2, (HZ0 + HZ1) / 2)
    RX, RZ, RR = 0.2135, 0.0805, 0.008          # roller axis (x, z) and radius: the nip at z 0.0725
    PY = -0.010                                  # the paper path's centre line
    a = math.atan2(0.115 - HZ1, 0.36 - HX1)
    ru, rn = Vector((math.cos(a), 0, math.sin(a))), Vector((-math.sin(a), 0, math.cos(a)))
    tray_len = math.hypot(0.36 - HX1, 0.115 - HZ1)
    low = Vector((HX1, 0, HZ1))
    body = K.Part("Feeder")
    body.box(hc, (HX1 - HX0, HY1 - HY0, HZ1 - HZ0), "Feed_Cream")
    body.box((RX, PY, HZ1 + 0.0004), (0.020, 0.246, 0.001), "Feed_Rubber")                    # roller well
    for y in (0.110, -0.130):
        body.box((RX, y, 0.080), (0.014, 0.008, 0.016), "Feed_Cream")                          # shaft supports
    tc = low + ru * (tray_len / 2) - rn * 0.0015 + Vector((0, PY, 0))
    body.box(tc, (tray_len, 0.250, 0.003), "Feed_Slate", (ru, Y, rn))                         # the input tray
    lip = low + ru * (tray_len - 0.002) + rn * 0.004 + Vector((0, PY, 0))
    body.box(lip, (0.004, 0.250, 0.008), "Feed_Slate", (ru, Y, rn))                           # the tray's back stop
    zu1 = HZ1 + (0.352 - HX1) * math.tan(a) - 0.0030
    for y0 in (0.109, -0.129):
        body.box((0.352, y0, (HZ0 + zu1) / 2), (0.006, 0.006, zu1 - HZ0), "Feed_Slate")                # tray legs
        P.prism(body, [(HX1 - 0.001, y0 - 0.003, HZ0), (HX1 - 0.001, y0 - 0.003, HZ0 + 0.012),
                       (0.349, y0 - 0.003, HZ0 + 0.004), (0.349, y0 - 0.003, HZ0)], (0, 0.006, 0), "Feed_Slate")  # floor rails
    body.box((0.352, PY, zu1 - 0.004), (0.006, 0.232, 0.006), "Feed_Slate")                             # cross bar
    for x, y in ((hc[0] - 0.018, 0.115), (hc[0] + 0.018, 0.115), (hc[0] - 0.018, -0.135), (hc[0] + 0.018, -0.135),
                 (0.352, 0.109), (0.352, -0.129)):
        body.box((x, y, HZ0 / 2), (0.010, 0.008, HZ0), "Feed_Rubber")
    led_c = (0.2475, HY1 + 0.0012, 0.056)
    body.box((led_c[0], HY1 + 0.0004, led_c[2]), (0.0095, 0.0010, 0.0095), "Feed_Rubber")      # lamp bezel
    feeder = body.build(COLL, bevel=0.0022, segments=2, angle=30)

    led = K.Part("FeedLED", led_c).box(led_c, (0.006, 0.0024, 0.006), "Feed_LED").build(COLL, bevel=0.0009, segments=2)
    pl_c = (hc[0] - 0.0005, HY1 + 0.0003, 0.030)
    plate = K.Part("FeederPlate", pl_c).quad(pl_c, (0.052, 0.013), "Feed_Plate", FRONT).build(COLL)
    rl = K.Part("Rollers", (RX, PY, RZ))
    for y in (PY - 0.065, PY + 0.065):
        P.cyl_axis(rl, (RX, y, RZ), RR, 0.050, "Feed_Rubber", Y, segs=18)
    P.cyl_axis(rl, (RX, PY, RZ), 0.002, 0.236, "Feed_Brass", Y, segs=8)
    rollers = rl.build(COLL, bevel=0.0012, segments=2, angle=40)
    guides = []
    for name, y in (("GuideFront", 0.0955), ("GuideBack", -0.1155)):
        c = low + ru * 0.065 + rn * 0.007 + Vector((0, y, 0))
        g = K.Part(name, c).box(c, (0.050, 0.003, 0.014), "Feed_Brass", (ru, Y, rn))
        g.box(c - rn * 0.0055 + Vector((0, 0.004 if y < 0 else -0.004, 0)), (0.030, 0.006, 0.003), "Feed_Brass", (ru, Y, rn))
        guides.append(g.build(COLL, bevel=0.0007, segments=1))
    feed_pt = P.empty(COLL, "FeedPoint", (RX, PY, RZ - RR))

    kids = [rollers, *guides, led, plate, feed_pt]
    objs = [feeder] + kids
    unwrap_parent([(k, feeder) for k in kids], objs)
    contract = {"Feeder": None, **{k: "Feeder" for k in ("Rollers", "GuideFront", "GuideBack", "FeedLED", "FeederPlate", "FeedPoint")}}

    def use(cam, cam_d, path, tgt, dist):
        scanner_stand_in()
        stage_mats(("Stage_Paper", MANILA, 0.8), ("Stage_PaperInk", "#6B5A4A", 0.8))
        pp = K.Part("StagePaper", (0.23, PY, 0.073))
        pp.box((0.2335, PY + 0.0, HZ1 + 0.0009), (0.057, 0.207, 0.0006), "Stage_Paper")
        sc = low + ru * (tray_len * 0.52) + rn * 0.0010 + Vector((0, PY, 0))
        pp.box(sc, (tray_len * 1.04, 0.207, 0.0006), "Stage_Paper", (ru, Y, rn))
        for i in range(6):
            pp.box(sc + ru * (-0.03 + i * 0.012) + rn * 0.0004 + Vector((0, 0.02 if i % 2 else 0.0, 0)),
                   (0.004, 0.12 if i % 2 else 0.15, 0.0003), "Stage_PaperInk", (ru, Y, rn))
        pp.build(COLL)
        rollers.rotation_euler.y = math.radians(30)
        K.toonify()
        glow(led, 4.0)
        shoot(cam, cam_d, path, (0.70, 0.46, 0.36), (0.22, -0.02, 0.06), lens=45)

    finish("scanner_feeder", "ScannerFeeder.fbx", feeder, objs, contract, use)


# ================================================================ 3. ANALYSIS BEACON (scanner coordinates)
def build_beacon():
    begin("AnalysisBeacon")
    K.material("Beacon_Slate", SLATE, 0.45, note="bracket and base: kit slate")
    K.material("Beacon_Brass", BRASS, 0.4, metal=0.3, note="rim band, pod collar: kit brass")
    K.material("Beacon_Ink", INK, 0.7, note="screws, lamp socket: kit ink")
    K.material("Beacon_Chrome", CHROME, 0.18, metal=0.9, shade="#6E6880", note="reflector mirror (as the torch's chrome)")
    K.material("Beacon_Dome", "#E8A23C", 0.15, alpha=0.5, note="amber dome, semi-transparent (Unlit/Lit Transparent ~0.5)")
    K.material("Beacon_Lamp", LED_AMBER, 0.3, emit=LED_AMBER, emit_strength=0.0, note="bulb (Unlit; off at rest, the game lights it)")
    K.material("Beacon_Plate", OXBLOOD, 0.5, tex=tex("Beacon_Plate.png"), note="plate; Beacon_Plate.png")
    CX, CY = -0.228, -0.110
    BASE_TOP = 0.064
    WALL = -0.2002
    br = K.Part("Beacon")
    br.box((WALL - 0.00125, CY, 0.014), (0.0025, 0.050, 0.016), "Beacon_Slate")                      # wall plate
    sx = (WALL - 0.0025 + -0.252) / 2
    br.box((sx, CY, 0.020), (abs(-0.252 - (WALL - 0.0025)), 0.050, 0.004), "Beacon_Slate")           # shelf
    br.box((sx, CY + 0.0245, 0.014), (abs(-0.252 - (WALL - 0.0025)), 0.003, 0.016), "Beacon_Slate")  # front apron
    P.prism(br, [(WALL - 0.0025, CY - 0.0015, 0.0065), (WALL - 0.0025, CY - 0.0015, 0.0182), (-0.245, CY - 0.0015, 0.0182)], (0, 0.003, 0), "Beacon_Slate")
    for y in (CY - 0.015, CY + 0.015):
        P.cyl_axis(br, (WALL - 0.0027, y, 0.012), 0.0022, 0.0008, "Beacon_Ink", X, segs=10)
    br.cyl((CX, CY, (0.022 + BASE_TOP) / 2), 0.020, BASE_TOP - 0.022, "Beacon_Slate", "Z", 24)
    br.cyl((CX, CY, 0.058), 0.0208, 0.005, "Beacon_Brass", "Z", 24)
    beacon = br.build(COLL, bevel=0.0007, segments=1, angle=30)
    pl_c = (sx, CY + 0.0263, 0.014)
    plate = K.Part("BeaconPlate", pl_c).quad(pl_c, (0.046, 0.0115), "Beacon_Plate", FRONT).build(COLL)

    pod = K.Part("BeaconPod", (CX, CY, BASE_TOP))
    pod.cyl((CX, CY, BASE_TOP + 0.003), 0.0185, 0.006, "Beacon_Brass", "Z", 24)
    pod.cyl((CX, CY, BASE_TOP - 0.015), 0.0165, 0.030, "Beacon_Slate", "Z", 20)
    pod.cyl((CX, CY, BASE_TOP + 0.008), 0.004, 0.004, "Beacon_Ink", "Z", 10)
    pod_ob = pod.build(COLL, bevel=0.0006, segments=1, angle=30)
    z0 = BASE_TOP + 0.006
    dome = K.Part("Dome", (CX, CY, z0))
    prof = [(z0, 0.0168), (z0 + 0.014, 0.0168)]
    for k in range(1, 7):
        t = k / 6 * math.pi / 2
        prof.append((z0 + 0.014 + 0.017 * math.sin(t), max(0.0006, 0.0168 * math.cos(t))))
    P.lathe_z(dome, (CX, CY), prof, "Beacon_Dome", segs=24, cap_lo=False)
    dome_ob = dome.build(COLL)
    LZ = z0 + 0.010
    refl = K.Part("Reflector", (CX, CY, LZ))
    P.shell_z(refl, (CX, CY), 0.0088, 0.0098, z0 + 0.002, z0 + 0.019, 180, 360, "Beacon_Chrome", segs=10)   # behind the bulb at rest
    refl_ob = refl.build(COLL)
    lamp = K.Part("BeaconLamp", (CX, CY, LZ))
    sphere = [(LZ - 0.0046 + 0.0092 * (1 - math.cos(math.pi * k / 6)) / 2, max(0.0005, 0.0046 * math.sin(math.pi * k / 6))) for k in range(7)]
    P.lathe_z(lamp, (CX, CY), sphere, "Beacon_Lamp", segs=12)
    lamp.cyl((CX, CY, (z0 + 0.002 + LZ - 0.0046) / 2), 0.0016, LZ - 0.0046 - z0 - 0.002, "Beacon_Lamp", "Z", 8)
    lamp_ob = lamp.build(COLL)

    objs = [beacon, plate, pod_ob, dome_ob, refl_ob, lamp_ob]
    unwrap_parent([(dome_ob, pod_ob), (refl_ob, pod_ob), (lamp_ob, pod_ob), (pod_ob, beacon), (plate, beacon)], objs)
    contract = {"Beacon": None, "BeaconPod": "Beacon", "Dome": "BeaconPod", "Reflector": "BeaconPod",
                "BeaconLamp": "BeaconPod", "BeaconPlate": "Beacon"}

    def use(cam, cam_d, path, tgt, dist):
        scanner_stand_in()
        pod_ob.location.z += 0.03
        refl_ob.rotation_euler.z = math.radians(-60)
        light_up("Beacon_Dome", "#FFB23C", 1.6)
        K.toonify()
        glow(lamp_ob, 4.0)
        shoot(cam, cam_d, path, (-0.46, 0.26, 0.24), (-0.215, -0.10, 0.065), lens=50)

    finish("analysis_beacon", "AnalysisBeacon.fbx", beacon, objs, contract, use)


# ================================================================ 4. INTERVIEW PROTOCOLS card ring
CARD_FAN = (9.0, 3.0, -3.0, -9.0)


def build_ring():
    begin("ProtocolRing")
    K.material("Proto_Slate", SLATE, 0.45, note="weighted base disc: kit slate")
    K.material("Proto_Brass", BRASS, 0.35, metal=0.4, note="post, hook, ring: kit brass")
    K.material("Proto_Card", CARD, 0.4, note="card edges and backs: kit card")
    K.material("Proto_Face", CARD, 0.3, tex=tex("Protocol_Card.png"), note="card front; Protocol_Card.png (the game prints live text on it)")
    PX, PY = -0.003, -0.022
    st = K.Part("ProtocolStand")
    st.cyl((0, 0, 0.004), 0.035, 0.008, "Proto_Slate", "Z", 32)
    st.cyl((PX, PY, 0.010), 0.006, 0.004, "Proto_Brass", "Z", 14)
    st.cyl((PX, PY, 0.008 + 0.065), 0.003, 0.130, "Proto_Brass", "Z", 12)
    HY, HZ = 0.004, 0.1415
    st.tube([(PX, PY, 0.1360), (PX, PY, 0.1405), (PX, PY + 0.003, HZ), (PX, HY, HZ), (0.004, HY, HZ),
             (0.0062, HY, HZ + 0.0016), (0.0068, HY, HZ + 0.0045)], 0.0016, "Proto_Brass", sides=8)
    stand = st.build(COLL, bevel=0.0012, segments=2, angle=30)
    RMAJ, RMIN = 0.012, 0.0011
    hook = Vector((0, HY, HZ + 0.0016))                 # the ring rests on the hook bar's top
    rc = hook - Vector((0, 0, RMAJ - RMIN))
    ring = K.Part("Ring", hook)
    P.torus_axis(ring, rc, RMAJ, RMIN, "Proto_Brass", X, segs=28, tube=6)
    ring_ob = ring.build(COLL)
    cards = []
    for i, phi in enumerate((18.0, 6.0, -6.0, -18.0)):
        p = rc + Vector((0, RMAJ * math.sin(math.radians(phi)), -RMAJ * math.cos(math.radians(phi))))
        c = K.Part(f"Card_{i}", p)
        c.box(p + Vector((0, 0, -0.0217)), (0.085, 0.0015, 0.055), "Proto_Card")
        c.quad(p + Vector((0, 0.00078, -0.0217)), (0.085, 0.055), "Proto_Face", FRONT)
        ob = c.build(COLL)
        ob.rotation_euler.x = math.radians(CARD_FAN[i])
        cards.append(ob)
    objs = [stand, ring_ob, *cards]
    unwrap_parent([(c, ring_ob) for c in cards] + [(ring_ob, stand)], objs)
    contract = {"ProtocolStand": None, "Ring": "ProtocolStand", **{f"Card_{i}": "Ring" for i in range(4)}}

    def use(cam, cam_d, path, tgt, dist):
        cards[0].rotation_euler.x = math.radians(140)
        K.toonify()
        shoot(cam, cam_d, path, (0.17, 0.36, 0.21), (0.0, 0.0, 0.088), lens=50)

    finish("protocol_ring", "ProtocolRing.fbx", stand, objs, contract, use, elev=18)


# ================================================================ 5. BABEL speech translator rack
CART = (("NearEast", BRASS, "Near East"), ("Mediterranean", SLATE, "Mediterranean"),
        ("EastAsia", SIGNAL, "East Asia"), ("NorthEurope", LAVENDER, "Northern Europe"))


def build_babel():
    begin("BabelRack")
    K.material("Babel_Slate", SLATE, 0.45, note="wedge body: kit slate")
    K.material("Babel_Deck", INK, 0.6, note="top deck (charcoal): kit ink")
    K.material("Babel_Bay", "#1A1016", 0.9, note="bay lining: ink, darker (the recess)")
    K.material("Babel_Grille", INK, 0.7, note="speaker slots, VU bezel, feet: kit ink")
    K.material("Babel_Brass", BRASS, 0.35, metal=0.4, note="needle hub: kit brass")
    K.material("Babel_VU", "#EEDEAA", 0.4, tex=tex("Babel_VU.png"), note="VU dial; Babel_VU.png")
    K.material("Babel_Glass", BONE, 0.08, alpha=0.12, note="the VU's small glass (Transparent ~0.12)")
    K.material("Babel_Needle", SIGNAL, 0.4, note="needle: kit signal red")
    K.material("Babel_Plate", SLATE, 0.5, tex=tex("Babel_Plate.png"), note="plate; Babel_Plate.png")
    for i, (key, col, nice) in enumerate(CART):
        K.material(f"Babel_Cart{i}", col, 0.45, note=f"cartridge {i} {nice}")
        K.material(f"Babel_CartLabel{i}", col, 0.45, tex=tex(f"Cartridge_{key}.png"), note=f"label; Cartridge_{key}.png")
        K.material(f"Babel_LED{i}", LED_GREEN, 0.3, emit=LED_GREEN, emit_strength=0.0, note=f"cartridge {i} lamp (off at rest; own material)")
    HXB, FY, BY, Z0, FZ, BZ = 0.110, 0.065, -0.065, 0.004, 0.050, 0.085
    su = Vector((0, BY - FY, BZ - FZ))
    slope_len = su.length
    SU = su.normalized()
    SN = Vector((0, SU.z, -SU.y))

    def zdeck(y):
        return FZ + (FY - y) / (FY - BY) * (BZ - FZ)

    b = K.Part("BabelRack")
    b.prism_yz([(BY, Z0), (FY, Z0), (FY, FZ), (BY, BZ)], -HXB, HXB, "Babel_Slate")
    body = b.build(COLL, bevel=0.003, segments=2, angle=30)
    DT = 0.002
    deck = K.Part("Deck")
    deck.box(Vector((0, 0, (FZ + BZ) / 2)) + SN * (DT / 2 + 0.00002), (2 * HXB - 0.008, slope_len - 0.008, DT), "Babel_Deck", (X, SU, SN))
    deck_ob = deck.build(COLL, bevel=0.0006, segments=1)
    YB = -0.028
    mouth = zdeck(YB) + DT / SN.z
    sunk = 0.065 * 2 / 3
    xs = (0.078, 0.026, -0.026, -0.078)          # Bay_0 on the viewer's left (+X)
    for ob in (body, deck_ob):
        for x in xs:
            zf = mouth - sunk - 0.0015
            P.cut(ob, (x, YB, (zf + 0.12) / 2), (0.049, 0.018, 0.12 - zf), STAGE, "Babel_Bay")
    K.merge(body, deck_ob)
    det = K.Part("Detail")
    for k in range(6):
        det.box((0.064, FY + 0.0004, 0.0135 + k * 0.0052), (0.056, 0.0010, 0.0026), "Babel_Grille")
    det.box((0, FY + 0.0006, 0.026), (0.062, 0.0012, 0.034), "Babel_Grille")
    for x in (-0.09, 0.09):
        for y in (-0.05, 0.05):
            det.box((x, y, Z0 / 2), (0.014, 0.014, Z0), "Babel_Grille")
    PIV_Z = 0.026 - 0.0135 - 0.2083 * 0.027       # the dial's arc centre (below the face, as the texture's)
    P.cyl_axis(det, (0, FY + 0.0032, PIV_Z), 0.0024, 0.0012, "Babel_Brass", Y, segs=14)
    K.merge(body, det.build(COLL, bevel=0.0004, segments=1))
    K.smooth(body)

    vu_c = (0, FY + 0.0013, 0.026)
    vu = K.Part("VUFace", vu_c).quad(vu_c, (0.054, 0.027), "Babel_VU", FRONT).build(COLL)
    gl_c = (0, FY + 0.0032, 0.026)
    glass = K.Part("VUGlass", gl_c).box(gl_c, (0.058, 0.0008, 0.030), "Babel_Glass").build(COLL)
    nd_p = (0, FY + 0.0022, PIV_Z)
    needle = K.Part("Needle", nd_p).box((0, FY + 0.0022, PIV_Z + 0.013), (0.0013, 0.0005, 0.026), "Babel_Needle").build(COLL)
    pl_c = (-0.067, FY + 0.0004, 0.026)
    plate = K.Part("BabelPlate", pl_c).quad(pl_c, (0.072, 0.0144), "Babel_Plate", FRONT).build(COLL)
    bays, carts, leds = [], [], []
    for i, x in enumerate(xs):
        bays.append(P.empty(COLL, f"Bay_{i}", (x, YB, mouth)))
        zb = mouth - sunk
        c = K.Part(f"Cartridge_{i}", (x, YB, zb))
        c.box((x, YB, zb + 0.0325), (0.045, 0.014, 0.065), f"Babel_Cart{i}")
        c.box((x + 0.005, YB, zb + 0.065 + 0.0006), (0.024, 0.008, 0.0012), "Babel_Grille")       # grip ridge
        c.quad((x, YB + 0.007 + 0.0003, zb + 0.065 - 0.0092), (0.042, 0.014), f"Babel_CartLabel{i}", FRONT)
        carts.append(c.build(COLL))
        lc = (x - 0.0145, YB, zb + 0.065 + 0.001)
        leds.append(K.Part(f"CartridgeLED_{i}", lc).box(lc, (0.007, 0.005, 0.002), f"Babel_LED{i}").build(COLL, bevel=0.0006, segments=1))
    objs = [body, vu, glass, needle, plate, *bays, *carts, *leds]
    pairs = [(o, body) for o in (vu, glass, needle, plate, *bays, *carts)] + [(l, c) for l, c in zip(leds, carts)]
    unwrap_parent(pairs, objs)
    contract = {"BabelRack": None, **{k: "BabelRack" for k in ("VUFace", "VUGlass", "Needle", "BabelPlate")},
                **{f"Bay_{i}": "BabelRack" for i in range(4)}, **{f"Cartridge_{i}": "BabelRack" for i in range(4)},
                **{f"CartridgeLED_{i}": f"Cartridge_{i}" for i in range(4)}}

    def use(cam, cam_d, path, tgt, dist):
        needle.rotation_euler.y = math.radians(-32)
        carts[2].location.z += 0.032                  # East Asia half out of its bay
        K.toonify()
        glow(leds[1], 4.0)
        shoot(cam, cam_d, path, (0.26, 0.40, 0.30), (0.0, 0.0, 0.048), lens=50)

    finish("babel_rack", "BabelRack.fbx", body, objs, contract, use)


# ================================================================ 6. TRANSLATION LENS kit
def build_lens():
    begin("LensKit")
    K.material("Lens_Base", OXBLOOD, 0.5, note="weighted base: kit oxblood")
    K.material("Lens_Brass", BRASS, 0.35, metal=0.4, note="post, loupe ring, arm, frame: kit brass")
    K.material("Lens_Ink", INK, 0.7, note="thumbscrew, felt: kit ink")
    K.material("Lens_Glass", GLASS, 0.05, alpha=0.35, note="loupe glass: pale cyan (the brief's), Transparent ~0.35")
    K.material("Lens_Bar", GLASS, 0.05, alpha=0.55, note="bar magnifier: pale cyan, Transparent ~0.55")
    K.material("Lens_Line", SIGNAL, 0.3, emit=LED_RED, emit_strength=2.0, note="bar line: signal red, emissive")
    K.material("Lens_Fresnel", GLASS, 0.1, alpha=0.4, tex=tex("LensKit_Fresnel.png"), note="Fresnel sheet; LensKit_Fresnel.png, Transparent ~0.4")
    BZ = 0.014
    PX, PY = 0.043, -0.023
    PTOP = BZ + 0.090
    st = K.Part("LensStand")
    st.box((0, 0, BZ / 2), (0.110, 0.070, BZ), "Lens_Base")
    st.cyl((PX, PY, BZ + 0.002), 0.0072, 0.004, "Lens_Brass", "Z", 16)
    st.cyl((PX, PY, BZ + 0.045), 0.0045, 0.090, "Lens_Brass", "Z", 14)
    stand = st.build(COLL, bevel=0.0025, segments=2, angle=30)

    swivel = (PX, PY, PTOP)
    lp = K.Part("Loupe", swivel)
    lp.cyl((PX, PY, PTOP + 0.004), 0.006, 0.008, "Lens_Brass", "Z", 16)
    LC = Vector((0.011, PY, PTOP + 0.004))
    lp.box((0.0365, PY, PTOP + 0.004), (0.010, 0.004, 0.004), "Lens_Brass")
    P.torus_axis(lp, LC, 0.0215, 0.0022, "Lens_Brass", Y, segs=32, tube=6)
    loupe = lp.build(COLL, bevel=0.0005, segments=1, angle=40)
    lg = K.Part("LoupeGlass", LC)
    P.disc(lg, LC + Vector((0, 0.0004, 0)), 0.0205, "Lens_Glass", (-X, Z, Y), segs=24)
    P.disc(lg, LC + Vector((0, -0.0004, 0)), 0.0205, "Lens_Glass", (X, Z, -Y), segs=24)
    loupe_glass = lg.build(COLL)

    BMY = 0.0235
    bm = K.Part("BarMagnifier", (0, BMY, BZ))
    for x in (-0.045, 0.045):
        bm.box((x, BMY, BZ + 0.002), (0.008, 0.012, 0.004), "Lens_Brass")
    prof = [(BMY - 0.010, BZ + 0.004), (BMY + 0.010, BZ + 0.004)]      # flat underside, then the dome
    for k in range(0, 9):
        t = math.pi * k / 8
        prof.append((BMY + 0.010 * math.cos(t), BZ + 0.0065 + 0.0045 * math.sin(t)))
    bm.prism_yz(prof, -0.060, 0.060, "Lens_Bar")
    bar = bm.build(COLL)
    blc = (0, BMY, BZ + 0.0113)
    barline = K.Part("BarLine", blc).box(blc, (0.116, 0.0022, 0.0008), "Lens_Line").build(COLL)

    clamp = Vector((PX, PY, 0.080))
    arm = P.empty(COLL, "FresnelArm", tuple(clamp))
    ab = K.Part("ArmBase", clamp)
    ab.cyl(clamp, 0.0068, 0.010, "Lens_Brass", "Z", 16)
    ab.box((PX, PY - 0.0105, 0.080), (0.010, 0.013, 0.008), "Lens_Brass")
    P.cyl_axis(ab, (PX + 0.0085, PY, 0.080), 0.0025, 0.004, "Lens_Ink", X, segs=10)
    arm_base = ab.build(COLL, bevel=0.0006, segments=1)
    HINGE = Vector((0, -0.0385, 0.080))
    au = K.Part("ArmUpper", HINGE)
    P.cyl_axis(au, HINGE, 0.0025, 0.156, "Lens_Brass", X, segs=10)
    S0, SH = 0.080 + 0.056, 0.110
    for s in (-1, 1):
        au.box((s * 0.0765, HINGE.y, (0.080 + S0 + SH) / 2), (0.003, 0.003, S0 + SH - 0.080), "Lens_Brass")
    for z in (S0 - 0.0015, S0 + SH + 0.0015):
        au.box((0, HINGE.y, z), (0.156, 0.003, 0.003), "Lens_Brass")
    arm_upper = au.build(COLL, bevel=0.0005, segments=1)
    shc = Vector((0, HINGE.y, S0 + SH / 2))
    sh = K.Part("FresnelSheet", shc)
    sh.quad(shc + Vector((0, 0.0003, 0)), (0.150, SH), "Lens_Fresnel", (-X, Z, Y))
    sh.quad(shc + Vector((0, -0.0003, 0)), (0.150, SH), "Lens_Fresnel", (X, Z, -Y))
    sheet = sh.build(COLL)

    objs = [stand, loupe, loupe_glass, bar, barline, arm, arm_base, arm_upper, sheet]
    unwrap_parent([(loupe_glass, loupe), (barline, bar), (sheet, arm_upper), (arm_base, arm), (arm_upper, arm),
                   (loupe, stand), (bar, stand), (arm, stand)], objs)
    contract = {"LensStand": None, "Loupe": "LensStand", "LoupeGlass": "Loupe", "BarMagnifier": "LensStand",
                "BarLine": "BarMagnifier", "FresnelArm": "LensStand", "ArmBase": "FresnelArm", "ArmUpper": "FresnelArm",
                "FresnelSheet": "ArmUpper"}

    def use(cam, cam_d, path, tgt, dist):
        arm_upper.rotation_euler.x = math.radians(-82)
        loupe.rotation_euler = (math.radians(-18), 0, math.radians(-28))
        stage_mats(("Stage_Paper", MANILA, 0.8), ("Stage_PaperInk", "#6B5A4A", 0.8))
        pp = K.Part("StagePaper", (0, 0.10, 0.0005))
        pp.box((0.0, 0.11, 0.0005), (0.16, 0.12, 0.001), "Stage_Paper")
        for i in range(6):
            pp.box((0.0 - (0.02 if i % 2 else 0), 0.07 + i * 0.016, 0.00115), (0.11 if i % 2 else 0.13, 0.003, 0.0003), "Stage_PaperInk")
        pp.build(COLL)
        K.toonify()
        glow(barline, 3.0)
        shoot(cam, cam_d, path, (0.34, 0.50, 0.40), (0.0, 0.03, 0.07), lens=45)

    finish("lens_kit", "LensKit.fbx", stand, objs, contract, use)


# ================================================================ 7. ROLODEX
def build_rolodex():
    begin("Rolodex")
    K.material("Rdx_Oxblood", OXBLOOD, 0.5, note="base, wheels: kit oxblood")
    K.material("Rdx_Brass", BRASS, 0.35, metal=0.4, note="axle, hub caps, knob: kit brass")
    K.material("Rdx_Slate", SLATE, 0.5, note="drum hub: kit slate")
    K.material("Rdx_Card", CARD, 0.6, note="card edges and backs: kit card")
    K.material("Rdx_Face", CARD, 0.5, tex=tex("Rolodex_Card.png"), note="card front; Rolodex_Card.png")
    K.material("Rdx_Gold", "#FAECC4", 0.4, tex=tex("Rolodex_GoldCard.png"), note="Card_00 front; Rolodex_GoldCard.png")
    AZ = 0.060
    b = K.Part("Rolodex")
    b.box((0, 0, 0.002), (0.110, 0.080, 0.004), "Rdx_Oxblood")
    for s in (-1, 1):
        b.box((s * 0.043, 0, 0.009), (0.008, 0.050, 0.010), "Rdx_Oxblood")
        b.cyl((s * 0.043, 0, AZ), 0.050, 0.004, "Rdx_Oxblood", "X", 32)
        b.cyl((s * 0.046, 0, AZ), 0.009, 0.002, "Rdx_Brass", "X", 16)
    b.cyl((0, 0, AZ), 0.003, 0.094, "Rdx_Brass", "X", 10)
    root = b.build(COLL, bevel=0.0012, segments=2, angle=30)
    kp = (-0.052, 0, AZ)
    kn = K.Part("Knob", kp)
    kn.cyl(kp, 0.011, 0.010, "Rdx_Brass", "X", 14)
    kn.cyl((-0.0565, 0, AZ), 0.0085, 0.002, "Rdx_Brass", "X", 14)
    knob = kn.build(COLL, bevel=0.0008, segments=2, angle=30)
    dr = K.Part("Drum", (0, 0, AZ))
    dr.cyl((0, 0, AZ), 0.0075, 0.078, "Rdx_Slate", "X", 16)
    drum = dr.build(COLL, bevel=0.0006, segments=1)
    cards = []
    for k in range(24):
        c = K.Part(f"Card_{k:02d}", (0, 0, AZ))
        c.box((0, 0, AZ + 0.0075 + 0.024), (0.076, 0.0006, 0.048), "Rdx_Card")
        c.quad((0, 0.00031, AZ + 0.0075 + 0.024), (0.076, 0.048), "Rdx_Gold" if k == 0 else "Rdx_Face", FRONT)
        ob = c.build(COLL)
        ob.rotation_euler.x = math.radians(15 * k)
        cards.append(ob)
    objs = [root, knob, drum, *cards]
    unwrap_parent([(c, drum) for c in cards] + [(drum, root), (knob, root)], objs)
    contract = {"Rolodex": None, "Knob": "Rolodex", "Drum": "Rolodex", **{f"Card_{k:02d}": "Drum" for k in range(24)}}

    def use(cam, cam_d, path, tgt, dist):
        fan = {0: 30, 1: 41, 2: 51, 3: 60, 4: 68, 5: 76, 6: 84, 23: -12, 22: -24, 21: -37}
        for k, a in fan.items():
            cards[k].rotation_euler.x = math.radians(a)
        knob.rotation_euler.x = math.radians(40)
        K.toonify()
        shoot(cam, cam_d, path, (-0.15, 0.33, 0.21), (0.0, 0.0, 0.058), lens=50)

    finish("rolodex", "Rolodex.fbx", root, objs, contract, use)


# ================================================================ 8. RING SERVICE relay panel
def build_panel():
    begin("RingPanel")
    K.material("Panel_Slate", SLATE, 0.45, note="console: kit slate")
    K.material("Panel_Ink", INK, 0.7, note="face bezel, breaker bases, feet: kit ink")
    K.material("Panel_Brass", BRASS, 0.35, metal=0.4, note="lamp bezels: kit brass")
    K.material("Panel_Lever", BONE, 0.4, note="breaker lever: kit bone")
    K.material("Panel_LeverTip", SIGNAL, 0.4, note="breaker tip: kit signal red")
    K.material("Panel_Face", SLATE, 0.5, tex=tex("Relay_Face.png"), note="face; Relay_Face.png (modules 01..05, 03 = RETURN)")
    K.material("Panel_Sticker", "#78C478", 0.5, tex=tex("Relay_Serviced.png"), note="SERVICED sticker; Relay_Serviced.png")
    for n in range(1, 6):
        K.material(f"Panel_Lamp{n}", "#1F5A36", 0.3, emit=LED_GREEN, emit_strength=0.0, note=f"module {n:02d} pilot lamp (off at rest; own material)")
    HX, FY, BY, FZ, BZ, TY = 0.130, 0.050, -0.050, 0.030, 0.070, -0.035
    SU = Vector((0, TY - FY, BZ - FZ)).normalized()
    SN = Vector((0, SU.z, -SU.y))
    SR = -X
    FASCIA = (SR, SU, SN)
    L = Vector((0, TY - FY, BZ - FZ)).length

    def on_slope(x, s, off=0.0):
        return Vector((x, FY, FZ)) + SU * s + SN * off

    def s_of(v):
        return L / 2 - 0.0625 / 2 + v * 0.0625

    b = K.Part("RingPanel")
    b.prism_yz([(BY, 0.004), (FY, 0.004), (FY, FZ), (TY, BZ), (BY, BZ)], -HX, HX, "Panel_Slate")
    root = b.build(COLL, bevel=0.003, segments=2, angle=30)
    det = K.Part("Detail")
    det.box(on_slope(0, L / 2, 0.0004), (0.256, 0.068, 0.0008), "Panel_Ink", FASCIA)
    for x in (-0.11, 0.11):
        for y in (-0.035, 0.035):
            det.box((x, y, 0.002), (0.016, 0.014, 0.004), "Panel_Ink")
    xs = [0.100 - (n - 1) * 0.050 for n in range(1, 6)]
    V_LAMP, V_STK = 0.645, 0.465
    for xc in xs:
        P.cyl_axis(det, on_slope(xc + 0.012, s_of(V_LAMP), 0.0009 + 0.0008), 0.0058, 0.0016, "Panel_Brass", SN, segs=16)
        det.box(on_slope(xc - 0.010, s_of(V_LAMP), 0.0009 + 0.0006), (0.008, 0.012, 0.0012), "Panel_Ink", FASCIA)
    K.merge(root, det.build(COLL, bevel=0.0004, segments=1))
    K.smooth(root)
    fc = on_slope(0, L / 2, 0.0009)
    face = K.Part("PanelFace", fc).quad(fc, (0.250, 0.0625), "Panel_Face", FASCIA).build(COLL)
    lamps, breakers, stickers = [], [], []
    for n, xc in enumerate(xs, start=1):
        lc = on_slope(xc + 0.012, s_of(V_LAMP), 0.0009 + 0.0022)
        lp = K.Part(f"Lamp_{n}", lc)
        P.cyl_axis(lp, lc, 0.0043, 0.0028, f"Panel_Lamp{n}", SN, segs=16)
        P.cyl_axis(lp, lc + SN * 0.0017, 0.0030, 0.0008, f"Panel_Lamp{n}", SN, segs=16)
        lamps.append(lp.build(COLL, bevel=0.0005, segments=1, angle=30))
        bp = on_slope(xc - 0.010, s_of(V_LAMP), 0.0009 + 0.0012)
        br = K.Part(f"Breaker_{n}", bp)
        br.box(bp + SN * 0.006, (0.0028, 0.0028, 0.012), "Panel_Lever", FASCIA)
        br.box(bp + SN * 0.0125, (0.0042, 0.0042, 0.004), "Panel_LeverTip", FASCIA)
        breakers.append(br.build(COLL, bevel=0.0005, segments=1, angle=30))
        al = math.radians(-6 + 3 * n)
        r_, u_ = SR * math.cos(al) + SU * math.sin(al), -SR * math.sin(al) + SU * math.cos(al)
        sc = on_slope(xc, s_of(V_STK), 0.0013)
        stickers.append(K.Part(f"Sticker_{n}", sc).quad(sc, (0.026, 0.0098), "Panel_Sticker", (r_, u_, SN)).build(COLL))
    objs = [root, face, *lamps, *breakers, *stickers]
    unwrap_parent([(o, root) for o in objs[1:]], objs)
    contract = {"RingPanel": None, "PanelFace": "RingPanel", **{f"Lamp_{n}": "RingPanel" for n in range(1, 6)},
                **{f"Breaker_{n}": "RingPanel" for n in range(1, 6)}, **{f"Sticker_{n}": "RingPanel" for n in range(1, 6)}}

    def use(cam, cam_d, path, tgt, dist):
        for n in range(5):
            breakers[n].rotation_euler.x = math.radians(25 if n in (1, 3) else -25)
            stickers[n].hide_render = n not in (1, 3)
        K.toonify()
        for n in range(5):
            glow(lamps[n], 4.0 if n in (1, 3) else 0.3)
        shoot(cam, cam_d, path, (0.14, 0.40, 0.30), (0.0, 0.0, 0.045), lens=50)

    finish("ring_panel", "RingPanel.fbx", root, objs, contract, use)


# ================================================================ run
BUILDERS = [("crate", build_crate), ("feeder", build_feeder), ("beacon", build_beacon), ("ring", build_ring),
            ("babel", build_babel), ("lens", build_lens), ("rolodex", build_rolodex), ("panel", build_panel)]
for key, fn in BUILDERS:
    if ONLY and key not in ONLY:
        continue
    fn()


def write_metas():
    """A .meta beside every new FBX / PNG in the machines folder (the chip reader's, with a fresh guid)."""
    tpl = {".fbx": os.path.join(OUT, "ChipReader.fbx.meta"), ".png": os.path.join(OUT, "ChipReader_Pad.png.meta")}
    made = []
    for n in sorted(os.listdir(OUT)):
        ext = os.path.splitext(n)[1].lower()
        if ext not in tpl or os.path.exists(os.path.join(OUT, n + ".meta")):
            continue
        lines = open(tpl[ext], encoding="utf-8").read().splitlines(True)
        lines = [f"guid: {uuid.uuid4().hex}\n" if ln.startswith("guid:") else ln for ln in lines]
        with open(os.path.join(OUT, n + ".meta"), "w", encoding="utf-8", newline="\n") as fh:
            fh.writelines(lines)
        made.append(n + ".meta")
    return made


metas = write_metas()
vpath = os.path.join(WORK, "verify_props.json")
old = {}
if os.path.exists(vpath):
    try:
        old = json.load(open(vpath, encoding="utf-8"))
    except Exception:  # noqa: BLE001
        old = {}
old.update(RESULTS)
old["_metas_written_last_run"] = metas
with open(vpath, "w", encoding="utf-8") as fh:
    json.dump(old, fh, indent=1, default=str)
print("VERIFY written", vpath)
for exe in ("python", "py"):
    try:
        subprocess.run([exe, os.path.join(HERE, "props_sheet.py"), RENDERS, *RESULTS.keys()], check=True, timeout=300)
        break
    except Exception as e:  # noqa: BLE001
        print("sheet helper failed with", exe, e)
print("SUMMARY", {k: (v["tris_total"], v["reimport"]["ok"], v["reimport"]["problems"]) for k, v in RESULTS.items()})
