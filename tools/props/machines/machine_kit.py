"""
Time Sorter (NOPE) - shared Blender helpers for the desk machines (chip reader, UV torch).

Imported by chip_reader_model.py and uv_torch_model.py, which run in Blender's background mode:
    "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" -b --python <script>.py
Never open Blender's window (Saleh uses this PC).

Conventions (as tools/props/scanner/scanner_model.py): Blender metres, Z up, the machine's FRONT
(the side facing the office camera / the chair) is Blender +Y. Exported with axis_forward=-Z,
axis_up=Y and the transform baked, Unity gets  unity = (-x, z, -y): the front faces Unity local -Z.

Materials are Principled BSDF for the FBX (base colour, emission, alpha read by Unity's importer);
for the turntable renders they are swapped for a two-tone cel ramp (purple-tinted shadows) with
Freestyle ink lines, the kit's look (80s/90s anime cel).
"""
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

# ---------------------------------------------------------------- the UI kit palette (Saleh's, run 7)
KIT = dict(oxblood="#8A2F3B", slate="#4A5872", signal="#C23A2E", brass="#D4A055", manila="#E2C994",
           bone="#EEE5D0", lavender="#9886A8", ink="#2B1C24", card="#FAF2E1")

AXIS = (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))


def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(srgb_to_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def reset_scene(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    coll = bpy.data.collections.new(name)
    scene.collection.children.link(coll)
    stage = bpy.data.collections.new("Stage")
    scene.collection.children.link(stage)
    return scene, coll, stage


MATS = {}
MAT_INFO = []


def material(name, hexcol, rough=0.55, alpha=1.0, emit=None, emit_strength=0.0, metal=0.0, tex=None, note="",
             shade=None):
    """A Principled material for the FBX; `shade` is the cel shadow colour for the renders (default: a purple-tinted darkening)."""
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:  # noqa: BLE001
        pass
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    rgb = hex_rgb(hexcol)
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Alpha"].default_value = alpha
    m.diffuse_color = (*rgb, alpha)
    if emit:
        bsdf.inputs["Emission Color"].default_value = (*hex_rgb(emit), 1.0)
        bsdf.inputs["Emission Strength"].default_value = emit_strength
    if tex and os.path.exists(tex):
        img = bpy.data.images.load(tex, check_existing=True)
        tn = nt.nodes.new("ShaderNodeTexImage")
        tn.image = img
        nt.links.new(tn.outputs["Color"], bsdf.inputs["Base Color"])
        if emit:
            nt.links.new(tn.outputs["Color"], bsdf.inputs["Emission Color"])
    if alpha < 1.0:
        for attr, val in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
            try:
                setattr(m, attr, val)
            except Exception:  # noqa: BLE001
                pass
        try:
            m.use_backface_culling = False
        except Exception:  # noqa: BLE001
            pass
    m["cel"] = dict(hex=hexcol, alpha=alpha, emit=emit, emit_strength=emit_strength,
                    tex=tex if tex and os.path.exists(tex) else "", shade=shade or "")
    MATS[name] = m
    MAT_INFO.append(dict(name=name, hex=hexcol.upper(), alpha=alpha, emit=(emit.upper() if emit else None),
                         emit_strength=emit_strength, tex=(os.path.basename(tex) if tex and os.path.exists(tex) else None),
                         note=note))
    return m


# ---------------------------------------------------------------- mesh builder
class Part:
    """Accumulates boxes / cylinders / quads (world coords) into one bmesh, origin at `origin`."""

    def __init__(self, name, origin=(0, 0, 0)):
        self.name = name
        self.origin = Vector(origin)
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.fixed = self.bm.faces.layers.int.new("fixed")
        self.mats = []
        self.has_explicit_uv = False

    def _mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def box(self, centre, size, mat, axes=AXIS):
        c = Vector(centre) - self.origin
        r, u, n = (a.normalized() for a in axes)
        w, h, d = size
        vs = []
        for sz in (-1, 1):
            for sy in (-1, 1):
                for sx in (-1, 1):
                    vs.append(self.bm.verts.new(c + r * (sx * w / 2) + u * (sy * h / 2) + n * (sz * d / 2)))
        idx = [(0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4), (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5)]
        mi = self._mi(mat)
        for q in idx:
            self.bm.faces.new([vs[i] for i in q]).material_index = mi
        return self

    def quad(self, centre, size, mat, axes):
        """One face with 0..1 UVs: U along axes[0], V along axes[1] (the decal's up); faces axes[2]."""
        c = Vector(centre) - self.origin
        r, u, _ = (a.normalized() for a in axes)
        w, h = size
        corners = [(-1, -1, (0, 0)), (1, -1, (1, 0)), (1, 1, (1, 1)), (-1, 1, (0, 1))]
        vs = [self.bm.verts.new(c + r * (sx * w / 2) + u * (sy * h / 2)) for sx, sy, _ in corners]
        f = self.bm.faces.new(vs)
        f.material_index = self._mi(mat)
        f[self.fixed] = 1
        for loop, (_, _, uv) in zip(f.loops, corners):
            loop[self.uv].uv = uv
        self.has_explicit_uv = True
        return self

    def cyl(self, centre, radius, length, mat, axis="Z", segs=16, radius2=None):
        c = Vector(centre) - self.origin
        rot = {"Z": Matrix.Identity(3), "X": Matrix.Rotation(math.pi / 2, 3, "Y"),
               "Y": Matrix.Rotation(-math.pi / 2, 3, "X")}[axis]
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segs, radius1=radius,
                                    radius2=radius if radius2 is None else radius2, depth=length,
                                    matrix=Matrix.Translation(c) @ rot.to_4x4())
        mi = self._mi(mat)
        for f in {f for v in res["verts"] for f in v.link_faces}:
            f.material_index = mi
        return self

    def lathe_y(self, profile, mat, segs=20, y0=0.0):
        """Revolve a (y, r) profile about the Blender Y axis (the torch's axis); caps both ends."""
        mi = self._mi(mat)
        rings = []
        for y, r in profile:
            ring = []
            for i in range(segs):
                a = 2 * math.pi * i / segs
                ring.append(self.bm.verts.new(Vector((r * math.cos(a), y0 + y, r * math.sin(a))) - self.origin))
            rings.append(ring)
        for k in range(len(rings) - 1):
            a, b = rings[k], rings[k + 1]
            for i in range(segs):
                j = (i + 1) % segs
                self.bm.faces.new([a[i], a[j], b[j], b[i]]).material_index = mi
        self.bm.faces.new(list(reversed(rings[0]))).material_index = mi
        self.bm.faces.new(rings[-1]).material_index = mi
        return self

    def ring_y(self, profile, mat, segs=24, closed=True, uv=False):
        """Revolve a (y, r) profile about the Y axis with no caps: closed=True joins its last point to its
        first (a bezel's section); uv=True lays U round the axis (0.5 on top, +Z; the seam underneath) and
        V along the profile's Y span (a wrap label)."""
        mi = self._mi(mat)
        ys = [y for y, _ in profile]
        ylo, yhi = min(ys), max(ys)
        cols = segs + 1
        rings = []
        for y, r in profile:
            ring = []
            for i in range(cols):
                t = -math.pi + 2 * math.pi * i / segs
                ring.append(self.bm.verts.new(Vector((r * math.sin(t), y, r * math.cos(t))) - self.origin))
            rings.append(ring)
        n = len(rings)
        spans = n if closed else n - 1
        for k in range(spans):
            k1 = (k + 1) % n
            a, b = rings[k], rings[k1]
            va = (profile[k][0] - ylo) / max(yhi - ylo, 1e-6)
            vb = (profile[k1][0] - ylo) / max(yhi - ylo, 1e-6)
            for i in range(segs):
                f = self.bm.faces.new([a[i], a[i + 1], b[i + 1], b[i]])
                f.material_index = mi
                f[self.fixed] = 1
                if uv:
                    for loop, val in zip(f.loops, ((i / segs, va), ((i + 1) / segs, va), ((i + 1) / segs, vb), (i / segs, vb))):
                        loop[self.uv].uv = val
        if uv:
            self.has_explicit_uv = True
        return self

    def arc_band_y(self, y0, y1, radius, a0_deg, a1_deg, mat, segs=12, reverse_u=False):
        """A sticker on a cylinder about the Y axis: the arc from a0 to a1 degrees (0 = +X, 90 = +Z), U along
        the axis (y0 -> y1, or y1 -> y0 with reverse_u), V round the arc (a0 -> a1); faces outward."""
        mi = self._mi(mat)
        cols = []
        for i in range(segs + 1):
            phi = math.radians(a0_deg + (a1_deg - a0_deg) * i / segs)
            cols.append([self.bm.verts.new(Vector((radius * math.cos(phi), y, radius * math.sin(phi))) - self.origin)
                         for y in (y0, y1)])
        for i in range(segs):
            a, b = cols[i], cols[i + 1]
            v0, v1 = i / segs, (i + 1) / segs
            us = (1.0, 0.0) if reverse_u else (0.0, 1.0)
            corners = [(a[0], (us[0], v0)), (a[1], (us[1], v0)), (b[1], (us[1], v1)), (b[0], (us[0], v1))]
            if a1_deg < a0_deg:   # the arc runs the other way round: keep the face outward
                corners.reverse()
            f = self.bm.faces.new([c[0] for c in corners])
            f.material_index = mi
            f[self.fixed] = 1
            for loop, (_, val) in zip(f.loops, corners):
                loop[self.uv].uv = val
        self.has_explicit_uv = True
        return self

    def disc_y(self, y, radius, mat, facing=1, segs=24):
        """A flat disc facing +Y (or -Y) with planar UVs (a lens face)."""
        mi = self._mi(mat)
        c = self.bm.verts.new(Vector((0, y, 0)) - self.origin)
        ring = [self.bm.verts.new(Vector((radius * math.cos(2 * math.pi * i / segs), y,
                                          radius * math.sin(2 * math.pi * i / segs))) - self.origin) for i in range(segs)]
        for i in range(segs):
            vs = [c, ring[i], ring[(i + 1) % segs]] if facing < 0 else [c, ring[(i + 1) % segs], ring[i]]
            f = self.bm.faces.new(vs)
            f.material_index = mi
            f[self.fixed] = 1
            for loop in f.loops:
                v = loop.vert.co + self.origin
                loop[self.uv].uv = (0.5 + v.x / (2 * radius), 0.5 + v.z / (2 * radius))
        self.has_explicit_uv = True
        return self

    def torus_x(self, centre, major, minor, mat, segs=16, tube=8):
        """A ring whose axis is X (a lanyard loop)."""
        mi = self._mi(mat)
        c = Vector(centre) - self.origin
        grid = []
        for i in range(segs):
            a = 2 * math.pi * i / segs
            ring = []
            for j in range(tube):
                b = 2 * math.pi * j / tube
                rr = major + minor * math.cos(b)
                ring.append(self.bm.verts.new(c + Vector((minor * math.sin(b), rr * math.cos(a), rr * math.sin(a)))))
            grid.append(ring)
        for i in range(segs):
            for j in range(tube):
                a, b = grid[i], grid[(i + 1) % segs]
                self.bm.faces.new([a[j], b[j], b[(j + 1) % tube], a[(j + 1) % tube]]).material_index = mi
        return self

    def tube(self, points, radius, mat, sides=6):
        """A tube along a polyline (a coiled cable): rings of `sides` verts, capped."""
        mi = self._mi(mat)
        rings = []
        for k, p in enumerate(points):
            p = Vector(p)
            t = (Vector(points[min(k + 1, len(points) - 1)]) - Vector(points[max(k - 1, 0)])).normalized()
            ref = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
            n1 = t.cross(ref).normalized()
            n2 = t.cross(n1).normalized()
            rings.append([self.bm.verts.new(p - self.origin + (n1 * math.cos(2 * math.pi * i / sides) +
                                                                 n2 * math.sin(2 * math.pi * i / sides)) * radius)
                          for i in range(sides)])
        for k in range(len(rings) - 1):
            a, b = rings[k], rings[k + 1]
            for i in range(sides):
                j = (i + 1) % sides
                self.bm.faces.new([a[i], a[j], b[j], b[i]]).material_index = mi
        self.bm.faces.new(list(reversed(rings[0]))).material_index = mi
        self.bm.faces.new(rings[-1]).material_index = mi
        return self

    def prism_yz(self, profile, x0, x1, mat):
        """Extrude a (y, z) profile polygon along X."""
        mi = self._mi(mat)
        a = [self.bm.verts.new(Vector((x0, y, z)) - self.origin) for y, z in profile]
        b = [self.bm.verts.new(Vector((x1, y, z)) - self.origin) for y, z in profile]
        self.bm.faces.new(a).material_index = mi
        self.bm.faces.new(list(reversed(b))).material_index = mi
        n = len(profile)
        for i in range(n):
            j = (i + 1) % n
            self.bm.faces.new([a[i], a[j], b[j], b[i]]).material_index = mi
        return self

    def build(self, coll, bevel=0.0, segments=2, angle=30.0):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=1e-6)
        free = [f for f in self.bm.faces if not f[self.fixed]]
        if free:
            bmesh.ops.recalc_face_normals(self.bm, faces=free)
        me = bpy.data.meshes.new(self.name + "_mesh")
        self.bm.to_mesh(me)
        self.bm.free()
        for m in self.mats:
            me.materials.append(MATS[m])
        ob = bpy.data.objects.new(self.name, me)
        ob.location = self.origin
        coll.objects.link(ob)
        ob["explicit_uv"] = self.has_explicit_uv
        if bevel > 0:
            mod = ob.modifiers.new("Bevel", "BEVEL")
            mod.width = bevel
            mod.segments = segments
            mod.limit_method = "ANGLE"
            mod.angle_limit = math.radians(angle)
            mod.use_clamp_overlap = True
            apply_mods(ob)
        smooth(ob)
        return ob


def apply_mods(ob):
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    old = ob.data
    ob.modifiers.clear()
    ob.data = me
    me.name = ob.name + "_mesh"
    if old.users == 0:
        bpy.data.meshes.remove(old)


def smooth(ob, angle=34.0):
    me = ob.data
    try:
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(angle))
    except Exception:  # noqa: BLE001
        for p in me.polygons:
            p.use_smooth = True


def boolean_cut(ob, centre, size, stage, mat):
    cutter = Part(ob.name + "_cut", centre).box(centre, size, mat).build(stage)
    mod = ob.modifiers.new("Cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cutter
    try:
        mod.solver = "EXACT"
    except Exception:  # noqa: BLE001
        pass
    apply_mods(ob)
    bpy.data.objects.remove(cutter, do_unlink=True)


def merge(dst, src):
    """Append src's mesh (and materials) into dst, keeping dst's origin; delete src."""
    off = src.location - dst.location
    bm = bmesh.new()
    bm.from_mesh(dst.data)
    base = len(dst.data.materials)
    for m in src.data.materials:
        dst.data.materials.append(m)
    sbm = bmesh.new()
    sbm.from_mesh(src.data)
    for f in sbm.faces:
        f.material_index += base
    bmesh.ops.translate(sbm, vec=off, verts=sbm.verts)
    tmp = bpy.data.meshes.new("tmp")
    sbm.to_mesh(tmp)
    sbm.free()
    bm.from_mesh(tmp)
    bm.to_mesh(dst.data)
    bm.free()
    bpy.data.meshes.remove(tmp)
    bpy.data.objects.remove(src, do_unlink=True)
    me = dst.data
    names = [m.name for m in me.materials]
    uniq = list(dict.fromkeys(names))
    per_poly = [uniq.index(names[p.material_index]) for p in me.polygons]
    me.materials.clear()
    for n in uniq:
        me.materials.append(bpy.data.materials[n])
    for p, i in zip(me.polygons, per_poly):
        p.material_index = i


def unwrap(objs):
    """Smart-UV-project every object that has no hand-laid decal UVs (each mesh gets a clean 0..1 layout)."""
    for ob in objs:
        if ob.get("explicit_uv"):
            continue
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        try:
            bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.02, scale_to_bounds=True)
        except TypeError:
            bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.02)
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")


def to_unity(v):
    return Vector((-v.x, v.z, -v.y))


def world_bounds(obs):
    lo = Vector((1e9, 1e9, 1e9))
    hi = -lo
    for ob in obs:
        for c in ob.bound_box:
            w = ob.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, w))
            hi = Vector(map(max, hi, w))
    return lo, hi


def tris(objs):
    out = {}
    for ob in objs:
        if ob.type == "MESH":
            out[ob.name] = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    return out


def export_fbx(objs, path, active):
    for ob in bpy.context.view_layer.objects:
        ob.select_set(ob in objs)
    bpy.context.view_layer.objects.active = active
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"}, use_mesh_modifiers=True,
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="FACE", use_tspace=False, add_leaf_bones=False, bake_anim=False,
        path_mode="RELATIVE", embed_textures=False, use_custom_props=False)
    print("exported", path)


# ---------------------------------------------------------------- renders (cel look)
def sock(sockets, identifier):
    return next(x for x in sockets if x.identifier == identifier)


def toonify(shadow_tint="#5A4470"):
    """Swap every machine material for a two-tone cel ramp: the lit tone, and a shadow tinted toward purple; emissive stays bright."""
    for m in list(MATS.values()):
        info = m.get("cel")
        if info is None:
            continue
        info = dict(info)
        nt = m.node_tree
        for n in list(nt.nodes):
            nt.nodes.remove(n)
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        base = Vector(hex_rgb(info["hex"]))
        tint = Vector(hex_rgb(shadow_tint))
        shade = Vector(hex_rgb(info["shade"])) if info.get("shade") else Vector([base[i] * 0.42 + tint[i] * 0.16 for i in range(3)])
        if info.get("emit"):
            em = nt.nodes.new("ShaderNodeEmission")
            em.inputs["Strength"].default_value = 0.35 if info["emit_strength"] <= 0 else max(1.2, min(4.0, info["emit_strength"]))
            em.inputs["Color"].default_value = (*hex_rgb(info["emit"]), 1)
            if info.get("tex"):
                tn = nt.nodes.new("ShaderNodeTexImage")
                tn.image = bpy.data.images.load(info["tex"], check_existing=True)
                nt.links.new(tn.outputs["Color"], em.inputs["Color"])
            shader = em
        else:
            diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
            s2r = nt.nodes.new("ShaderNodeShaderToRGB")
            nt.links.new(diff.outputs[0], s2r.inputs[0])
            ramp = nt.nodes.new("ShaderNodeValToRGB")
            ramp.color_ramp.interpolation = "CONSTANT"
            ramp.color_ramp.elements[0].position = 0.0
            ramp.color_ramp.elements[0].color = (*shade, 1)
            ramp.color_ramp.elements[1].position = 0.32
            ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
            hi = ramp.color_ramp.elements.new(0.86)
            hi.color = (1.18, 1.16, 1.12, 1)
            bw = nt.nodes.new("ShaderNodeRGBToBW")
            nt.links.new(s2r.outputs["Color"], bw.inputs[0])
            nt.links.new(bw.outputs[0], ramp.inputs["Fac"])
            mix = nt.nodes.new("ShaderNodeMix")
            mix.data_type = "RGBA"
            mix.blend_type = "MULTIPLY"
            mix.inputs[0].default_value = 1.0
            col_in = sock(mix.inputs, "A_Color")
            if info.get("tex"):
                tn = nt.nodes.new("ShaderNodeTexImage")
                tn.image = bpy.data.images.load(info["tex"], check_existing=True)
                nt.links.new(tn.outputs["Color"], col_in)
            else:
                col_in.default_value = (*base, 1)
            nt.links.new(ramp.outputs["Color"], sock(mix.inputs, "B_Color"))
            # the ramp is a multiplier on the base colour (or texture): its shadow step is the shade relative
            # to the base, so a textured decal darkens by the same purple-tinted step
            ramp.color_ramp.elements[0].color = (*[min(1.0, shade[i] / max(base[i], 1e-3)) for i in range(3)], 1)
            em = nt.nodes.new("ShaderNodeEmission")
            em.inputs["Strength"].default_value = 1.0
            nt.links.new(sock(mix.outputs, "Result_Color"), em.inputs["Color"])
            shader = em
        if info["alpha"] < 1.0:
            tr = nt.nodes.new("ShaderNodeBsdfTransparent")
            mx = nt.nodes.new("ShaderNodeMixShader")
            mx.inputs[0].default_value = info["alpha"]
            nt.links.new(tr.outputs[0], mx.inputs[1])
            nt.links.new(shader.outputs[0], mx.inputs[2])
            shader = mx
        nt.links.new(shader.outputs[0], out.inputs["Surface"])


def look_at(ob, target):
    d = Vector(target) - ob.location
    ob.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()


def setup_stage(scene, coll, stage, ink="#2B1C24", bg="#E9DCC6"):
    material("Stage_Desk", "#A98667", 0.6)
    MAT_INFO[:] = [m for m in MAT_INFO if not m["name"].startswith("Stage_")]
    world = bpy.data.worlds.new("Warm")
    scene.world = world
    try:
        world.use_nodes = True
    except Exception:  # noqa: BLE001
        pass
    b = world.node_tree.nodes.get("Background")
    b.inputs[0].default_value = (*hex_rgb(bg), 1)
    b.inputs[1].default_value = 1.0

    def light(name, kind, loc, target, energy, color="#FFFFFF", size=0.5, angle=None):
        ld = bpy.data.lights.new(name, kind)
        ld.energy = energy
        ld.color = hex_rgb(color)
        if kind == "AREA":
            ld.size = size
        if kind == "SUN" and angle is not None:
            ld.angle = math.radians(angle)
        ob = bpy.data.objects.new(name, ld)
        stage.objects.link(ob)
        ob.location = loc
        look_at(ob, target)
        return ob

    light("Key", "SUN", (0.8, 1.2, 1.6), (0, 0, 0), 4.0, "#FFF1DC", angle=3)

    cam_d = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_d)
    stage.objects.link(cam)
    scene.camera = cam
    scene.render.engine = "BLENDER_EEVEE"
    try:
        scene.eevee.taa_render_samples = 64
    except Exception:  # noqa: BLE001
        pass
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    try:
        scene.render.use_freestyle = True
        scene.render.line_thickness_mode = "ABSOLUTE"
        scene.render.line_thickness = 1.6
        vl = scene.view_layers[0]
        vl.use_freestyle = True
        fs = vl.freestyle_settings
        ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("Lines")
        if ls.linestyle is None:
            ls.linestyle = bpy.data.linestyles.new("Ink")
        ls.select_by_collection = True
        ls.collection = coll
        ls.select_silhouette = True
        ls.select_border = True
        ls.select_crease = True
        fs.crease_angle = math.radians(128)
        ls.linestyle.color = hex_rgb(ink)
        ls.linestyle.thickness = 1.8
    except Exception as e:  # noqa: BLE001
        print("freestyle unavailable:", e)
    return cam, cam_d


def render(scene, cam, cam_d, path, loc, target, lens=50.0, res=(900, 700), ortho=None):
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    cam.location = loc
    if ortho:
        cam_d.type = "ORTHO"
        cam_d.ortho_scale = ortho
    else:
        cam_d.type = "PERSP"
        cam_d.lens = lens
    look_at(cam, target)
    cam_d.clip_start = 0.005
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("rendered", path)


def turntable(scene, cam, cam_d, out_dir, prefix, target, dist, elev_deg, lens, steps=8, res=(700, 560)):
    """`steps` shots around the target at one elevation; returns their paths (the sheet is composed outside Blender)."""
    paths = []
    el = math.radians(elev_deg)
    for i in range(steps):
        a = math.radians(90 + 360.0 * i / steps)   # i = 0: from the front (+Y)
        loc = (Vector(target) + Vector((math.cos(a) * math.cos(el), math.sin(a) * math.cos(el), math.sin(el))) * dist)
        p = os.path.join(out_dir, f"{prefix}_tt{i:02d}.png")
        render(scene, cam, cam_d, p, tuple(loc), target, lens=lens, res=res)
        paths.append(p)
    return paths
