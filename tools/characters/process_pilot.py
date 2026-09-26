"""Processes the character-art pilot (batch01-pilot) into game-ready layers.

    python tools/characters/process_pilot.py [--out DIR] [--qa DIR] [--report FILE] [--no-meta]

Reads the raw green-and-magenta sources in
ArtDeliverables/TimeDesk/Characters/Raw/batch01-pilot/, keys them, registers
every layer to its base figure and the base figures to LookCanvas, splits and
bakes the variants the brief asks Claude for, and writes each game key as a
1024 x 1536 RGBA PNG (untrimmed) into the Resources folder CharacterArt loads
from (default: Assets/Art/Characters/Resources/Characters), with a Unity
.meta for each new file. Every decision is in tools/characters/README.md.

--qa DIR also writes the QA composites (full stacks, desk size, passport
crops, keying contact sheet, alpha-edge check) and qa.json into DIR.
"""

import argparse
import json
import os
import sys
import time

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from charkit import contract, imgops as io, keying, layers, measure, register as rg, unitymeta, warp  # noqa: E402
from charkit import landmarks as lm  # noqa: E402

RAW = "ArtDeliverables/TimeDesk/Characters/Raw/batch01-pilot"

# Every selected raw source: its layer, gender, place and how it is registered.
#   base      the whole figure, registered to LookCanvas (scalp, soles, centre line)
#   mannequin drawn on the full magenta mannequin: registered by the mannequin's contour to the base
#   head      a head-only fitting form: registered by the face (eyes, nose, mouth, chin) to the base
SOURCES = [
    {"raw": "base_m_skin1_facea", "kind": "base", "g": "m"},
    {"raw": "base_f_skin1_facea", "kind": "base", "g": "f"},
    {"raw": "outfit_m_greece_ancient", "kind": "mannequin", "g": "m", "layer": "outfit", "place": ("greece", "ancient")},
    {"raw": "hair_m_greece_ancient", "kind": "mannequin", "g": "m", "layer": "hair", "place": ("greece", "ancient")},
    {"raw": "facialhair_m_greece_ancient", "kind": "mannequin", "g": "m", "layer": "facialhair", "place": ("greece", "ancient")},
    {"raw": "headwear_m_greece_ancient", "kind": "mannequin", "g": "m", "layer": "headwear", "place": ("greece", "ancient")},
    {"raw": "accessory_m_greece_ancient", "kind": "mannequin", "g": "m", "layer": "accessory", "place": ("greece", "ancient")},
    {"raw": "outfit_f_greece_ancient", "kind": "mannequin", "g": "f", "layer": "outfit", "place": ("greece", "ancient")},
    {"raw": "hair_f_greece_ancient", "kind": "head", "g": "f", "layer": "hair", "place": ("greece", "ancient")},
    {"raw": "headwear_f_greece_ancient", "kind": "head", "g": "f", "layer": "headwear", "place": ("greece", "ancient")},
    {"raw": "outfit_f_egypt_medieval", "kind": "mannequin", "g": "f", "layer": "outfit", "place": ("egypt", "medieval")},
    {"raw": "hair_f_egypt_medieval", "kind": "head", "g": "f", "layer": "hair", "place": ("egypt", "medieval")},
    {"raw": "headwear_f_egypt_medieval", "kind": "head", "g": "f", "layer": "headwear", "place": ("egypt", "medieval")},
    {"raw": "hair_f_egypt_ancient", "kind": "head", "g": "f", "layer": "hair", "place": ("egypt", "ancient")},
    {"raw": "accessory_m_japan_ancient", "kind": "mannequin", "g": "m", "layer": "accessory", "place": ("japan", "ancient")},
]

SLOT = {"outfit": "outfit", "hair": "hair", "facialhair": "facialHair", "headwear": "headwear", "accessory": "accessory"}
FACE_POINTS = ("eye_l", "eye_r", "nose", "mouth", "chin")
MANNEQUIN_MARKS = ("eye_l", "eye_r", "nose")
HEAD_LAYERS = ("hair", "facialhair", "headwear")
NECK_PX = 70  # the head-and-neck contour ends this far below the chin


def r1(v):
    return round(float(v), 1)


def pt(p):
    return [r1(p[0]), r1(p[1])]


class Pilot:
    def __init__(self, out_dir, write_meta):
        self.C = contract.canvas()
        self.size = (self.C["Width"], self.C["Height"])
        self.out_dir = out_dir
        self.write_meta = write_meta
        self.swatches = contract.skin_swatches()
        self.hair = contract.hair_colours()
        self.bases = {}
        self.report = {"canvas": self.C, "sources": {}, "keys": {}}
        self.keyed = {}  # raw name -> (source rgb, keyed dict) for the QA contact sheet
        self.canvas_layers = {}  # key -> (rgb, alpha) on the canvas, for QA

    # --- output ------------------------------------------------------------------------------

    def save(self, key, rgb, alpha, source):
        path = os.path.join(self.out_dir, key + ".png")
        visible = alpha > 0
        filled, _ = io.bleed(np.where(visible[..., None], rgb, 0), visible, 6)
        rgb = np.where(visible[..., None], rgb, filled)
        io.save_rgba(path, rgb, alpha)
        if self.write_meta:
            unitymeta.texture_meta(path, self.C["Height"], self.C["Feet"])
        ys, xs = np.nonzero(alpha > 0.5)
        self.report["keys"][key] = {"source": source, "bbox": [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]
                                    if len(xs) else None}
        self.canvas_layers[key] = (rgb, alpha)

    # --- bases -------------------------------------------------------------------------------

    def process_base(self, src):
        g = src["g"]
        rgb = io.load_rgb(os.path.join(contract.ROOT, RAW, src["raw"] + ".png"))
        k = keying.key(rgb, mannequin=False)
        self.keyed[src["raw"]] = (rgb, k)
        m = measure.base(rgb, k)
        C = self.C
        s = (C["Feet"] - C["HeadTop"]) / (m["soles"] - m["top"])
        T = rg.Similarity(s, C["CenterX"] - s * m["centre_x"], C["HeadTop"] - s * m["top"])
        crgb, calpha = warp.warp(k["rgb"], k["alpha"], T, self.size)

        face_c = {n: T.apply([m["face"][n]])[0] for n in FACE_POINTS if n in m["face"]}
        body_mid = lm.centre_x(m["figure"], m["top"] + 400, m["top"] + 700)
        chin_c = face_c["chin"][1]
        fig = m["figure"]
        # skin: normalise tone 1 to its swatch; bodies 2..5 recoloured from tone 1
        skin_src = layers.skin_colour(crgb, calpha)
        head_a, body_rgb, body_a = layers.split_head_body(crgb, calpha, chin_c, C["CenterX"])
        w_head = layers.skin_weight(crgb, head_a, skin_src)
        head_rgb = layers.recolour(crgb, w_head, skin_src, self.swatches[0])
        self.save(contract.head_key(g, 1, "a"), head_rgb, head_a, src["raw"])
        w_body = layers.skin_weight(body_rgb, body_a, skin_src)
        for tone in range(1, 6):
            body = layers.recolour(body_rgb, w_body, skin_src, self.swatches[tone - 1])
            self.save(contract.body_key(g, tone), body, body_a, src["raw"])

        # contour and face of the base in its own (source) frame, for the layers
        edge = fig & ~io.erode(fig, 1)
        self.bases[g] = {"T": T, "edge": edge, "edge_pts": lm.contour(fig), "face": m["face"], "face_canvas": face_c,
                         "alpha": calpha, "head_alpha": head_a}
        self.report["sources"][src["raw"]] = {
            "kind": "base", "transform_to_canvas": T.as_dict(),
            "measured_source": {"head_top": r1(m["top"]), "soles": r1(m["soles"]), "skull_centre_x": r1(m["centre_x"]),
                                "body_centre_x": r1(body_mid), "face": {n: pt(m["face"][n]) for n in FACE_POINTS if n in m["face"]}},
            "canvas": {"head_top": r1(T.apply([[0, m["top"]]])[0][1]), "soles": r1(T.apply([[0, m["soles"]]])[0][1]),
                       "centre_x": r1(T.apply([[m["centre_x"], 0]])[0][0]),
                       "chin": r1(chin_c), "face": {n: pt(p) for n, p in face_c.items()}},
            "error_vs_LookCanvas_px": {"head_top": r1(T.apply([[0, m["top"]]])[0][1] - C["HeadTop"]),
                                       "chin": r1(chin_c - C["Chin"]),
                                       "soles": r1(T.apply([[0, m["soles"]]])[0][1] - C["Feet"]),
                                       "centre_line": r1(T.apply([[m["centre_x"], 0]])[0][0] - C["CenterX"]),
                                       "body_centre_line": r1(T.apply([[body_mid, 0]])[0][0] - C["CenterX"])},
            "head_height_canvas_px": r1(chin_c - C["HeadTop"]),
            "skin_measured": [int(v) for v in skin_src], "skin_swatches": self.swatches,
        }

    # --- layers ------------------------------------------------------------------------------

    def register_layer(self, src, rgb, k):
        base = self.bases[src["g"]]
        info = {"kind": src["kind"]}
        if src["kind"] == "mannequin":
            pts = measure.mannequin_contour(k)
            face = measure.mannequin_face(rgb, k)
            if face:
                # the mannequin's mouth and chin marks are faint: only the eyes and the nose are measured
                face = {n: face[n] for n in MANNEQUIN_MARKS}
            T_src, stats = rg.chamfer_fit(pts, base["edge"], base["edge_pts"], rg.Similarity())
            info["body_fit"] = dict(stats, transform=T_src.as_dict())
            if src["layer"] in HEAD_LAYERS:
                # A head item must fit the head: ChatGPT can redraw the head a few pixels off the
                # body, so the fit uses only the head-and-neck contour plus the eye and nose marks.
                limit = base["face"]["chin"][1] + NECK_PX
                head_pts = pts[pts[:, 1] < limit]
                edge = base["edge"].copy()
                edge[int(limit):] = False
                dst = base["edge_pts"][base["edge_pts"][:, 1] < limit]
                marks = [n for n in MANNEQUIN_MARKS if face and n in face]
                T_src, stats = rg.chamfer_fit(head_pts, edge, dst, T_src,
                                              marks_src=[face[n] for n in marks] if marks else None,
                                              marks_dst=[base["face"][n] for n in marks] if marks else None)
                info["head_fit"] = dict(stats, marks=marks)
            info.update(stats)
        else:
            face = measure.head_only(rgb, k)
            use = [n for n in FACE_POINTS if n in face and n in base["face"]]
            T_src = rg.fit_points([face[n] for n in use], [base["face"][n] for n in use])
            info["fit_points"] = use
        T = T_src.then(base["T"])
        info["transform_to_base"] = T_src.as_dict()
        info["transform_to_canvas"] = T.as_dict()
        # Face residuals on the canvas: this layer's face marks vs the base's face (independent
        # of the fit for mannequin sources; the fit's own residual for head-only sources).
        if face:
            res = {}
            for n in FACE_POINTS:
                if n in face and n in base["face_canvas"]:
                    d = T.apply([face[n]])[0] - base["face_canvas"][n]
                    res[n] = [r1(d[0]), r1(d[1])]
            info["face_residual_px"] = res
            info["face_residual_max_px"] = r1(max(np.hypot(*v) for v in res.values())) if res else None
        return T, info

    def process_layer(self, src):
        g, layer = src["g"], src["layer"]
        nation, era = src["place"]
        item = contract.wardrobe(nation, era)[g][SLOT[layer]]
        rgb = io.load_rgb(os.path.join(contract.ROOT, RAW, src["raw"] + ".png"))
        k = keying.key(rgb, mannequin=True)
        self.keyed[src["raw"]] = (rgb, k)
        T, info = self.register_layer(src, rgb, k)
        crgb, calpha = warp.warp(k["rgb"], k["alpha"], T, self.size)
        mag_w = warp.warp_mask(k["magenta"], T, self.size)
        green_w = warp.warp_mask(k["green"], T, self.size)
        base = self.bases[g]
        crgb, calpha, sealed = layers.seal_gaps(crgb, calpha, mag_w, green_w, base["alpha"])
        info["sealed_gap_px"] = sealed
        if layer == "headwear" and "Hair" in (item.get("covers") or []):
            info["hair_left_uncovered_px"] = self.hair_uncovered(calpha, g)

        keys = []
        if layer == "hair":
            back = bool(item.get("back"))
            front_a, back_a = calpha, None
            if back:
                fc = base["face_canvas"]
                front_a, back_a = layers.split_hairback(crgb, calpha, fc["chin"][1], self.C["CenterX"],
                                                        face_half_width=abs(fc["eye_r"][0] - fc["eye_l"][0]))
                info["hairback_px"] = int((back_a > 0.5).sum())
            if item.get("wig"):
                self.save(contract.garment_key("hair", g, nation, era), crgb, front_a, src["raw"])
                keys.append(contract.garment_key("hair", g, nation, era))
                if back:
                    self.save(contract.garment_key("hairback", g, nation, era), crgb, back_a, src["raw"])
                    keys.append(contract.garment_key("hairback", g, nation, era))
            else:
                keys += self.bake_hair_set("hair", g, nation, era, crgb, front_a, back_a, src["raw"], info)
        elif layer == "facialhair":
            keys += self.bake_hair_set("facialhair", g, nation, era, crgb, calpha, None, src["raw"], info)
        else:
            key = contract.garment_key(layer, g, nation, era)
            self.save(key, crgb, calpha, src["raw"])
            keys.append(key)
        info["keys"] = keys
        self.report["sources"][src["raw"]] = info

    def bake_hair_set(self, layer, g, nation, era, rgb, alpha, back_alpha, raw, info):
        src_col = layers.hair_colour(rgb, alpha)
        info["hair_measured"] = [int(v) for v in src_col]
        keys = []
        for name, target in self.hair:
            baked = layers.bake_hair(rgb, alpha, src_col, target, keep_outline=(name != "black"))
            key = contract.garment_key(layer, g, nation, era, name)
            self.save(key, baked, alpha, raw)
            keys.append(key)
            if back_alpha is not None:
                bkey = contract.garment_key("hairback", g, nation, era, name)
                self.save(bkey, baked, back_alpha, raw)
                keys.append(bkey)
        return keys

    def hair_uncovered(self, headwear_alpha, g):
        """For headwear that hides the hair (covers: Hair, so the game draws no hair under it):
        per processed hairstyle of the gender, the pixels where that hair would lie on the
        head but the headwear leaves the head bare (bald scalp showing under the headwear)."""
        head = self.bases[g]["head_alpha"] > 0.5
        out = {}
        for key, (_, a) in self.canvas_layers.items():
            if key.startswith(f"hair_{g}_") and (key.endswith("_brown") or not any(key.endswith("_" + c) for c, _ in self.hair)):
                out[key] = int((head & (a > 0.5) & (headwear_alpha < 0.5)).sum())
        return out


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--out", default=os.path.join(contract.ROOT, *contract.resources_folder().split("/")))
    ap.add_argument("--qa", default=None, help="write QA composites and qa.json here")
    ap.add_argument("--report", default=None, help="write the processing report (JSON) here")
    ap.add_argument("--no-meta", action="store_true", help="do not write Unity .meta files")
    ap.add_argument("--only", nargs="*", default=None, help="process only these raw sources (the bases always run)")
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    if not args.no_meta:
        # the folders from Assets/Art/Characters down, each with its own meta
        rel = os.path.relpath(args.out, contract.ROOT).replace("\\", "/")
        if rel.startswith("Assets/"):
            parts = rel.split("/")
            for i in range(3, len(parts) + 1):
                unitymeta.folder_meta(os.path.join(contract.ROOT, *parts[:i]))

    t0 = time.time()
    p = Pilot(args.out, not args.no_meta)
    for src in SOURCES:
        if args.only is not None and src["kind"] != "base" and src["raw"] not in args.only:
            continue
        print(f"[{time.time() - t0:6.1f}s] {src['raw']}", flush=True)
        if src["kind"] == "base":
            p.process_base(src)
        else:
            p.process_layer(src)

    contract.check_keys(p.report["keys"].keys())
    if args.only is not None:
        args.qa = None
    print(f"[{time.time() - t0:6.1f}s] {len(p.report['keys'])} keys written to {args.out}")

    if args.qa:
        from charkit import qa
        os.makedirs(args.qa, exist_ok=True)
        p.report["qa"] = qa.run(p, args.qa)
        print(f"[{time.time() - t0:6.1f}s] QA written to {args.qa}")

    report_path = args.report or (os.path.join(args.qa, "report.json") if args.qa else None)
    if report_path:
        with open(report_path, "w", encoding="utf-8") as f:
            json.dump(p.report, f, indent=1, default=lambda o: o.tolist() if hasattr(o, "tolist") else str(o))


if __name__ == "__main__":
    main()
