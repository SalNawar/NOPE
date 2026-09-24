"""Offline Blender asset view; never opens Unity or enters game Play mode."""
import bpy,math
from pathlib import Path
from mathutils import Vector,Matrix
P=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(P/'DeskFinish.blend'))
keep={'Finish_PC':(1,0,0),'Finish_Keyboard':(1,0,-.70),'Finish_Till':(-.80,0,.12)}
def v(p):return Vector((-p[0],-p[2],p[1]))
for col in list(bpy.data.collections):
 if col.name not in keep:col.hide_render=True;continue
 transform=Matrix.Translation(v(keep[col.name]))
 for ob in col.objects:ob.matrix_world=transform@ob.matrix_world
scene=bpy.context.scene
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.003))
floor=bpy.context.object
m=bpy.data.materials.new('Authoring neutral floor');m.diffuse_color=(.13,.12,.10,1);floor.data.materials.append(m)
for col in list(floor.users_collection):col.objects.unlink(floor)
scene.collection.objects.link(floor)
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.30,.34,.36,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6
for name,pos,power,size in [('Key',(-3,5,-3),650,5),('Fill',(4,3,-1),180,4)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
 ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=v(pos)
 ob.rotation_euler=(v((.1,.4,0))-ob.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Hardware authoring camera');cam=bpy.data.objects.new('Hardware authoring camera',data);scene.collection.objects.link(cam)
cam.location=v((-2.5,2.7,-5));cam.rotation_euler=(v((.10,.48,-.04))-cam.location).to_track_quat('-Z','Y').to_euler()
data.type='ORTHO';data.ortho_scale=3.2;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.view_settings.view_transform='AgX'
scene.render.resolution_x=1400;scene.render.resolution_y=950;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(P/'Hardware_authoring.png')
bpy.ops.render.render(write_still=True)
