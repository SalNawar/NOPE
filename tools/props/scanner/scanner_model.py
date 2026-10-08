"""
Time Sorter (NOPE) - cream 1980s document scanner prop.

Run:  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" -b --python scanner_model.py
      (re-runnable: it starts from an empty scene every time and overwrites its outputs)

Outside Blender, `python scanner_model.py --textures-only` draws the two label textures
(readout_tex.png, badge_tex.png) with Pillow; the Blender run calls that itself when the PNGs
are missing, and falls back to flat colours if Pillow is not there.

Coordinates (Blender, metres, Z up). The scanner's FRONT (control strip, the side that faces the
office camera) is Blender +Y. Exported with axis_forward=-Z, axis_up=Y and Apply Transform, Unity
gets   unity = (-x_blender, z_blender, -y_blender)   so the front faces Unity local -Z and the hinge
is at Unity +Z, as DeskScanner's contract has it (sweepZ runs from the front edge -0.115 to the back
edge +0.135; the old placeholder's hinge is at +0.15).
"""
import math
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
READOUT_PNG = os.path.join(HERE, "readout_tex.png")
BADGE_PNG = os.path.join(HERE, "badge_tex.png")


# --------------------------------------------------------------------------------------------
# Textures (plain Python + Pillow)
# --------------------------------------------------------------------------------------------
def make_textures():
    from PIL import Image, ImageDraw, ImageFilter, ImageFont

    def font(names, size):
        for n in names:
            p = os.path.join(r"C:\Windows\Fonts", n)
            if os.path.exists(p):
                return ImageFont.truetype(p, size)
        return ImageFont.load_default()

    # Green phosphor readout, 760 x 104 (the screen is 85 x 11.5 mm).
    W, H = 760, 104
    base = Image.new("RGB", (W, H), (6, 28, 16))
    glow = Image.new("RGB", (W, H), (0, 0, 0))
    d = ImageDraw.Draw(glow)
    f1 = font(["consolab.ttf", "consola.ttf"], 40)
    f2 = font(["consola.ttf"], 30)
    green = (110, 255, 150)
    d.text((22, 6), "READY", font=f1, fill=green)
    d.text((22, 56), "INSERT DOCUMENT", font=f2, fill=green)
    # small scan grid icon on the right
    gx, gy, gw, gh = 600, 16, 120, 72
    for i in range(7):
        x = gx + i * gw // 6
        d.line([(x, gy), (x, gy + gh)], fill=(70, 200, 110), width=2)
    for j in range(5):
        y = gy + j * gh // 4
        d.line([(gx, y), (gx + gw, y)], fill=(70, 200, 110), width=2)
    soft = glow.filter(ImageFilter.GaussianBlur(5))
    px_base = base.load()
    out = Image.new("RGB", (W, H))
    po, pg, ps = out.load(), glow.load(), soft.load()
    for y in range(H):
        scan = 0.82 if y % 4 == 3 else 1.0
        for x in range(W):
            b, g, s = px_base[x, y], pg[x, y], ps[x, y]
            po[x, y] = tuple(min(255, int((b[k] + g[k] + 0.9 * s[k]) * scan)) for k in range(3))
    out.save(READOUT_PNG)

    # Manufacturer badge, 1000 x 120 (the plate is 100 x 12 mm): charcoal plate, cream logo + model.
    W, H = 1000, 120
    img = Image.new("RGB", (W, H), (44, 44, 48))
    d = ImageDraw.Draw(img)
    cream = (236, 226, 204)
    d.rectangle([4, 4, W - 5, H - 5], outline=(120, 116, 108), width=3)
    # logo: a square with an arch (time gate) cut in it
    d.rounded_rectangle([22, 18, 106, 102], radius=10, fill=cream)
    d.pieslice([38, 40, 90, 110], 180, 360, fill=(44, 44, 48))
    d.rectangle([38, 74, 90, 102], fill=(44, 44, 48))
    d.rectangle([54, 66, 74, 102], fill=cream)
    fb = font(["bahnschrift.ttf", "arialbd.ttf"], 50)
    fs = font(["bahnschrift.ttf", "arial.ttf"], 26)
    d.text((128, 12), "DOCUMENT SCANNER", font=fb, fill=cream)
    d.text((130, 74), "MODEL DS-2150  \u00b7  CUSTOMS EQUIPMENT", font=fs, fill=(196, 188, 170))
    d.rectangle([840, 26, 976, 94], outline=(226, 100, 58), width=4)
    d.text((854, 30), "2150", font=font(["bahnschrift.ttf", "arialbd.ttf"], 52), fill=(226, 100, 58))
    img.save(BADGE_PNG)
    print("textures written:", READOUT_PNG, BADGE_PNG)


try:
    import bpy  # noqa: F401
    IN_BLENDER = True
except ImportError:
    IN_BLENDER = False

if not IN_BLENDER:
    make_textures()
    sys.exit(0)

# --------------------------------------------------------------------------------------------
# Blender part
# --------------------------------------------------------------------------------------------
import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

if not (os.path.exists(READOUT_PNG) and os.path.exists(BADGE_PNG)):
    for exe in ("python", "py"):
        try:
            subprocess.run([exe, os.path.abspath(__file__), "--textures-only"], check=True, timeout=120)
            break
        except Exception as e:  # noqa: BLE001
            print("texture helper failed with", exe, e)
HAVE_TEX = os.path.exists(READOUT_PNG) and os.path.exists(BADGE_PNG)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0

COLL = bpy.data.collections.new("Scanner")
scene.collection.children.link(COLL)
STAGE = bpy.data.collections.new("Stage")
scene.collection.children.link(STAGE)


# ---------------------------------------------------------------- dimensions (Blender coords)
BODY_X = 0.200          # half width  -> 0.40 m (Unity X)
BODY_Y = 0.160          # half depth  -> 0.32 m (Unity Z)
FOOT_H = 0.0045
BODY_Z0 = 0.004
BODY_TOP = 0.060
SLANT_BOTTOM = (0.160, 0.016)     # (y, z) bottom of the slanted control face (above a vertical kick)
SLANT_TOP = (0.136, 0.060)        # (y, z) where the slant meets the top deck
GLASS_X = 0.175
GLASS_Y0, GLASS_Y1 = -0.140, 0.120      # Unity Z +0.14 (back) .. -0.12 (front)
GLASS_Z0, GLASS_Z1 = 0.0555, 0.0585
RECESS_X = 0.178
RECESS_Y0, RECESS_Y1 = -0.143, 0.123
RECESS_FLOOR = 0.051
BEDGLOW_Z = 0.0530
SWEEP_REST_Y = 0.115                    # Unity Z -0.115 (front edge of the sweep)
SWEEP_END_Y = -0.135                    # Unity Z +0.135
SWEEP_Z = (0.0537, 0.0551)
HINGE_Y, HINGE_Z = -0.158, 0.0605       # Unity (0, 0.0605, +0.158)
LID_X = 0.193
LID_Y0, LID_Y1 = -0.158, 0.127
LID_FRAME_TOP = 0.0805
LID_HOUSING_Y1 = -0.090
LID_HOUSING_TOP = 0.0985
LID_OPEN_DEG = 70.0
WIN_X, WIN_Y0, WIN_Y1 = 0.172, -0.088, 0.113          # lid window (through the frame)

# slant frame
_su = Vector((0.0, SLANT_TOP[0] - SLANT_BOTTOM[0], SLANT_TOP[1] - SLANT_BOTTOM[1]))
SLANT_LEN = _su.length
SU = _su.normalized()                                  # up the slant
SN = Vector((0.0, SU.z, -SU.y))                        # outward normal (forward-up)
SR = Vector((-1.0, 0.0, 0.0))                          # viewer's right
SLANT_DEG = math.degrees(math.atan2(SU.z, -SU.y))      # angle of the face from the desk


def on_slant(x, s, off=0.0):
    return Vector((x, SLANT_BOTTOM[0], SLANT_BOTTOM[1])) + SU * s + SN * off


AXIS = (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
FASCIA = (SR, SU, SN)


# ---------------------------------------------------------------- materials
def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(srgb_to_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


MATS = {}
MAT_INFO = []


def material(name, hexcol, rough=0.55, alpha=1.0, emit=None, emit_strength=0.0, metal=0.0, tex=None, note=""):
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
    if tex and HAVE_TEX:
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
    MATS[name] = m
    MAT_INFO.append(dict(name=name, hex=hexcol.upper(), alpha=alpha, emit=(emit.upper() if emit else None),
                         emit_strength=emit_strength, tex=(os.path.basename(tex) if tex and HAVE_TEX else None), note=note))
    return m


material("Scanner_Cream", "#DCD0B4", 0.5, note="body shell, lid hinge housing (Desk Anime)")
material("Scanner_CreamDark", "#BCAE90", 0.55, note="paper-slot lip, recessed trims (Desk Anime)")
material("Scanner_Charcoal", "#38393E", 0.45, note="control strip panels, glass-bed bezel (Desk Anime)")
material("Scanner_LidFrame", "#5F5C5A", 0.4, note="smoke-grey lid frame + grip (Desk Anime)")
material("Scanner_LidSmoke", "#262C31", 0.15, alpha=0.6, note="LidGlass: smoked translucent (URP Unlit/Lit Transparent, alpha 0.6)")
material("Scanner_Glass", "#A8EEE8", 0.05, alpha=0.22, note="bed glass (URP Unlit Transparent, alpha ~0.22, like Detain_Glass)")
material("Scanner_BedGlow", "#1A8A84", 0.5, emit="#33D9CC", emit_strength=0.75, note="cyan bed light (URP Unlit, or Lit + emission)")
material("Scanner_Sweep", "#E4FFFB", 0.5, emit="#BFFFF6", emit_strength=8.0, note="scan light bar (URP Unlit, like Placeholder_ScannerSweep)")
material("Scanner_ButtonCream", "#ECE4D0", 0.45, note="EJECT / AUTO / LIGHT / FEED keys")
material("Scanner_ButtonBlue", "#3F6FA8", 0.45, note="MODE key")
material("Scanner_ButtonOrange", "#E2643A", 0.45, note="START SCAN key")
material("Scanner_ReadoutGreen", "#3CFF86", 0.4, emit="#3CFF86", emit_strength=2.0, note="resolution 'STD' pilot (URP Unlit)")
material("Scanner_Readout", "#0A2414", 0.3, emit="#6EFF96", emit_strength=1.6, tex=READOUT_PNG,
         note="green phosphor screen; base map readout_tex.png (URP Unlit with the texture)")
material("Scanner_LED", "#FF2A1A", 0.3, emit="#FF2A1A", emit_strength=6.0, note="red power LED (URP Unlit)")
material("Scanner_Badge", "#2C2C30", 0.35, tex=BADGE_PNG, note="badge plate; base map badge_tex.png (Desk Anime with _BaseMap)")
material("Scanner_Vent", "#24242A", 0.6, note="vent slots (Desk Anime)")
material("Scanner_Slot", "#121214", 0.7, note="paper feed slit")
material("Scanner_Rubber", "#2A2826", 0.8, note="feet, hinge knuckles")


# ---------------------------------------------------------------- mesh builder
class Part:
    """Accumulates boxes / cylinders / quads (in world coords) into one bmesh, origin at `origin`."""

    def __init__(self, name, origin=(0, 0, 0), mats=()):
        self.name = name
        self.origin = Vector(origin)
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.mats = list(mats)

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
            f = self.bm.faces.new([vs[i] for i in q])
            f.material_index = mi
        return self

    def quad(self, centre, size, mat, axes=FASCIA):
        """A single face with UVs: U along axes[0] (viewer's right), V along axes[1] (up)."""
        c = Vector(centre) - self.origin
        r, u, _ = (a.normalized() for a in axes)
        w, h = size
        corners = [(-1, -1, (0, 0)), (1, -1, (1, 0)), (1, 1, (1, 1)), (-1, 1, (0, 1))]
        vs = [self.bm.verts.new(c + r * (sx * w / 2) + u * (sy * h / 2)) for sx, sy, _ in corners]
        f = self.bm.faces.new(vs)
        f.material_index = self._mi(mat)
        for loop, (_, _, uv) in zip(f.loops, corners):
            loop[self.uv].uv = uv
        return self

    def cyl(self, centre, radius, length, mat, axis="Z", segs=16):
        c = Vector(centre) - self.origin
        rot = {"Z": Matrix.Identity(3), "X": Matrix.Rotation(math.pi / 2, 3, "Y"), "Y": Matrix.Rotation(math.pi / 2, 3, "X")}[axis]
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segs, radius1=radius,
                                    radius2=radius, depth=length, matrix=Matrix.Translation(c) @ rot.to_4x4())
        mi = self._mi(mat)
        faces = {f for v in res["verts"] for f in v.link_faces}
        for f in faces:
            f.material_index = mi
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

    def build(self, bevel=0.0, segments=3, angle=25.0, coll=COLL):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me)
        self.bm.free()
        for m in self.mats:
            me.materials.append(MATS[m])
        ob = bpy.data.objects.new(self.name, me)
        ob.location = self.origin
        coll.objects.link(ob)
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
    me.name = ob.name
    if old.users == 0:
        bpy.data.meshes.remove(old)


def smooth(ob, angle=32.0):
    me = ob.data
    try:
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(angle))
    except Exception:  # noqa: BLE001
        for p in me.polygons:
            p.use_smooth = True


def boolean_cut(ob, centre, size, mat=None):
    cutter = Part(ob.name + "_cut", centre, mats=[mat] if mat else ["Scanner_Charcoal"]).box(centre, size, mat or "Scanner_Charcoal").build(coll=STAGE)
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
    # collapse duplicate material slots
    me = dst.data
    names = [m.name for m in me.materials]
    uniq = list(dict.fromkeys(names))
    per_poly = [uniq.index(names[p.material_index]) for p in me.polygons]
    me.materials.clear()
    for n in uniq:
        me.materials.append(bpy.data.materials[n])
    for p, i in zip(me.polygons, per_poly):
        p.material_index = i


# ================================================================ BODY
body_profile = [(-BODY_Y, BODY_Z0), (BODY_Y, BODY_Z0), SLANT_BOTTOM, SLANT_TOP, (-BODY_Y, BODY_TOP)]
body = Part("Body").prism_yz(body_profile, -BODY_X, BODY_X, "Scanner_Cream").build(bevel=0.005, segments=3, angle=24)
# the glass bed recess
rc = ((0, (RECESS_Y0 + RECESS_Y1) / 2, (RECESS_FLOOR + 0.08) / 2))
boolean_cut(body, rc, (RECESS_X * 2, RECESS_Y1 - RECESS_Y0, 0.08 - RECESS_FLOOR))
ch = list(body.data.materials).index(MATS["Scanner_Cream"])
body.data.materials.append(MATS["Scanner_Charcoal"])
ci = len(body.data.materials) - 1
for p in body.data.polygons:
    c = p.center
    if abs(c.x) <= RECESS_X + 1e-4 and RECESS_Y0 - 1e-4 <= c.y <= RECESS_Y1 + 1e-4 and c.z < BODY_TOP - 2e-4:
        p.material_index = ci
smooth(body)

det = Part("BodyDetail", (0, 0, 0))
for sx in (-1, 1):
    for sy in (-1, 1):
        det.cyl((sx * 0.165, sy * 0.125, FOOT_H / 2), 0.011, FOOT_H, "Scanner_Rubber", "Z", 16)
for sx in (-1, 1):  # hinge knuckles on the hinge line
    det.cyl((sx * 0.125, HINGE_Y, HINGE_Z + 0.001), 0.0065, 0.036, "Scanner_Rubber", "X", 14)
# paper feed slot (front, viewer's right, lower row)
det.box(on_slant(-0.1315, 0.016, 0.0015 - 0.0006), (0.093, 0.016, 0.003), "Scanner_CreamDark", FASCIA)
det.box(on_slant(-0.1315, 0.016, 0.003 + 0.0003), (0.078, 0.0035, 0.0012), "Scanner_Slot", FASCIA)
det_ob = det.build(bevel=0.0008, segments=2, angle=40)
merge(body, det_ob)
smooth(body)

# ================================================================ GLASS BED
glass = Part("Glass", (0, (GLASS_Y0 + GLASS_Y1) / 2, (GLASS_Z0 + GLASS_Z1) / 2)).box(
    (0, (GLASS_Y0 + GLASS_Y1) / 2, (GLASS_Z0 + GLASS_Z1) / 2), (GLASS_X * 2, GLASS_Y1 - GLASS_Y0, GLASS_Z1 - GLASS_Z0),
    "Scanner_Glass").build(bevel=0.0008, segments=1)
glow_c = (0, (GLASS_Y0 + GLASS_Y1) / 2, BEDGLOW_Z)
bedglow = Part("BedGlow", glow_c).quad(glow_c, (GLASS_X * 2 - 0.004, GLASS_Y1 - GLASS_Y0 - 0.004), "Scanner_BedGlow",
                                       axes=(Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))).build()
sw_c = (0, SWEEP_REST_Y, (SWEEP_Z[0] + SWEEP_Z[1]) / 2)
sweep = Part("SweepBar", sw_c).box(sw_c, (0.340, 0.008, SWEEP_Z[1] - SWEEP_Z[0]), "Scanner_Sweep").build()

# ================================================================ FRONT CONTROL FACE
cs = Part("ControlStrip", on_slant(0.028, 0.0255))
cs.box(on_slant(0.028, 0.016, 0.001 - 0.0004), (0.180, 0.017, 0.0022), "Scanner_Charcoal", FASCIA)      # key panel
cs.box(on_slant(-0.015, 0.0355, 0.0012 - 0.0004), (0.120, 0.0165, 0.0026), "Scanner_Charcoal", FASCIA)  # readout bezel
cs_ob = cs.build(bevel=0.0009, segments=2, angle=40)

btn = Part("Buttons", on_slant(0.06, 0.016))
KEY_D = 0.0058
for x, w, m, base in [(0.163, 0.021, "Scanner_ButtonCream", 0.0), (0.136, 0.021, "Scanner_ButtonCream", 0.0),
                      (0.098, 0.021, "Scanner_ButtonBlue", 0.0018), (0.026, 0.021, "Scanner_ButtonCream", 0.0018),
                      (-0.001, 0.021, "Scanner_ButtonCream", 0.0018), (-0.040, 0.032, "Scanner_ButtonOrange", 0.0018)]:
    btn.box(on_slant(x, 0.016, base + KEY_D / 2 - 0.0005), (w, 0.0125, KEY_D), m, FASCIA)
for x, m in [(0.074, "Scanner_Vent"), (0.062, "Scanner_ReadoutGreen"), (0.050, "Scanner_Vent")]:  # FINE / STD / FAST pilots
    btn.box(on_slant(x, 0.0145, 0.0018 + 0.0006), (0.0045, 0.0030, 0.0016), m, FASCIA)
btn_ob = btn.build(bevel=0.0016, segments=3, angle=40)

ro_c = on_slant(-0.0085, 0.0355, 0.0012 + 0.0013 + 0.00035)
readout = Part("Readout", ro_c).quad(ro_c, (0.085, 0.0115), "Scanner_Readout").build()
led_c = on_slant(-0.0645, 0.0355, 0.0024 + 0.0009)
led = Part("LED", led_c).box(led_c, (0.0048, 0.0048, 0.0022), "Scanner_LED", FASCIA).build(bevel=0.0007, segments=2)
bd_c = on_slant(0.1265, 0.0355, 0.0006 + 0.0004)
badge = Part("Badge", bd_c).box(on_slant(0.1265, 0.0355, 0.0004), (0.101, 0.0128, 0.0010), "Scanner_Charcoal", FASCIA)
badge.quad(on_slant(0.1265, 0.0355, 0.0009 + 0.00012), (0.100, 0.012), "Scanner_Badge")
badge_ob = badge.build()

vents = Part("Vents", (0, 0, 0.036))
for i in range(8):  # front, upper row, viewer's right: vertical slats
    vents.box(on_slant(-0.103 - i * 0.0092, 0.0355, 0.0003), (0.0028, 0.0125, 0.0012), "Scanner_Vent", FASCIA)
for sx in (-1, 1):  # both sides: horizontal slots
    for j in range(5):
        vents.box((sx * (BODY_X + 0.0002), -0.080, 0.024 + j * 0.0062), (0.0014, 0.060, 0.0026), "Scanner_Vent")
for j in range(4):  # back, low
    vents.box((0.0, -BODY_Y - 0.0002, 0.020 + j * 0.0062), (0.16, 0.0014, 0.0026), "Scanner_Vent")
vents_ob = vents.build(bevel=0.0005, segments=1, angle=40)

# ================================================================ LID (pivot on the hinge line)
hinge = Vector((0, HINGE_Y, HINGE_Z))
lid_shell = Part("Lid", hinge).box((0, (LID_Y0 + 0.012 + LID_Y1) / 2, (HINGE_Z + LID_FRAME_TOP) / 2),
                                   (LID_X * 2 - 0.002, LID_Y1 - LID_Y0 - 0.012, LID_FRAME_TOP - HINGE_Z), "Scanner_LidFrame")
lid = lid_shell.build(bevel=0.0055, segments=3, angle=30)
boolean_cut(lid, (0, (WIN_Y0 + WIN_Y1) / 2, (HINGE_Z + LID_FRAME_TOP) / 2), (WIN_X * 2, WIN_Y1 - WIN_Y0, 0.08), "Scanner_LidFrame")
housing = Part("LidHousing", hinge).box((0, (LID_Y0 + LID_HOUSING_Y1) / 2, (HINGE_Z + LID_HOUSING_TOP) / 2),
                                        (LID_X * 2, LID_HOUSING_Y1 - LID_Y0, LID_HOUSING_TOP - HINGE_Z), "Scanner_Cream")
housing_ob = housing.build(bevel=0.007, segments=4, angle=30)
merge(lid, housing_ob)
grip = Part("LidGrip", hinge).box((0, LID_Y1 + 0.0015, 0.0700), (0.06, 0.006, 0.010), "Scanner_LidFrame")
merge(lid, grip.build(bevel=0.002, segments=2, angle=30))
smooth(lid)
lg_world = Vector((0, (WIN_Y0 + WIN_Y1) / 2, LID_FRAME_TOP - 0.0045))
lidglass = Part("LidGlass", lg_world).box(lg_world, (WIN_X * 2 + 0.004, WIN_Y1 - WIN_Y0 + 0.004, 0.004), "Scanner_LidSmoke").build(bevel=0.0008, segments=1)
lidglass.parent = lid
lidglass.location = lg_world - hinge

ALL = [body, lid, lidglass, glass, bedglow, sweep, cs_ob, btn_ob, readout, led, badge_ob, vents_ob]
for ob in ALL:
    ob.data.name = ob.name + "_mesh"


# ================================================================ helpers for report
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


bpy.context.view_layer.update()
lo, hi = world_bounds(ALL)
glo, ghi = world_bounds([glass])
llo, lhi = world_bounds([lid])
REPORT = {
    "footprint_x": hi.x - lo.x, "footprint_z": hi.y - lo.y, "height": hi.z,
    "body_top": BODY_TOP, "glass_top": GLASS_Z1, "lid_top": lhi.z,
    "glass_x": ghi.x - glo.x, "glass_z": ghi.y - glo.y,
    "glass_unity_z": (-ghi.y, -glo.y), "glass_centre_unity": tuple(round(a, 4) for a in to_unity(glass.location)),
    "hinge_unity": tuple(round(a, 4) for a in to_unity(hinge)),
    "sweep_rest_unity": tuple(round(a, 4) for a in to_unity(sweep.location)),
    "sweep_end_unity_z": -SWEEP_END_Y,
    "slant_deg": SLANT_DEG,
    "tris": {ob.name: sum(len(p.vertices) - 2 for p in ob.data.polygons) for ob in ALL},
}
print("REPORT", REPORT)

# ================================================================ EXPORT (lid closed)
FBX = os.path.join(HERE, "scanner.fbx")
for ob in bpy.context.view_layer.objects:
    ob.select_set(ob in ALL)
bpy.context.view_layer.objects.active = body
bpy.ops.export_scene.fbx(
    filepath=FBX, use_selection=True, object_types={"MESH"}, use_mesh_modifiers=True,
    apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
    axis_forward="-Z", axis_up="Y", bake_space_transform=True,
    mesh_smooth_type="FACE", use_tspace=False, add_leaf_bones=False, bake_anim=False,
    path_mode="RELATIVE", embed_textures=False, use_custom_props=False)
print("exported", FBX)

# ================================================================ STAGE + RENDERS
def look_at(ob, target):
    d = Vector(target) - ob.location
    ob.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()


desk_m = material("Stage_Desk", "#A98667", 0.6)
wall_m = material("Stage_Wall", "#ECDCC6", 0.9)
MAT_INFO[:] = [m for m in MAT_INFO if not m["name"].startswith("Stage_")]
desk = Part("StageDesk").box((0, 0, -0.01), (2.4, 2.4, 0.02), "Stage_Desk").build(coll=STAGE)
wall = Part("StageWall").box((0, -0.75, 0.6), (3.0, 0.02, 1.4), "Stage_Wall").build(coll=STAGE)

world = bpy.data.worlds.new("Warm")
scene.world = world
try:
    world.use_nodes = True
except Exception:  # noqa: BLE001
    pass
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (*hex_rgb("#EFE1CF"), 1)
bg.inputs[1].default_value = 0.55


def light(name, kind, loc, target, energy, color="#FFFFFF", size=0.5, angle=None):
    ld = bpy.data.lights.new(name, kind)
    ld.energy = energy
    ld.color = hex_rgb(color)
    if kind == "AREA":
        ld.size = size
    if kind == "SUN" and angle is not None:
        ld.angle = math.radians(angle)
    ob = bpy.data.objects.new(name, ld)
    STAGE.objects.link(ob)
    ob.location = loc
    look_at(ob, target)
    return ob


light("Key", "SUN", (0.9, 1.1, 1.6), (0, 0, 0), 3.2, "#FFF1DC", angle=4)
light("Fill", "AREA", (-0.9, 0.6, 0.6), (0, 0, 0.05), 40, "#D8E6FF", size=1.0)
light("Rim", "AREA", (0.2, -0.9, 0.7), (0, 0, 0.05), 30, "#FFE2C0", size=0.8)

cam_d = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_d)
STAGE.objects.link(cam)
scene.camera = cam

scene.render.engine = "BLENDER_EEVEE"
try:
    scene.eevee.taa_render_samples = 96
except Exception:  # noqa: BLE001
    pass
for attr, val in (("use_shadows", True), ("use_raytracing", True)):
    try:
        setattr(scene.eevee, attr, val)
    except Exception:  # noqa: BLE001
        pass
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "None"
scene.view_settings.exposure = 0.0
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False

# 80s cel line art (Freestyle), on the scanner only
try:
    scene.render.use_freestyle = True
    scene.render.line_thickness_mode = "ABSOLUTE"
    scene.render.line_thickness = 1.5
    vl = scene.view_layers[0]
    vl.use_freestyle = True
    fs = vl.freestyle_settings
    ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("Lines")
    if ls.linestyle is None:
        ls.linestyle = bpy.data.linestyles.new("Ink")
    ls.select_by_collection = True
    ls.collection = COLL
    ls.select_silhouette = True
    ls.select_border = True
    ls.select_crease = True
    fs.crease_angle = math.radians(140)
    ls.linestyle.color = hex_rgb("#3A2A22")
    ls.linestyle.thickness = 1.6
    print("freestyle on")
except Exception as e:  # noqa: BLE001
    print("freestyle unavailable:", e)


def render(path, loc, target, lens=50.0, res=(1600, 1000), ortho=None, rot=None):
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    cam.location = loc
    if ortho:
        cam_d.type = "ORTHO"
        cam_d.ortho_scale = ortho
    else:
        cam_d.type = "PERSP"
        cam_d.lens = lens
    if rot:
        cam.rotation_euler = rot
    else:
        look_at(cam, target)
    cam_d.clip_start = 0.01
    scene.render.filepath = os.path.join(HERE, path)
    bpy.ops.render.render(write_still=True)
    print("rendered", path)


render("preview_front34.png", (0.40, 0.74, 0.46), (0.0, 0.0, 0.035), lens=58)
render("preview_side.png", (1.05, 0.10, 0.20), (0.0, 0.0, 0.05), lens=85)
render("preview_top.png", (0.0, 0.0, 1.0), None, res=(1400, 1200), ortho=0.50, rot=(0.0, 0.0, math.pi))
lid.rotation_euler.x = math.radians(LID_OPEN_DEG)
sweep.location.y = SWEEP_REST_Y + (SWEEP_END_Y - SWEEP_REST_Y) * 0.42   # mid-scan, for the picture
render("preview_lid_open.png", (0.52, 0.98, 0.70), (0.0, -0.035, 0.105), lens=50)
lid.rotation_euler.x = 0.0
sweep.location.y = SWEEP_REST_Y

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "scanner.blend"))

# ================================================================ VERIFY: re-import the FBX
import json  # noqa: E402

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, axis_forward="-Z", axis_up="Y")
bpy.context.view_layer.update()
ver = {}
for ob in bpy.context.scene.objects:
    lo2, hi2 = world_bounds([ob])
    ver[ob.name] = dict(parent=ob.parent.name if ob.parent else None,
                        loc=[round(a, 4) for a in ob.matrix_world.translation],
                        rot=[round(math.degrees(a), 2) for a in ob.matrix_world.to_euler()],
                        scale=[round(a, 4) for a in ob.matrix_world.to_scale()],
                        lo=[round(a, 4) for a in lo2], hi=[round(a, 4) for a in hi2],
                        mats=[m.name for m in ob.data.materials] if ob.type == "MESH" else [])
lo3, hi3 = world_bounds([o for o in bpy.context.scene.objects if o.type == "MESH"])
ver["_all"] = dict(lo=[round(a, 4) for a in lo3], hi=[round(a, 4) for a in hi3])
# open the re-imported lid: it must swing about its own origin (the hinge line) and lift the front
L = bpy.data.objects["Lid"]
L.rotation_euler.x += math.radians(LID_OPEN_DEG)
bpy.context.view_layer.update()
lo4, hi4 = world_bounds([L, bpy.data.objects["LidGlass"]])
ver["_lid_open70"] = dict(lo=[round(a, 4) for a in lo4], hi=[round(a, 4) for a in hi4])
ver["_report"] = {k: (v if not isinstance(v, float) else round(v, 4)) for k, v in REPORT.items()}
ver["_materials"] = MAT_INFO
with open(os.path.join(HERE, "verify_reimport.json"), "w") as fh:
    json.dump(ver, fh, indent=1)
print("VERIFY written")
_fbm = os.path.join(HERE, "scanner.fbm")   # the importer's texture folder: not part of the hand-off
if os.path.isdir(_fbm):
    import shutil  # noqa: E402
    shutil.rmtree(_fbm, ignore_errors=True)
