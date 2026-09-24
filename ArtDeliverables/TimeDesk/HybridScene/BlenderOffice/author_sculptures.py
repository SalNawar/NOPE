"""Museum-derived sculpture adaptations. See SOURCES.md for individual licenses."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from artlib import *
material('Sculpture_Bronze','617267',.90,.10)
material('Sculpture_Patina','788479',.95)
material('Sculpture_Stone','C3C0AD',.97)
material('Sculpture_Dust','A6A18C',.98)
material('Nefertiti_Limestone','B49D78',.95)
material('Nefertiti_Crown','496674',.96)
material('Nefertiti_Band','987B52',.96)

for name,height,base in [('Thinker',1.86,'Sculpture_Bronze'),('Discobolus',1.75,'Sculpture_Stone'),('Nefertiti',.68,'Nefertiti_Limestone')]:
    group('Art_'+name)
    bpy.ops.wm.stl_import(filepath=str(HERE/'Sources'/f'{name}.stl'))
    o=bpy.context.object;o.name=name+' simplified museum geometry';move_collection(o)
    # Normalize to the existing exhibit height, retain recognizable silhouette.
    mins=Vector([min(v.co[i] for v in o.data.vertices) for i in range(3)])
    maxs=Vector([max(v.co[i] for v in o.data.vertices) for i in range(3)])
    print(name,'source bounds',list(mins),list(maxs),flush=True)
    scale=height/(maxs.z-mins.z)
    for vert in o.data.vertices:vert.co=(vert.co-Vector(((mins.x+maxs.x)/2,(mins.y+maxs.y)/2,mins.z)))*scale
    # Smooth scanner noise before reducing geometry. Broad planes, no scan textures.
    mod=o.modifiers.new('Remove scan surface noise','SMOOTH');mod.factor=.65;mod.iterations=6
    active(o);bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=o.modifiers.new('Simplified exhibit silhouette','DECIMATE');mod.ratio=min(1,7000/len(o.data.polygons))
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=o.modifiers.new('Soften simplified surface','SMOOTH');mod.factor=.22;mod.iterations=2
    bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.append(materials[base])
    second='Sculpture_Patina' if name=='Thinker' else 'Sculpture_Dust'
    o.data.materials.append(materials[second])
    if name=='Nefertiti':
        o.data.materials.append(materials['Nefertiti_Crown']);o.data.materials.append(materials['Nefertiti_Band'])
    for p in o.data.polygons:
        p.use_smooth=True
        z=p.center.z/height
        f=math.sin(p.center.x/height*23)+math.sin(p.center.y/height*19+z*8)
        if z<.12 and f>.5:p.material_index=1
        if name=='Nefertiti' and z>.63:p.material_index=2
        if name=='Nefertiti' and .635<z<.660:p.material_index=3
    # Front view chosen from the source turntable: positive Blender Y is the
    # clerk-facing surface after FBX export, matching the other asset sources.
    o.rotation_euler.z=math.radians(180 if name=='Discobolus' else 90)
    # Split color regions for predictable one-material renderer nodes in Unity.
    active(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='MATERIAL');bpy.ops.object.mode_set(mode='OBJECT')
    for part in list(collections['Art_'+name].objects):
        if part.type!='MESH':continue
        used=part.data.polygons[0].material_index;mat=part.data.materials[used]
        part.data.materials.clear();part.data.materials.append(mat)
        for p in part.data.polygons:p.material_index=0

export_library('Sculptures')

# Authoring turntable sheet, not a Unity or gameplay capture.
if '--preview' in sys.argv:
    for row,(name,h) in enumerate([('Thinker',1.86),('Discobolus',1.75),('Nefertiti',.68)]):
        col=collections['Art_'+name]
        for o in list(col.objects):o.hide_render=True
        for i in range(4):
            for src in list(col.objects):
                ob=src.copy();ob.data=src.data;scene.collection.objects.link(ob);ob.hide_render=False
                ob.scale*=1.8/h;ob.rotation_euler.z=math.radians(i*90);ob.location=(i*2.4-3.6,0,4.4-row*2.2)
    bpy.ops.object.camera_add(location=(0,18,6.9));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,3.4))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=10.4;scene.camera=cam
    scene.world.color=(.5,.5,.5)
    for pos,power,size in [((3,7,10),1500,7),((-4,3,6),900,6)]:
        bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(Vector((0,0,3))-l.location).to_track_quat('-Z','Y').to_euler()
    scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1500;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard'
    scene.render.filepath=str(HERE/'Sculpture_authoring_angles.png');bpy.ops.render.render(write_still=True)
