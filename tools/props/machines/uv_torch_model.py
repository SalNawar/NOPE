"""
Time Sorter (NOPE) - the UV torch, a handheld blacklight and its desk cradle (spec 2026-10-17-desk-machines-design.md).

Run (background only; never open Blender's window, Saleh uses this PC):
    "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" -b --python uv_torch_model.py -- <render_dir>
Re-runnable: it starts from an empty scene and overwrites its outputs.

Writes Assets/Art/Office/Gameplay/Machines/UvTorch.fbx (+ textures from machine_textures.py),
UvTorch.blend and verify_uv_torch.json (the FBX re-imported) beside <render_dir>, and the turntable
frames + a use shot to <render_dir> (default: ./renders).

The torch is modelled along Blender +Y (the lens at the front), resting in its cradle; after export
(Unity axes) the lens points along local -Z. The prop contract:
    UvTorch     the torch's body: barrel, ribbed rubber grip, chrome tail cap and bezel, the switch's brass
                housing, the lanyard loop (root; its origin is the grip's centre, the hand's hold point)
    Switch      the red thumb slide on top of the grip (child; slides SWITCH_TRAVEL toward the lens = on)
    Lens        the violet lens face (child; texture UvTorch_Lens; its origin is the lens centre, where the
                game's cone starts; its emission is the on/off glow)
    Label       the wrap label round the barrel (child; texture UvTorch_Label)
    BeamFX      a soft violet cone from the lens (child; hidden at rest, a helper for the in-use look; the
                papers' reveal itself is the game's light mask)
    Cradle      the desk cradle (a separate root: it stays on the desk when the torch is lifted)
    CradleLabel the cradle's plate (child of Cradle; texture UvTorch_Cradle)
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


if not all(os.path.exists(tex(n)) for n in ("UvTorch_Label.png", "UvTorch_Cradle.png", "UvTorch_Lens.png")):
    for exe in ("python", "py"):
        try:
            subprocess.run([exe, os.path.join(HERE, "machine_textures.py")], check=True, timeout=180)
            break
        except Exception as e:  # noqa: BLE001
            print("texture helper failed with", exe, e)

scene, COLL, STAGE = K.reset_scene("UvTorch")

# ---------------------------------------------------------------- dimensions (Blender metres; the torch's axis is Y)
AXIS_Z = 0.034                # the axis's height over the desk while it rests in the cradle
GRIP_R = 0.0150
RIB_R = 0.0168
BARREL_R = 0.0165
HEAD_R = 0.0245
TAIL_Y, LENS_Y = -0.101, 0.0935
GRIP_C = -0.035               # the hand's hold point (the torch's origin)
SWITCH_Y0 = -0.020            # the slide's centre when off
SWITCH_TRAVEL = 0.010         # toward the lens = on

K.material("Torch_Body", "#4A5872", 0.45, note="barrel and head: kit slate (Desk Anime)")
K.material("Torch_Rubber", "#2B1C24", 0.85, note="ribbed grip, cradle pads: kit ink (Desk Anime)")
K.material("Torch_Chrome", "#C9CCD4", 0.18, metal=0.9, shade="#6E6880", note="tail cap, bezel (Desk Anime, light grey)")
K.material("Torch_Brass", "#D4A055", 0.4, metal=0.3, note="switch housing: kit brass")
K.material("Torch_Switch", "#C23A2E", 0.45, note="thumb slide: kit signal red")
K.material("Torch_Lens", "#5A2AB0", 0.1, emit="#A070FF", emit_strength=0.0, tex=tex("UvTorch_Lens.png"),
           note="lens face; URP Unlit with UvTorch_Lens.png; the game raises its emission when on")
K.material("Torch_Label", "#9886A8", 0.5, tex=tex("UvTorch_Label.png"), note="wrap label; UvTorch_Label.png")
K.material("Torch_Beam", "#8A5CFF", 0.5, alpha=0.18, emit="#9C6BFF", emit_strength=2.0,
           note="BeamFX: violet, additive or Unlit Transparent ~0.18, hidden at rest")
K.material("Cradle_Body", "#8A2F3B", 0.5, note="cradle: kit oxblood (Desk Anime)")
K.material("Cradle_Label", "#4A5872", 0.5, tex=tex("UvTorch_Cradle.png"), note="cradle plate; UvTorch_Cradle.png")

# Every torch part is laid with its axis on the world Y axis (z = 0), then lifted onto the cradle's axis
# height (AXIS_Z) as a whole, so the FBX holds the torch resting in its cradle.
O = Vector((0, GRIP_C, 0))   # the torch's origin: the grip's centre

# ================================================================ BODY (one lathe, tail to head) + details
body = K.Part("UvTorch", O)
body.lathe_y([(TAIL_Y + 0.009, GRIP_R), (0.013, GRIP_R), (0.017, BARREL_R), (0.058, BARREL_R),
              (0.073, HEAD_R), (0.088, HEAD_R)], "Torch_Body", segs=28)
body_ob = body.build(COLL)
det = K.Part("Detail", O)
for k in range(7):   # the ribbed rubber grip
    y0 = -0.086 + k * 0.0082
    det.ring_y([(y0, GRIP_R - 0.0006), (y0, RIB_R), (y0 + 0.0048, RIB_R), (y0 + 0.0048, GRIP_R - 0.0006)], "Torch_Rubber", segs=28)
det.lathe_y([(TAIL_Y, 0.0125), (TAIL_Y + 0.003, 0.0158), (TAIL_Y + 0.0105, 0.0158), (TAIL_Y + 0.0115, GRIP_R - 0.0005)],
            "Torch_Chrome", segs=28)   # tail cap
det.ring_y([(0.0865, HEAD_R - 0.0035), (0.0865, HEAD_R + 0.0016), (0.0965, HEAD_R + 0.0012), (0.0965, HEAD_R - 0.0028)],
           "Torch_Chrome", segs=28)    # bezel
det.box((0, SWITCH_Y0 + SWITCH_TRAVEL / 2, GRIP_R + 0.0018), (0.012, 0.024 + SWITCH_TRAVEL, 0.0050), "Torch_Brass")  # switch housing
det.torus_x((0, TAIL_Y - 0.0055, 0), 0.0060, 0.0016, "Torch_Chrome", segs=16, tube=8)                         # lanyard loop
K.merge(body_ob, det.build(COLL))
K.smooth(body_ob, 40)

# ================================================================ SWITCH, LENS, LABEL, BEAM (children)
sw_c = Vector((0, SWITCH_Y0, GRIP_R + 0.0060))
sw = K.Part("Switch", sw_c)
sw.box(sw_c, (0.0085, 0.0105, 0.0042), "Torch_Switch")
for k in (-1, 0, 1):
    sw.box(sw_c + Vector((0, k * 0.0028, 0.0024)), (0.0080, 0.0011, 0.0010), "Torch_Switch")
sw_ob = sw.build(COLL, bevel=0.0007, segments=2, angle=40)

lens = K.Part("Lens", (0, LENS_Y, 0)).disc_y(LENS_Y, HEAD_R - 0.0030, "Torch_Lens", facing=1, segs=28).build(COLL)
# the label: a sticker along the barrel on each side (the words run along the axis, toward the lens as seen
# from the +X side, the side the cradle turns to the camera; the -X copy reads toward the lens too)
label = K.Part("Label", (0, 0.040, 0))
label.arc_band_y(0.0185, 0.0555, BARREL_R + 0.0004, -12, 50, "Torch_Label", segs=10)
label.arc_band_y(0.0185, 0.0555, BARREL_R + 0.0004, 230, 168, "Torch_Label", segs=10, reverse_u=True)
label = label.build(COLL)
beam = K.Part("BeamFX", (0, LENS_Y + 0.002, 0)).ring_y([(LENS_Y + 0.002, HEAD_R - 0.004), (LENS_Y + 0.32, 0.105)],
                                                     "Torch_Beam", segs=28, closed=False).build(COLL)
TORCH = [body_ob, sw_ob, lens, label, beam]
for ob in TORCH:
    ob.location.z += AXIS_Z

# ================================================================ CRADLE (a separate root)
cr = K.Part("Cradle", (0, -0.010, 0))
cr.box((0, -0.010, 0.004), (0.090, 0.175, 0.008), "Cradle_Body")
for y in (-0.062, 0.012):
    cr.box((0.0, y, 0.0185), (0.050, 0.016, 0.021), "Cradle_Body")
cradle = cr.build(COLL, bevel=0.0025, segments=2, angle=30)
for y, r in ((-0.062, GRIP_R + 0.0012), (0.012, BARREL_R + 0.0012)):
    cut = K.Part("CradleCut", (0, y, AXIS_Z)).cyl((0, y, AXIS_Z), r, 0.04, "Cradle_Body", "Y", 28).build(STAGE)
    mod = cradle.modifiers.new("Cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cut
    try:
        mod.solver = "EXACT"
    except Exception:  # noqa: BLE001
        pass
    K.apply_mods(cradle)
    bpy.data.objects.remove(cut, do_unlink=True)
pads = K.Part("CradlePads", (0, -0.010, 0))
for y in (-0.062, 0.012):
    pads.box((0.0, y, AXIS_Z - (GRIP_R if y < 0 else BARREL_R) - 0.0006), (0.020, 0.012, 0.0016), "Torch_Rubber")
K.merge(cradle, pads.build(COLL))
K.smooth(cradle)
# the plate reads from the cradle's +X side (the side the game turns to the camera): text along +Y
cl_c = (0.0335, -0.010, 0.0081)
cradle_label = K.Part("CradleLabel", cl_c).quad(cl_c, (0.088, 0.020), "Cradle_Label",
                                                (Vector((0, 1, 0)), Vector((-1, 0, 0)), Vector((0, 0, 1)))).build(COLL)


PARTS = TORCH + [cradle, cradle_label]
K.unwrap(PARTS)
bpy.context.view_layer.update()
for ob in (sw_ob, lens, label, beam):
    mw = ob.matrix_world.copy()
    ob.parent = body_ob
    ob.matrix_world = mw
mw = cradle_label.matrix_world.copy()
cradle_label.parent = cradle
cradle_label.matrix_world = mw
bpy.context.view_layer.update()

lo, hi = K.world_bounds([body_ob, sw_ob, lens, label])
clo, chi = K.world_bounds([cradle])
REPORT = {
    "torch_length": round(hi.y - lo.y, 4), "torch_head_diameter": round(2 * (HEAD_R + 0.0016), 4),
    "torch_origin_unity": tuple(round(a, 4) for a in K.to_unity(body_ob.location)),
    "lens_centre_unity": tuple(round(a, 4) for a in K.to_unity(lens.matrix_world.translation)),
    "lens_forward_unity": (0, 0, -1),
    "switch_travel_unity_z": -SWITCH_TRAVEL,
    "cradle_footprint_unity_xz": (round(chi.x - clo.x, 4), round(chi.y - clo.y, 4)), "cradle_height": round(chi.z, 4),
    "tris": K.tris(PARTS), "tris_total": sum(K.tris(PARTS).values()),
}
print("REPORT", REPORT)

os.makedirs(OUT, exist_ok=True)
FBX = os.path.join(OUT, "UvTorch.fbx")
K.export_fbx(PARTS, FBX, body_ob)

# ================================================================ RENDERS
K.toonify()
cam, cam_d = K.setup_stage(scene, COLL, STAGE)
beam.hide_render = True
frames = K.turntable(scene, cam, cam_d, RENDERS, "uv_torch", (0.0, -0.005, 0.025), 0.42, 30, 50)

# use shot: the torch lifted and switched on over a paper: the violet cone, a watermark showing inside it
K.material("Stage_Paper", "#E2C994", 0.8)
K.material("Stage_Ink", "#6B5A4A", 0.8)
K.material("Stage_Pool", "#7A4CE0", 0.5, alpha=0.38, emit="#8E62FF", emit_strength=1.6)
K.material("Stage_Mark", "#C7A8FF", 0.5, emit="#D9C2FF", emit_strength=3.0)
K.MAT_INFO[:] = [m for m in K.MAT_INFO if not m["name"].startswith("Stage_")]
paper = K.Part("StagePaper", (0, 0.18, 0.0005))
paper.box((0, 0.18, 0.0005), (0.21, 0.28, 0.001), "Stage_Paper")
for i in range(9):   # printed lines
    paper.box((0.02 - (0.03 if i % 3 == 0 else 0), 0.09 + i * 0.022, 0.00115), (0.13 if i % 3 else 0.08, 0.0035, 0.0003), "Stage_Ink")
paper_ob = paper.build(STAGE)
for ob in (cradle, cradle_label):
    ob.hide_render = True
# lift and aim the torch: pivot on its grip, lens down at the paper
body_ob.location = Vector((0.09, 0.02, 0.17))
body_ob.rotation_euler = (math.radians(-48), 0.0, math.radians(28))
sw_ob.location.y += SWITCH_TRAVEL   # local: the slide pushed forward = on
beam.hide_render = False
bpy.context.view_layer.update()
lens_w = lens.matrix_world.translation
fwd = (lens.matrix_world.to_3x3() @ Vector((0, 1, 0))).normalized()
t = (0.0012 - lens_w.z) / fwd.z
hit = lens_w + fwd * t
beam.scale = (0.62, t / (0.318 - 0.002), 0.62)
pool = K.Part("StagePool", hit)
pool.cyl((hit.x, hit.y, 0.0013), 0.064, 0.0004, "Stage_Pool", "Z", 40)
mark = K.Part("StageMark", hit)
for k in range(3):   # a watermark: the Bureau's arch, and a ring of microprint dots
    pass
for k in range(40):   # the watermark's ring
    a = 2 * math.pi * k / 40
    mark.box((hit.x + math.cos(a) * 0.026, hit.y + math.sin(a) * 0.026, 0.0018), (0.0045, 0.0045, 0.0004), "Stage_Mark")
for x0, y0, w, h in ((-0.010, 0.002, 0.005, 0.022), (0.010, 0.002, 0.005, 0.022), (0.0, -0.010, 0.025, 0.005)):  # the arch
    mark.box((hit.x + x0, hit.y + y0, 0.0018), (w, h, 0.0004), "Stage_Mark")
for k in range(14):
    a = 2 * math.pi * k / 14
    mark.box((hit.x + math.cos(a) * 0.040, hit.y + math.sin(a) * 0.040, 0.0018), (0.004, 0.004, 0.0004), "Stage_Mark")
pool_ob = pool.build(STAGE)
mark_ob = mark.build(STAGE)
for m in ("Stage_Paper", "Stage_Ink", "Stage_Pool", "Stage_Mark"):
    pass
K.toonify()
lm = lens.data.materials[0]
em = next((n for n in lm.node_tree.nodes if n.type == "EMISSION"), None)
if em is not None:
    em.inputs["Strength"].default_value = 3.5
scene.world.node_tree.nodes.get("Background").inputs[0].default_value = (*K.hex_rgb("#B9A8C4"), 1)
K.render(scene, cam, cam_d, os.path.join(RENDERS, "uv_torch_use.png"), (0.34, 0.52, 0.42), (0.03, 0.10, 0.04), lens=45, res=(1200, 900))

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, "UvTorch.blend"))

# ================================================================ VERIFY: re-import the FBX
import json  # noqa: E402

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, axis_forward="-Z", axis_up="Y")
bpy.context.view_layer.update()
ver = {}
for ob in bpy.context.scene.objects:
    lo2, hi2 = K.world_bounds([ob])
    ver[ob.name] = dict(type=ob.type, parent=ob.parent.name if ob.parent else None,
                        loc=[round(a, 4) for a in ob.matrix_world.translation],
                        lo=[round(a, 4) for a in lo2], hi=[round(a, 4) for a in hi2],
                        uv=(len(ob.data.uv_layers) if ob.type == "MESH" else None),
                        mats=[m.name for m in ob.data.materials] if ob.type == "MESH" else [])
ver["_report"] = REPORT
ver["_materials"] = K.MAT_INFO
ver["_frames"] = frames
with open(os.path.join(WORK, "verify_uv_torch.json"), "w") as fh:
    json.dump(ver, fh, indent=1, default=str)
print("VERIFY written")
fbm = os.path.join(OUT, "UvTorch.fbm")
if os.path.isdir(fbm):
    import shutil  # noqa: E402
    shutil.rmtree(fbm, ignore_errors=True)
