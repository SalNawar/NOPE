"""The heavy brass stamp drawer (Track BR, Saleh 2026-10-08: "a heavy brass drawer
that makes the sound a typewriter makes when the carriage returns"), modelled
headless in Blender 5.2 and exported for Unity.

Run (re-runnable; it rebuilds everything from an empty scene):

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup \
        --python tools/props/brass_drawer.py -- <repo root> [<preview folder>]

It writes Assets/Art/Office/Props/BrassDrawer/BrassDrawer.fbx and its brushed
brass texture, and (with a preview folder) three Cycles previews of the open
drawer (front 3/4, side, top) plus one of it closed, on a neutral background.

The prop contract (DeskStampTray / BrassDrawer in the game; the builder checks
these names, PropArt.Missing, and falls back to the built rack without them):

    Tray                       the cast-brass drawer: front rail, domed end caps,
                               rings, rivets and filigree, side runners, back
                               plate, the bed rails the daters lie on, bearings
    CradleDenied/Approved      the yoke a dater stands in; origin on its pivot
                               (the dater's back-foot edge); turns about x
    PinionDenied/Approved      the gear on the cradle's shaft; origin on the shaft
    RackDenied/Approved        the toothed bar riding on the pinion (slides along z)
                               with the slotted block the lever's pin rides in
    LeverDenied/Approved       the arm whose pin drives the rack; origin on its
                               pivot (turns about y); modelled pointing along x
    PlateDenied/Approved       the enamel nameplates on the front rail (no words:
                               the game prints them, live TMP text); origin on the
                               face's centre

Everything is modelled in the OPEN pose (the daters upright) in the game's rack
space: metres, x to the right, y up, z away from the chair, the origin between
the two daters' feet. Blender's axes are (-x, -z, y) of it, so the FBX (forward
-Z, up Y, the space transform baked) lands in Unity exactly in that space.

The daters themselves are NOT in the file: the game builds them (the prop
contract of the daters) and stands them in the cradles. Their measures are
mirrored here: 68 x 46 mm, 81 mm tall, 140 mm apart (OfficeSceneUIBuilder).
"""

import math
import os
import sys

import bpy
import bmesh
from mathutils import Matrix, Vector

# --- the measures (metres, rack space). Keep in step with OfficeSceneUIBuilder.Hardware / BrassDrawer. ---
SPACING = 0.14            # the daters' distance apart (StampSpacing)
DATER_HW = 0.034          # a dater's half width
DATER_HD = 0.023          # a dater's half depth
DATER_H = 0.081           # a dater's height (frame top 0.033 + body 0.048)
PIVOT_Y, PIVOT_Z = 0.0, DATER_HD   # the cradle's pivot: the dater's back-foot edge
PINION_R = 0.011          # BrassDrawer.pinionRadius
PINION_X = 0.114          # the pinion's and the rack's x (outside each dater)
LEVER_L = 0.021           # BrassDrawer.leverLength
LEVER_X = 0.135           # the lever's pivot x
HALF = 0.152              # the drawer's half width (to the side runners' middle)
RAIL_R = 0.017            # the front rail's radius
RAIL_Y, RAIL_Z = 0.002, -0.044
PLATE_TILT = 35.0         # the nameplates' tilt toward the chair (degrees from flat)
PLATE_W, PLATE_H = 0.092, 0.017

TRAVEL = PINION_R * math.pi / 2.0          # the rack's travel over the cradle's quarter turn
RACK_LEN = 0.045
RACK_Y = PINION_R + 0.0025                 # the rack's middle height (on the pinion's top)
RACK_Z_UP = PIVOT_Z - 0.004                # the rack's middle along z, upright
PIN_DZ = RACK_LEN / 2.0 + 0.0045           # the slotted block's pin past the rack's middle
LEVER_Z = RACK_Z_UP + PIN_DZ + TRAVEL / 2.0  # the lever's pivot z (the pin's mid travel)


def u2b(v):
    """Rack space (x right, y up, z away from the chair) to Blender's (-x, -z, y)."""
    return Vector((-v[0], -v[2], v[1]))


# --- materials (named: the game maps each slot by its name to its own NOPE/Desk Anime material) ---

def material(name, colour, metallic, roughness):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*colour, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    m.diffuse_color = (*colour, 1.0)
    return m


def brushed_brass_texture(folder):
    """A 256 px brushed-brass albedo (soft streaks along u, a little mottling): the game's Brass material's _BaseMap."""
    size = 256
    img = bpy.data.images.new("BrassDrawer_Brass", size, size, alpha=False)
    px = []
    import random
    rnd = random.Random(2150)
    rows = [0.9 + 0.1 * rnd.random() for _ in range(size)]
    for y in range(size):
        streak = rows[y]
        for x in range(size):
            mottle = 0.04 * math.sin(x * 0.07 + y * 0.031) * math.sin(y * 0.05)
            v = max(0.0, min(1.0, streak + mottle + 0.03 * (rnd.random() - 0.5)))
            px.extend((v, v * 0.985, v * 0.96, 1.0))
    img.pixels = px
    path = os.path.join(folder, "BrassDrawer_Brass.png")
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    return img


# --- primitives in rack space ---

_parts = {}


def _finish(obj, mat, bevel, segments=2):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.limit_method = "ANGLE"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def _add(group, obj):
    _parts.setdefault(group, []).append(obj)
    return obj


def box(group, centre, size, mat, bevel=0.0012):
    """A box of rack-space size (x, y, z) centred at a rack-space point."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=u2b(centre))
    o = bpy.context.active_object
    o.scale = (size[0], size[2], size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return _add(group, _finish(o, mat, min(bevel, min(size) * 0.45)))


def cylinder(group, centre, radius, length, axis, mat, verts=28, bevel=0.0008):
    """A cylinder along a rack-space axis ('x', 'y' or 'z')."""
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=length, location=u2b(centre))
    o = bpy.context.active_object
    if axis == "x":
        o.rotation_euler = (0.0, math.radians(90), 0.0)
    elif axis == "z":
        o.rotation_euler = (math.radians(90), 0.0, 0.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return _add(group, _finish(o, mat, min(bevel, radius * 0.4, length * 0.4)))


def sphere(group, centre, radius, scale, mat):
    small = radius < 0.004  # a rivet or a pin head: a few faces are plenty
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10 if small else 28, ring_count=6 if small else 14, radius=radius, location=u2b(centre))
    o = bpy.context.active_object
    o.scale = (scale[0], scale[2], scale[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return _add(group, _finish(o, mat, 0.0))


def torus_x(group, centre, major, minor, mat):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=32, minor_segments=10, location=u2b(centre))
    o = bpy.context.active_object
    o.rotation_euler = (0.0, math.radians(90), 0.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return _add(group, _finish(o, mat, 0.0))


def rivet(group, centre, mat, r=0.0022, normal="y"):
    """A domed rivet head on a face whose outward normal is the rack-space axis given ('y' up, '-z' toward the chair)."""
    if normal == "y":
        return sphere(group, centre, r, (1.0, 0.5, 1.0), mat)
    return sphere(group, centre, r, (1.0, 1.0, 0.5), mat)


def gear(group, centre, radius, width, teeth, mat):
    """A spur gear on an axis along x."""
    cylinder(group, centre, radius * 0.86, width, "x", mat, verts=32)
    for i in range(teeth):
        a = 2.0 * math.pi * i / teeth
        c = (centre[0], centre[1] + math.cos(a) * radius * 0.93, centre[2] + math.sin(a) * radius * 0.93)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=u2b(c))
        o = bpy.context.active_object
        o.scale = (width, radius * 0.28, radius * 0.26)
        # Turn the tooth about the gear's axis (Blender x) so it points out radially.
        o.rotation_euler = (-a, 0.0, 0.0)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        _add(group, _finish(o, mat, 0.0004))
    cylinder(group, centre, radius * 0.32, width + 0.003, "x", mat, verts=20)


def scroll(group, centre, r, mat, flip=1.0):
    """A filigree scroll on the front rail's face: a half ring curling into a small ball (raised brass)."""
    bpy.ops.mesh.primitive_torus_add(major_radius=r, minor_radius=0.0011, major_segments=20, minor_segments=6,
                                     location=u2b(centre))
    o = bpy.context.active_object
    # Keep the half facing out of the rail's tilted face.
    bm = bmesh.new()
    bm.from_mesh(o.data)
    kill = [v for v in bm.verts if v.co.x * flip > 0.0]
    bmesh.ops.delete(bm, geom=kill, context="VERTS")
    bm.to_mesh(o.data)
    bm.free()
    o.rotation_euler = (math.radians(-PLATE_TILT), 0.0, 0.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    _add(group, _finish(o, mat, 0.0))
    sphere(group, centre, 0.0018, (1, 1, 1), mat)


def rail_point(angle_deg, out=0.0, x=0.0):
    """A point on the front rail's surface at angle_deg from straight up toward the chair (+out past it)."""
    a = math.radians(angle_deg)
    r = RAIL_R + out
    return (x, RAIL_Y + r * math.cos(a), RAIL_Z - r * math.sin(a))


# --- the build ---

def build(brass, dark, steel, red, green, plate_brass):
    side_x = HALF
    # The front rail: a fat cast tube with domed end caps, rings at the joins and a fluted band.
    cylinder("Tray", (0.0, RAIL_Y, RAIL_Z), RAIL_R, 2 * side_x, "x", brass, verts=40, bevel=0.001)
    for s in (-1.0, 1.0):
        cylinder("Tray", (s * (side_x + 0.012), RAIL_Y, RAIL_Z), RAIL_R + 0.002, 0.024, "x", brass, verts=40, bevel=0.0015)
        sphere("Tray", (s * (side_x + 0.024), RAIL_Y, RAIL_Z), RAIL_R + 0.002, (0.55, 1.0, 1.0), brass)
        torus_x("Tray", (s * side_x, RAIL_Y, RAIL_Z), RAIL_R + 0.0015, 0.0022, brass)
        torus_x("Tray", (s * (side_x + 0.024), RAIL_Y, RAIL_Z), RAIL_R + 0.0005, 0.0018, dark)
        # A screw boss on each cap's top.
        cylinder("Tray", (s * (side_x + 0.012), RAIL_Y + RAIL_R + 0.0018, RAIL_Z), 0.0045, 0.003, "y", dark, verts=20)
    # The centre escutcheon on the rail between the two plates, with its rivets and scrolls.
    plate_c = rail_point(PLATE_TILT, 0.0016)
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.0075, depth=0.003, location=u2b(plate_c))
    o = bpy.context.active_object
    o.rotation_euler = (math.radians(-PLATE_TILT), 0.0, 0.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    _add("Tray", _finish(o, dark, 0.0006))
    sphere("Tray", rail_point(PLATE_TILT, 0.0032), 0.003, (1, 1, 1), brass)
    for s in (-1.0, 1.0):
        scroll("Tray", rail_point(PLATE_TILT, 0.0016, s * 0.015), 0.0045, brass, flip=s)
        # Rivets at the plates' outer ends and along the rail's back.
        for x in (s * (SPACING / 2 + PLATE_W / 2 + 0.008), s * (SPACING / 2 - PLATE_W / 2 - 0.006)):
            sphere("Tray", rail_point(PLATE_TILT, 0.0006, x), 0.0021, (1, 1, 1), brass)
        for x in (s * 0.03, s * 0.11):
            sphere("Tray", rail_point(-40, 0.0005, x), 0.0018, (1, 1, 1), dark)

    # The side runners and the back plate (a frame: the papers under the drawer show through it).
    for s in (-1.0, 1.0):
        box("Tray", (s * side_x, 0.003, 0.034), (0.010, 0.022, 0.156), brass, 0.002)
        box("Tray", (s * side_x, -0.006, 0.034), (0.016, 0.004, 0.156), dark, 0.001)
        for z in (-0.02, 0.03, 0.08):
            rivet("Tray", (s * side_x, 0.0145, z), dark, 0.0018)
    box("Tray", (0.0, 0.004, 0.113), (2 * side_x + 0.01, 0.024, 0.012), brass, 0.002)
    for x in (-0.12, -0.04, 0.04, 0.12):
        rivet("Tray", (x, 0.0165, 0.113), dark, 0.0019)
    # The centre bridge between the two lanes and the decks under the mechanism.
    box("Tray", (0.0, -0.005, 0.04), (0.06, 0.006, 0.14), brass, 0.0015)
    for s in (-1.0, 1.0):
        box("Tray", (s * 0.126, -0.006, 0.04), (0.038, 0.004, 0.14), dark, 0.001)
        # The bed rails the lying dater rests on (its back), two a lane.
        for dx in (-0.022, 0.022):
            box("Tray", (s * SPACING / 2 + dx, -0.0075, 0.068), (0.008, 0.005, 0.074), brass, 0.0012)
        # The cross bars that tie the bed rails to the bridge and the decks.
        box("Tray", (s * SPACING / 2, -0.0075, 0.104), (0.07, 0.004, 0.006), dark, 0.001)
        # The bearings: a pillow block each side of the lane on the pivot's axis, and a cheek boss.
        for inner in (-1.0, 1.0):
            bx = s * SPACING / 2 + inner * (DATER_HW + 0.0075)
            box("Tray", (bx, -0.004, PIVOT_Z), (0.006, 0.012, 0.016), brass, 0.0015)
            cylinder("Tray", (bx, PIVOT_Y, PIVOT_Z), 0.0085, 0.0068, "x", brass, verts=32, bevel=0.001)
            cylinder("Tray", (bx + inner * 0.0036, PIVOT_Y, PIVOT_Z), 0.0035, 0.0012, "x", dark, verts=16)
        # The lever's pivot post.
        cylinder("Tray", (s * LEVER_X, RACK_Y - 0.006, LEVER_Z), 0.005, 0.016, "y", dark, verts=20)
        # The rack's guide: two rails along z either side of it.
        for g in (-1.0, 1.0):
            box("Tray", (s * PINION_X + g * 0.0062, RACK_Y - 0.003, RACK_Z_UP + TRAVEL / 2), (0.002, 0.008, RACK_LEN + TRAVEL + 0.004), dark, 0.0006)

    # The cradles (upright), their shafts, pinions, racks and levers.
    for name, s in (("Denied", -1.0), ("Approved", 1.0)):
        cx = s * SPACING / 2
        g = "Cradle" + name
        for side in (-1.0, 1.0):
            px = cx + side * (DATER_HW + 0.0022)
            # The side cheek: a plate hugging the dater's frame, and a round ear on the pivot.
            box(g, (px, 0.017, PIVOT_Z - 0.019), (0.003, 0.03, 0.03), brass, 0.001)
            cylinder(g, (px, PIVOT_Y + 0.001, PIVOT_Z - 0.002), 0.012, 0.0034, "x", brass, verts=32, bevel=0.0008)
            sphere(g, (px + side * 0.0018, 0.026, PIVOT_Z - 0.026), 0.0016, (1, 1, 1), dark)
        # The back plate the dater leans on, and the shaft through both bearings to the pinion.
        box(g, (cx, 0.016, PIVOT_Z + 0.0022), (2 * DATER_HW + 0.008, 0.03, 0.0035), brass, 0.001)
        shaft_from, shaft_to = cx - s * (DATER_HW + 0.012), s * (PINION_X + 0.006)
        cylinder(g, ((shaft_from + shaft_to) / 2, PIVOT_Y, PIVOT_Z), 0.0028, abs(shaft_to - shaft_from), "x", steel, verts=16)
        gear("Pinion" + name, (s * PINION_X, PIVOT_Y, PIVOT_Z), PINION_R, 0.006, 12, brass)
        # The rack on the pinion's top, its teeth underneath, and the slotted block at its back end.
        r = "Rack" + name
        box(r, (s * PINION_X, RACK_Y + 0.0005, RACK_Z_UP), (0.0085, 0.004, RACK_LEN), brass, 0.0008)
        n = 15
        for i in range(n):
            z = RACK_Z_UP - RACK_LEN / 2 + (i + 0.5) * RACK_LEN / n
            box(r, (s * PINION_X, RACK_Y - 0.002, z), (0.0085, 0.0018, RACK_LEN / n * 0.5), brass, 0.0003)
        pin_z = RACK_Z_UP + PIN_DZ
        for g2 in (-1.0, 1.0):
            box(r, (s * PINION_X, RACK_Y + 0.0012, pin_z + g2 * 0.0035), (0.016, 0.005, 0.0025), dark, 0.0005)
        # The lever: a boss on its pivot, a tapered arm toward the rack (along x) and the pin riding in the slot.
        lv = "Lever" + name
        toward = -s  # the arm points toward the middle
        cylinder(lv, (s * LEVER_X, RACK_Y + 0.004, LEVER_Z), 0.0058, 0.004, "y", brass, verts=28)
        box(lv, (s * LEVER_X + toward * LEVER_L / 2, RACK_Y + 0.004, LEVER_Z), (LEVER_L, 0.003, 0.0055), brass, 0.001)
        cylinder(lv, (s * LEVER_X + toward * LEVER_L, RACK_Y + 0.0015, LEVER_Z), 0.0019, 0.008, "y", steel, verts=16)
        sphere(lv, (s * LEVER_X, RACK_Y + 0.0062, LEVER_Z), 0.0028, (1, 0.6, 1), dark)
        # The handle stub past the pivot (the lever reads as a lever).
        box(lv, (s * LEVER_X - toward * 0.006, RACK_Y + 0.004, LEVER_Z), (0.012, 0.0028, 0.004), brass, 0.0008)

        # The enamel nameplate: a brass bezel and the inlay a step lower; no words (the game prints them).
        p = "Plate" + name
        enamel = red if name == "Denied" else green
        _plate(p, rail_point(PLATE_TILT, 0.0016, cx), plate_brass, enamel)


def _plate(group, centre, bezel, enamel):
    """A rounded-rectangle nameplate lying in its own xz plane, tilted PLATE_TILT toward the chair."""
    w, h = PLATE_W, PLATE_H
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0))
    o = bpy.context.active_object
    o.scale = (w, h, 0.0024)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    _finish(o, bezel, 0.003, 4)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.0009))
    i = bpy.context.active_object
    i.scale = (w - 0.006, h - 0.0055, 0.0012)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    _finish(i, enamel, 0.0018, 3)
    for sx in (-1.0, 1.0):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.0014, location=(sx * (w / 2 - 0.0028), 0, 0.0012))
        rv = bpy.context.active_object
        rv.scale = (1, 1, 0.5)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        _finish(rv, bezel, 0.0)
        _add(group, rv)
    _add(group, o)
    _add(group, i)
    # Lying flat its face is Blender +z (rack +y), its long side Blender x (rack x), its height Blender y (rack z).
    tilt = Matrix.Rotation(math.radians(-PLATE_TILT), 4, "X")  # its top edge tips away from the chair, the face toward it
    move = Matrix.Translation(u2b(centre))
    for part in _parts[group][-4:]:
        part.data.transform(tilt)
        part.data.transform(move)
        part.data.update()


def join(group, origin_rack):
    objs = _parts[group]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = group
    obj.data.name = group
    bpy.context.scene.cursor.location = u2b(origin_rack)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(38))
    return obj


def stand_in_daters(frame_mat, red_body, green_body, window):
    """Preview-only daters (not exported): a white frame and a red or green body, upright or lying on their backs."""
    made = []
    for name, s, body in (("Denied", -1.0, red_body), ("Approved", 1.0, green_body)):
        cx = s * SPACING / 2
        parts = []
        for g in (-1.0, 1.0):
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=u2b((cx + g * (DATER_HW - 0.0025), 0.0165, 0.0)))
            o = bpy.context.active_object
            o.scale = (0.005, 2 * DATER_HD, 0.031)
            parts.append(_finish(o, frame_mat, 0.001))
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=u2b((cx, 0.033 + 0.0125, 0.0)))
        o = bpy.context.active_object
        o.scale = (2 * DATER_HW - 0.002, 2 * DATER_HD, 0.025)
        parts.append(_finish(o, body, 0.004, 3))
        bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=DATER_HD, depth=2 * DATER_HW - 0.005, location=u2b((cx, 0.058, 0.0)))
        o = bpy.context.active_object
        o.rotation_euler = (0.0, math.radians(90), 0.0)
        parts.append(_finish(o, body, 0.0))
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=u2b((cx, DATER_H + 0.0004, 0.0)))
        o = bpy.context.active_object
        o.scale = (DATER_HW * 1.4, DATER_HD * 0.9, 0.001)
        parts.append(_finish(o, window, 0.0))
        for w, dx in ((0.012, -0.017), (0.016, -0.002), (0.02, 0.016)):
            bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.0095, depth=w, location=u2b((cx + dx, 0.0175, 0.0)))
            o = bpy.context.active_object
            o.rotation_euler = (0.0, math.radians(90), 0.0)
            parts.append(_finish(o, bpy.data.materials["Steel"], 0.0))
        for o in parts:
            o.select_set(False)
        bpy.ops.object.select_all(action="DESELECT")
        for o in parts:
            o.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        bpy.ops.object.join()
        d = bpy.context.active_object
        d.name = "StandIn" + name
        bpy.context.scene.cursor.location = u2b((cx, PIVOT_Y, PIVOT_Z))
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        made.append(d)
    return made


def pose(objs, raise_):
    """Poses the mechanism for a raise of 0 (flat) to 1 (upright): the same kinematics as BrassDrawer.Pose in the game."""
    theta = math.radians(90.0 * (1.0 - raise_))
    for name, s in (("Denied", -1.0), ("Approved", 1.0)):
        # Rack +x rotation by theta (the top toward +z) is Blender's rotation about x by theta as well ((-x,-z,y) keeps the x axis' sense).
        for key in ("Cradle", "Pinion", "StandIn"):
            o = objs.get(key + name)
            if o is not None:
                o.rotation_euler = (theta, 0.0, 0.0)
        rack = objs["Rack" + name]
        dz = PINION_R * theta
        base = rack.get("base")
        rack.location = Vector(base) + u2b((0.0, 0.0, dz))
        pin_z = RACK_Z_UP + PIN_DZ + dz
        a = math.asin(max(-1.0, min(1.0, (pin_z - LEVER_Z) / LEVER_L)))
        lever = objs["Lever" + name]
        toward = -s
        # The tip sits at pivot + L*(toward*cos a, 0, sin a) in rack space: pick the Blender z turn that puts it there.
        best = None
        for sign in (1.0, -1.0):
            lever.rotation_euler = (0.0, 0.0, sign * a)
            bpy.context.view_layer.update()
            tip = lever.matrix_world @ u2b((toward * LEVER_L, 0.0, 0.0))
            want = u2b((s * LEVER_X + toward * LEVER_L * math.cos(a), 0.0, pin_z))
            err = (Vector((tip.x, tip.y)) - Vector((want.x, want.y))).length
            if best is None or err < best[0]:
                best = (err, sign)
        lever.rotation_euler = (0.0, 0.0, best[1] * a)
    bpy.context.view_layer.update()


def preview(folder, objs):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 64
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 1400, 1000
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("Neutral")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.42, 0.42, 0.43, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.35
    scene.world = world
    # A neutral desk-grey floor 3 cm under the daters' feet (where the desk is).
    bpy.ops.mesh.primitive_plane_add(size=3.0, location=u2b((0.0, -0.03, 0.0)))
    floor = bpy.context.active_object
    fm = material("PreviewFloor", (0.5, 0.5, 0.5), 0.0, 0.8)
    floor.data.materials.append(fm)
    bpy.ops.object.light_add(type="AREA", location=u2b((-0.25, 0.55, -0.45)))
    key = bpy.context.active_object
    key.data.energy = 7.0
    key.data.size = 0.6
    key.rotation_euler = (Vector((0, 0, 0)) - key.location).to_track_quat("-Z", "Y").to_euler()
    bpy.ops.object.light_add(type="AREA", location=u2b((0.4, 0.3, 0.5)))
    fill = bpy.context.active_object
    fill.data.energy = 2.5
    fill.data.size = 0.8
    fill.rotation_euler = (Vector((0, 0, 0)) - fill.location).to_track_quat("-Z", "Y").to_euler()

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.lens = 50
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    def shot(name, at_rack, look_rack, straight_down=False):
        cam.location = u2b(at_rack)
        if straight_down:
            cam.rotation_euler = (0.0, 0.0, math.pi)  # looking down, the far side (the counter) at the top, as in the reading view
        else:
            cam.rotation_euler = (u2b(look_rack) - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(folder, name)
        bpy.ops.render.render(write_still=True)

    look = (0.0, 0.02, 0.03)
    pose(objs, 1.0)
    shot("brass_drawer_front34.png", (-0.32, 0.36, -0.52), look)
    shot("brass_drawer_side.png", (0.62, 0.1, 0.03), look)
    shot("brass_drawer_top.png", (0.0, 0.72, 0.03), look, straight_down=True)
    pose(objs, 0.0)
    shot("brass_drawer_closed_front34.png", (-0.32, 0.36, -0.52), look)
    pose(objs, 1.0)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    root = argv[0] if argv else os.getcwd()
    previews = argv[1] if len(argv) > 1 else None
    out = os.path.join(root, "Assets", "Art", "Office", "Props", "BrassDrawer")
    os.makedirs(out, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0

    brass = material("Brass", (0.80, 0.56, 0.22), 1.0, 0.32)
    dark = material("BrassDark", (0.48, 0.32, 0.12), 1.0, 0.42)
    steel = material("Steel", (0.32, 0.32, 0.34), 1.0, 0.35)
    red = material("EnamelRed", (0.62, 0.08, 0.07), 0.0, 0.18)
    green = material("EnamelGreen", (0.07, 0.36, 0.17), 0.0, 0.18)
    tex = brushed_brass_texture(out)
    nodes = brass.node_tree.nodes
    t = nodes.new("ShaderNodeTexImage")
    t.image = tex
    mix = nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    mix.inputs[6].default_value = (0.80, 0.56, 0.22, 1.0)
    brass.node_tree.links.new(t.outputs["Color"], mix.inputs[7])
    brass.node_tree.links.new(mix.outputs[2], nodes["Principled BSDF"].inputs["Base Color"])

    build(brass, dark, steel, red, green, brass)

    origins = {
        "Tray": (0.0, 0.0, 0.0),
        "CradleDenied": (-SPACING / 2, PIVOT_Y, PIVOT_Z),
        "CradleApproved": (SPACING / 2, PIVOT_Y, PIVOT_Z),
        "PinionDenied": (-PINION_X, PIVOT_Y, PIVOT_Z),
        "PinionApproved": (PINION_X, PIVOT_Y, PIVOT_Z),
        "RackDenied": (-PINION_X, RACK_Y, RACK_Z_UP),
        "RackApproved": (PINION_X, RACK_Y, RACK_Z_UP),
        "LeverDenied": (-LEVER_X, RACK_Y, LEVER_Z),
        "LeverApproved": (LEVER_X, RACK_Y, LEVER_Z),
        "PlateDenied": rail_point(PLATE_TILT, 0.0016, -SPACING / 2),
        "PlateApproved": rail_point(PLATE_TILT, 0.0016, SPACING / 2),
    }
    objs = {name: join(name, o) for name, o in origins.items()}
    for name in ("Tray", "CradleDenied", "CradleApproved"):
        bpy.ops.object.select_all(action="DESELECT")
        objs[name].select_set(True)
        bpy.context.view_layer.objects.active = objs[name]
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.01)
        bpy.ops.object.mode_set(mode="OBJECT")

    # Export (open pose, every part at rest; the game poses the mechanism).
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs.values():
        o.select_set(True)
    fbx = os.path.join(out, "BrassDrawer.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH"}, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True, axis_forward="-Z", axis_up="Y",
                             mesh_smooth_type="FACE", use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False,
                             path_mode="STRIP", embed_textures=False, use_custom_props=False)
    print("[brass_drawer] wrote", fbx)
    for name, o in objs.items():
        tri = sum(len(p.vertices) - 2 for p in o.data.polygons)
        print(f"[brass_drawer] {name}: {tri} tris")

    if previews:
        os.makedirs(previews, exist_ok=True)
        for name in ("RackDenied", "RackApproved"):
            objs[name]["base"] = list(objs[name].location)
        frame = material("PreviewDaterFrame", (0.9, 0.9, 0.88), 0.0, 0.4)
        redb = material("PreviewDaterRed", (0.52, 0.11, 0.12), 0.0, 0.25)
        greenb = material("PreviewDaterGreen", (0.13, 0.38, 0.21), 0.0, 0.25)
        window = material("PreviewDaterWindow", (0.86, 0.87, 0.88), 0.0, 0.2)
        for d in stand_in_daters(frame, redb, greenb, window):
            objs[d.name] = d
        preview(previews, objs)


main()
