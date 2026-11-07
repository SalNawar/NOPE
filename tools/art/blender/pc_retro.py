"""The desk PC as a retro sci-fi 80s anime machine with a cyberpunk twist (Saleh 2026-11-07, 20:25 to 20:29).

Run headless:
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P pc_retro.py -- --out <fbx path>

The script builds every mesh by code and exports one FBX (Unity scale: 1 unit = 1 m, Y up, transforms applied).
The frame is the painted desk's (Desk_PC.fbx, anchors.json): x right, y away from the chair, z up, in metres,
with the origin on PC_Crt's pivot on the mat's pad. Unity reads it as (x, z, y), so the runtime puts the model's
root on PC_Crt's pivot, square to the desk (PaintedPcModel.Wear).

The contract (measured on Desk_PC.fbx in Blender 5.2):
  PC_Crt: pivot (-1.51426, 0.14181, 1.069); 0.66 x 0.50 m from above, turned 35 degrees about z; 0.576 m high.
  PC_Keyboard: pivot 0.5467 m toward the chair from PC_Crt's (dx -0.0045); 0.561 x 0.239 m, turned 15 degrees.
  The screen (anchors.json pc_screen): 0.36 x 0.29 m, its centre 0.215 m in front of the pivot and 0.355 m over
  the pad, facing the chair turned 35 degrees. The painted desk lays its own Screen quad there (PaintedCrt.Fit
  makes it the CRT dome); this model leaves the bezel's hole for it: the dome's rim (0.38 x 0.306 m, 9 mm behind
  the screen face) sits behind the bezel's face (7 mm behind the screen face).

Objects: PcRetro_Monitor (shell, bezel, rear tube, the deck under the monitor as its chin), PcRetro_Keyboard (slab,
keys, coiled cable). Material slots by name: Pc_Body, Pc_Cream, Pc_Ink, Pc_Neon, Pc_Glass, Pc_Keys. The ink
outline is an inverted hull (the mesh pushed out along its normals, faces reversed) in Pc_Ink.
"""
import math
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

SLOTS = ["Pc_Body", "Pc_Cream", "Pc_Ink", "Pc_Neon", "Pc_Glass", "Pc_Keys"]
BODY, CREAM, INK, NEON, GLASS, KEYS = range(6)

# The contract (see the docstring).
CRT_TURN = 35.0
KEYBOARD_TURN = 15.0
KEYBOARD_OFFSET = Vector((-0.0045, -0.5467, 0.0))
SCREEN_W, SCREEN_H, SCREEN_CORNER = 0.36, 0.29, 0.028
SCREEN_FACE_Y = -0.215
SCREEN_Z = 0.355
BEZEL_FACE_Y = SCREEN_FACE_Y + 0.007

# The ink hull's thickness (metres): about 1.5 px at the desk camera.
HULL = 0.0045


def append(bm, src):
    """Copies src's geometry into bm (material indices kept)."""
    me = bpy.data.meshes.new("tmp")
    src.to_mesh(me)
    bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def merge(bm, tmp, mat, hull):
    """Appends tmp to bm in material mat (and its inverted hull in Pc_Ink when asked), then frees tmp."""
    for f in tmp.faces:
        f.material_index = mat
    if hull:
        shell = tmp.copy()
        shell.normal_update()
        for v in shell.verts:
            v.co += v.normal * HULL
        bmesh.ops.reverse_faces(shell, faces=shell.faces)
        for f in shell.faces:
            f.material_index = INK
        append(bm, shell)
        shell.free()
    append(bm, tmp)
    tmp.free()


def box(bm, lo, hi, mat, bevel=0.0, segments=3, hull=False):
    """Adds an axis-aligned box from lo to hi (bevelled when asked)."""
    tmp = bmesh.new()
    bmesh.ops.create_cube(tmp, size=1.0)
    size = Vector(hi) - Vector(lo)
    centre = (Vector(hi) + Vector(lo)) / 2
    bmesh.ops.scale(tmp, vec=size, verts=tmp.verts)
    bmesh.ops.translate(tmp, vec=centre, verts=tmp.verts)
    if bevel > 0:
        bmesh.ops.bevel(tmp, geom=list(tmp.edges), offset=min(bevel, min(size) * 0.45), segments=segments,
                        profile=0.5, affect='EDGES', clamp_overlap=True)
    merge(bm, tmp, mat, hull)


def rounded_loop(cx, cz, w, h, r, seg=6):
    """A rounded rectangle's outline in the x-z plane (counter-clockwise from +x), 4 * (seg + 1) points."""
    pts = []
    corners = [(cx + w / 2 - r, cz + h / 2 - r, 0), (cx - w / 2 + r, cz + h / 2 - r, 90),
               (cx - w / 2 + r, cz - h / 2 + r, 180), (cx + w / 2 - r, cz - h / 2 + r, 270)]
    for (ox, oz, a0) in corners:
        for i in range(seg + 1):
            a = math.radians(a0 + 90.0 * i / seg)
            pts.append((ox + r * math.cos(a), oz + r * math.sin(a)))
    return pts


def ring(bm, outer, inner, y0, y1, mat):
    """A frame between two loops of the same count (outer, inner), from y0 (front) to y1 (back)."""
    tmp = bmesh.new()
    n = len(outer)
    of = [tmp.verts.new((x, y0, z)) for x, z in outer]
    ob = [tmp.verts.new((x, y1, z)) for x, z in outer]
    inf = [tmp.verts.new((x, y0, z)) for x, z in inner]
    ib = [tmp.verts.new((x, y1, z)) for x, z in inner]
    for i in range(n):
        j = (i + 1) % n
        tmp.faces.new((of[i], inf[i], inf[j], of[j]))
        tmp.faces.new((ob[j], ib[j], ib[i], ob[i]))
        tmp.faces.new((of[j], ob[j], ob[i], of[i]))
        tmp.faces.new((inf[i], ib[i], ib[j], inf[j]))
    bmesh.ops.recalc_face_normals(tmp, faces=tmp.faces)
    merge(bm, tmp, mat, False)


def slab(bm, loop, y0, y1, mat, hull=False):
    """A solid plate from a loop in the x-z plane, from y0 (front) to y1 (back)."""
    tmp = bmesh.new()
    front = [tmp.verts.new((x, y0, z)) for x, z in loop]
    back = [tmp.verts.new((x, y1, z)) for x, z in loop]
    tmp.faces.new(front)
    tmp.faces.new(list(reversed(back)))
    n = len(loop)
    for i in range(n):
        j = (i + 1) % n
        tmp.faces.new((front[i], front[j], back[j], back[i]))
    bmesh.ops.recalc_face_normals(tmp, faces=tmp.faces)
    merge(bm, tmp, mat, hull)


def disc_y(bm, x, z, y0, y1, r, mat, seg=20):
    """A round knob along y (its face toward the chair at y0)."""
    loop = [(x + r * math.cos(2 * math.pi * i / seg), z + r * math.sin(2 * math.pi * i / seg)) for i in range(seg)]
    slab(bm, loop, y0, y1, mat)


def tapered(bm, front_lo, front_hi, back_lo, back_hi, y0, y1, mat, hull=False):
    """A box whose front (y0) and back (y1) rectangles differ (the CRT's tube)."""
    tmp = bmesh.new()
    (fx0, fz0), (fx1, fz1) = front_lo, front_hi
    (bx0, bz0), (bx1, bz1) = back_lo, back_hi
    f = [tmp.verts.new(p) for p in ((fx0, y0, fz0), (fx1, y0, fz0), (fx1, y0, fz1), (fx0, y0, fz1))]
    b = [tmp.verts.new(p) for p in ((bx0, y1, bz0), (bx1, y1, bz0), (bx1, y1, bz1), (bx0, y1, bz1))]
    tmp.faces.new(f)
    tmp.faces.new(list(reversed(b)))
    for i in range(4):
        j = (i + 1) % 4
        tmp.faces.new((f[i], b[i], b[j], f[j]))
    bmesh.ops.recalc_face_normals(tmp, faces=tmp.faces)
    bmesh.ops.bevel(tmp, geom=list(tmp.edges), offset=0.018, segments=3, profile=0.5, affect='EDGES', clamp_overlap=True)
    merge(bm, tmp, mat, hull)


def build_monitor(bm):
    """The monitor on its deck, unturned: x right, -y toward the chair, z up, origin on the pivot at the pad."""
    # The deck under the monitor (its chin): the cartridge slot, the counter window, the status lamps, the vents.
    box(bm, (-0.33, -0.25, 0.004), (0.33, 0.25, 0.082), BODY, bevel=0.012, hull=True)
    box(bm, (-0.31, -0.2545, 0.066), (0.31, -0.20, 0.072), NEON)                     # the neon seam on the deck's lip
    box(bm, (-0.27, -0.256, 0.026), (-0.05, -0.245, 0.052), INK)                    # the cartridge slot
    box(bm, (-0.245, -0.275, 0.031), (-0.075, -0.245, 0.047), CREAM, bevel=0.003)   # the cartridge in it
    box(bm, (-0.215, -0.2765, 0.036), (-0.13, -0.274, 0.042), NEON)                 # its label stripe
    box(bm, (-0.03, -0.258, 0.026), (0.07, -0.248, 0.052), GLASS, bevel=0.002)      # the counter window
    for i, mat in enumerate((NEON, CREAM, NEON)):                                   # the three status lamps
        x = 0.09 + 0.02 * i
        box(bm, (x - 0.006, -0.262, 0.033), (x + 0.006, -0.250, 0.045), mat, bevel=0.002)
    for i in range(5):                                                              # the vents
        x = 0.16 + 0.028 * i
        box(bm, (x, -0.254, 0.020), (x + 0.013, -0.248, 0.058), INK)

    # The shell: dark gunmetal, chunky and rounded; the tube behind it, tapered.
    box(bm, (-0.33, -0.198, 0.084), (0.33, 0.075, 0.585), BODY, bevel=0.04, segments=4, hull=True)
    tapered(bm, (-0.25, 0.13), (0.25, 0.53), (-0.16, 0.21), (0.16, 0.46), 0.07, 0.248, BODY, hull=True)

    # The cream bezel: a thick chin, the hole for the CRT dome (the screen's 36 x 29 cm with 2.8 cm corners).
    outer = rounded_loop(0.0, 0.335, 0.62, 0.47, 0.045)
    inner = rounded_loop(0.0, SCREEN_Z, SCREEN_W, SCREEN_H, SCREEN_CORNER)
    ring(bm, outer, inner, BEZEL_FACE_Y, -0.192, CREAM)
    # The ink line round the glass, proud of the bezel; the bezel's outer ink edge.
    ring(bm, rounded_loop(0.0, SCREEN_Z, SCREEN_W + 0.016, SCREEN_H + 0.016, SCREEN_CORNER + 0.008), inner,
         BEZEL_FACE_Y - 0.0025, BEZEL_FACE_Y + 0.002, INK)
    ring(bm, rounded_loop(0.0, 0.335, 0.634, 0.484, 0.052), outer, BEZEL_FACE_Y + 0.0005, -0.190, INK)

    # The chin: one neon line, the worn decal, the speaker grille, the power knob (right and low, where
    # GlassFrame.PowerKnob puts its click).
    box(bm, (-0.24, BEZEL_FACE_Y - 0.004, 0.166), (0.16, BEZEL_FACE_Y + 0.001, 0.173), NEON)
    box(bm, (-0.275, BEZEL_FACE_Y - 0.003, 0.116), (-0.16, BEZEL_FACE_Y + 0.001, 0.150), INK, bevel=0.002)  # the decal
    box(bm, (-0.266, BEZEL_FACE_Y - 0.0045, 0.124), (-0.236, BEZEL_FACE_Y, 0.142), NEON)                   # its glyph
    box(bm, (-0.229, BEZEL_FACE_Y - 0.0045, 0.124), (-0.168, BEZEL_FACE_Y, 0.130), CREAM)                  # its worn stripes
    box(bm, (-0.229, BEZEL_FACE_Y - 0.0045, 0.136), (-0.196, BEZEL_FACE_Y, 0.142), CREAM)
    for i in range(4):
        z = 0.116 + 0.011 * i
        box(bm, (0.06, BEZEL_FACE_Y - 0.002, z), (0.19, BEZEL_FACE_Y + 0.001, z + 0.005), INK)
    disc_y(bm, 0.272, 0.226, BEZEL_FACE_Y - 0.014, BEZEL_FACE_Y + 0.002, 0.025, INK)                     # the knob's rim
    disc_y(bm, 0.272, 0.226, BEZEL_FACE_Y - 0.020, BEZEL_FACE_Y - 0.004, 0.019, CREAM)                   # the knob
    box(bm, (0.269, BEZEL_FACE_Y - 0.0215, 0.214), (0.275, BEZEL_FACE_Y - 0.019, 0.240), NEON)           # its mark

    # The cyberpunk twist: neon accent lines down both sides, side vents, the holo-strip on top.
    for side in (-1, 1):
        x = side * 0.3305
        box(bm, (x - 0.004, -0.17, 0.296), (x + 0.004, 0.05, 0.304), NEON)
        for i in range(5):
            z = 0.40 + 0.03 * i
            box(bm, (x - 0.003, -0.12, z), (x + 0.003, 0.02, z + 0.012), INK)
    box(bm, (-0.225, -0.177, 0.581), (0.225, -0.159, 0.588), INK)                    # the holo-strip's socket
    box(bm, (-0.21, -0.172, 0.584), (0.21, -0.164, 0.598), NEON)                     # the holo-strip


def build_keyboard(bm):
    """The slab keyboard, unturned, centred on its own pivot at the pad: big keycaps, a neon back edge."""
    half_w, half_d = 0.28, 0.115
    front_h, back_h = 0.026, 0.046
    tmp = bmesh.new()
    bmesh.ops.create_cube(tmp, size=1.0)
    for v in tmp.verts:
        x = half_w if v.co.x > 0 else -half_w
        y = half_d if v.co.y > 0 else -half_d
        z = 0.002 if v.co.z < 0 else (back_h if y > 0 else front_h)
        v.co = Vector((x, y, z))
    bmesh.ops.bevel(tmp, geom=list(tmp.edges), offset=0.008, segments=3, profile=0.5, affect='EDGES', clamp_overlap=True)
    merge(bm, tmp, BODY, True)

    def top(y):
        return front_h + (back_h - front_h) * (y + half_d) / (2 * half_d)

    # The cream deck plate the keys stand in.
    plate = bmesh.new()
    bmesh.ops.create_cube(plate, size=1.0)
    for v in plate.verts:
        x = 0.265 if v.co.x > 0 else -0.265
        y = 0.104 if v.co.y > 0 else -0.106
        z = top(y) + (0.003 if v.co.z > 0 else -0.004)
        v.co = Vector((x, y, z))
    merge(bm, plate, CREAM, False)

    pitch, cap = 0.046, 0.039
    rows = [0.074, 0.030, -0.014, -0.058]
    for r, y in enumerate(rows):
        for c in range(11):
            x = -0.23 + pitch * c
            mat = BODY if r == 0 else KEYS
            if r == 2 and c == 10:
                mat = NEON          # the EXEC key: the one teal key
            z0 = top(y) + 0.002
            box(bm, (x - cap / 2, y - cap / 2, z0), (x + cap / 2, y + cap / 2, z0 + 0.013), mat, bevel=0.004, segments=2)
    y = -0.092
    z0 = top(y) + 0.002
    box(bm, (-0.15, y - 0.016, z0), (0.15, y + 0.016, z0 + 0.012), KEYS, bevel=0.004, segments=2)       # the space bar
    box(bm, (0.19, y - 0.016, z0), (0.25, y + 0.016, z0 + 0.012), CREAM, bevel=0.004, segments=2)       # the cream bars
    box(bm, (-0.25, y - 0.016, z0), (-0.19, y + 0.016, z0 + 0.012), CREAM, bevel=0.004, segments=2)
    box(bm, (-0.26, half_d - 0.006, top(half_d) - 0.006), (0.26, half_d + 0.003, top(half_d)), NEON)    # the neon back edge


def coiled_cable(bm, start, end, coil=0.009, pitch=0.011, tube=0.0032, sides=6):
    """A coiled cable (Pc_Ink) from start to end along the pad."""
    axis = end - start
    length = axis.length
    d = axis.normalized()
    side = d.cross(Vector((0, 0, 1))).normalized()
    up = side.cross(d).normalized()
    turns = length / pitch
    steps = int(turns * 10)
    path = []
    for i in range(steps + 1):
        t = i / steps
        a = 2 * math.pi * turns * t
        path.append(start + axis * t + side * math.cos(a) * coil + up * math.sin(a) * coil)
    tmp = bmesh.new()
    rings = []
    for i, p in enumerate(path):
        tan = (path[min(i + 1, steps)] - path[max(i - 1, 0)]).normalized()
        n1 = tan.cross(Vector((0, 0, 1)))
        if n1.length < 1e-4:
            n1 = tan.cross(Vector((1, 0, 0)))
        n1.normalize()
        n2 = tan.cross(n1).normalized()
        rings.append([tmp.verts.new(p + (n1 * math.cos(2 * math.pi * k / sides) + n2 * math.sin(2 * math.pi * k / sides)) * tube)
                      for k in range(sides)])
    for i in range(steps):
        for k in range(sides):
            k2 = (k + 1) % sides
            tmp.faces.new((rings[i][k], rings[i][k2], rings[i + 1][k2], rings[i + 1][k]))
    bmesh.ops.recalc_face_normals(tmp, faces=tmp.faces)
    merge(bm, tmp, INK, False)


def make_object(name, bm, mats):
    """A mesh object from bm with the six slots in order (flat shaded: cel faces)."""
    me = bpy.data.meshes.new(name)
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    for p in me.polygons:
        p.use_smooth = False
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def turned(bm, degrees, offset=Vector((0, 0, 0))):
    """Turns bm about the vertical by degrees (counter-clockwise from above), then moves it by offset."""
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(degrees), 3, 'Z'))
    bmesh.ops.translate(bm, vec=offset, verts=bm.verts)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = argv[argv.index("--out") + 1]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    mats = [bpy.data.materials.new(name) for name in SLOTS]

    mon = bmesh.new()
    build_monitor(mon)
    turned(mon, CRT_TURN)

    kb = bmesh.new()
    build_keyboard(kb)
    turned(kb, KEYBOARD_TURN, KEYBOARD_OFFSET)
    # The coiled cable: from the keyboard's back, left of centre, to the deck's front, left (both turned).
    start = KEYBOARD_OFFSET + Matrix.Rotation(math.radians(KEYBOARD_TURN), 3, 'Z') @ Vector((-0.17, 0.118, 0.012))
    end = Matrix.Rotation(math.radians(CRT_TURN), 3, 'Z') @ Vector((-0.29, -0.252, 0.012))
    coiled_cable(kb, start, end)

    monitor = make_object("PcRetro_Monitor", mon, mats)
    keyboard = make_object("PcRetro_Keyboard", kb, mats)

    # The measurements for the contract (the frame, from PC_Crt's pivot).
    for obj in (monitor, keyboard):
        vs = [obj.matrix_world @ v.co for v in obj.data.vertices]
        lo = [min(v[i] for v in vs) for i in range(3)]
        hi = [max(v[i] for v in vs) for i in range(3)]
        print("PCRETRO", obj.name, "verts", len(vs), "tris", sum(len(p.vertices) - 2 for p in obj.data.polygons),
              "min", [round(x, 4) for x in lo], "max", [round(x, 4) for x in hi])
    c = Matrix.Rotation(math.radians(CRT_TURN), 3, 'Z') @ Vector((0.0, SCREEN_FACE_Y, SCREEN_Z))
    print("PCRETRO screen centre (frame, from the pivot)", [round(x, 4) for x in c], "size", SCREEN_W, SCREEN_H)

    bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={'MESH'}, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='FACE',
                             add_leaf_bones=False, bake_anim=False, use_custom_props=False)
    print("PCRETRO wrote", out)


main()
