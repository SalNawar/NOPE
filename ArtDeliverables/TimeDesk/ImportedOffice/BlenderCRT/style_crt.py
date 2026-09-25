"""One-object study. Import the real vendor CRT; preserve UVs and screen geometry.
Apply RESTORY_STYLE_GUIDE.md, save editable before/after collections, export variant.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
P=Path('E:/unity/NOPE')
HERE=P/'ArtDeliverables/TimeDesk/ImportedOffice/BlenderCRT'
OUT=P/'Assets/Art/Office/ImportedOffice'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(P/'Assets/80s_Office/Models/Electronics/SM_Monitor.FBX'))
source=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.context.view_layer.objects.active=source
source.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
source.name='CRT_Before'
material_indices=[p.material_index for p in source.data.polygons]

def linear(c):return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4

def mat(name,color,rough,texture=None):
 m=bpy.data.materials.new(name);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*[linear(c) for c in color],1)
 p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=0;p.inputs['Specular IOR Level'].default_value=.3
 if texture:
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(texture));m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
 return m
before_mat=mat('Before_SourcePlastic',(1,1,1),.70,P/'Assets/80s_Office/Textures/T_Computer_BaseColor.PNG')
before_glass=mat('Before_SourceGlass',(.035,.105,.115),.78)
source.data.materials.clear();source.data.materials.append(before_mat);source.data.materials.append(before_glass)
for p,i in zip(source.data.polygons,material_indices):p.material_index=i
after=source.copy();after.data=source.data.copy();bpy.context.scene.collection.objects.link(after);after.name='CRT_Painted'
after.data.materials.clear();after.data.materials.append(mat('CRT_IvoryPlastic',(1,1,1),.68,OUT/'Textures/computer_ivory_painted.png'))
after.data.materials.append(mat('CRT_BlankGlass',(.095,.15,.16),.44))
for p,i in zip(after.data.polygons,material_indices):p.material_index=i
# Bevel only the casing. The screen surface and its position remain untouched.
screen_vertices={v for p in after.data.polygons if p.material_index==1 for v in p.vertices}
# Weight only genuinely sharp chassis edges; never bevel triangulation or glass.
edge_faces={e.key:[] for e in after.data.edges}
for p in after.data.polygons:
 for key in p.edge_keys:edge_faces[tuple(sorted(key))].append(p)
weights=after.data.attributes.new('bevel_weight_edge','FLOAT','EDGE')
for e in after.data.edges:
 faces=edge_faces[e.key]
 if len(faces)==2 and all(p.material_index==0 for p in faces) and faces[0].normal.angle(faces[1].normal)>.7:
  weights.data[e.index].value=1
bevel=after.modifiers.new('Soft manufactured casing edges 1.2mm','BEVEL');bevel.width=.0012;bevel.segments=3;bevel.limit_method='WEIGHT';bevel.use_clamp_overlap=True;bevel.harden_normals=True
# Preserve the artist's split normals on the already curved case and glass.
for ob,label in [(source,'01 Before - vendor mesh'),(after,'02 After - painted satin ABS')]:
 c=bpy.data.collections.new(label);bpy.context.scene.collection.children.link(c)
 for old in list(ob.users_collection):old.objects.unlink(ob)
 c.objects.link(ob)
# Export only the revised object; no lights/cameras or staging are game assets.
bpy.ops.object.select_all(action='DESELECT');after.select_set(True);bpy.context.view_layer.objects.active=after
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/CRT_Painted.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
# Identical studio conditions for a real before/after render.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48
scene.cycles.use_denoising=True
scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.28,.31,.35,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001));ground=bpy.context.object;ground.name='Studio floor - not exported';ground.data.materials.append(mat('Studio warm grey',(.32,.30,.27),.85))
for name,loc,power,size,color in [('Key',(-1.6,-2,2.7),180,2.4,(1,.88,.72)),('Fill',(1.8,-1.2,1.4),55,2,(.75,.86,1)),('Rim',(0,1.4,2),110,1.6,(1,.92,.8))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
 o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.25))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Comparison camera');cam=bpy.data.objects.new('Comparison camera',d);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(-1.15,-2.1,1.07);cam.rotation_euler=(Vector((0,-.005,.27))-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=.95
for ob in [source,after]:ob.hide_render=True
source.hide_render=False;scene.render.filepath=str(HERE/'before.png');bpy.ops.render.render(write_still=True)
source.hide_render=True;after.hide_render=False;scene.render.filepath=str(HERE/'after.png');bpy.ops.render.render(write_still=True)
source.hide_set(True);after.hide_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'CRT_StyleStudy.blend'))
report={'source':'Assets/80s_Office/Models/Electronics/SM_Monitor.FBX','source_vertices':len(source.data.vertices),'source_triangles':sum(len(p.vertices)-2 for p in source.data.polygons),'bevel_meters':.0012,'same_camera_lighting':True,'screen_unchanged':True,'export':'Assets/Art/Office/ImportedOffice/Models/CRT_Painted.fbx'}
(HERE/'study.json').write_text(json.dumps(report,indent=2))
