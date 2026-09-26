"""Targeted phone/mouse revision and a Blender-rendered 2D paper prop (art side).
Only exports the changed models.

Purpose: re-exports the phone (refine_phone.py), the mouse with its cable, and the
two-triangle spare paper (Clean_Paper2D, lit by NOPE/Desk Anime in Unity).
Inputs: DeskClean.blend and DeskClean_manifest.json here (from author_desk_clean.py);
../../HybridScene/BlenderOffice/artlib.py; refine_phone.py.
Outputs: the changed Clean_*.fbx in Assets/Art/Office/DeskClean/Models, the updated
DeskClean_manifest.json, DeskRevision_manifest.json and DeskClean.blend here, and
phone_refined.png. Paths resolve from this file (any checkout or worktree).
Run: blender --background --python ArtDeliverables/TimeDesk/ImportedOffice/DeskClean/revise_desktop.py
(author_desk_clean.py runs it at the end).
"""
import bpy,sys,json,math,time,os
from pathlib import Path
from mathutils import Vector
H=Path(__file__).resolve().parent;P=H.parents[3]  # the repo root
sys.path.insert(0,str(P/'ArtDeliverables/TimeDesk/HybridScene/BlenderOffice'))
sys.path.insert(0,str(H))
import artlib as A
import refine_phone
A.HERE=H;A.OUT=P/'Assets/Art/Office/DeskClean'
# Unity may briefly hold a just-imported FBX. Retry that handoff, never write a
# partially generated model into the Assets directory.
original_replace=os.replace
def replace_after_import(src,dst):
    for attempt in range(20):
        try:return original_replace(src,dst)
        except PermissionError:
            if attempt==19:raise
            time.sleep(.3)
A.os.replace=replace_after_import
bpy.ops.wm.open_mainfile(filepath=str(H/'DeskClean.blend'))
A.scene=bpy.context.scene;A.materials={m.name:m for m in bpy.data.materials if m.name.startswith('DeskClean_')}
manifest=json.loads((H/'DeskClean_manifest.json').read_text());A.specs=manifest['materials']
old=bpy.data.collections.get('Clean_Phone')
if old:
    for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(old)
A.collections={};refine_phone.build(A)
mouse=bpy.data.collections['Clean_Mouse'];A.current=mouse
for o in list(mouse.objects):
    if o.name.startswith('Mouse cable'):bpy.data.objects.remove(o,do_unlink=True)
# Cable root and socket are both embedded in their housings. Unity X/Y/Z coordinates,
# converted to the mouse's local frame: root (-.78,1.06,-.75), yaw -5, scale .9.
def mouse_local(p):
    dx=p[0]+.78;dz=p[2]+.75;t=math.radians(-5)
    return ((math.cos(t)*dx-math.sin(t)*dz)/.9,(p[1]-1.06)/.9,(math.sin(t)*dx+math.cos(t)*dz)/.9)
route=[(0,.025,.080),(0,.023,.100),(.012,.010,.15)]
route += [mouse_local(p) for p in [(-.87,1.073,-.48),(-1.02,1.073,-.30),(-1.11,1.087,-.20),(-1.23,1.12,-.14),(-1.38,1.12,-.14)]]
A.tube('Mouse cable to system side socket',route,.0027,'DeskClean_Rubber')
A.collections['Clean_Mouse']=mouse
# Render the existing paper and clip as an RGBA desk illustration. Same geometry,
# palette and soft shading as the 3D library; no photographic/image-generated insert.
scene=A.scene;paper=bpy.data.collections['Clean_PaperBundle']
for o in bpy.data.objects:
    o.hide_render=o.name not in paper.objects
scene.render.engine='CYCLES';scene.cycles.samples=64;scene.cycles.use_denoising=True
scene.world.color=(.32,.32,.32);scene.view_settings.view_transform='Standard'
scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.render.film_transparent=True
bpy.ops.object.camera_add(location=(0,0,1));cam=bpy.context.object;cam.name='Paper illustration camera';cam.data.type='ORTHO';cam.data.ortho_scale=.35;scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(-.2,.15,.7));light=bpy.context.object;light.name='Paper illustration softbox';light.data.energy=6;light.data.size=.55;light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
texture=A.OUT/'Textures/PaperDetail2D.png';texture.parent.mkdir(exist_ok=True)
scene.render.resolution_x=1024;scene.render.resolution_y=768;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.filepath=str(texture)
bpy.ops.render.render(write_still=True)
for o in [cam,light]:bpy.data.objects.remove(o,do_unlink=True)
for col in bpy.data.collections:
    if col.name.startswith('Clean_'):
        for o in col.objects:o.hide_render=o.type=='FONT'
old=bpy.data.collections.get('Clean_Paper2D')
if old:
    for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(old)
if bpy.data.materials.get('DeskClean_Paper2D'):bpy.data.materials.remove(bpy.data.materials['DeskClean_Paper2D'])
A.group('Clean_Paper2D');A.material('DeskClean_Paper2D','FFFFFF',.94,texture=texture)
# An open card has no enclosed volume: do not run the closed-solid normal
# recalculation used by artlib.mesh, which can choose the opposite face.
card=bpy.data.meshes.new('Paper illustration card')
card.from_pydata([A.v(p) for p in [(-.175,.0015,-.13125),(.175,.0015,-.13125),(.175,.0015,.13125),(-.175,.0015,.13125)]],[],[(0,1,2,3)])
card.update();plane=bpy.data.objects.new('Printed paper and clip - 2D',card);A.current.objects.link(plane)
card.materials.append(A.materials['DeskClean_Paper2D'])
assert plane.data.polygons[0].normal.z>.99, 'The rendered paper must face upward.'
uv=plane.data.uv_layers.new(name='IllustrationUV')
for loop in plane.data.loops:
    co=plane.data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(co.x/.35+.5,co.y/.2625+.5)
result=A.export_library('DeskClean')
for item in result['models']:item['assetPath']='Assets/Art/Office/DeskClean/Models/'+item['name']+'.fbx'
manifest['models']=[i for i in manifest['models'] if i['name'] not in A.collections]+result['models'];manifest['materials']=A.specs
(H/'DeskClean_manifest.json').write_text(json.dumps(manifest,indent=2))
(H/'DeskRevision_manifest.json').write_text(json.dumps(result,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(H/'DeskClean.blend'))
# One current close-up to inspect the refined receiver and softened base.
for o in bpy.data.objects:o.hide_render=o.name not in A.collections['Clean_Phone'].objects
scene.render.film_transparent=False;scene.world.color=(.16,.16,.16);scene.view_settings.view_transform='AgX'
bpy.ops.object.camera_add(location=(-.38,.55,.43));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.07))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=.46;scene.camera=cam
for loc,energy,size in [((.3,.4,.6),17,.5),((-.4,-.2,.5),10,.4)]:
    bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=energy;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(-l.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.filepath=str(H/'phone_refined.png');bpy.ops.render.render(write_still=True)
print('REVISION EXPORTED',[(m['name'],m['triangles']) for m in result['models']])
