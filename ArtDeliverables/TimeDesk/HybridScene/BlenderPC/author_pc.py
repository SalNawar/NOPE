"""Editable Blender source for the reference-led PC art pass.

Run with Blender --background --python this_file. No Unity editor/runtime code.
Coordinates passed to helpers are Unity-style: X right, Y up, Z back.
Source objects retain their modeling modifiers in the .blend. Only export copies
are evaluated and grouped by material. Nothing in the game creates this art.
"""
import bpy, math, json, sys, shutil, os
from pathlib import Path
from mathutils import Vector

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[3]
OUT = PROJECT / 'Assets/Art/Office/Hybrid/BlenderPC'
OUT.mkdir(parents=True, exist_ok=True)
(OUT / 'Models').mkdir(exist_ok=True)
(OUT / 'Textures').mkdir(exist_ok=True)
(HERE / 'Exports').mkdir(exist_ok=True)
RENDER = '--render-art' in sys.argv

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
    if c.name != 'Collection': bpy.data.collections.remove(c)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
materials = {}
specs = {}
collections = {}
current = None

def linear(c): return c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4
def material(name, hx, rough=.75, metal=0, texture=None):
    rgb = tuple(int(hx[i:i+2],16)/255 for i in (0,2,4))
    m=bpy.data.materials.new(name); m.diffuse_color=(*[linear(c) for c in rgb],1)
    m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=m.diffuse_color
    p.inputs['Roughness'].default_value=rough
    p.inputs['Metallic'].default_value=metal
    p.inputs['Specular IOR Level'].default_value=.18 if name!='CRT_Glass' else .3
    if texture and Path(texture).exists():
        t=m.node_tree.nodes.new('ShaderNodeTexImage'); t.image=bpy.data.images.load(str(texture))
        m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
    materials[name]=m
    specs[name]={'color':rgb,'smoothness':1-rough,'metallic':metal,'texture':str(texture) if texture else None}
    return m

material('CRT_Terracotta','B9B6AD',.81) # Stable material ID; now the neutral face casing.
material('CRT_BackShell','AFADA5',.83)
material('Plastic_Ivory','C7C5BD',.81)
material('Plastic_WarmGrey','999C97',.84)
material('Plastic_DeepGrey','626B69',.87)
material('Recess_Charcoal','2C3334',.91)
material('CRT_Glass','263E42',.28)
material('Case_Steel','ABAFA9',.8,.08)
material('Power_Amber','BA7850',.76)
material('Indicator_Green','88AA76',.63)
material('Wear_Light','C5BFAF',.95)
material('Wear_Dark','8F897C',.95)
material('Paper_Label','C4BFAE',.95)
material('Cable_Rubber','393E36',.92)
material('Desk_Edge','5B4436',.86)
material('Desk_Metal','414D49',.8,.05)
material('Desk_WornEdge','8D6B4C',.94)
wood=OUT/'Textures/desk_walnut_albedo.png'
material('Desk_Walnut','806247',.86,texture=wood if wood.exists() else None)

def v(p): return (-p[0],-p[2],p[1])
def group(name):
    global current
    current=bpy.data.collections.new(name); scene.collection.children.link(current)
    collections[name]=current
    return current
def move_collection(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    current.objects.link(o)
def finish(o,mat):
    move_collection(o)
    if mat: o.data.materials.append(materials[mat])
    return o
def active(o):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
def smooth(o,weighted=True):
    for p in o.data.polygons: p.use_smooth=True
    if weighted:
        m=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');m.keep_sharp=True;m.weight=50
    return o
def bevel(o,width=.003,segments=3):
    m=o.modifiers.new('Small manufactured edge','BEVEL');m.width=width;m.segments=segments
    m.limit_method='ANGLE';m.angle_limit=math.radians(32);m.harden_normals=True
    return smooth(o)
def box(name,p,d,mat,b=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=v(p));o=bpy.context.object;o.name=name
    o.dimensions=(d[0],d[2],d[1]);active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    finish(o,mat)
    if b:bevel(o,b)
    return o
def mesh(name,verts,faces,mat,shading=False):
    me=bpy.data.meshes.new(name);me.from_pydata([v(p) for p in verts],[],faces);me.update()
    o=bpy.data.objects.new(name,me);current.objects.link(o)
    if mat:me.materials.append(materials[mat])
    active(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    if shading:smooth(o)
    return o
def roundrect(w,h,r,n=8):
    pts=[]
    for cx,cy,a in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
        for i in range(n+1):
            t=math.radians(a+i*90/n);pts.append((cx+r*math.cos(t),cy+r*math.sin(t)))
    return pts
def loft(name,layers,mat,caps=True):
    # Each layer: depth, width, height, radius, vertical center.
    verts=[]
    for z,w,h,r,y in layers: verts.extend([(x,yy+y,z) for x,yy in roundrect(w,h,r)])
    n=len(verts)//len(layers);faces=[]
    for j in range(len(layers)-1):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    if caps:faces.extend([tuple(reversed(range(n))),tuple(range((len(layers)-1)*n,len(layers)*n))])
    return mesh(name,verts,faces,mat,True)
def cylinder(name,p,r,depth,mat,axis='y',vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=v(p))
    o=bpy.context.object;o.name=name
    if axis=='z':o.rotation_euler.x=math.pi/2
    elif axis=='x':o.rotation_euler.y=math.pi/2
    active(o);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);finish(o,mat);bevel(o,min(.0015,r*.1),2)
    return o
def tube(name,points,r,mat):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=16
    s=curve.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for b,p in zip(s.bezier_points,points):b.co=v(p);b.handle_left_type='AUTO';b.handle_right_type='AUTO'
    curve.bevel_depth=r;curve.bevel_resolution=3;curve.resolution_u=12
    o=bpy.data.objects.new(name,curve);current.objects.link(o);curve.materials.append(materials[mat]);return o
def cut(target,cutter):
    active(target);m=target.modifiers.new('Recess '+cutter.name,'BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cutter
    # Apply only the cut, keep final normals modifier at end of stack.
    bpy.ops.object.modifier_move_up(modifier=m.name)
    bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(cutter,do_unlink=True)
def screw(name,p,axis='z',mat='Plastic_DeepGrey'):
    return cylinder(name,p,.0035,.0018,mat,axis,12)
def patch(name,p,w,h,mat='Wear_Light',plane='front'):
    # Authored sparse chips, without painted light/shadow. Irregular silhouettes.
    x,y,z=p;poly=[(-.5,-.26),(-.36,-.5),(.16,-.35),(.5,-.05),(.33,.23),(-.22,.5),(-.46,.2)]
    verts=[(x+a*w,y+b*h,z) if plane=='front' else (x+a*w,y,z+b*h) for a,b in poly]
    return mesh(name,verts,[tuple(range(len(verts)))],mat)

group('PC_CRT_Desktop')
# Horizontal desktop chassis: a low, broad machine, not a monitor stand substitute.
box('Chassis steel cover',(0,.119,.055),(1.03,.186,.73),'Case_Steel',.008)
box('Chassis lower folded rim',(0,.035,.046),(1.046,.045,.736),'Plastic_DeepGrey',.004)
for x in [-.41,.41]:
    for z in [-.23,.32]:box('Rubber case foot',(x,.012,z),(.075,.024,.09),'Cable_Rubber',.004)
front=box('Desktop molded face',(0,.115,-.327),(1.054,.180,.050),'Plastic_Ivory',.006)
# Shallow real recesses for drive faces, with no baked text or brand marks.
for y in [.097,.158]:
    cutter=box('Drive opening',(-.135,y,-.35),(.443,.053,.038),None,.002)
    cut(front,cutter)
    box('Drive recess',(-.135,y,-.337),(.435,.048,.006),'Recess_Charcoal',.001)
    box('Drive fascia',(-.135,y,-.348),(.420,.043,.010),'Plastic_WarmGrey',.002)
box('Lower drive slot',(-.15,.095,-.355),(.31,.004,.004),'Recess_Charcoal',.001)
box('Optical drawer seam',(-.14,.163,-.355),(.345,.004,.003),'Plastic_DeepGrey',.0005)
box('Diskette eject',(.016,.087,-.359),(.023,.010,.008),'Plastic_Ivory',.001)
box('Optical eject',(.047,.153,-.358),(.020,.008,.007),'Plastic_Ivory',.001)
box('Empty maker insert',(-.448,.157,-.354),(.068,.026,.004),'Plastic_WarmGrey',.001)
box('Machine asset label',(-.424,.080,-.354),(.091,.040,.002),'Paper_Label',.001)
for i in range(6):
    box('Front ventilation slot',(.332,.064+i*.008,-.354),(.172,.003,.003),'Recess_Charcoal',.0005)
box('Power switch recess',(.363,.153,-.355),(.081,.040,.006),'Plastic_DeepGrey',.004)
box('Rectangular power switch',(.364,.153,-.361),(.061,.029,.012),'Power_Amber',.003)
for x in [.219,.241]:cylinder('Desktop status diode',(x,.15,-.356),.003,.003,'Indicator_Green','z',12)
for x in [-.49,.49]:screw('Face fastener',(x,.05,-.355))
for z in [.03,.06,.09,.12,.15,.18,.21,.24]:
    box('Side chassis vent',(.516,.112,z),(.002,.030,.009),'Recess_Charcoal',.001)
for x,y,w in [(-.32,.198,.055),(.24,.202,.075),(.46,.067,.018)]:patch('Case paint wear',(x,y,-.355),w,.002,'Wear_Dark')

# Low swivel foot and its visible tilt yoke.
box('Monitor plinth',(0,.227,.065),(.60,.027,.46),'Plastic_WarmGrey',.009)
cylinder('Circular swivel bearing',(0,.255,.075),.163,.033,'Plastic_DeepGrey','y',48)
loft('Tilt pedestal',[(-.11,.30,.068,.015,.279),(.20,.22,.073,.014,.281)],'CRT_BackShell')

# Tapered tube housing: broad flat planes with narrow corner radii.
shell=loft('Injection molded tube shell',[
    (-.259,.903,.676,.028,.66),(-.19,.898,.673,.027,.66),
    (.125,.838,.62,.032,.673),(.415,.552,.481,.026,.676),
    (.454,.512,.440,.022,.676)],'CRT_BackShell')
# Ventilation is genuinely recessed on the near side; cutters only touch shell surface.
for side in [-1,1]:
    for i in range(10):
        z=-.14+i*.023
        # Skin tapers along the tube. Follow its side plane.
        sx=.45-(z+.259)*.105
        cutter=box('Side vent cutter',(side*sx,.647,z),(.040,.129,.008),None,0)
        cut(shell,cutter)
        box('Vent dark interior',(side*(sx-.018),.647,z),(.003,.127,.007),'Recess_Charcoal',0)
for i in range(11):
    x=(i-5)*.029
    box('Rear upper vent',(x,.917,.370),(.014,.002,.045),'Plastic_DeepGrey',.001)
# Separate face bezel: large planar areas and a precise recessed screen opening.
loft('Face perimeter seam',[(-.268,.906,.680,.028,.66),(-.277,.906,.680,.028,.66)],'Recess_Charcoal')
loft('Terracotta outer bezel',[
    (-.277,.906,.680,.028,.66),(-.294,.920,.685,.030,.66),
    (-.315,.920,.685,.030,.66),(-.328,.903,.667,.024,.66),
    (-.328,.746,.569,.024,.690),(-.307,.710,.538,.027,.690),
    (-.291,.696,.524,.027,.690)],'CRT_Terracotta',False)
loft('Inner dark gasket',[
    (-.291,.697,.525,.027,.69),(-.294,.687,.51525,.025,.69),
    (-.285,.680,.510,.024,.69)],'Recess_Charcoal',False)
# Convex CRT glass. No wallpaper, reflections, glints, scan lines or text painted on it.
verts=[];faces=[];nx=32;ny=24;w=.686;h=.5145
for j in range(ny+1):
    yy=(j/ny-.5)*h
    for i in range(nx+1):
        xx=(i/nx-.5)*w
        # Slight CRT bow: edge recedes; center bulges just beyond gasket inner edge.
        depth=-.305+.027*((xx/(w/2))**2+(yy/(h/2))**2)/2
        verts.append((xx,yy+.69,depth))
for j in range(ny):
    for i in range(nx):a=j*(nx+1)+i;faces.append((a,a+1,a+nx+2,a+nx+1))
glass=mesh('CRT blank 4x3 glass',verts,faces,'CRT_Glass')
for p in glass.data.polygons:p.use_smooth=True
# Broad lower chin: discrete power switch, three recessed adjusters, blank branding insert.
box('Blank monitor badge',(-.256,.355,-.329),(.145,.017,.002),'Plastic_WarmGrey',.001)
for x in [.065,.096,.127]:
    box('Adjustment key recess',(x,.354,-.330),(.025,.007,.003),'Plastic_DeepGrey',.001)
    box('Adjustment key',(x,.354,-.334),(.016,.004,.006),'CRT_BackShell',.001)
cylinder('Power switch surround',(.354,.352,-.330),.027,.006,'Plastic_DeepGrey','z')
cylinder('Monitor power button',(.354,.352,-.337),.022,.015,'Power_Amber','z')
cylinder('Monitor power diode',(.294,.352,-.331),.004,.003,'Indicator_Green','z',16)
for x in [-.395,.395]:screw('Bezel recessed fastener',(x,.970,-.329),mat='CRT_Terracotta')
# Hand-placed sparse wear near touched areas. Never all-over noisy plastic.
for p,w,h in [((-.387,.993,-.319),.055,.003),((.396,.363,-.329),.020,.003),((-.40,.356,-.329),.017,.005),((.421,.862,-.328),.003,.022)]:patch('Bezel worn edge',p,w,h)
patch('Old service sticker',(-.389,.403,-.329),.056,.028,'Paper_Label')
tube('Monitor power lead',[(.16,.49,.443),(.22,.36,.50),(.27,.223,.49),(.39,.220,.39)],.008,'Cable_Rubber')
tube('Desktop rear cable',[(.26,.12,.422),(.46,.030,.48),(.61,.021,.41),(.64,.019,.08)],.009,'Cable_Rubber')

group('PC_Keyboard')
# A real staggered ANSI keyboard. Rows have sculpted dished key tops.
base=box('Keyboard lower pan',(0,.028,0),(.955,.041,.342),'Plastic_WarmGrey',.009)
upper=box('Keyboard upper shell',(0,.051,.004),(.959,.047,.348),'Plastic_Ivory',.008)
box('Keyboard lower seam',(0,.042,-.175),(.927,.003,.003),'Plastic_DeepGrey',.001)
box('Recessed main key well',(-.150,.076,.016),(.609,.006,.240),'Recess_Charcoal',.006)
box('Recessed navigation well',(.280,.076,.016),(.116,.006,.240),'Recess_Charcoal',.004)
box('Recessed numpad well',(.401,.076,.016),(.110,.006,.240),'Recess_Charcoal',.004)
unit=.039
def key(name,x,z,width=1,depth=1,mat='Plastic_Ivory',row=0):
    ww=width*unit-.004;dd=depth*unit-.005
    low=.077;high=.102+row*.003
    # Beveled trapezoid skirt plus curved dish. Smooth top, planar skirts.
    verts=[];faces=[]
    for y,sw,sd in [(low,ww,dd),(high-.006,ww-.006,dd-.007),(high,ww-.011,dd-.011)]:
        verts.extend([(x+xx,y,z+zz) for xx,zz in [(-sw/2,-sd/2),(sw/2,-sd/2),(sw/2,sd/2),(-sw/2,sd/2)]])
    for j in range(2):
        for i in range(4):faces.append((j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i))
    # 5x5 shallow concave cap surface with broad manufacturable top edges.
    start=len(verts);res=4
    for j in range(res+1):
        for i in range(res+1):
            u=i/res*2-1;t=j/res*2-1
            dish=.0025*(1-u*u)*(1-t*t)
            verts.append((x+u*(ww-.011)/2,high-dish,z+t*(dd-.011)/2))
    for j in range(res):
        for i in range(res):a=start+j*(res+1)+i;faces.append((a,a+1,a+res+2,a+res+1))
    o=mesh(name,verts,faces,mat);bevel(o,.0006,2)
    return o
left=-.445
rows=[
    [1]*13+[2],
    [1.5]+[1]*12+[1.5],
    [1.75]+[1]*11+[2.25],
    [2.25]+[1]*10+[2.75],
    [1.25,1.25,1.25,6.25,1.25,1.25,1.25,1.25]
]
for row,widths in enumerate(rows):
    x=left
    for i,ww in enumerate(widths):
        xcenter=x+ww*unit/2
        mod=ww>1.3 or (row==4 and ww<2)
        mat='Plastic_WarmGrey' if mod else 'Plastic_Ivory'
        if row==2 and i==len(widths)-1:mat='Power_Amber'
        key('Keycap row%02d key%02d'%(row,i),xcenter,.078-row*.041,ww,mat=mat,row=4-row)
        x+=ww*unit
# Function strip in four deliberate groups.
key('Escape',left+unit/2,.136,mat='Power_Amber',row=5)
for i in range(12):key('Function key %02d'%i,left+.079+i*.040+(i//4)*.012,.136,mat='Plastic_WarmGrey',row=5)
for j in range(2):
    for i in range(3):key('Navigation %d %d'%(j,i),.234+i*.039,.078-j*.041,mat='Plastic_WarmGrey',row=3)
for x,z in [(.273,-.044),(.234,-.085),(.273,-.085),(.312,-.085)]:key('Arrow key',x,z,mat='Plastic_WarmGrey')
for row in range(4):
    for i in range(3):key('Numpad %d %d'%(row,i),.361+i*.039,.079-row*.041,row=4-row)
key('Numpad zero',.3805,-.085,2)
key('Numpad decimal',.439,-.085)
for i in range(3):
    box('Indicator recess',(.366+i*.028,.079,.144),(.012,.002,.005),'Plastic_DeepGrey',.001)
    box('Keyboard diode',(.366+i*.028,.081,.144),(.006,.002,.003),'Indicator_Green',.0005)
box('Blank keyboard asset tag',(-.321,.076,-.15),(.073,.001,.015),'Paper_Label',.001)
for x in [-.377,.377]:box('Keyboard foot',(x,.010,.11),(.06,.02,.055),'Cable_Rubber',.003)
tube('Keyboard lead',[(.25,.04,.179),(.40,.025,.25),(.57,.019,.34),(.61,.019,.68),(.59,.024,1.07),(.47,.096,1.17)],.005,'Cable_Rubber')
for x,z,w in [(-.32,-.170,.035),(.13,-.170,.025),(.41,-.17,.016)]:patch('Keyboard edge wear',(x,.060,z),w,.002,'Wear_Light','top')

group('PC_Desk')
# Preserve the current playable tabletop height and footprint. This is the actual
# full tabletop so there is no overlapping sample slab on the right-hand corner.
box('Solid walnut desk top',(0,1.005,0),(5.8,.11,2.52),'Desk_Walnut',.012)
box('Front timber nosing',(0,.950,-1.242),(5.8,.110,.055),'Desk_Edge',.009)
box('Front underside reveal',(0,.885,-1.20),(5.70,.019,.05),'Recess_Charcoal',.002)
box('Desk front apron',(0,.773,-1.17),(5.56,.210,.095),'Desk_Walnut',.005)
for x in [-2.64,2.64]:
    box('Desk steel trestle',(x,.436,.11),(.075,.872,1.99),'Desk_Metal',.008)
    box('Desk trestle foot',(x,.038,.11),(.16,.076,2.06),'Desk_Metal',.01)
for x,z,w,d in [(-2.14,-1.252,.14,.003),(-.89,-1.252,.075,.003),(.88,-1.252,.11,.004),(2.30,-1.252,.083,.002)]:
    patch('Front worn timber',(x,.982,z),w,d,'Desk_WornEdge')
for x,z,w,d in [(2.12,-.87,.15,.003),(2.3,-.80,.047,.005),(-1.72,-.64,.086,.003)]:patch('Table surface scuff',(x,1.0606,z),w,d,'Desk_WornEdge','top')

# Explicit UVs for all source meshes; dominant-plane unwrap is appropriate to
# these machined surfaces. Desk uses a single physically proportioned top grain.
for col in collections.values():
    for o in col.objects:
        if o.type!='MESH':continue
        uv=o.data.uv_layers.new(name='SurfaceUV')
        for p in o.data.polygons:
            n=p.normal;axis=max(range(3),key=lambda i:abs(n[i]))
            for li in p.loop_indices:
                co=o.data.vertices[o.data.loops[li].vertex_index].co
                if o.name=='CRT blank 4x3 glass':
                    uv.data[li].uv=(-co.x/.686+.5,(co.z-.690)/.5145+.5)
                    continue
                if axis==2:a,b=co.x,co.y
                elif axis==1:a,b=co.x,co.z
                else:a,b=co.y,co.z
                uv.data[li].uv=(a/2.9+.5,b/2.9+.5)

manifest={'source':'ReStory user-supplied screenshot; original unbranded interpretation',
          'materials':specs,'models':[],'screenLocalCenter':[0,.690,-.305],
          'screenSize':[.686,.5145],'tabletopHeight':1.060}

# Export copies are evaluated; the source below retains its modeling modifiers.
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.world.color=(.15,.15,.15)

for name,col in collections.items():
    exportcol=bpy.data.collections.new('EXPORT_'+name);scene.collection.children.link(exportcol)
    deps=bpy.context.evaluated_depsgraph_get();parts={}
    for source in list(col.objects):
        if source.type not in {'MESH','CURVE'}:continue
        ev=source.evaluated_get(deps)
        me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=deps)
        ob=bpy.data.objects.new(source.name+'_export',me);ob.matrix_world=source.matrix_world.copy();exportcol.objects.link(ob)
        matname=me.materials[0].name
        parts.setdefault(matname,[]).append(ob)
    nodes={}
    for matname,obs in parts.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in obs:o.select_set(True)
        bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=bpy.context.object
        o.name=name+'__'+matname
        scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        # Join can retain duplicate copies of a material; collapse to one slot.
        o.data.materials.clear();o.data.materials.append(materials[matname])
        for p in o.data.polygons:p.material_index=0
        nodes[o.name]=matname
    bpy.ops.object.select_all(action='DESELECT')
    for o in exportcol.objects:o.select_set(True)
    export_file=HERE/'Exports'/f'{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(export_file),use_selection=True,
        object_types={'MESH'},use_mesh_modifiers=True,mesh_smooth_type='OFF',
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
    # Stage a complete file before exposing it to Unity's automatic importer.
    pending=OUT/'Models'/f'{name}.fbx.pending'
    shutil.copyfile(export_file,pending)
    os.replace(pending,OUT/'Models'/f'{name}.fbx')
    meshstats=[{'node':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons)} for o in exportcol.objects]
    manifest['models'].append({'name':name,'assetPath':f'Assets/Art/Office/Hybrid/BlenderPC/Models/{name}.fbx','nodes':nodes,'meshStats':meshstats})
    for o in list(exportcol.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(exportcol)
(HERE/'asset_manifest.json').write_text(json.dumps(manifest,indent=2))

# Assemble the editable source for opening in Blender. Export geometry above
# remains at each asset's own origin. Authoring lights/camera never enter FBX.
if True:
    for name,col in collections.items():
        for o in col.objects:
            if name=='PC_CRT_Desktop':o.location.z+=1.06
            elif name=='PC_Keyboard':o.location.z+=1.06;o.location.y+=.74
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.34,.39,.42,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
    def light(name,p,power,size,color):
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
        ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=v(p)
        direction=Vector(v((0,1.5,0)))-ob.location;ob.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
    light('Large softbox',(-2.5,4,-3),430,4,(1,.93,.82))
    light('Cool fill',(2.8,3,1),220,3,(.80,.91,1))
    data=bpy.data.cameras.new('Asset authoring camera');cam=bpy.data.objects.new('Asset authoring camera',data);scene.collection.objects.link(cam)
    cam.location=v((2.15,2.7,-3.8));target=Vector(v((0,1.43,-.1)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=2.50;scene.camera=cam
    scene.view_settings.view_transform='AgX'
    scene.render.resolution_x=1500;scene.render.resolution_y=1250;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'PC_authoring_view.png')
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
    bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'TimeDesk_PC.blend'))
    if RENDER:bpy.ops.render.render(write_still=True)
print('PC_ART_EXPORTED '+str(HERE/'asset_manifest.json'),flush=True)
