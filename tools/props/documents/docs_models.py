"""Time Sorter desk documents as real low-poly models (Saleh 2026-10-17: "use canva and blender
to make actual 3d documents"). Run headless only (never open Blender's window), from the repo root:
    python tools/props/documents/paint_textures.py Assets/Art/Office/Gameplay/Documents/docs_edges.png
    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup         --python tools/props/documents/docs_models.py -- Assets/Art/Office/Gameplay/Documents         Assets/Art/UI/Resources/Forms <preview folder> [render]
It writes the five FBX models, folder_shade.png and booklet_shade.png (each model's baked cel shadows on
itself) and, with "render", three Eevee toon previews. Build Office UI wires them (DeskPaperDepth).

Every model is built in metres with Z up (h: the height over the document's face plane, the plane
its printed texts and click boxes lie on), then written in the face quad's unit space for Unity:
x / width, y / depth, and the depth under the face (-h) along the third axis, so it replaces the
quad its paper or folder already scales to its size. The readable tops are UV-mapped by a planar
projection from above (u = x / width + 0.5, v = y / depth + 0.5): the existing Canva face art
lands exactly where the quad showed it. Sides and edges take the edge atlas (docs_edges.png).
Baked shading lives in the vertex colours (the gutter, the spine, the curl, the bevels).

Meshes (each its own object, so Unity gets one mesh per name):
  FolderTop / FolderBody   the open manila rulebook folder (art: Forms/rulebook_folder)
  FolderTabTop / FolderTabBody   a raised index tab (art: Forms/rulebook_tab_*)
  BookletTop / BookletBody  the open passport (art: the paper's face)
  SheetTop / SheetBody      a single sheet, a gentle curl and a lifted corner
  CardTop / CardBody        a thick card with rounded corners
"""
import math
import os
import sys

import bpy
import bmesh

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = argv[0]
ART = argv[1]
RENDER_DIR = argv[2]
DO_RENDER = len(argv) > 3 and argv[3] == 'render'

EDGE_PAGES, EDGE_BOARD, EDGE_SHEET = (0.75, 1.0), (0.5, 0.75), (0.25, 0.5)
SOLID = {'thread': 0.125, 'plastic': 0.375, 'brass': 0.625, 'ink': 0.875}
TILE = 0.03  # metres of perimeter per u repeat of an edge band


class Part:
    """One output mesh: faces with their own corners (flat cel facets), per-corner UV and colour."""

    def __init__(self, name, width, depth):
        self.name, self.width, self.depth = name, width, depth
        self.faces = []  # (points [(x, y, h)], uvs [(u, v)], colours [(r, g, b, a)])
        self.flex = 1.0  # the vertex colours' alpha: how much the part bends with the body's motion (DocumentCel's _Flex)
        self.dice = None  # (across, up): cut into a grid so a bend (PaperBend.hlsl, per vertex) curves it smoothly

    def face(self, pts, uvs, cols):
        self.faces.append((pts, uvs, [(c[0], c[1], c[2], self.flex) for c in cols]))

    def art_uv(self, x, y):
        return (x / self.width + 0.5, y / self.depth + 0.5)


def grey(k, a=1.0):
    return (k, k, k, a)


def rounded_rect(x0, y0, x1, y1, radii, seg=5):
    """Counter-clockwise outline; radii (bottom-left, bottom-right, top-right, top-left) in metres."""
    pts = []
    corners = [((x0, y0), radii[0], math.pi), ((x1, y0), radii[1], 1.5 * math.pi),
               ((x1, y1), radii[2], 0.0), ((x0, y1), radii[3], 0.5 * math.pi)]
    for (cx, cy), r, a0 in corners:
        if r <= 0:
            pts.append((cx, cy))
            continue
        ox = cx + (r if cx == x0 else -r)
        oy = cy + (r if cy == y0 else -r)
        for i in range(seg + 1):
            a = a0 + (math.pi / 2) * i / seg
            pts.append((ox + r * math.cos(a), oy + r * math.sin(a)))
    return pts


def area(poly):
    return sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly))) / 2


def inset(poly, d):
    """Moves each corner d inward along its mean normal (a convex, counter-clockwise outline)."""
    n = len(poly)
    out = []
    for i in range(n):
        a, b, c = poly[i - 1], poly[i], poly[(i + 1) % n]
        n1 = (b[1] - a[1], -(b[0] - a[0]))
        n2 = (c[1] - b[1], -(c[0] - b[0]))
        l1 = math.hypot(*n1) or 1
        l2 = math.hypot(*n2) or 1
        nx, ny = n1[0] / l1 + n2[0] / l2, n1[1] / l1 + n2[1] / l2
        ln = math.hypot(nx, ny) or 1
        out.append((b[0] - nx / ln * d, b[1] - ny / ln * d))
    return out


def edge_uv(band, s, frac):
    """An edge band's UV: s metres along the perimeter, frac 0 at its top, 1 at its foot."""
    v0, v1 = band
    return (s / TILE, v1 - (v1 - v0) * frac)


def solid_uv(kind):
    return (SOLID[kind], 0.12)


def rim(body, poly, h, width, scale=(1.0, 1.0)):
    """The cel ink line round a thing's foot: a flat band `width` metres wide outside poly (counter-clockwise),
    at height h, in the atlas's ink (scale: metres per unit of poly's space, for parts built in unit space)."""
    if width <= 0:
        return
    if area(poly) < 0:
        poly = poly[::-1]
    m = [(x * scale[0], y * scale[1]) for x, y in poly]
    out = [(x / scale[0], y / scale[1]) for x, y in inset(m, -width)]
    uv = solid_uv('ink')
    for i in range(len(poly)):
        a, b, oa, ob = poly[i], poly[(i + 1) % len(poly)], out[i], out[(i + 1) % len(poly)]
        body.face([(a[0], a[1], h), (oa[0], oa[1], h), (ob[0], ob[1], h), (b[0], b[1], h)], [uv] * 4, [grey(1.0)] * 4)


def slab(top, body, poly, h_top, h_bot, band, bevel=0.0004, top_col=None, bevel_shade=0.9, side_shade=1.0, side_solid=None, side_art=False, ink=0.0, scale=(1.0, 1.0)):
    """A card or sheet of outline poly (counter-clockwise, metres) from h_bot up to h_top:
    its top (fan from the centroid, art-mapped or a solid) on part `top`, a bevel ring down to the outline
    (art-mapped, a shade darker: the cel highlight's edge), and its side walls in an edge band on `body`."""
    if area(poly) < 0:
        poly = poly[::-1]
    rim(body, poly, h_bot + 0.00005, ink, scale)
    inner = inset(poly, bevel) if bevel > 0 else poly
    col = top_col or (lambda x, y: grey(1.0))
    cx = sum(p[0] for p in inner) / len(inner)
    cy = sum(p[1] for p in inner) / len(inner)
    for i in range(len(inner)):
        a, b = inner[i], inner[(i + 1) % len(inner)]
        pts = [(cx, cy, h_top), (a[0], a[1], h_top), (b[0], b[1], h_top)]
        top.face(pts, [top.art_uv(p[0], p[1]) for p in pts], [col(p[0], p[1]) for p in pts])
    h_rim = h_top - bevel * 0.8
    if bevel > 0:
        for i in range(len(poly)):
            a, b, ia, ib = poly[i], poly[(i + 1) % len(poly)], inner[i], inner[(i + 1) % len(poly)]
            pts = [(ia[0], ia[1], h_top), (a[0], a[1], h_rim), (b[0], b[1], h_rim), (ib[0], ib[1], h_top)]
            top.face(pts, [top.art_uv(p[0], p[1]) for p in pts],
                     [tuple(c * bevel_shade for c in col(p[0], p[1])[:3]) + (1.0,) for p in pts])
    s = 0.0
    for i in range(len(poly)):
        a, b = poly[i], poly[(i + 1) % len(poly)]
        ln = math.hypot(b[0] - a[0], b[1] - a[1])
        pts = [(a[0], a[1], h_rim), (a[0], a[1], h_bot), (b[0], b[1], h_bot), (b[0], b[1], h_rim)]
        if side_art:
            # the side takes the art's colour just inside the outline (a booklet's cover: its painted border)
            top.face(pts, [top.art_uv(p[0] * 0.996, p[1] * 0.996) for p in pts], [grey(side_shade)] * 4)
        else:
            uvs = [solid_uv(side_solid)] * 4 if side_solid else [edge_uv(band, s, 0), edge_uv(band, s, 1), edge_uv(band, s + ln, 1), edge_uv(band, s + ln, 0)]
            body.face(pts, uvs, [grey(side_shade)] * 4)
        s += ln


def solid_box(body, x0, y0, x1, y1, h0, h1, kind, shade=1.0):
    """A small box in one solid colour (a stitch, a hinge pin)."""
    uv = solid_uv(kind)
    c = grey(shade)
    P = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    body.face([(p[0], p[1], h1) for p in P], [uv] * 4, [c] * 4)
    for i in range(4):
        a, b = P[i], P[(i + 1) % 4]
        body.face([(a[0], a[1], h1), (a[0], a[1], h0), (b[0], b[1], h0), (b[0], b[1], h1)], [uv] * 4, [grey(shade * 0.8)] * 4)


# ---------------------------------------------------------------------------------------------
# The rulebook folder (art Forms/rulebook_folder, 2048 x 1152 on a 0.46 x 0.25875 m card)
# ---------------------------------------------------------------------------------------------
FW, FD = 0.46, 0.25875


def F(sx, sy):
    """A place on the folder art (shares from its top left) in metres."""
    return ((sx - 0.5) * FW, (0.5 - sy) * FD)


def folder():
    top = Part('FolderTop', FW, FD)
    body = Part('FolderBody', FW, FD)
    top.flex = body.flex = 0.0  # the boards, spine, flaps and clip are stiff; the loose sheets flex
    board_top, board_bot = -0.0032, -0.0052
    sheet = 0.0004
    spine_l, spine_r = 0.466, 0.543
    y_top, y_bot = 0.072, 0.983
    ramp = 0.045  # shares of the width each half dips over toward the spine (the halves lie open)
    dip = 0.0006

    def shade_x(sx):
        # the halves darken as they dip into the spine (baked: the folder lying open)
        d = min(abs(sx - spine_l), abs(sx - spine_r)) if not (spine_l <= sx <= spine_r) else 0.0
        return 1.0 - 0.14 * max(0.0, 1.0 - d / ramp)

    # Each half: a flat board with its outer corners rounded, then a ramp dipping into the spine.
    for side in ('left', 'right'):
        if side == 'left':
            x0, x1, xr = 0.027, spine_l - ramp, spine_l
            radii = (0.006, 0.0, 0.0, 0.006)
        else:
            x0, x1, xr = spine_r + ramp, 0.979, spine_r
            radii = (0.0, 0.006, 0.006, 0.0)
        (ax, ay), (bx, by) = F(x0, y_bot), F(x1, y_top)
        poly = rounded_rect(ax, ay, bx, by, radii)
        slab(top, body, poly, board_top, board_bot, EDGE_BOARD, bevel=0.0007, ink=0.0011,
             top_col=lambda x, y: grey(shade_x(x / FW + 0.5)))
        # the ramp: a strip of quads from the flat board down into the spine
        steps = 4
        xs = [x1 + (xr - x1) * i / steps for i in range(steps + 1)] if side == 'left' else [xr + (x0 - xr) * i / steps for i in range(steps + 1)]
        for i in range(steps):
            sa, sb = xs[i], xs[i + 1]
            ha = board_top - dip * (abs(sa - (x1 if side == 'left' else x0)) / ramp) ** 1.5
            hb = board_top - dip * (abs(sb - (x1 if side == 'left' else x0)) / ramp) ** 1.5
            (pax, py0), (pbx, py1) = F(sa, y_bot), F(sb, y_top)
            pts = [(pax, py0, ha), (F(sb, y_bot)[0], py0, hb), (pbx, py1, hb), (F(sa, y_top)[0], py1, ha)]
            top.face(pts, [top.art_uv(p[0], p[1]) for p in pts], [grey(shade_x(p[0] / FW + 0.5)) for p in pts])
        # its outer end walls along the top and bottom of the ramp
        for sy in (y_bot, y_top):
            for i in range(steps):
                sa, sb = xs[i], xs[i + 1]
                pa, pb = F(sa, sy), F(sb, sy)
                pts = [(pa[0], pa[1], board_top - 0.0003), (pa[0], pa[1], board_bot), (pb[0], pb[1], board_bot), (pb[0], pb[1], board_top - 0.0003)]
                body.face(pts, [edge_uv(EDGE_BOARD, 0, 0), edge_uv(EDGE_BOARD, 0, 1), edge_uv(EDGE_BOARD, 0.01, 1), edge_uv(EDGE_BOARD, 0.01, 0)], [grey(1.0)] * 4)

    # The spine: a creased strip between the halves (a U across its width, darkest in its crease).
    cols = 8
    for i in range(cols):
        ta, tb = i / cols, (i + 1) / cols
        sa, sb = spine_l + (spine_r - spine_l) * ta, spine_l + (spine_r - spine_l) * tb
        ha = board_top - dip - 0.0005 * math.sin(math.pi * ta)
        hb = board_top - dip - 0.0005 * math.sin(math.pi * tb)
        ka = 0.86 - 0.12 * math.sin(math.pi * ta)
        kb = 0.86 - 0.12 * math.sin(math.pi * tb)
        (pax, py0), (pbx, py1) = F(sa, y_bot), F(sb, y_top)
        pts = [(pax, py0, ha), (F(sb, y_bot)[0], py0, hb), (pbx, py1, hb), (F(sa, y_top)[0], py1, ha)]
        top.face(pts, [top.art_uv(p[0], p[1]) for p in pts], [grey(ka), grey(kb), grey(kb), grey(ka)])
        for sy in (y_bot, y_top):
            pa, pb = F(sa, sy), F(sb, sy)
            pts = [(pa[0], pa[1], ha), (pa[0], pa[1], board_bot - 0.0002), (pb[0], pb[1], board_bot - 0.0002), (pb[0], pb[1], hb)]
            body.face(pts, [edge_uv(EDGE_BOARD, 0, 0), edge_uv(EDGE_BOARD, 0, 1), edge_uv(EDGE_BOARD, 0.01, 1), edge_uv(EDGE_BOARD, 0.01, 0)], [grey(0.9)] * 4)

    # The rivets on the spine: low brass domes (art-mapped: the art paints them).
    for sx, sy in ((0.508, 0.203), (0.506, 0.845)):
        cx, cy = F(sx, sy)
        r = 0.0125 * FW
        base = board_top - dip - 0.0004
        ring = [(cx + r * math.cos(2 * math.pi * k / 12), cy + r * math.sin(2 * math.pi * k / 12)) for k in range(12)]
        mid = [(cx + 0.6 * r * math.cos(2 * math.pi * k / 12), cy + 0.6 * r * math.sin(2 * math.pi * k / 12)) for k in range(12)]
        for k in range(12):
            a, b, ma, mb = ring[k], ring[(k + 1) % 12], mid[k], mid[(k + 1) % 12]
            pts = [(a[0], a[1], base), (b[0], b[1], base), (mb[0], mb[1], base + 0.0011), (ma[0], ma[1], base + 0.0011)]
            top.face(pts, [top.art_uv(p[0], p[1]) for p in pts], [grey(0.8)] * 4)
            pts = [(ma[0], ma[1], base + 0.0011), (mb[0], mb[1], base + 0.0011), (cx, cy, base + 0.0014)]
            top.face(pts, [top.art_uv(p[0], p[1]) for p in pts], [grey(1.05 if k < 6 else 0.95)] * 3)

    # The pocket flaps: a card on each half (the art's), standing proud of the board.
    flaps = [
        [(0.027, 0.962), (0.095, 0.895), (0.098, 0.335), (0.090, 0.312), (0.075, 0.305), (0.032, 0.312), (0.027, 0.330)],
        [(0.924, 0.910), (0.935, 0.925), (0.978, 0.962), (0.978, 0.468), (0.930, 0.548), (0.924, 0.565)],
    ]
    for poly in flaps:
        pts = [F(sx, sy) for sx, sy in poly]
        slab(top, body, pts, board_top + 0.0006, board_top, EDGE_BOARD, bevel=0.0004, ink=0.0006)

    # The loose sheets: the cover sheet on the left (3), the rules on the right (8, the top one exactly the art's).
    import random
    rnd = random.Random(11)
    top.flex = body.flex = 1.0
    for (x0, y0, x1, y1), n, top_h in (((0.098, 0.105, 0.470, 0.962), 3, board_top + 3 * sheet),
                                       ((0.545, 0.139, 0.922, 0.968), 8, 0.0)):
        for k in range(n):
            h_t = top_h - k * sheet
            dx = 0 if k == 0 else rnd.uniform(-0.0008, 0.0026)
            dy = 0 if k == 0 else rnd.uniform(-0.0026, 0.0006)
            ang = 0 if k == 0 else math.radians(rnd.uniform(-0.5, 0.5))
            (ax, ay), (bx, by) = F(x0, y1), F(x1, y0)
            poly = rounded_rect(ax, ay, bx, by, (0.0015,) * 4, seg=2)
            cx, cy = (ax + bx) / 2, (ay + by) / 2
            poly = [(cx + (px - cx) * math.cos(ang) - (py - cy) * math.sin(ang) + dx,
                     cy + (px - cx) * math.sin(ang) + (py - cy) * math.cos(ang) + dy) for px, py in poly]
            slab(top, body, poly, h_t, h_t - sheet, EDGE_SHEET, bevel=0.0, top_col=lambda x, y: grey(0.97 if k else 1.0), ink=0.0005)

    top.flex = body.flex = 0.0
    # The clip on the rules: a brass bar and its ring, low over the top sheet (under the parts' 1 mm).
    (bx0, by0), (bx1, by1) = F(0.651, 0.204), F(0.816, 0.160)
    slab(top, body, rounded_rect(bx0, by0, bx1, by1, (0.0045,) * 4, seg=3), 0.0008, 0.0, None, bevel=0.0006, bevel_shade=0.75, side_solid='brass', side_shade=0.8, ink=0.0005)
    (rx0, ry0), (rx1, ry1) = F(0.716, 0.165), F(0.750, 0.085)
    slab(top, body, rounded_rect(rx0, ry0, rx1, ry1, (0.0, 0.0, 0.0075, 0.0075), seg=4), 0.0005, board_top + 0.0002, None, bevel=0.0004, bevel_shade=0.8, side_solid='brass', side_shade=0.7)
    return [top, body]


# ---------------------------------------------------------------------------------------------
# A raised index tab (its art is the whole quad; a slight trapezoid, the top corners round)
# ---------------------------------------------------------------------------------------------
def tab():
    tw, td = 1.0, 1.0  # unit space: the builder scales the tab plate to its art's size
    top = Part('FolderTabTop', tw, td)
    body = Part('FolderTabBody', tw, td)
    top.flex = body.flex = 0.0
    poly = [(-0.5, -0.49), (0.498, -0.49), (0.481, -0.262), (0.466, 0.214), (0.435, 0.405), (-0.447, 0.405), (-0.477, 0.31), (-0.483, 0.024)]
    slab(top, body, poly, 0.0, -0.0006, EDGE_BOARD, bevel=0.0, ink=0.0005, scale=(0.0907, 0.0161))
    return [top, body]


# ---------------------------------------------------------------------------------------------
# The passport: an open booklet (its face art covers the unit square, its painted border the cover)
# ---------------------------------------------------------------------------------------------
def booklet():
    top = Part('BookletTop', 1.0, 1.0)
    body = Part('BookletBody', 1.0, 1.0)
    # Unit space across, metres in height; the booklet is about 0.14 x 0.22 m, so a millimetre is
    # 1/140 of the width and 1/220 of the depth.
    MX, MY = 1 / 0.14, 1 / 0.22
    page_top, cover_top, cover_bot = 0.0, -0.0026, -0.0039
    bx, by = 0.0165, 0.0105  # the painted cover border (shares)
    # The cover: one board under both pages; its top band (the art's painted border) shows round the
    # pages, and its sides take the border's colour, so its edge is the cover's.
    corner = 0.0025
    outline = rounded_rect(-0.5, -0.5, 0.5, 0.5, (corner * MX,) * 4, seg=3)
    # The whole booklet bends as one (Saleh's 1018a playtest: "real bend and flex on ... passport pages"): its
    # cover and its pages together, so the pages never bend into the cover; how much is the game's knob
    # (MotionKnobs.bookletFlex), and the grid lets it curve.
    top.flex = body.flex = 1.0
    top.dice = body.dice = (10, 12)
    slab(top, body, outline, cover_top, cover_bot, EDGE_SHEET, bevel=0.0, side_shade=0.72, side_art=True, ink=0.0008, scale=(0.14, 0.22))
    # The page blocks: two, each its page, curving down into the gutter at y 0 and stitched there.
    gutter = 0.07  # shares of the height the pages curve over toward the fold
    rows = 6
    x0, x1 = -0.5 + bx, 0.5 - bx
    for sign in (1, -1):
        y_outer = sign * (0.5 - by)
        # the ink line where the page block meets the cover
        rim(body, [(x0, min(y_outer, 0.0)), (x1, min(y_outer, 0.0)), (x1, max(y_outer, 0.0)), (x0, max(y_outer, 0.0))], cover_top + 0.00005, 0.0005, (0.14, 0.22))
        # the page's profile from its outer edge to the fold: flat, then curving down (baked darker)
        prof = [(y_outer, page_top, 1.0), (sign * gutter, page_top, 1.0)]
        for r in range(1, rows + 1):
            t = r / rows
            prof.append((sign * gutter * (1 - t), page_top - 0.0013 * t ** 2, 1.0 - 0.3 * t ** 1.6))
        for (ya, ha, ka), (yb, hb, kb) in zip(prof, prof[1:]):
            pts = [(x0, ya, ha), (x1, ya, ha), (x1, yb, hb), (x0, yb, hb)]
            cs = [grey(ka), grey(ka), grey(kb), grey(kb)]
            if sign > 0:
                pts, cs = pts[::-1], cs[::-1]
            top.face(pts, [top.art_uv(p[0], p[1]) for p in pts], cs)
            # the block's two ends follow the profile, page lines down to the cover
            for x, flip in ((x1, False), (x0, True)):
                q = [(x, ya, ha), (x, ya, cover_top), (x, yb, cover_top), (x, yb, hb)]
                if flip != (sign < 0):
                    q = [q[3], q[2], q[1], q[0]]
                sa = abs(ya) / MY
                sb = abs(yb) / MY
                body.face(q, [edge_uv(EDGE_PAGES, sa, 0), edge_uv(EDGE_PAGES, sa, 1), edge_uv(EDGE_PAGES, sb, 1), edge_uv(EDGE_PAGES, sb, 0)], [grey(1.0)] * 4)
        # the outer edge
        q = [(x0, y_outer, page_top), (x0, y_outer, cover_top), (x1, y_outer, cover_top), (x1, y_outer, page_top)]
        if sign > 0:
            q = [q[3], q[2], q[1], q[0]]
        body.face(q, [edge_uv(EDGE_PAGES, 0, 0), edge_uv(EDGE_PAGES, 0, 1), edge_uv(EDGE_PAGES, 1 / MX, 1), edge_uv(EDGE_PAGES, 1 / MX, 0)], [grey(1.0)] * 4)
    # The stitches along the fold: short thread dashes in the crease.
    for k in range(7):
        xc = -0.5 + bx + (1 - 2 * bx) * (k + 0.5) / 7
        solid_box(body, xc - 0.0025 * MX, -0.0004 * MY, xc + 0.0025 * MX, 0.0004 * MY, page_top - 0.0013, page_top - 0.00118, 'thread')
    return [top, body]


# ---------------------------------------------------------------------------------------------
# A single sheet: a gentle curl along its long edges and its bottom right corner lifted
# ---------------------------------------------------------------------------------------------
def sheet():
    top = Part('SheetTop', 1.0, 1.0)
    body = Part('SheetBody', 1.0, 1.0)
    nx, ny = 10, 14
    th = 0.0007
    lift_max = 0.0007  # under the parts' 1 mm: nothing printed over it pokes through the paper above

    def h(u, v):
        # u, v in 0..1 from the bottom left; the bottom right corner lifts, the edges barely curl
        d = math.hypot(1 - u, v)  # distance from the bottom right corner
        corner = lift_max * max(0.0, 1 - d / 0.28) ** 2
        curl = 0.0002 * (max(0.0, 1 - u / 0.06) ** 2 + max(0.0, 1 - (1 - u) / 0.06) ** 2)
        return corner + curl

    def k(u, v):
        d = math.hypot(1 - u, v)
        return 1.0 - 0.1 * max(0.0, 1 - d / 0.28)

    P = lambda u, v: (u - 0.5, v - 0.5, h(u, v))
    for i in range(nx):
        for j in range(ny):
            us, vs = (i / nx, (i + 1) / nx), (j / ny, (j + 1) / ny)
            pts = [P(us[0], vs[0]), P(us[1], vs[0]), P(us[1], vs[1]), P(us[0], vs[1])]
            cs = [grey(k(us[0], vs[0])), grey(k(us[1], vs[0])), grey(k(us[1], vs[1])), grey(k(us[0], vs[1]))]
            top.face(pts, [top.art_uv(p[0], p[1]) for p in pts], cs)
    # the edge, following the curl
    loop = [(i / nx, 0) for i in range(nx)] + [(1, j / ny) for j in range(ny)] + [(1 - i / nx, 1) for i in range(nx)] + [(0, 1 - j / ny) for j in range(ny)]
    s = 0
    for q in range(len(loop)):
        (ua, va), (ub, vb) = loop[q], loop[(q + 1) % len(loop)]
        a, b = P(ua, va), P(ub, vb)
        ln = math.hypot((ub - ua) * 0.26, (vb - va) * 0.34)
        pts = [a, (a[0], a[1], a[2] - th), (b[0], b[1], b[2] - th), b]
        body.face(pts, [edge_uv(EDGE_SHEET, s, 0), edge_uv(EDGE_SHEET, s, 1), edge_uv(EDGE_SHEET, s + ln, 1), edge_uv(EDGE_SHEET, s + ln, 0)], [grey(1.0)] * 4)
        s += ln
    rim(body, [(-0.5, -0.5), (0.5, -0.5), (0.5, 0.5), (-0.5, 0.5)], -th + 0.00005, 0.0006, (0.26, 0.34))
    return [top, body]


# ---------------------------------------------------------------------------------------------
# A thick card (the transponder card, the ticket): rounded corners, a bevelled rim
# ---------------------------------------------------------------------------------------------
def card():
    top = Part('CardTop', 1.0, 1.0)
    body = Part('CardBody', 1.0, 1.0)
    top.flex = body.flex = 1.0
    top.dice = body.dice = (8, 5)
    # The card is about 0.21 x 0.12 m on the desk: a 7 mm corner, a 0.6 mm bevel.
    W, D = 0.21, 0.12
    outline = rounded_rect(-W / 2, -D / 2, W / 2, D / 2, (0.007,) * 4, seg=5)
    tmp_top, tmp_body = Part('t', W, D), Part('b', W, D)
    tmp_top.flex = tmp_body.flex = 1.0  # plastic barely gives: how much is the game's knob (MotionKnobs.cardFlex)
    slab(tmp_top, tmp_body, outline, 0.0, -0.0012, EDGE_SHEET, bevel=0.0006, bevel_shade=0.82, ink=0.0007)
    for src, dst in ((tmp_top, top), (tmp_body, body)):
        for pts, uvs, cs in src.faces:
            dst.face([(p[0] / W, p[1] / D, p[2]) for p in pts], uvs, cs)
    return [top, body]


# ---------------------------------------------------------------------------------------------
# Writing: Blender objects, FBX in the quad's unit space, and a preview render
# ---------------------------------------------------------------------------------------------
def to_object(part, scale=(1.0, 1.0), unit=False, collection=None):
    me = bpy.data.meshes.new(part.name)
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new('UVMap')
    col_layer = bm.loops.layers.color.new('Col')
    for pts, uvs, cs in part.faces:
        verts = []
        for x, y, hh in pts:
            if unit:
                # the quad's unit space: x and y over the size, the depth under the face along z
                # Unity reads an FBX vertex (bx, by, bz) as (-bx, bz, -by); this writes (-x, h, y) so
                # Unity sees (x, y, -h): the quad's own space, its front (the face) toward -z.
                xu = x / part.width if part.width != 1.0 else x
                yu = y / part.depth if part.depth != 1.0 else y
                verts.append(bm.verts.new((-xu, hh, yu)))
            else:
                verts.append(bm.verts.new((x * scale[0], y * scale[1], hh)))
        try:
            f = bm.faces.new(verts)
        except ValueError:
            continue
        for loop, uv, c in zip(f.loops, uvs, cs):
            loop[uv_layer].uv = uv
            loop[col_layer] = (min(c[0], 1.0), min(c[1], 1.0), min(c[2], 1.0), c[3])
    if unit and part.dice:
        # the grid's cuts: x across (Blender -x), y up the page (Blender +z); each cut splits every face it
        # crosses, interpolating its UVs and colours, so the face's look is unchanged and a bend can curve it
        nx, ny = part.dice
        for k in range(1, nx):
            c = -0.5 + k / nx
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(-c, 0, 0), plane_no=(1, 0, 0))
        for k in range(1, ny):
            c = -0.5 + k / ny
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, 0, c), plane_no=(0, 0, 1))
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(part.name, me)
    (collection or bpy.context.scene.collection).objects.link(ob)
    return ob


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def export(parts, path):
    clear()
    obs = [to_object(p, unit=True) for p in parts]
    # Unit-space top faces must face the camera along -z (the quad's front): the faces were built
    # counter-clockwise seen from above (+h); with h flipped to -z they now face -z.
    for ob in obs:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'}, mesh_smooth_type='FACE',
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             axis_forward='-Z', axis_up='Y', use_mesh_modifiers=False, add_leaf_bones=False)
    print('exported', path, [o.name for o in obs])


def art_material(name, image_path, edges_path):
    """A toon preview material: the art (or the edge atlas) times the vertex colour, stepped light."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(image_path)
    tex.interpolation = 'Linear'
    vc = nt.nodes.new('ShaderNodeVertexColor')
    vc.layer_name = 'Col'
    mul = nt.nodes.new('ShaderNodeMix')
    mul.data_type = 'RGBA'
    mul.blend_type = 'MULTIPLY'
    mul.inputs[0].default_value = 1.0
    nt.links.new(tex.outputs['Color'], mul.inputs[6])
    nt.links.new(vc.outputs['Color'], mul.inputs[7])
    diff = nt.nodes.new('ShaderNodeBsdfDiffuse')
    s2r = nt.nodes.new('ShaderNodeShaderToRGB')
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation = 'CONSTANT'
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = (0.62, 0.55, 0.72, 1)
    ramp.color_ramp.elements[1].position = 0.18
    ramp.color_ramp.elements[1].color = (0.84, 0.8, 0.9, 1)
    e = ramp.color_ramp.elements.new(0.45)
    e.color = (1, 1, 1, 1)
    nt.links.new(diff.outputs['BSDF'], s2r.inputs['Shader'])
    nt.links.new(s2r.outputs['Color'], ramp.inputs['Fac'])
    tone = nt.nodes.new('ShaderNodeMix')
    tone.data_type = 'RGBA'
    tone.blend_type = 'MULTIPLY'
    tone.inputs[0].default_value = 1.0
    nt.links.new(mul.outputs[2], tone.inputs[6])
    nt.links.new(ramp.outputs['Color'], tone.inputs[7])
    em = nt.nodes.new('ShaderNodeEmission')
    nt.links.new(tone.outputs[2], em.inputs['Color'])
    nt.links.new(em.outputs['Emission'], out.inputs['Surface'])
    return mat


def render(parts_sets, path, cam_loc=(-0.05, -0.32, 0.78), cam_tilt=22, lens=32):
    clear()
    scene = bpy.context.scene
    for engine in ('BLENDER_EEVEE', 'BLENDER_EEVEE_NEXT', 'BLENDER_WORKBENCH'):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue
    print('render engine', scene.render.engine)
    scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
    scene.render.film_transparent = False
    world = bpy.data.worlds.new('w')
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.25, 0.08, 0.1, 1)
    edges = os.path.join(OUT, 'docs_edges.png')
    # desk
    bpy.ops.mesh.primitive_plane_add(size=2.0, location=(0, 0, -0.0045))
    desk = bpy.context.active_object
    dm = bpy.data.materials.new('desk')
    dm.use_nodes = True
    dm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.32, 0.09, 0.12, 1)
    desk.data.materials.append(dm)
    for parts, offset, art, scale in parts_sets:
        for p in parts:
            ob = to_object(p, scale=scale)
            ob.location = offset
            is_top = p.name.endswith('Top')
            ob.data.materials.append(art_material(p.name + '_m', art if is_top else edges, edges))
    sun = bpy.data.lights.new('sun', 'SUN')
    sun.energy = 3.0
    so = bpy.data.objects.new('sun', sun)
    so.rotation_euler = (math.radians(35), math.radians(-30), math.radians(20))
    scene.collection.objects.link(so)
    cam = bpy.data.cameras.new('cam')
    cam.lens = lens
    co = bpy.data.objects.new('cam', cam)
    co.location = cam_loc
    co.rotation_euler = (math.radians(cam_tilt), 0, 0)
    scene.collection.objects.link(co)
    scene.camera = co
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print('rendered', path)


def bake_shade(part, px_w, px_h, metres, path, rise=0.5, reach=0.014, tint=(0.64, 0.56, 0.76)):
    """The baked cel shadows of a model's top on itself (its sheets on its boards, its clip on its rules, its
    page blocks on its covers): a height map of the top faces (each face at its mean height, the highest
    wins), then each texel is in shadow when something toward the light (the top left of the reading
    view, rise metres up per metre across) stands above the ray; written as a multiplier (white, or the
    purple-tinted shade) over the UV square, so the shader multiplies it under the face art."""
    import numpy as np
    sx, sy = metres
    H = np.full((px_h, px_w), -1.0, dtype=np.float64)
    us = (np.arange(px_w) + 0.5) / px_w
    vs = 1.0 - (np.arange(px_h) + 0.5) / px_h
    for pts, uvs, cs in part.faces:
        poly = [(u, v) for u, v in uvs]
        h = sum(p[2] for p in pts) / len(pts)
        u0, u1 = max(0.0, min(q[0] for q in poly)), min(1.0, max(q[0] for q in poly))
        v0, v1 = max(0.0, min(q[1] for q in poly)), min(1.0, max(q[1] for q in poly))
        if u1 <= u0 or v1 <= v0:
            continue
        c0, c1 = int(u0 * px_w), min(px_w, int(u1 * px_w) + 1)
        r0, r1 = int((1 - v1) * px_h), min(px_h, int((1 - v0) * px_h) + 1)
        U, V = np.meshgrid(us[c0:c1], vs[r0:r1])
        inside = np.zeros(U.shape, dtype=bool)
        n = len(poly)
        for k in range(n):
            (ax, ay), (bx, by) = poly[k], poly[(k + 1) % n]
            if ay == by:
                continue
            cond = ((ay > V) != (by > V)) & (U < (bx - ax) * (V - ay) / (by - ay) + ax)
            inside ^= cond
        block = H[r0:r1, c0:c1]
        H[r0:r1, c0:c1] = np.where(inside & (h > block), h, block)
    # march toward the light: up-left in metres (-LeanRight, +LeanNear), one texel at a time
    shade = np.zeros_like(H, dtype=bool)
    step_m = min(sx / px_w, sy / px_h)
    steps = int(reach / step_m)
    for k in range(1, steps + 1):
        dx_m, dy_m = -0.5 * k * step_m, 0.866 * k * step_m
        dc = int(round(dx_m / (sx / px_w)))
        dr = -int(round(dy_m / (sy / px_h)))
        shifted = np.full_like(H, -1.0)
        rs = slice(max(0, -dr), px_h - max(0, dr))
        rd = slice(max(0, dr), px_h - max(0, -dr))
        cs_ = slice(max(0, -dc), px_w - max(0, dc))
        cd = slice(max(0, dc), px_w - max(0, -dc))
        shifted[rs, cs_] = H[rd, cd]
        shade |= (shifted > H + rise * k * step_m + 0.0001) & (H > -0.5)
    img = np.ones((px_h, px_w, 4), dtype=np.float32)
    for ch in range(3):
        img[:, :, ch] = np.where(shade, tint[ch], 1.0)
    # one texel of softening for the cel edge
    soft = img.copy()
    soft[1:-1, 1:-1, :3] = (img[1:-1, 1:-1, :3] * 4 + img[:-2, 1:-1, :3] + img[2:, 1:-1, :3] + img[1:-1, :-2, :3] + img[1:-1, 2:, :3]) / 8
    im = bpy.data.images.new(os.path.basename(path), px_w, px_h, alpha=False)
    im.pixels = soft[::-1].ravel()
    im.filepath_raw = path
    im.file_format = 'PNG'
    im.save()
    print('baked', path, int(shade.sum()), 'texels in shade')


os.makedirs(OUT, exist_ok=True)
models = {'Folder': folder(), 'FolderTab': tab(), 'Booklet': booklet(), 'Sheet': sheet(), 'Card': card()}
for name, parts in models.items():
    export(parts, os.path.join(OUT, f'{name}.fbx'))
bake_shade(models['Folder'][0], 1024, 576, (FW, FD), os.path.join(OUT, 'folder_shade.png'))
bake_shade(models['Booklet'][0], 640, 980, (0.14, 0.22), os.path.join(OUT, 'booklet_shade.png'))

if DO_RENDER:
    os.makedirs(RENDER_DIR, exist_ok=True)
    fol = (models['Folder'], (-0.24, 0.0, 0.0), os.path.join(ART, 'rulebook_folder.png'), (1.0, 1.0))
    pas = (models['Booklet'], (0.22, 0.0, 0.0), os.path.join(ART, 'paper_tc101.png'), (0.19, 0.29))
    render([fol, pas], os.path.join(RENDER_DIR, 'blender_folder_passport.png'))
    render([fol], os.path.join(RENDER_DIR, 'blender_folder_close.png'), cam_loc=(-0.2, -0.36, 0.32), cam_tilt=42, lens=35)
    render([pas], os.path.join(RENDER_DIR, 'blender_passport_close.png'), cam_loc=(0.26, -0.3, 0.26), cam_tilt=44, lens=35)
