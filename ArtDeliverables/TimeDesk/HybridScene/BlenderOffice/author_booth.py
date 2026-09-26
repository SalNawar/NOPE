"""Reference-led desk hardware, authored in Blender, with no gameplay scripts (art side).

Purpose: the booth hardware: the Office_* groups (blotter, calendar, instrument board,
intercom, newspaper, NEXT sign, partition, stamp, till, tray, and the Office_Clock and
Office_Stability wall displays).
Inputs: artlib.py (this folder); Assets/Art/Office/Hybrid/Images/newspaper_print.png.
Outputs: Assets/Art/Office/Hybrid/BlenderOffice/Models/Office_*.fbx (+ Exports/ copies),
Booth_Hardware_manifest.json and Booth_Hardware.blend here. DeskClean/author_desk_clean.py
loads Office_Next and Office_Stamp from that .blend.
Run: blender --background --python ArtDeliverables/TimeDesk/HybridScene/BlenderOffice/author_booth.py
"""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from artlib import *

material('Office_Teal','57736E',.85)
material('Office_TealDark','354E4D',.88)
material('Office_Trim','87938E',.82,.04)
material('Office_Paper','D4CDBB',.94)
material('Office_Brass','9F8A60',.78,.13)
material('Stamp_Wood','805239',.88)
material('Mat_Fabric','3B5551',.94)
material('Mat_Stitch','9D9B82',.96)
material('Newspaper_Print','FFFFFF',.96,texture=PROJECT/'Assets/Art/Office/Hybrid/Images/newspaper_print.png')

group('Office_Till')
box('Cash drawer',(0,.05,0),(.61,.1,.48),'Plastic_WarmGrey',.006)
box('Drawer face',(0,.056,-.242),(.594,.087,.016),'Plastic_Ivory',.004)
box('Drawer seam',(0,.103,-.248),(.572,.004,.003),'Recess_Charcoal',.0005)
box('Recessed drawer grip',(0,.052,-.256),(.18,.023,.008),'Plastic_DeepGrey',.003)
# Wedge housing with a shared planar keyboard deck.
vertices=[(-.29,.112,-.218),(.29,.112,-.218),(.29,.112,.224),(-.29,.112,.224),(-.276,.215,-.212),(.276,.215,-.212),(.276,.352,.207),(-.276,.352,.207)]
body=mesh('Till upper wedge',vertices,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],'CRT_BackShell');bevel(body,.008)
for row in range(4):
    for col in range(5):
        z=-.146+row*.063;y=.222+(z+.212)*.327
        cap=box('Accounting key',((col-2)*.075,y+.01,z),(.058,.022,.047),'Office_Teal' if col==4 else 'Plastic_Ivory',.002)
        cap.rotation_euler.x=math.radians(-18)
box('Readout housing',(0,.43,.153),(.390,.190,.14),'Plastic_Ivory',.006)
box('Readout seal',(0,.443,.079),(.341,.13,.006),'Plastic_DeepGrey',.003)
box('Blank credit screen',(0,.443,.075),(.316,.112,.005),'CRT_Glass',.003)
box('Till maker blank',(-.204,.161,-.223),(.067,.022,.002),'Paper_Label',.001)
for x in [-.255,.255]:screw('Drawer fixing',(x,.057,-.252))
for z in [.00,.024,.048,.072,.096]:box('Till ventilation',(.288,.159,z),(.003,.035,.008),'Plastic_DeepGrey',.001)
patch('Drawer rubbed edge',(-.16,.095,-.253),.055,.002,'Wear_Light')

group('Office_Next')
box('Sign rubber sole',(0,.012,0),(.62,.024,.21),'Cable_Rubber',.006)
box('Sign wedge foot',(0,.032,.014),(.62,.037,.19),'Office_TealDark',.004)
box('Next casing',(0,.145,.012),(.586,.227,.106),'CRT_BackShell',.006)
box('Next screen surround',(0,.15,-.044),(.536,.168,.008),'Plastic_DeepGrey',.003)
box('Next blank screen',(0,.15,-.05),(.494,.141,.005),'CRT_Glass',.003)
for x in [-.269,.269]:screw('Sign fastener',(x,.071,-.043))
box('Call switch',(0,.264,.008),(.135,.013,.06),'Power_Amber',.003)

def wall_display(name,w,h,cy,screen_y,screen_h):
    group(name)
    box('Back service box',(0,cy,.028),(w-.018,h-.018,.088),'Plastic_WarmGrey',.005)
    box('Molded front',(0,cy,-.021),(w,h,.042),'Plastic_Ivory',.006)
    box('Recessed bezel',(0,screen_y,-.045),(w-.045,screen_h+.025,.009),'Plastic_DeepGrey',.003)
    box('Blank live display',(0,screen_y,-.051),(w-.071,screen_h,.006),'CRT_Glass',.003)
    for x in [-w/2+.018,w/2-.018]:screw('Case screw',(x,cy-h/2+.026,-.044))
    for i in range(4):box('Side case vent',(w/2+.0005,cy-.02+i*.012,.012),(.002,.005,.038),'Plastic_DeepGrey',.001)

wall_display('Office_Clock',.47,.212,.11,.116,.129)
box('Clock time-set rocker',(.145,.216,.003),(.057,.011,.027),'Office_Teal',.002)
wall_display('Office_Stability',.36,.355,.185,.234,.157)
for x,mat in [(-.095,'Indicator_Green'),(.095,'Power_Amber')]:cylinder('Status lamp',(x,.065,-.048),.010,.008,mat,'z',20)
box('Display service label',(0,.076,-.044),(.065,.014,.002),'Paper_Label',.001)

group('Office_Calendar')
box('Calendar board',(0,.20,.005),(.28,.419,.024),'Office_TealDark',.003)
box('Tear-off paper block',(0,.20,-.013),(.242,.356,.013),'Office_Paper',.001)
for y in [.038,.041,.044]:box('Paper leaf edge',(0,y,-.021),(.239,.001,.002),'Paper_Label',0)
box('Metal spring binding',(0,.384,-.026),(.266,.032,.023),'Office_Trim',.002)
for x in [-.085,.085]:cylinder('Calendar pin',(x,.389,-.041),.006,.01,'Office_Brass','z',20)

group('Office_Intercom')
verts=[(-.18,0,-.13),(.18,0,-.13),(.18,0,.13),(-.18,0,.13),(-.172,.062,-.123),(.172,.062,-.123),(.172,.165,.122),(-.172,.165,.122)]
o=mesh('Intercom wedge',verts,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],'Plastic_Ivory');bevel(o,.005)
for i in range(7):
    z=-.015+i*.015;y=.062+(z+.123)*.42
    grille=box('Speaker aperture',(-.025,y+.001,z),(.15-abs(i-3)*.012,.002,.005),'Plastic_DeepGrey',.001);grille.rotation_euler.x=math.radians(-22.8)
button=cylinder('Push-to-talk switch',(.110,.098,-.055),.020,.016,'Power_Amber','y',32)
button.rotation_euler.x=math.radians(-22.8)
for x in [-.143,.143]:screw('Intercom face screw',(x,.049,-.127))
tube('Intercom cable',[(.13,.055,.126),(.18,.013,.19),(.26,.01,.17)],.005,'Cable_Rubber')

group('Office_Tray')
box('Pressed tray base',(0,.012,0),(.79,.024,.435),'Office_Trim',.005)
box('Tray felt liner',(0,.026,-.005),(.736,.005,.385),'Office_TealDark',.004)
for x in [-.39,.39]:box('Folded tray side',(x,.050,0),(.019,.080,.43),'CRT_BackShell',.003)
box('Tray back upstand',(0,.059,.207),(.79,.096,.018),'CRT_BackShell',.003)
box('Tray open front lip',(0,.027,-.208),(.752,.025,.012),'CRT_BackShell',.003)
for x in [-.342,.342]:cylinder('Tray rivet',(x,.026,-.171),.004,.003,'Office_Brass','y',12)
patch('Tray worn paint',(.28,.109,.196),.053,.002,'Wear_Light','top')

group('Office_Stamp')
box('Stamp rubber face',(0,.008,0),(.150,.016,.092),'Recess_Charcoal',.001)
box('Stamp wooden pad',(0,.028,0),(.163,.027,.099),'Stamp_Wood',.004)
lathe('Turned stamp grip',[(.039,.031),(.048,.034),(.058,.023),(.112,.017),(.139,.026),(.164,.041),(.183,.041),(.197,.026),(.200,.001)],'Stamp_Wood',40)
cylinder('Stamp brass ferrule',(0,.060,0),.023,.011,'Office_Brass','y',32)

group('Office_Partition')
box('Partition core',(0,1.005,0),(.065,2.01,2.03),'Office_Teal',.005)
box('Inside lower panel',(.034,.49,0),(.006,.81,1.91),'Office_TealDark',.003)
box('Outside lower panel',(-.034,.49,0),(.006,.81,1.91),'Office_TealDark',.003)
for z in [-1.024,1.024]:box('Freestanding terminal rail',(0,1.057,z),(.101,2.114,.054),'Office_Trim',.005)
box('Partition cap',(0,2.092,0),(.109,.055,2.115),'Office_Trim',.006)
box('Kick strip',(0,.090,0),(.082,.122,2.044),'Plastic_DeepGrey',.003)
for z in [-.76,.62]:box('Partition foot',(0,.023,z),(.25,.046,.19),'Plastic_DeepGrey',.003)

group('Office_InstrumentBoard')
box('Board back',(0,0,.003),(.68,1.12,.035),'Office_Trim',.004)
box('Board inset',(0,0,-.018),(.65,1.09,.01),'Office_TealDark',.002)
for x in [-.301,.301]:
    for y in [-.522,.522]:screw('Panel mounting bolt',(x,y,-.025),mat='Office_Brass')

group('Office_Blotter')
box('Blotter backing',(0,.004,0),(2.25,.008,.93),'Office_TealDark',.004)
box('Cloth work surface',(0,.009,0),(2.214,.005,.894),'Mat_Fabric',.003)
for x in [-1.098,1.098]:box('Mat edge stitch',(x,.012,0),(.002,.001,.866),'Mat_Stitch',0)
for z in [-.438,.438]:box('Mat edge stitch',(0,.012,z),(2.188,.001,.002),'Mat_Stitch',0)

group('Office_Newspaper')
box('Folded newspaper body',(0,.010,0),(.36,.020,.24),'Office_Paper',.001)
for i in range(3):box('Folded leaf',(0,.004+i*.004,-.001),(.361,.001,.243),'Paper_Label',0)
verts=[(-.18,.020,-.12),(.18,.020,-.12),(.18,.020,.12),(-.18,.020,.12)]
o=mesh('Newspaper printed face',verts,[(0,1,2,3)],'Newspaper_Print')
uv=o.data.uv_layers.new(name='PrintUV')
for p in o.data.polygons:
    for li in p.loop_indices:
        co=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(-co.x/.36+.5,-co.y/.24+.5)

export_library('Booth_Hardware')
