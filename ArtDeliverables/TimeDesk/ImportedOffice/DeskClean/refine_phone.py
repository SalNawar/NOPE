"""Keep the licensed pack's manufactured phone silhouette, refine its surfaces (art side).
Called by author_desk_clean.py and the targeted desktop revision script.

Purpose: build(A) imports the 3DreaMax SM_Landline_Phone.fbx into the Clean_Phone group,
with clean DeskClean_PhoneBody/PhoneDial materials and the original curved receiver.
Inputs: Assets/3DreaMax Studio/036_Retro Office Props Pack Vol-1 (the phone mesh and its
dial texture: keep that pack while the phone is built from it).
Outputs: Blender objects only; the caller exports them.
Run: not on its own; author_desk_clean.py and revise_desktop.py import it.
Paths resolve from this file (any checkout or worktree).
"""
import bpy,math
from pathlib import Path
P=Path(__file__).resolve().parents[4]  # the repo root
SOURCE=P/'Assets/3DreaMax Studio/036_Retro Office Props Pack Vol-1/Art/Meshes/SM_Landline_Phone.fbx'
DIAL=P/'Assets/3DreaMax Studio/036_Retro Office Props Pack Vol-1/Art/Textures/2K/T_Landline_Phone_Basecolor.png'

def build(A):
    col=A.group('Clean_Phone')
    for name in ('DeskClean_PhoneBody','DeskClean_PhoneDial'):
        if bpy.data.materials.get(name):bpy.data.materials.remove(bpy.data.materials[name])
    A.material('DeskClean_PhoneBody','637A69',.69)
    A.material('DeskClean_PhoneDial','FFFFFF',.80,texture=DIAL)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE));bpy.context.view_layer.update()
    source=bpy.context.selected_objects[0];A.active(source)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    vs=source.data.vertices;adj=[set() for _ in vs]
    for e in source.data.edges:
        a,b=e.vertices;adj[a].add(b);adj[b].add(a)
    remaining=set(range(len(vs)));parts=[]
    while remaining:
        todo=[min(remaining)];remaining.remove(todo[0]);ids=set(todo)
        while todo:
            v=todo.pop()
            for n in adj[v]:
                if n in remaining:remaining.remove(n);ids.add(n);todo.append(n)
        parts.append(ids)
    assert len(parts)==11, 'Source mesh changed; inspect component/material mapping before export.'
    names=['Numbered rotary dial','Base cord socket','Dial centre','Contoured phone body',
           'Left cradle hook','Receiver cord socket','Connected coiled cord','Finger stop',
           'Curved receiver','Dial backing','Right cradle hook']
    material={0:'PhoneDial',1:'Rubber',2:'PhoneDial',3:'PhoneBody',4:'PhoneBody',
              5:'Rubber',6:'Rubber',7:'Metal',8:'PhoneBody',9:'PhoneDial',10:'PhoneBody'}
    for i,ids in enumerate(parts):
        # Copy the authored corner normals and UVs exactly. Recalculating weighted
        # normals on this triangulated source introduces visible diagonal wedges.
        ordered=sorted(ids);index={old:new for new,old in enumerate(ordered)}
        faces=[p for p in source.data.polygons if p.vertices[0] in ids]
        me=bpy.data.meshes.new(names[i]);me.from_pydata([vs[j].co for j in ordered],[],[[index[j] for j in p.vertices] for p in faces]);me.update()
        uv=me.uv_layers.new(name='SourceUV');normals=[]
        for new,old in zip(me.polygons,faces):
            new.use_smooth=True
            for nl,ol in zip(new.loop_indices,old.loop_indices):
                uv.data[nl].uv=source.data.uv_layers.active.data[ol].uv
                normals.append(tuple(source.data.corner_normals[ol].vector))
        me.normals_split_custom_set(normals)
        ob=bpy.data.objects.new(names[i],me);col.objects.link(ob)
        me.materials.clear();me.materials.append(A.materials['DeskClean_'+material[i]])
        for f in me.polygons:f.material_index=0
        ob.rotation_euler.z=math.pi  # Match the authored desk library's front axis.
        if i in (4,7,10):
            bevel=ob.modifiers.new('Soft manufactured edges','BEVEL')
            bevel.width=.0013 if i in (3,8) else .00055
            bevel.segments=4;bevel.limit_method='ANGLE';bevel.angle_limit=math.radians(38)
            bevel.harden_normals=True
    bpy.data.objects.remove(source,do_unlink=True)
    return col
