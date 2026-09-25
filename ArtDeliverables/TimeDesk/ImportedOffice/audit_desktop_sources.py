import bpy,json
from pathlib import Path
P=Path('E:/unity/NOPE')
with bpy.data.libraries.load(str(P/'ArtDeliverables/TimeDesk/DeskFinish/DeskFinish.blend'),link=False) as (s,d):print('COLLECTIONS',s.collections)
files=['Assets/80s_Office/Models/Electronics/SM_Keyboard.FBX','Assets/3DreaMax Studio/036_Retro Office Props Pack Vol-1/Art/Meshes/SM_Landline_Phone.fbx','Assets/3DreaMax Studio/036_Retro Office Props Pack Vol-1/Art/Meshes/SM_Calculator.fbx']
result=[]
for f in files:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(P/f))
 result.append({'file':f,'objects':[{'name':o.name,'dimension':list(o.dimensions),'rotation':list(o.rotation_euler),'materials':[m.name for m in o.data.materials]} for o in bpy.context.scene.objects if o.type=='MESH']})
print(json.dumps(result,indent=2))
