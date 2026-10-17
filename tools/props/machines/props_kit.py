"""
Time Sorter (NOPE) - extra Blender mesh helpers for the upgrade desk props (upgrade_props_model.py).

They extend machine_kit.Part with shapes the chip reader and torch did not need (lathes about Z, a
half-shell, cylinders and rings on any axis, flat discs on any plane, quads with a UV window, extruded
polygons), plus a boolean cut that keeps the cutter's material, empties, and parenting that keeps the
world pose. All coordinates are Blender world metres; every function honours the Part's origin.
Runs inside Blender only (background mode).
"""
import math

import bmesh
import bpy
from mathutils import Matrix, Vector

import machine_kit as K


def _v(part, p):
    return part.bm.verts.new(Vector(p) - part.origin)


def quad_uv(part, centre, size, mat, axes, uv0=(0.0, 0.0), uv1=(1.0, 1.0)):
    """A quad like Part.quad, but its UVs span the window uv0..uv1 (U along axes[0], V along axes[1])."""
    c = Vector(centre)
    r, u, _ = (Vector(a).normalized() for a in axes)
    w, h = size
    corners = [(-1, -1, (uv0[0], uv0[1])), (1, -1, (uv1[0], uv0[1])), (1, 1, (uv1[0], uv1[1])), (-1, 1, (uv0[0], uv1[1]))]
    vs = [_v(part, c + r * (sx * w / 2) + u * (sy * h / 2)) for sx, sy, _ in corners]
    f = part.bm.faces.new(vs)
    f.material_index = part._mi(mat)
    f[part.fixed] = 1
    for loop, (_, _, uv) in zip(f.loops, corners):
        loop[part.uv].uv = uv
    part.has_explicit_uv = True
    return part


def basis_from(axis):
    """A rotation taking +Z onto `axis`."""
    return Vector(axis).normalized().to_track_quat("Z", "Y").to_matrix()


def cyl_axis(part, centre, radius, length, mat, axis, segs=16, radius2=None):
    """A capped cylinder (or cone with radius2) centred on `centre`, its axis along any vector."""
    rot = basis_from(axis).to_4x4()
    res = bmesh.ops.create_cone(part.bm, cap_ends=True, cap_tris=False, segments=segs, radius1=radius,
                                radius2=radius if radius2 is None else radius2, depth=length,
                                matrix=Matrix.Translation(Vector(centre) - part.origin) @ rot)
    mi = part._mi(mat)
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mi
    return part


def lathe_z(part, cxy, profile, mat, segs=20, cap_lo=True, cap_hi=True):
    """Revolve a (z, r) profile about the vertical axis through (cx, cy); caps where r > 0."""
    mi = part._mi(mat)
    cx, cy = cxy
    rings = []
    for z, r in profile:
        rings.append([_v(part, (cx + r * math.cos(2 * math.pi * i / segs), cy + r * math.sin(2 * math.pi * i / segs), z))
                      for i in range(segs)])
    for k in range(len(rings) - 1):
        a, b = rings[k], rings[k + 1]
        for i in range(segs):
            j = (i + 1) % segs
            part.bm.faces.new([a[i], a[j], b[j], b[i]]).material_index = mi
    if cap_lo:
        part.bm.faces.new(list(reversed(rings[0]))).material_index = mi
    if cap_hi:
        part.bm.faces.new(rings[-1]).material_index = mi
    return part


def shell_z(part, cxy, r_in, r_out, z0, z1, a0_deg, a1_deg, mat, segs=10):
    """A closed curved plate: the arc a0..a1 (0 = +X, 90 = +Y) between r_in and r_out, from z0 to z1."""
    mi = part._mi(mat)
    cx, cy = cxy
    grid = []
    for i in range(segs + 1):
        a = math.radians(a0_deg + (a1_deg - a0_deg) * i / segs)
        ca, sa = math.cos(a), math.sin(a)
        grid.append([_v(part, (cx + r * ca, cy + r * sa, z)) for r, z in ((r_out, z0), (r_out, z1), (r_in, z1), (r_in, z0))])
    for i in range(segs):
        a, b = grid[i], grid[i + 1]
        for k in range(4):
            k1 = (k + 1) % 4
            part.bm.faces.new([a[k], b[k], b[k1], a[k1]]).material_index = mi
    part.bm.faces.new(grid[0]).material_index = mi
    part.bm.faces.new(list(reversed(grid[-1]))).material_index = mi
    return part


def torus_axis(part, centre, major, minor, mat, axis, segs=20, tube=6):
    """A ring about any axis."""
    mi = part._mi(mat)
    rot = basis_from(axis)
    grid = []
    for i in range(segs):
        a = 2 * math.pi * i / segs
        ring = []
        for j in range(tube):
            b = 2 * math.pi * j / tube
            rr = major + minor * math.cos(b)
            ring.append(_v(part, Vector(centre) + rot @ Vector((rr * math.cos(a), rr * math.sin(a), minor * math.sin(b)))))
        grid.append(ring)
    for i in range(segs):
        a, b = grid[i], grid[(i + 1) % segs]
        for j in range(tube):
            j1 = (j + 1) % tube
            part.bm.faces.new([a[j], b[j], b[j1], a[j1]]).material_index = mi
    return part


def disc(part, centre, radius, mat, axes, segs=24):
    """A flat disc in the plane of axes[0], axes[1], facing axes[0] x axes[1], planar UVs (0..1 over its box)."""
    mi = part._mi(mat)
    c = Vector(centre)
    r, u, _ = (Vector(a).normalized() for a in axes)
    cv = _v(part, c)
    ring = []
    for i in range(segs):
        t = 2 * math.pi * i / segs
        ring.append((_v(part, c + r * (radius * math.cos(t)) + u * (radius * math.sin(t))),
                     (0.5 + 0.5 * math.cos(t), 0.5 + 0.5 * math.sin(t))))
    for i in range(segs):
        (va, uva), (vb, uvb) = ring[i], ring[(i + 1) % segs]
        f = part.bm.faces.new([cv, va, vb])
        f.material_index = mi
        f[part.fixed] = 1
        for loop, uv in zip(f.loops, ((0.5, 0.5), uva, uvb)):
            loop[part.uv].uv = uv
    part.has_explicit_uv = True
    return part


def prism(part, pts, vec, mat):
    """Extrude the planar polygon `pts` (3-D points) by the vector `vec`; a closed solid."""
    mi = part._mi(mat)
    a = [_v(part, p) for p in pts]
    b = [_v(part, Vector(p) + Vector(vec)) for p in pts]
    part.bm.faces.new(a).material_index = mi
    part.bm.faces.new(list(reversed(b))).material_index = mi
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        part.bm.faces.new([a[i], a[j], b[j], b[i]]).material_index = mi
    return part


def cut(ob, centre, size, stage, mat, axes=K.AXIS):
    """Boolean-subtract a box; the new faces take the cutter's material (`mat`, e.g. a dark bay lining)."""
    cutter = K.Part(ob.name + "_cut", centre).box(centre, size, mat, axes).build(stage)
    mod = ob.modifiers.new("Cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cutter
    for attr, val in (("solver", "EXACT"), ("material_mode", "TRANSFER")):
        try:
            setattr(mod, attr, val)
        except Exception:  # noqa: BLE001
            pass
    K.apply_mods(ob)
    bpy.data.objects.remove(cutter, do_unlink=True)


def empty(coll, name, loc, size=0.02):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = "PLAIN_AXES"
    e.empty_display_size = size
    coll.objects.link(e)
    e.location = loc
    return e


def parent_keep(child, parent):
    bpy.context.view_layer.update()
    mw = child.matrix_world.copy()
    child.parent = parent
    child.matrix_world = mw


def export_fbx_nested(objs, path, active):
    """K.export_fbx, made safe for hierarchies deeper than root -> child.

    Blender 5.2's FBX exporter (io_scene_fbx fbx_utils.fbx_object_matrix) mis-writes the local transform of an
    object whose parent itself has a parent when bake_space_transform is on: it writes
    G P^-1 G^-1 P C G^-1 instead of G C G^-1 (G = the axis conversion, P / C = the parent's / object's Blender
    local matrices), so a grandchild lands rotated 90 degrees and displaced in Unity. Depth-1 children are right.
    Here every object deeper than that gets its Blender local matrix pre-compensated
    (C' = P'^-1 G P C, P' = the parent's local as handed to the exporter) for the export only, then restored.
    """
    from bpy_extras.io_utils import axis_conversion
    G = axis_conversion(to_forward="-Z", to_up="Y").to_4x4()
    obs = [o for o in objs]

    def depth(o):
        d = 0
        while o.parent is not None:
            o, d = o.parent, d + 1
        return d

    true_local = {o.name: o.matrix_local.copy() for o in obs}
    sent = {}
    for o in sorted(obs, key=depth):
        p = o.parent
        if p is not None and p.parent is not None:
            m = sent.get(p.name, true_local[p.name]).inverted() @ G @ true_local[p.name] @ true_local[o.name]
            sent[o.name] = m
    for o in sorted(obs, key=depth):
        if o.name in sent:
            o.matrix_local = sent[o.name]
            bpy.context.view_layer.update()
    try:
        K.export_fbx(objs, path, active)
    finally:
        for o in sorted(obs, key=depth):
            o.matrix_local = true_local[o.name]
            bpy.context.view_layer.update()
