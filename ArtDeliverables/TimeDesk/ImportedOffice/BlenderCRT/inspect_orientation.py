import bpy,json
from mathutils import Vector
from pathlib import Path
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath='E:/unity/NOPE/Assets/80s_Office/Models/Electronics/SM_Monitor.FBX')
o=bpy.context.object
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 faces=[p for p in o.data.polygons if p.material_index==1]
 print('screen',len(faces),'center',list(o.matrix_world@(sum((p.center for p in faces),Vector())/len(faces))), 'normal',list(o.matrix_world.to_3x3()@(sum((p.normal*p.area for p in faces),Vector()).normalized())))
 print('bbox',[list(o.matrix_world@Vector(p)) for p in o.bound_box])
