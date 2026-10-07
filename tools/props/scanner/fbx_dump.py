import bpy, sys, os, addon_utils
mod = None
for m in addon_utils.modules():
    if m.__name__.endswith("io_scene_fbx"):
        mod = m
import importlib
pf = importlib.import_module(mod.__name__ + ".parse_fbx")
root, ver = pf.parse(os.path.join(os.path.dirname(bpy.data.filepath) if bpy.data.filepath else os.getcwd(), "scanner.fbx"))
def name(e): return e.id.decode()
for e in root.elems:
    if name(e) == "GlobalSettings":
        for p70 in e.elems:
            if name(p70)=="Properties70":
                for p in p70.elems:
                    print("GS", p.props[0].decode(), p.props[4:] )
    if name(e) == "Objects":
        for m in e.elems:
            if name(m) == "Model":
                nm = m.props[1].split(b"\x00")[0].decode()
                props = {}
                for p70 in m.elems:
                    if name(p70)=="Properties70":
                        for p in p70.elems:
                            k = p.props[0].decode()
                            if k.startswith("Lcl") or "Rotation" in k or "Scaling" in k:
                                props[k] = tuple(round(float(x),4) for x in p.props[4:])
                print("MODEL", nm, props)
