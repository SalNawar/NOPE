"""Small, separately exported booth furnishings in the same material family (art side).

Purpose: build_desk_stationery() models Finish_FormSorter, Finish_PaperBundle and
Finish_ComputerMedia; build_panel_ephemera(project) the Finish_LeftEphemera and
Finish_RightEphemera pinned to the booth panels.
Inputs: artlib (already imported by the caller); build_panel_ephemera's project argument is unused.
Outputs: Blender collections only; the caller exports them.
Run: not on its own; author_desk_finish.py imports it.
"""
import math
from artlib import *


def build_desk_stationery():
    group('Finish_FormSorter')
    box('Rubber sorter feet',(0,.009,0),(.40,.018,.22),'Finish_BlackRubber',.007)
    box('Pressed sorter base',(0,.025,0),(.45,.023,.25),'Finish_EnamelDark',.009)
    box('Sorter front low lip',(0,.057,-.122),(.443,.052,.009),'Finish_Enamel',.004)
    for x in [-.219,.219]:
        mesh('Sorter sloping side',[(x,.035,-.118),(x,.07,-.118),(x,.255,.112),(x,.035,.112)],[(0,1,2,3)],'Finish_Enamel')
    for i,z in enumerate([-.045,.032,.102]):
        h=.19+i*.029
        box('Sheet metal divider',(0,h*.5+.027,z),(.423,h,.007),'Finish_EnamelDark',.005)
        # Each compartment carries a modest sheaf and one colour-tabbed folder.
        for j in range(4):
            box('Unprinted administrative form',((j%3-1)*.003,h*.5+.047,z-.020+j*.002),(.364,h+.015,.0017),'Finish_Paper',.0005)
        folder=box('Faded manila file',(.003,h*.5+.043,z-.026),(.394,h+.022,.003),'Hardware_Paper',.002)
        tabx=[-.13,.015,.13][i]
        box('Blank coloured folder tab',(tabx,h+.074,z-.025),(.09,.032,.006),['Finish_Coral','Finish_Brass','Finish_Enamel'][i],.003)
        # Narrow rolled edges preserve the simple pressed-metal silhouette.
        cylinder('Divider rolled lip',(0,h+.027,z),.004,.423,'Finish_Enamel','x',16)

    group('Finish_PaperBundle')
    for i in range(8):
        leaf=box('Blank carbon-copy sheet',((i%3-1)*.002,.0014+i*.0017,(i%2)*.002),(.275,.0015,.182),'Finish_Paper' if i%3 else 'Hardware_Paper',.0005)
        leaf.rotation_euler.z=math.radians((i%3-1)*1.1)
    # A lifted corner is geometry, not a shadow painted into the paper.
    vv=[]
    for j in range(5):
        z=-.092+j*.046
        for i in range(7):
            x=-.138+i*.046
            curl=max(0,(x-.085)/.053)*max(0,(-z-.032)/.060)*.021
            vv.append((x,.016+curl,z))
    ff=[]
    for j in range(4):
        for i in range(6):
            k=j*7+i;ff.append((k,k+1,k+8,k+7))
    mesh('Blank top sheet curled corner',vv,ff,'Finish_Paper',True)
    box('Binder clip jaws',(-.104,.016,.075),(.032,.026,.019),'Finish_Charcoal',.003)
    for dz in [-.012,.012]:
        tube('Binder clip wire handle',[(-.117,.025,.075+dz),(-.118,.039,.090+dz),(-.108,.044,.097+dz),(-.096,.039,.090+dz),(-.094,.025,.075+dz)],.0013,'Hardware_BareMetal')

    group('Finish_ComputerMedia')
    # Data disks and an unprinted sleeve belong beside the computer. They are
    # office equipment, not invented historical museum artefacts.
    box('Paper disk sleeve',(-.032,.004,.017),(.106,.007,.113),'Hardware_Paper',.003)
    for ox,oz,y,mat in [(-.035,.003,.009,'Finish_Charcoal'),(.047,-.026,.019,'Finish_Enamel')]:
        w=.089;d=.094
        outline=[(-w/2,-d/2),(w/2,-d/2),(w/2,d/2-.008),(w/2-.008,d/2),(-w/2,d/2)]
        vv=[(ox+x,y+yy,oz+z) for yy in [0,.0033] for x,z in outline]
        ff=[(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)]
        o=mesh('Three and a half inch data disk',vv,ff,mat);bevel(o,.001,2)
        box('Disk sliding shutter',(ox+.003,y+.004,oz+.030),(.058,.0013,.033),'Hardware_BareMetal',.001)
        box('Shutter access slot',(ox-.007,y+.005,oz+.030),(.013,.0006,.023),'Finish_Charcoal',.0005)
        box('Blank replaceable disk label',(ox,y+.004,oz-.015),(.069,.0005,.039),'Finish_Paper',.002)
        box('Write protect tab',(ox-.035,y+.004,oz+.037),(.009,.0008,.008),'Finish_BlackRubber',.0005)
    # One folded receipt at the sleeve edge, kept clear of the mouse movement.
    box('Unprinted computer receipt',(-.079,.006,-.044),(.034,.001,.072),'Finish_Paper',.001)


def build_panel_ephemera(project):
    def pinned_paper(name,x,y,w,h,angle=0,mat='Finish_Paper'):
        import artlib as A
        from mathutils import Matrix,Vector
        before=set(A.current.objects)
        ob=box(name,(x,y,-.044),(w,h,.0018),mat,.0006)
        # Empty form fields, with no letters, logos or history-dependent content.
        # Low-contrast rules give the paper a designed surface without baking text.
        box('Blank form header rule',(x-w*.08,y+h*.26,-.0455),(w*.58,h*.024,.0004),'Finish_KeyGrey',0)
        for i in range(3):
            yy=y+h*(.09-i*.16)
            box('Empty form writing rule',(x+w*.055,yy,-.0455),(w*.61,.001,.0004),'Finish_KeyGrey',0)
            box('Empty form field marker',(x-w*.32,yy+.004,-.0455),(.005,.007,.0004),'Finish_KeyGrey',0)
        cylinder('Push pin metal stem',(x,y+h*.40,-.049),.0015,.012,'Hardware_BareMetal','z',12)
        cylinder('Push pin round head',(x,y+h*.40,-.058),.007,.006,'Finish_Coral','z',24)
        centre=Vector(v((x,y,-.044)))
        rotation=Matrix.Translation(centre)@Matrix.Rotation(math.radians(-angle),4,'Y')@Matrix.Translation(-centre)
        for part in set(A.current.objects)-before:part.matrix_world=rotation@part.matrix_world
        return ob

    group('Finish_LeftEphemera')
    pinned_paper('Old folded receipt',-.268,.16,.096,.19,4,'Hardware_Paper')
    pinned_paper('Blank appointment slip',.08,.223,.20,.062,-3)
    pinned_paper('Blank docket card',-.19,-.405,.255,.166,7)
    pinned_paper('Small memo tucked under docket',-.24,-.443,.111,.114,-10,'Hardware_Paper')

    group('Finish_RightEphemera')
    pinned_paper('Clipped administrative slip',-.235,.10,.126,.192,-5)
    pinned_paper('Blank inspection card',-.16,-.274,.219,.218,-7)
    pinned_paper('Narrow claim stub',.13,-.487,.22,.063,3,'Hardware_Paper')
