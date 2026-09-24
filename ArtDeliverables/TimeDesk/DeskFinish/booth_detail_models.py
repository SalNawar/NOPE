"""Small, separately exported booth furnishings in the same material family."""
import math
from artlib import *


def build_booth_frame():
    group('Finish_BoothFrame')
    # Freestanding clerk station: posts meet the existing desktop. It is not a
    # new wall across the main hall. The open centre remains available to visitors.
    for side in [-1, 1]:
        x = side * 2.22
        box('Booth upright', (x, 1.445, .96), (.095, 2.89, .105), 'Finish_EnamelDark', .009)
        box('Post desk collar', (x, 1.069, .96), (.15, .028, .16), 'Finish_Charcoal', .006)
        box('Upright inside trim', (x-side*.042, 2.0, .897), (.014, 1.61, .018), 'Finish_WoodEdge', .003)
        # An outer shelf is supported by the upright and partition cap.
        box('Side shelf', (side*2.54, 2.53, .71), (.67, .055, .39), 'Finish_Wood', .008)
        box('Shelf worn front lip', (side*2.54, 2.533, .508), (.68, .057, .024), 'Finish_WoodEdge', .004)
        for dx in [.08, .43]:
            xx=side*(2.22+dx)
            tube('Bent shelf support', [(xx,2.53,.87),(xx,2.33,.91),(xx,2.51,.55)], .013, 'Finish_Charcoal')
        box('Crown angle bracket', (side*2.15,2.785,.955), (.12,.19,.082), 'Finish_Enamel', .005)
        for y in [2.725,2.84]:
            cylinder('Frame bolt', (side*2.145,y,.909), .009,.009,'Hardware_BareMetal','z',20)
    box('Shallow booth crown', (0,2.906,1.00), (4.54,.132,.54), 'Finish_EnamelDark', .014)
    box('Crown front fascia', (0,2.89,.718), (4.60,.162,.038), 'Finish_Wood', .009)
    box('Fascia lower trim', (0,2.804,.70), (4.61,.019,.051), 'Finish_WoodEdge', .004)
    # Flat blank agency plate. Any name/logo belongs to live history-dependent UI.
    box('Blank agency plate backing',(0,2.898,.69),(1.00,.106,.014),'Finish_Charcoal',.012)
    box('Blank agency plate face',(0,2.898,.680),(.955,.073,.004),'Finish_Brass',.007)
    # Fixed wiring runs along the frame, with short clips rather than hanging
    # across the visitor opening. No paint contains directional illumination.
    tube('Frame electrical conduit',[(-2.24,1.10,1.024),(-2.24,2.82,1.024),(-2.0,2.837,1.025),(2.16,2.837,1.025)],.009,'Finish_BlackRubber')
    for x in [-1.8,-.9,.9,1.8]:
        box('Conduit saddle',(x,2.834,1.031),(.026,.029,.031),'Hardware_BareMetal',.004)


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
    material('Canvas_StarryNight','FFFFFF',.99,texture=project/'Assets/Art/Office/Hybrid/Images/starry_night.png')
    material('Canvas_Mondrian','FFFFFF',.99,texture=project/'Assets/Art/Office/Hybrid/Images/mondrian.png')

    def pinned_paper(name,x,y,w,h,angle=0,mat='Finish_Paper'):
        ob=box(name,(x,y,-.044),(w,h,.0018),mat,.0006)
        ob.rotation_euler.y=math.radians(angle)
        cylinder('Push pin metal stem',(x,y+h*.40,-.049),.0015,.012,'Hardware_BareMetal','z',12)
        cylinder('Push pin round head',(x,y+h*.40,-.058),.007,.006,'Finish_Coral','z',24)
        return ob

    def postcard(name,x,y,w,h,canvas,uvcorners,angle):
        import artlib as A
        from mathutils import Matrix,Vector
        before=set(A.current.objects)
        box(name+' white card',(x,y,-.047),(w,h,.002),'Finish_Paper',.001)
        o=mesh(name+' image',[(x-w*.455,y-h*.425,-.049),(x+w*.455,y-h*.425,-.049),(x+w*.455,y+h*.425,-.049),(x-w*.455,y+h*.425,-.049)],[(0,1,2,3)],canvas)
        uv=o.data.uv_layers.new(name='PostcardUV')
        for i,loop in enumerate(o.data.loops):
            u,vv=uvcorners[i];uv.data[loop.index].uv=(u,1-vv)
        for px in [-w*.36,w*.36]:
            box('Old postcard tape',(x+px,y+h*.48,-.051),(w*.20,.021,.001),'Hardware_Paper',.001)
        centre=Vector(v((x,y,-.047)))
        rotation=Matrix.Translation(centre)@Matrix.Rotation(math.radians(-angle),4,'Y')@Matrix.Translation(-centre)
        for ob in set(A.current.objects)-before:ob.matrix_world=rotation@ob.matrix_world

    group('Finish_LeftEphemera')
    pinned_paper('Old folded receipt',-.268,.16,.096,.19,4,'Hardware_Paper')
    pinned_paper('Blank appointment slip',.08,.223,.20,.062,-3)
    postcard('Starry Night keepsake',-.19,-.405,.255,.166,'Canvas_StarryNight',[(.119,.795),(.88,.795),(.88,.166),(.119,.166)],7)
    pinned_paper('Small memo tucked under keepsake',-.24,-.443,.111,.114,-10,'Hardware_Paper')

    group('Finish_RightEphemera')
    pinned_paper('Clipped administrative slip',-.235,.10,.126,.192,-5)
    postcard('Mondrian keepsake',-.16,-.274,.219,.218,'Canvas_Mondrian',[(.164,.80),(.82,.80),(.834,.181),(.161,.168)],-7)
    pinned_paper('Narrow claim stub',.13,-.487,.22,.063,3,'Hardware_Paper')
