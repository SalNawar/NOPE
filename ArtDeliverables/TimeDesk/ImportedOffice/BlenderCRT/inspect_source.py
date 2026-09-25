import bpy,json
from pathlib import Path
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath='E:/unity/NOPE/Assets/80s_Office/Models/Electronics/SM_Monitor.FBX')
out=[]
for o in bpy.context.scene.objects:
 out.append({'name':o.name,'type':o.type,'dim':list(o.dimensions),'scale':list(o.scale),'rotation':list(o.rotation_euler),'materials':[m.name for m in o.data.materials] if o.type=='MESH' else []})
Path('E:/unity/NOPE/ArtDeliverables/TimeDesk/ImportedOffice/BlenderCRT/source_audit.json').write_text(json.dumps(out,indent=2))
print(json.dumps(out))
