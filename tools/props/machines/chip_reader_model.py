"""
Time Sorter (NOPE) - the chip reader, an 80s card terminal (desk machine, spec 2026-10-17-desk-machines-design.md).

Run (background only; never open Blender's window, Saleh uses this PC):
    "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" -b --python chip_reader_model.py -- <render_dir>
Re-runnable: it starts from an empty scene and overwrites its outputs.

Writes Assets/Art/Office/Gameplay/Machines/ChipReader.fbx (+ its textures, drawn by machine_textures.py),
ChipReader.blend and verify_chip_reader.json (the FBX re-imported) beside <render_dir>, and the turntable
frames + two use shots to <render_dir> (default: ./renders).

The prop contract (object names the game will bind; Unity axes after export: front = local -Z):
    Body        the gunmetal shell, cream top cap, feet, keys bezel, cable strain relief (root)
    Pad         the RF pad on the top deck, where a passport lies (texture ChipReader_Pad)
    SlotFlap    the card slot's sprung dust flap (pivot on its top edge: it swings in as a card enters)
    Readout     the green VFD line (texture ChipReader_VFD; the game draws live text over it)
    LED_Power, LED_Read, LED_Fault   the three status lamps (green, amber, red), one material each
    Keys        three chunky keys (CLR, READ, EJECT)
    Badge       the TCB brand plate (texture ChipReader_Badge)
    SlotLabel   "INSERT CARD" under the slot (texture ChipReader_SlotLabel)
    Cable       the coiled cord out of the back
    CardSlot    (empty) the slot's mouth: a card's centre as it starts in; it slides along local +Z (Unity)
    PadCentre   (empty) the pad's top centre: where a passport is laid
"""
import math
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import machine_kit as K  # noqa: E402

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(REPO, "Assets", "Art", "Office", "Gameplay", "Machines")
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
RENDERS = os.path.abspath(argv[0]) if argv else os.path.join(HERE, "renders")
os.makedirs(RENDERS, exist_ok=True)
WORK = os.path.dirname(RENDERS)   # the .blend and the re-import report go beside the renders, outside the repo


def tex(name):
    return os.path.join(OUT, name)


if not all(os.path.exists(tex(n)) for n in ("ChipReader_VFD.png", "ChipReader_Badge.png", "ChipReader_Pad.png", "ChipReader_SlotLabel.png")):
    for exe in ("python", "py"):
        try:
            subprocess.run([exe, os.path.join(HERE, "machine_textures.py")], check=True, timeout=180)
            break
        except Exception as e:  # noqa: BLE001
            print("texture helper failed with", exe, e)

scene, COLL, STAGE = K.reset_scene("ChipReader")

# ---------------------------------------------------------------- dimensions (Blender: X right-to-left of the viewer, +Y front, Z up)
HX = 0.115                    # half width  -> 0.23 m
FRONT_Y, BACK_Y = 0.100, -0.110
BASE_Z = 0.004
FRONT_TOP = (FRONT_Y, 0.030)  # top of the vertical front (the slot's face)
SLOPE_TOP = (0.030, 0.066)    # the slope's top edge, where the flat top deck starts
BACK_TOP = (BACK_Y, 0.070)
CAP_T = 0.004                 # cream top cap thickness
SLOT_Z = 0.020
SLOT_W, SLOT_H = 0.094, 0.0045
PAD_C = (0.0, -0.052)         # (x, y) of the pad's centre
PAD_W, PAD_D = 0.120, 0.092   # a passport (0.125 x 0.088) overhangs it a little, like a real pad

_su = Vector((0.0, SLOPE_TOP[0] - FRONT_TOP[0], SLOPE_TOP[1] - FRONT_TOP[1]))
SU = _su.normalized()
SN = Vector((0.0, SU.z, -SU.y))           # outward (forward-up)
SR = Vector((-1.0, 0.0, 0.0))             # the viewer's right
FASCIA = (SR, SU, SN)
FRONT = (SR, Vector((0, 0, 1)), Vector((0, 1, 0)))
TOP = (SR, Vector((0, -1, 0)), Vector((0, 0, 1)))   # decals on the top deck read from the front


def on_slope(x, s, off=0.0):
    return Vector((x, FRONT_TOP[0], FRONT_TOP[1])) + SU * s + SN * off


def top_z(y):
    """The top deck's height at y (it rises 4 mm toward the back)."""
    t = (SLOPE_TOP[0] - y) / (SLOPE_TOP[0] - BACK_Y)
    return SLOPE_TOP[1] + (BACK_TOP[1] - SLOPE_TOP[1]) * t


# ---------------------------------------------------------------- materials (kit palette)
K.material("Chip_Gunmetal", "#4E5560", 0.45, metal=0.2, note="shell (Desk Anime)")
K.material("Chip_Cream", "#E6DCC4", 0.5, note="top cap (Desk Anime)")
K.material("Chip_Charcoal", "#2F3036", 0.5, note="fascia bezels, slot lip (Desk Anime)")
K.material("Chip_Slot", "#0E0E10", 0.8, note="slot mouth (Desk Anime)")
K.material("Chip_Rubber", "#2A2628", 0.85, note="feet, flap, cable (Desk Anime)")
K.material("Chip_KeySlate", "#4A5872", 0.45, note="CLR key: kit slate")
K.material("Chip_KeyBrass", "#D4A055", 0.4, note="READ key: kit brass")
K.material("Chip_KeySignal", "#C23A2E", 0.45, note="EJECT key: kit signal red")
K.material("Chip_Pad", "#2B2630", 0.8, tex=tex("ChipReader_Pad.png"), note="RF pad; base map ChipReader_Pad.png")
K.material("Chip_VFD", "#061A12", 0.3, emit="#6CFF9A", emit_strength=1.6, tex=tex("ChipReader_VFD.png"),
           note="VFD; URP Unlit with ChipReader_VFD.png (live text drawn over it)")
K.material("Chip_VFDGlass", "#203A30", 0.1, alpha=0.35, note="the VFD's smoked window (Unlit Transparent)")
K.material("Chip_Badge", "#8A2F3B", 0.4, tex=tex("ChipReader_Badge.png"), note="brand plate; ChipReader_Badge.png")
K.material("Chip_SlotLabel", "#464C56", 0.5, tex=tex("ChipReader_SlotLabel.png"), note="slot label; ChipReader_SlotLabel.png")
K.material("Chip_LEDGreen", "#3CFF86", 0.3, emit="#3CFF86", emit_strength=4.0, note="POWER lamp (Unlit; the game dims/lights it)")
K.material("Chip_LEDAmber", "#FFB23C", 0.3, emit="#FFB23C", emit_strength=0.0, note="READ lamp (off at rest)")
K.material("Chip_LEDRed", "#FF3A2A", 0.3, emit="#FF3A2A", emit_strength=0.0, note="FAULT lamp (off at rest)")

# ================================================================ BODY
profile = [(BACK_Y, BASE_Z), (FRONT_Y, BASE_Z), FRONT_TOP, SLOPE_TOP, BACK_TOP]
body = K.Part("Body").prism_yz(profile, -HX, HX, "Chip_Gunmetal").build(COLL, bevel=0.0045, segments=3, angle=24)
# the pad's recess in the top deck
K.boolean_cut(body, (PAD_C[0], PAD_C[1], top_z(PAD_C[1]) + 0.02), (PAD_W + 0.008, PAD_D + 0.008, 0.044), STAGE, "Chip_Charcoal")
# the slot's mouth through the front
K.boolean_cut(body, (0, FRONT_Y, SLOT_Z), (SLOT_W, 0.05, SLOT_H), STAGE, "Chip_Slot")
K.smooth(body)

cap = K.Part("Cap")
cap_profile = [(SLOPE_TOP[0] - 0.004, SLOPE_TOP[1] - 0.0005), (BACK_Y + 0.004, BACK_TOP[1] - 0.0005),
               (BACK_Y + 0.004, BACK_TOP[1] + CAP_T), (SLOPE_TOP[0] - 0.004, SLOPE_TOP[1] + CAP_T)]
cap.prism_yz(cap_profile, -HX + 0.004, HX - 0.004, "Chip_Cream")
cap_ob = cap.build(COLL, bevel=0.0018, segments=2, angle=30)
K.boolean_cut(cap_ob, (PAD_C[0], PAD_C[1], top_z(PAD_C[1]) + 0.02), (PAD_W + 0.012, PAD_D + 0.012, 0.044), STAGE, "Chip_Cream")

det = K.Part("Detail")
for sx in (-1, 1):
    for sy in (-1, 1):
        det.cyl((sx * 0.092, sy * 0.080, BASE_Z / 2), 0.010, BASE_Z, "Chip_Rubber", "Z", 14)
# slot lip (a charcoal bezel round the mouth) and the fascia bezels on the slope
det.box((0, FRONT_Y + 0.0015, SLOT_Z), (SLOT_W + 0.016, 0.003, SLOT_H + 0.010), "Chip_Charcoal")
det.box(on_slope(0.020, 0.055, 0.0008), (0.112, 0.026, 0.0016), "Chip_Charcoal", FASCIA)   # VFD bezel
det.box(on_slope(0.020, 0.022, 0.0008), (0.110, 0.020, 0.0016), "Chip_Charcoal", FASCIA)   # key well
# the pad's floor (under the Pad) and the cable's strain relief at the back
det.box((PAD_C[0], PAD_C[1], top_z(PAD_C[1]) - 0.006), (PAD_W + 0.008, PAD_D + 0.008, 0.002), "Chip_Charcoal")
det.cyl((0.075, BACK_Y - 0.008, 0.020), 0.0065, 0.018, "Chip_Rubber", "Y", 12)
det_ob = det.build(COLL, bevel=0.0007, segments=2, angle=40)
K.merge(body, cap_ob)
K.merge(body, det_ob)
K.smooth(body)

# ================================================================ PAD (separate: the game flashes it while reading)
pad_top = top_z(PAD_C[1]) - 0.0012
pad = K.Part("Pad", (PAD_C[0], PAD_C[1], pad_top))
pad.box((PAD_C[0], PAD_C[1], pad_top - 0.0022), (PAD_W, PAD_D, 0.0040), "Chip_Rubber")
pad.quad((PAD_C[0], PAD_C[1], pad_top + 0.00005), (PAD_W - 0.004, PAD_D - 0.004), "Chip_Pad", TOP)
pad_ob = pad.build(COLL)

# ================================================================ SLOT FLAP (pivot: its top edge, just inside the mouth)
flap_pivot = Vector((0, FRONT_Y - 0.004, SLOT_Z + SLOT_H / 2))
flap = K.Part("SlotFlap", flap_pivot).box((0, FRONT_Y - 0.004, SLOT_Z), (SLOT_W - 0.004, 0.0012, SLOT_H - 0.0006), "Chip_Rubber").build(COLL)

# ================================================================ FASCIA: VFD, LEDs, keys
ro_c = on_slope(0.020, 0.055, 0.0016 + 0.0004)
readout = K.Part("Readout", ro_c).quad(ro_c, (0.100, 0.0185), "Chip_VFD", FASCIA).build(COLL)
vg_c = on_slope(0.020, 0.055, 0.0016 + 0.0014)
vfd_glass = K.Part("ReadoutGlass", vg_c).box(vg_c, (0.104, 0.0215, 0.0012), "Chip_VFDGlass", FASCIA).build(COLL)

LEDS = []
for i, (nm, m) in enumerate((("LED_Power", "Chip_LEDGreen"), ("LED_Read", "Chip_LEDAmber"), ("LED_Fault", "Chip_LEDRed"))):
    c = on_slope(-0.064 - i * 0.0145, 0.055, 0.0016 + 0.0016)
    led = K.Part(nm, c)
    led.box(c, (0.0068, 0.0068, 0.0032), m, FASCIA)
    LEDS.append(led.build(COLL, bevel=0.0012, segments=2, angle=30))
# LED surrounds (charcoal) merged into the body
sur = K.Part("LedSurrounds")
for i in range(3):
    sur.box(on_slope(-0.064 - i * 0.0145, 0.055, 0.0016 + 0.0004), (0.0105, 0.0105, 0.0010), "Chip_Charcoal", FASCIA)
K.merge(body, sur.build(COLL, bevel=0.0006, segments=1))

keys = K.Part("Keys", on_slope(0.020, 0.022))
for x, w, m in ((0.058, 0.026, "Chip_KeySlate"), (0.020, 0.034, "Chip_KeyBrass"), (-0.022, 0.034, "Chip_KeySignal")):
    keys.box(on_slope(x, 0.022, 0.0016 + 0.0035), (w, 0.0135, 0.0070), m, FASCIA)
keys_ob = keys.build(COLL, bevel=0.0020, segments=3, angle=35)

# ================================================================ DECALS: badge, slot label
bd_y = SLOPE_TOP[0] - 0.016
bd_c = (0.0, bd_y, top_z(bd_y) + CAP_T + 0.0003)
badge = K.Part("Badge", bd_c).quad(bd_c, (0.090, 0.018), "Chip_Badge", TOP).build(COLL)
sl_c = (0.0, FRONT_Y + 0.0032, SLOT_Z - 0.0118)
slot_label = K.Part("SlotLabel", sl_c).quad(sl_c, (0.072, 0.009), "Chip_SlotLabel", FRONT).build(COLL)

# ================================================================ CABLE (a coiled cord off the back, down onto the desk)
pts = []
turns, n = 11, 11 * 12
start = Vector((0.075, BACK_Y - 0.017, 0.020))
for k in range(n + 1):
    t = k / n
    a = 2 * math.pi * turns * t
    centre = start + Vector((0.020 * t, -0.150 * t, -0.012 * min(1.0, t * 2.2)))
    pts.append(centre + Vector((0.0075 * math.cos(a), 0.0, 0.0075 * math.sin(a))))
tail_start = pts[-1]
for k in range(1, 7):
    pts.append(tail_start + Vector((0.006 * k, -0.012 * k, -0.0012 * k if tail_start.z - 0.0012 * k > 0.003 else 0)))
cable = K.Part("Cable", start).tube(pts, 0.0021, "Chip_Rubber", sides=6).build(COLL)

# ================================================================ EMPTIES (the contract's points)
def empty(name, loc, parent):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = "PLAIN_AXES"
    e.empty_display_size = 0.02
    COLL.objects.link(e)
    e.location = loc
    return e


card_slot = empty("CardSlot", (0, FRONT_Y + 0.002, SLOT_Z), body)
pad_centre = empty("PadCentre", (PAD_C[0], PAD_C[1], pad_top + 0.0002), body)

PARTS = [body, pad_ob, flap, readout, vfd_glass, *LEDS, keys_ob, badge, slot_label, cable]
K.unwrap(PARTS)
bpy.context.view_layer.update()
for ob in PARTS[1:] + [card_slot, pad_centre]:
    mw = ob.matrix_world.copy()
    ob.parent = body
    ob.matrix_world = mw
ALL = PARTS + [card_slot, pad_centre]

bpy.context.view_layer.update()
lo, hi = K.world_bounds([o for o in PARTS if o is not cable])
REPORT = {
    "machine_footprint_unity_xz": (round(hi.x - lo.x, 4), round(hi.y - lo.y, 4)), "height": round(hi.z, 4),
    "card_slot_unity": tuple(round(a, 4) for a in K.to_unity(card_slot.location)),
    "pad_centre_unity": tuple(round(a, 4) for a in K.to_unity(pad_centre.location)),
    "pad_size": (PAD_W, PAD_D), "slot": (SLOT_W, SLOT_H),
    "tris": K.tris(PARTS), "tris_total": sum(K.tris(PARTS).values()),
}
print("REPORT", REPORT)

os.makedirs(OUT, exist_ok=True)
FBX = os.path.join(OUT, "ChipReader.fbx")
K.export_fbx(ALL, FBX, body)

# ================================================================ RENDERS
K.toonify()
cam, cam_d = K.setup_stage(scene, COLL, STAGE)
target = (0.0, -0.01, 0.035)
frames = K.turntable(scene, cam, cam_d, RENDERS, "chip_reader", target, 0.62, 28, 50)

# use shot 1: a transponder card half in the slot, READ lamp lit, the VFD reading
card_m = K.material("Stage_Card", "#C9D4E6", 0.4)
chip_m = K.material("Stage_CardChip", "#D4A055", 0.35)
stripe_m = K.material("Stage_CardStripe", "#4A5872", 0.4)
for m in (card_m, chip_m, stripe_m):
    pass
K.MAT_INFO[:] = [m for m in K.MAT_INFO if not m["name"].startswith("Stage_")]
card = K.Part("StageCard", (0, FRONT_Y + 0.022, SLOT_Z))
card.box((0, FRONT_Y + 0.022, SLOT_Z), (0.0856, 0.054, 0.0009), "Stage_Card")
card.box((0, FRONT_Y + 0.036, SLOT_Z + 0.0006), (0.0856, 0.010, 0.0004), "Stage_CardStripe")
card.box((0.024, FRONT_Y + 0.030, SLOT_Z + 0.0007), (0.012, 0.010, 0.0004), "Stage_CardChip")
card_ob = card.build(COLL)
K.toonify()
for led, on in zip(LEDS, (True, True, False)):
    m = led.data.materials[0]
    em = next((n for n in m.node_tree.nodes if n.type == "EMISSION"), None)
    if em is not None:
        em.inputs["Strength"].default_value = 3.0 if on else 0.35
K.render(scene, cam, cam_d, os.path.join(RENDERS, "chip_reader_use_card.png"), (0.30, 0.52, 0.30), (0.0, 0.03, 0.03), lens=55, res=(1200, 900))
bpy.data.objects.remove(card_ob, do_unlink=True)

# use shot 2: an open passport on the pad, FAULT lamp lit
pp_m = K.material("Stage_Passport", "#8A2F3B", 0.6)
pg_m = K.material("Stage_Page", "#FAF2E1", 0.7)
K.MAT_INFO[:] = [m for m in K.MAT_INFO if not m["name"].startswith("Stage_")]
pp = K.Part("StagePassport", (PAD_C[0], PAD_C[1], pad_top + 0.003))
pp.box((PAD_C[0], PAD_C[1], pad_top + 0.0012), (0.176, 0.125, 0.0024), "Stage_Passport")
pp.box((PAD_C[0] + 0.044, PAD_C[1], pad_top + 0.0028), (0.084, 0.119, 0.0012), "Stage_Page")
pp.box((PAD_C[0] - 0.044, PAD_C[1], pad_top + 0.0028), (0.084, 0.119, 0.0012), "Stage_Page")
pp_ob = pp.build(COLL)
K.toonify()
for led, on in zip(LEDS, (True, False, True)):
    m = led.data.materials[0]
    em = next((n for n in m.node_tree.nodes if n.type == "EMISSION"), None)
    if em is not None:
        em.inputs["Strength"].default_value = 3.0 if on else 0.35
K.render(scene, cam, cam_d, os.path.join(RENDERS, "chip_reader_use_passport.png"), (-0.28, 0.46, 0.42), (0.0, -0.02, 0.05), lens=50, res=(1200, 900))
bpy.data.objects.remove(pp_ob, do_unlink=True)

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, "ChipReader.blend"))

# ================================================================ VERIFY: re-import the FBX
import json  # noqa: E402

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, axis_forward="-Z", axis_up="Y")
bpy.context.view_layer.update()
ver = {}
for ob in bpy.context.scene.objects:
    lo2, hi2 = K.world_bounds([ob]) if ob.type == "MESH" else (ob.matrix_world.translation, ob.matrix_world.translation)
    ver[ob.name] = dict(type=ob.type, parent=ob.parent.name if ob.parent else None,
                        loc=[round(a, 4) for a in ob.matrix_world.translation],
                        lo=[round(a, 4) for a in lo2], hi=[round(a, 4) for a in hi2],
                        uv=(len(ob.data.uv_layers) if ob.type == "MESH" else None),
                        mats=[m.name for m in ob.data.materials] if ob.type == "MESH" else [])
ver["_report"] = REPORT
ver["_materials"] = K.MAT_INFO
ver["_frames"] = frames
with open(os.path.join(WORK, "verify_chip_reader.json"), "w") as fh:
    json.dump(ver, fh, indent=1, default=str)
print("VERIFY written")
fbm = os.path.join(OUT, "ChipReader.fbm")
if os.path.isdir(fbm):
    import shutil  # noqa: E402
    shutil.rmtree(fbm, ignore_errors=True)
