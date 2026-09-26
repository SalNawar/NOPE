"""Final desktop art pass, local Blender geometry only. No room or gameplay edits (art side).
Reuses editable project props where useful and reconstructs the desk equipment.

Purpose: the editable source of the live desk props (the Clean_* FBX): keyboard,
calculator, lamp, pen pot, stapler, binder, keys, blotter, the phone (refine_phone.py), and
the Finish_*/Office_* props it loads and renames Clean_*.
Inputs: ../../HybridScene/BlenderOffice/artlib.py; the Finish_Mouse, Till, Inkpad, FormSorter,
Tray and PaperBundle collections of DeskFinish/DeskFinish.blend; Office_Stamp and Office_Next
of HybridScene/BlenderOffice/Booth_Hardware.blend; refine_phone.py (the 3DreaMax phone).
Outputs: Assets/Art/Office/DeskClean/Models/Clean_*.fbx (+ Exports/ copies),
DeskClean_manifest.json and DeskClean.blend here; then it runs revise_desktop.py.
Unity picks the FBX up on import; the Debt Relief pass owns their materials and layout, so
there is no installer to rerun. Paths resolve from this file (any checkout or worktree).
Run: blender --background --python ArtDeliverables/TimeDesk/ImportedOffice/DeskClean/author_desk_clean.py
"""
import sys,math,json
from pathlib import Path
HERE=Path(__file__).resolve().parent;P=HERE.parents[3]  # the repo root
sys.path.insert(0,str(P/'ArtDeliverables/TimeDesk/HybridScene/BlenderOffice'))
import artlib as A
from artlib import *
HERE=Path(__file__).resolve().parent  # again: artlib's star import replaced it
A.HERE=HERE;A.OUT=P/'Assets/Art/Office/DeskClean'
(A.OUT/'Models').mkdir(parents=True,exist_ok=True);(HERE/'Exports').mkdir(exist_ok=True)
A.materials.clear();A.specs.clear()
palette={'ABS':('C5B89E',.75,0),'Case':('A39883',.78,0),'Grey':('ADAF9F',.78,0),
 'Green':('496A60',.68,0),'GreenDark':('344B46',.78,0),'Teal':('5C8178',.73,0),
 'Rubber':('2D3330',.92,0),'Dark':('343C39',.83,0),'Paper':('DBD1BA',.94,0),
 'Manila':('C9B98C',.91,0),'Wood':('896448',.76,0),'Orange':('BB7F49',.73,0),
 'Metal':('AAA999',.47,.62),'Brass':('A18A63',.55,.42),'Glass':('273333',.48,0),
 'Ink':('505A51',.86,0),'Pad':('5B5549',.90,0),'PadEdge':('302D27',.91,0)}
for key,(hx,rough,metal) in palette.items():material('DeskClean_'+key,hx,rough,metal)
def m(key):return 'DeskClean_'+key
def rounded_pad(name,w,d,h,y,z,r,mat):
    outline=roundrect(w,d,r,10);n=len(outline)
    verts=[(x,y+dy,zz+z) for dy in [-h/2,h/2] for x,zz in outline]
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,verts,faces,m(mat),True)
mapping={'Finish_Ivory':'ABS','Finish_WarmGrey':'Grey','Finish_Enamel':'Green','Finish_EnamelDark':'GreenDark',
 'Finish_Charcoal':'Dark','Finish_KeyGrey':'Grey','Finish_KeyCream':'ABS','Finish_Coral':'Orange','Finish_Brass':'Brass',
 'Finish_BlackRubber':'Rubber','Finish_Glass':'Glass','Finish_Paper':'Paper','Finish_Ink':'Dark','Finish_Wood':'Wood',
 'Finish_WoodEdge':'Wood','Hardware_Cream':'ABS','Hardware_Steel':'Case','Hardware_Modifier':'Teal','Hardware_Keys':'Grey',
 'Hardware_Seam':'Dark','Hardware_Glass':'Glass','Hardware_Rubber':'Rubber','Hardware_CashPaint':'Green',
 'Hardware_CashEdge':'GreenDark','Hardware_BareMetal':'Metal','Hardware_Paper':'Manila','Hardware_Amber':'Orange',
 'Indicator_Green':'Teal','Office_Brass':'Brass','Stamp_Wood':'Wood','Recess_Charcoal':'Dark','Cable_Rubber':'Rubber',
 'Office_TealDark':'GreenDark','CRT_BackShell':'ABS','Plastic_DeepGrey':'Dark','CRT_Glass':'Glass','Power_Amber':'Orange',
 'Plastic_Ivory':'ABS','Wear_Light':'ABS','Wear_Dark':'Case'}
def load(source,names):
    with bpy.data.libraries.load(str(source),link=False) as (src,dst):dst.collections=names
    for col in dst.collections:
        old=col.name;new='Clean_'+old.replace('Finish_','').replace('Office_','');col.name=new
        scene.collection.children.link(col);A.collections[new]=col
        for o in list(col.objects):
            if any(t in o.name.lower() for t in ['scuff','wear','worn']):bpy.data.objects.remove(o,do_unlink=True);continue
            for slot in o.material_slots:
                if slot.material:
                    key=slot.material.name.split('.')[0]
                    if key not in mapping:raise ValueError('Unmapped material '+key)
                    slot.material=A.materials[m(mapping[key])]
            for mod in o.modifiers:
                if mod.type=='BEVEL':mod.width*=1.6;mod.segments=max(4,mod.segments)
            if 'Sculpted two-button mouse' in o.name:
                o.modifiers.clear();sub=o.modifiers.new('Smooth close-view mouse shell','SUBSURF');sub.levels=2;sub.render_levels=2
            if o.type=='MESH':
                for f in o.data.polygons:f.use_smooth=True
    return dst.collections
load(P/'ArtDeliverables/TimeDesk/DeskFinish/DeskFinish.blend',['Finish_Mouse','Finish_Till','Finish_Inkpad','Finish_FormSorter','Finish_Tray','Finish_PaperBundle'])
load(P/'ArtDeliverables/TimeDesk/HybridScene/BlenderOffice/Booth_Hardware.blend',['Office_Stamp','Office_Next'])
A.current=A.collections['Clean_Mouse']
for o in list(A.current.objects):
    if o.name.startswith('Mouse pad'):bpy.data.objects.remove(o,do_unlink=True)
rounded_pad('Rounded mouse pad',.30,.30,.004,.002,.015,.032,'GreenDark')
def text_top(name,body,p,size,mat='Ink'):
    cu=bpy.data.curves.new(name,'FONT');cu.body=body;cu.size=size;cu.align_x='CENTER';cu.align_y='CENTER';cu.resolution_u=3
    o=bpy.data.objects.new(name,cu);A.current.objects.link(o);o.location=v(p);o.rotation_euler.z=math.pi;cu.materials.append(materials[m(mat)]);return o
def wedge(name,w,d,front,back,mat):
    ob=mesh(name,[(-w/2,.005,-d/2),(w/2,.005,-d/2),(w/2,.005,d/2),(-w/2,.005,d/2),(-w/2,front,-d/2),(w/2,front,-d/2),(w/2,back,d/2),(-w/2,back,d/2)],[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],m(mat));bevel(ob,.006,5);return ob
# Complete keyboard: broad softly rounded shell, shallow keys and actual legends.
group('Clean_Keyboard');wedge('Rounded keyboard housing',.598,.224,.021,.035,'ABS')
box('Keyboard lower seam',(0,.009,0),(.583,.015,.211),m('Case'),.004)
def deck(z):return .021+(z+.112)*(.014/.224)
for x,w,z,d in [(-.097,.360,-.011,.145),(.135,.073,-.012,.144),(.245,.086,-.008,.15),(-.096,.366,.090,.029)]:
    o=box('Key well',(x,deck(z)+.001,z),(w,.002,d),m('Dark'),.003);o.rotation_euler.x=-math.atan(.014/.224)
unit=.0237
def key(legend,x,z,width=1,depth=1,color='Grey'):
    top=deck(z)+.010
    o=box('Key '+legend,(x,top-.0037,z),(unit*width-.0023,.008,unit*depth-.0025),m(color),.00115)
    o.rotation_euler.x=-math.atan(.014/.224)
    if legend:
        t=text_top('Legend '+legend,legend,(x,top+.00055,z),.0045 if len(legend)<3 else .0028,'Paper' if color=='Teal' else 'Ink');t.rotation_euler.x=math.atan(.014/.224)
rows=[(['`','1','2','3','4','5','6','7','8','9','0','-','=','Back'],[1]*13+[2]),
 (['Tab','Q','W','E','R','T','Y','U','I','O','P','[',']','\\'],[1.5]+[1]*12+[1.5]),
 (['Caps','A','S','D','F','G','H','J','K','L',';',"'",'Enter'],[1.75]+[1]*11+[2.25]),
 (['Shift','Z','X','C','V','B','N','M',',','.','/','Shift'],[2.25]+[1]*10+[2.75]),
 (['Ctrl','Alt','','Alt','Ctrl'],[1.5,1.5,9,1.5,1.5])]
for row,(legends,widths) in enumerate(rows):
    x=-.276
    for legend,width in zip(legends,widths):
        color='Orange' if legend=='Enter' else ('Teal' if width>1 and legend else 'Grey')
        key(legend,x+width*unit/2,.049-row*.027,width,color=color);x+=width*unit
key('Esc',-.264,.090,color='Orange')
for i in range(12):key('F'+str(i+1),-.224+i*.0257,.090,width=.9,color='Teal')
for row,labels in enumerate([['Ins','Home','PgUp'],['Del','End','PgDn']]):
    for j,label in enumerate(labels):key(label,.111+j*.025,.049-row*.028,color='Teal')
for label,x,z in [('^',.136,-.024),('<',.111,-.051),('v',.136,-.051),('>',.161,-.051)]:key(label,x,z,color='Teal')
for row,labels in enumerate([['7','8','9'],['4','5','6'],['1','2','3'],['0','.','+']]):
    for j,label in enumerate(labels):key(label,.219+j*.025,.049-row*.027)
text_top('Keyboard maker','OFFICE / INPUT',(.23,.036,.088),.004)
for i in range(3):box('Status lens',(.201+i*.021,.037,.074),(.006,.002,.003),m('Teal'),.001)
tube('Keyboard flex',[(.21,.026,.112),(.26,.013,.16),(.30,.006,.27)],.0022,m('Rubber'))
# Refine the pack silhouette and preserve its original dial UVs.
sys.path.insert(0,str(HERE))
import refine_phone
refine_phone.build(A)
# Readable calculator with quiet broad panels and softly capped functional keys.
group('Clean_Calculator');wedge('Calculator housing',.124,.190,.016,.040,'Case')
box('Calculator lower seam',(0,.008,0),(.120,.014,.184),m('Dark'),.006)
o=box('Display rim',(0,.039,.055),(.105,.008,.050),m('ABS'),.005);o.rotation_euler.x=-.12
o=box('Blank LCD',(0,.044,.055),(.089,.003,.029),m('GreenDark'),.003);o.rotation_euler.x=-.12
for row,labels in enumerate([['MC','MR','C','/'],['7','8','9','x'],['4','5','6','-'],['1','2','3','+'],['0','.','%','=']]):
    for j,s in enumerate(labels):
        x=-.044+j*.029;z=.012-row*.0215;y=.016+(z+.095)*(.024/.190)+.006
        color='Orange' if s=='=' else 'Teal' if j==3 or row==0 else 'ABS'
        o=box('Calc key '+s,(x,y,z),(.023,.009,.017),m(color),.002);o.rotation_euler.x=-.12
        t=text_top('Calc legend '+s,s,(x,y+.0052,z),.0045 if len(s)==1 else .0035,'Paper' if color=='Teal' else 'Ink');t.rotation_euler.x=.12
# Banker lamp: smooth arched enamel shade instead of a hard flat roof.
group('Clean_Lamp');lathe('Weighted oval lamp base',[(.002,.088),(.009,.103),(.023,.101),(.034,.080),(.040,.041)],m('GreenDark'),64)
for x in [-.051,.051]:tube('Curved lamp support',[(x,.035,.020),(x,.22,.042),(x,.43,-.025)],.008,m('Brass'))
vv=[];ff=[];n=36
for x in [-.235,.235]:
    for j in range(n+1):
        t=math.pi*j/n;vv.append((x,.422+math.sin(t)*.079,-.025+math.cos(t)*.082))
for j in range(n):ff.append((j,j+1,n+2+j,n+1+j))
shade=mesh('Smooth banker shade',vv,ff,m('Green'),True)
solid=shade.modifiers.new('Enamel shade thickness','SOLIDIFY');solid.thickness=.004
bevel(shade,.003,4)
for x in [-.235,.235]:
    mesh('Shade end',[(x,.422,-.107)]+[(x,.422+math.sin(math.pi*j/n)*.079,-.025+math.cos(math.pi*j/n)*.082) for j in range(n+1)], [tuple(range(n+2))],m('Green'),True)
tube('Warm lamp tube',[(-.177,.430,-.025),(.177,.430,-.025)],.012,m('Paper'))
cylinder('Lamp switch',(.064,.032,-.031),.008,.014,m('Orange'),'y',32)
tube('Lamp cable',[(0,.012,.090),(.025,.006,.20),(.09,.007,.31)],.0022,m('Rubber'))
# Pen cup, stapler, office binder and keys: each remains a separate useful desk item.
group('Clean_PenPot')
lathe('Hollow pen cup',[(.002,.034),(.009,.039),(.112,.040),(.120,.038),(.120,.034),(.012,.032)],m('ABS'),64)
for i,(x,z,h) in enumerate([(-.019,-.010,.216),(.001,.014,.241),(.018,-.005,.224),(.005,-.022,.200)]):
    pencil=cylinder('Pencil shaft',(x,(h+.03)/2,z),.0035,h-.03,m(['Wood','Teal','Orange','Green'][i]),'y',12)
    cylinder('Pencil ferrule',(x,h-.008,z),.004,.014,m('Metal'),'y',24)
    cylinder('Pencil eraser',(x,h+.001,z),.004,.008,m('Orange'),'y',24)
group('Clean_Stapler')
box('Stapler rubber sole',(0,.004,0),(.044,.008,.163),m('Rubber'),.006)
box('Stapler base',(0,.012,0),(.046,.012,.162),m('Case'),.007)
box('Stapler metal channel',(0,.027,.009),(.031,.012,.140),m('Metal'),.004)
o=box('Soft stapler cap',(0,.044,.014),(.044,.028,.143),m('ABS'),.011);o.rotation_euler.x=math.radians(-6)
cylinder('Stapler hinge',(0,.028,.070),.012,.046,m('Case'),'x',32)
group('Clean_Binder')
box('Binder back cover',(0,.170,.005),(.058,.340,.257),m('GreenDark'),.006)
box('Binder paper block',(.004,.169,-.003),(.044,.319,.240),m('Paper'),.002)
box('Binder front cover',(.030,.170,.005),(.005,.340,.257),m('Green'),.003)
box('Binder rounded spine',(0,.170,-.122),(.064,.340,.011),m('GreenDark'),.005)
box('Blank spine label',(0,.224,-.129),(.041,.118,.001),m('Paper'),.003)
cylinder('Spine finger hole',(0,.064,-.130),.010,.003,m('Metal'),'z',40)
cylinder('Dark spine hole',(0,.064,-.132),.007,.002,m('Dark'),'z',40)
group('Clean_Keys')
ring=[(.014*math.cos(j*math.tau/40),.003,.014*math.sin(j*math.tau/40)) for j in range(41)]
key_ring=tube('Key ring',ring,.0011,m('Metal'));key_ring.data.resolution_u=1;key_ring.data.bevel_resolution=2
for i,x in enumerate([-.012,.014]):
    cylinder('Key bow',(x,.003,-.023),.009,.002,m('Metal'),'y',32)
    box('Key shaft',(x,.003,-.047),(.005,.003,.038),m('Metal'),.001)
    for z in [-.053,-.062]:box('Key tooth',(x+.004,.003,z),(.007,.003,.004),m('Metal'),.0008)
# Replace the existing rectangular patch with an actual rounded office blotter.
group('Clean_Blotter')
rounded_pad('Office blotter edge',2.15,1.04,.008,.004,0,.042,'PadEdge')
rounded_pad('Plain writing face',2.125,1.015,.002,.008,0,.035,'Pad')
# Export separate groups and retain editable source. No comparison renders.
# The shared exporter handles mesh and curve objects; keep editable FONT sources
# and add evaluated glyph meshes so the actual game also receives the legends.
deps=bpy.context.evaluated_depsgraph_get()
for col in A.collections.values():
    for source in list(col.objects):
        if source.type!='FONT':continue
        ev=source.evaluated_get(deps)
        me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=deps)
        ob=bpy.data.objects.new(source.name+' glyphs',me);ob.matrix_world=source.matrix_world.copy();col.objects.link(ob)
        source.hide_render=True;source.hide_set(True)
result=A.export_library('DeskClean')
for item in result['models']:item['assetPath']='Assets/Art/Office/DeskClean/Models/'+item['name']+'.fbx'
(HERE/'DeskClean_manifest.json').write_text(json.dumps(result,indent=2))
print('EXPORTED',[(x['name'],x['triangles']) for x in result['models']])

# Rebuild the connected cable and 2D paper after the full library export.
import runpy
runpy.run_path(str(HERE / "revise_desktop.py"), run_name="__main__")
