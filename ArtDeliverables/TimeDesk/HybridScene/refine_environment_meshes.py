"""Offline environment/exhibit art authoring. No Unity runtime installation."""
from pathlib import Path
import math, json
import mesh_authoring as a

HERE=Path(__file__).resolve().parent
a.BACKUP=HERE/'BeforeHallRefinement/Models'
a.BACKUP.mkdir(parents=True,exist_ok=True)
COLORS={**a.COLORS,
 'plaster':(.63,.60,.49),'plaster_wear':(.43,.43,.36),'stone':(.42,.44,.39),
 'floor_main':(.49,.47,.39),'floor_alt':(.46,.44,.37),'floor_edge':(.25,.34,.30),
 'grout':(.32,.34,.29),'window_frame':(.16,.22,.22),
 'city_concrete':(.40,.46,.47),'city_light':(.51,.53,.48),'city_dark':(.27,.33,.34),
 'city_glass':(.14,.22,.25),'city_paleglass':(.36,.46,.46),'city_teal':(.18,.34,.33),
 'timber':(.29,.18,.105),'timber_wear':(.44,.30,.17),'canvas':(.72,.67,.52),
 'canvas_patch':(.50,.48,.38),'ochre':(.65,.43,.12),
}

class Mesh(a.Mesh):
    def block(self,name,c,s,mat):
        x,y,z=c;w,h,d=[v/2 for v in s]
        p=[(x-w,y-h,z-d),(x+w,y-h,z-d),(x+w,y+h,z-d),(x-w,y+h,z-d),
           (x-w,y-h,z+d),(x+w,y-h,z+d),(x+w,y+h,z+d),(x-w,y+h,z+d)]
        for f in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]:self.face(name,mat,[p[i] for i in f])
    def horizontal(self,name,mat,x,z,y,w,d):
        self.face(name,mat,[(x-w/2,y,z-d/2),(x-w/2,y,z+d/2),(x+w/2,y,z+d/2),(x+w/2,y,z-d/2)])
    def beam(self,name,start,end,r,mat,n=8):self.tube(name,[start,end],r,mat,n)
    def cylinder_axis(self,name,c,r,h,mat,axis='z',n=24):
        if axis in ['z','y']:self.cyl(name,c,r,h,mat,axis,n);return
        before=len(self.faces);self.cyl(name,(0,0,0),r,h,mat,'z',n)
        for i in range(before,len(self.faces)):
            nm,mt,pts=self.faces[i];self.faces[i]=(nm,mt,[(p[2]+c[0],p[1]+c[1],-p[0]+c[2]) for p in pts])
    def wheel(self,c,r):
        x,y,z=c
        pts=[(x,y+r*math.sin(i*2*math.pi/32),z+r*math.cos(i*2*math.pi/32)) for i in range(33)]
        self.tube('wheel_tyres',pts,.035,'rubber',8)
        for i in range(12):
            ang=i*2*math.pi/12
            self.beam('wheel_spokes',(x,y,z),(x,y+(r-.025)*math.sin(ang),z+(r-.025)*math.cos(ang)),.017,'ochre',6)
        self.cylinder_axis('wheel_hubs',(x,y,z),.065,.09,'brass','x',16)
    def wing(self,name,span,chord,y,z):
        xs=[-span/2,-span/2+.08]+[-span/2+.08+(span-.16)*i/12 for i in range(1,12)]+[span/2-.08,span/2]
        ts=[0,.06,.20,.40,.65,.85,1]
        def p(x,t,under=False):
            taper=.80 if abs(x)>span/2-.04 else 1
            return (x,y+.025*math.sin(math.pi*t)-(.024 if under else 0),z+(t-.5)*chord*taper)
        for x0,x1 in zip(xs,xs[1:]):
            for t0,t1 in zip(ts,ts[1:]):
                for under in [False,True]:
                    pts=[p(x0,t0,under),p(x0,t1,under),p(x1,t1,under),p(x1,t0,under)]
                    self.face(name,'canvas',list(reversed(pts)) if under else pts)
            for t in [0,1]:self.face(name,'canvas',[p(x0,t),p(x1,t),p(x1,t,True),p(x0,t,True)] if t==0 else [p(x1,t),p(x0,t),p(x0,t,True),p(x1,t,True)])
        for x in xs[1:-1]:self.tube('fabric_ribs',[p(x,t) for t in ts],.003,'canvas_patch',5)
        for x in [xs[0],xs[-1]]:
            self.face(name,'canvas',[p(x,t) for t in ts]+[p(x,t,True) for t in reversed(ts)])

models=[]
def model(name):m=Mesh(name);models.append(m);return m

# Continuous physical hall, simpler glazing rhythm and broad worn paint shapes.
m=model('hall_structure')
for x in [-14,14]:
    m.box('side_dado',(x,.7,12),(.30,1.4,30),'teal',.045)
    m.box('side_sill',(x,1.46,12),(.42,.13,30),'stone',.028)
    m.box('side_skirting',(x,.15,12),(.34,.30,30),'metal',.02)
    for z in [-3,7,17,27]:
        m.box('side_piers',(x,7,z),(.70,14,.72),'plaster',.065)
        m.box('pier_feet',(x,.43,z),(.82,.86,.82),'stone',.055)
        m.box('pier_capitals',(x,12.85,z),(.89,.36,.91),'plaster',.05)
    m.box('side_header',(x,13.55,12),(.95,.8,30),'plaster',.07)
    for y in [1.6,6.8,12.8]:m.block('side_window_rails',(x,y,12),(.10,.075,30),'window_frame')
    for z in [2,12,22]:m.block('side_window_mullions',(x,7.2,z),(.085,11.25,.085),'window_frame')
    # Exposed service conduit follows the wall and is not free-floating decoration.
    xx=x-0.25 if x>0 else x+0.25
    m.beam('wall_conduit',(xx,1.17,-2.9),(xx,1.17,26.9),.025,'rust')
    for z in [1,10,19]:m.block('conduit_clips',(xx,1.17,z),(.08,.10,.035),'metal')
m.box('rear_dado',(0,.7,27),(28,1.4,.30),'teal',.045)
m.box('rear_sill',(0,1.46,27),(28,.13,.42),'stone',.03)
m.box('rear_skirting',(0,.15,27),(28,.30,.34),'metal',.02)
for x in [-14,-7,0,7,14]:
    m.box('rear_piers',(x,7,27),(.70,14,.72),'plaster',.065)
    m.box('rear_pier_feet',(x,.43,27),(.82,.86,.82),'stone',.055)
    m.box('rear_capitals',(x,12.85,27),(.9,.36,.9),'plaster',.05)
for x in [-10.5,-3.5,3.5,10.5]:m.block('rear_mullions',(x,7.2,27),(.085,11.25,.10),'window_frame')
for y in [1.6,6.8,12.8]:m.block('rear_rails',(0,y,27),(28,.075,.10),'window_frame')
for x,y,w,h in [(-10,1,.8,.18),(-6.8,2.8,.34,.47),(7.05,5.1,.23,.5),(11.5,.9,1.2,.16),(0.1,8,.22,.58)]:
    z=26.62 if abs(x)<.4 or 6.5<abs(x)<7.5 else 26.837
    m.face('chipped_plaster','plaster_wear',[(x-w/2,y-h/2,z),(x-w*.35,y+h*.25,z),(x-w*.08,y+h*.35,z),(x+w*.1,y+h/2,z),(x+w/2,y+h*.15,z),(x+w*.3,y-h*.42,z)])
m.box('ceiling',(0,14.2,12),(28,.30,30),'plaster',.04)
for z in [-3,7,17,27]:m.box('ceiling_beams',(0,13.5,z),(28,.65,.47),'stone',.06)
for x in [-9,-3,3,9]:
    for z in [5,12,21]:
        m.box('light_housings',(x,13.24,z),(1.85,.14,.60),'metal',.025)
        m.box('ceiling_light_faces',(x,13.157,z),(1.68,.015,.46),'ivory_edge',.008)

m=model('hall_floor')
m.block('grout_bed',(0,-.10,12),(28,.16,30),'grout')
for x in range(-14,14):
    for z in range(-3,27):
        mat='floor_edge' if x in [-7,6] or z in [3,23] and -7<x<6 else ('floor_alt' if (x*7+z*11)%9==0 else 'floor_main')
        m.horizontal('tiles_'+mat,mat,x+.5,z+.5,-.015,.994,.994)
for x in [-6.03,6.03]:m.horizontal('floor_inlay','brass',x,13,-.013,.027,20)
for x,z in [(-7.7,8.4),(8.2,10.1),(-9.1,15.1),(5.5,21.8)]:
    m.face('worn_tiles','plaster_wear',[(x,-.011,z),(x+.40,-.011,z+.15),(x+.75,-.011,z+.12),(x+.46,-.011,z-.10),(x+.22,-.011,z-.15)])

m=model('banner')
m.box('top_crossbar',(0,0,0),(1.87,.095,.11),'brass',.02)
nx,ny=12,10
def cloth(i,j):
    x=-.825+1.65*i/nx;v=j/ny
    bottom=.10 if i==nx else (.20 if i==nx-1 else 0)
    return (x,-.08-4.40*v+bottom*v**8,.07*math.sin(i*math.pi/2.7)*(.25+.75*v)+.07*v*v)
for i in range(nx):
    for j in range(ny):
        p=[cloth(i,j),cloth(i,j+1),cloth(i+1,j+1),cloth(i+1,j)]
        m.face('cloth_blank','teal',p);m.face('cloth_back','teal',list(reversed(p)))
for x in [-.65,.65]:m.beam('ceiling_suspension',(x,.04,0),(x,1.70,0),.015,'metal')
m.beam('bottom_hem',cloth(0,ny),cloth(nx,ny),.025,'brass')

m=model('departure_board')
m.box('housing',(0,0,0),(7,2.2,.30),'metal',.08)
m.ring('bezel',(0,0,0),(6.82,2.04,.04),(6.58,1.80,.025),-.17,-.185,'ivory_dark')
m.panel('runtime_departures',(0,0,-.19),6.58,1.80,'screen',.025)
for y in [-.59,-.29,.01,.31,.61]:m.block('row_dividers',(0,y,-.194),(6.5,.014,.007),'metal')
for x in [-2.8,2.8]:m.beam('ceiling_rods',(x,1.05,0),(x,4.20,0),.025,'metal')
for x in [-3.34,3.34]:
    for y in [-.87,.87]:m.cyl('board_screws',(x,y,-.165),.035,.02,'brass','z',12)

m=model('public_bench')
for x in [-1.05,-.35,.35,1.05]:
    m.box('seat',(x,.50,0),(.63,.095,.56),'teal',.036,axis='y')
    m.box('seat_back',(x,.96,.25),(.63,.76,.085),'teal',.04)
    m.box('back_inset',(x,.95,.20),(.51,.49,.025),'teal_edge',.025)
    for z in [-.16,.19]:m.cyl('seat_fasteners',(x,.552,z),.009,.004,'metal',n=10)
for x in [-1.1,1.1]:
    m.box('leg',(x,.25,.08),(.08,.50,.48),'metal',.018)
    m.box('floor_foot',(x,.035,.08),(.20,.07,.61),'metal',.015)
m.box('rail',(0,.40,.1),(2.9,.09,.09),'metal',.015)
m.face('seat_wear','ivory_dark',[(-.39,.551,-.21),(-.47,.551,-.04),(-.28,.551,.08),(-.22,.551,-.13)])

m=model('exhibit_plinth')
m.box('base',(0,.41,0),(.87,.82,.88),'stone',.035)
m.box('plinth_cap',(0,.86,0),(1,.08,1),'plaster',.025,axis='y')
m.box('plinth_foot',(0,.045,0),(.95,.09,.96),'metal',.018,axis='y')
m.panel('blank_caption',(0,.63,-.448),.39,.12,'brass',.004)
m.face('plinth_damage','plaster_wear',[(-.431,.15,-.446),(-.431,.38,-.446),(-.20,.31,-.446),(-.30,.22,-.446)])

m=model('portal_cabinet')
m.box('cabinet',(0,1.1,0),(1.05,2.2,.65),'teal',.05)
m.box('door',(0,1.05,-.343),(.92,1.94,.04),'metal',.025)
m.panel('blank_diagnostic',(0,1.66,-.371),.64,.32,'screen',.018)
m.box('service_handle',(.34,1.08,-.38),(.04,.28,.04),'ivory_dark',.01)
for y in [.31,.42,.53,.64]:m.box('vent',(0,y,-.373),(.66,.023,.015),'rubber',.003)
for x,mat in [(-.21,'green'),(0,'ochre'),(.21,'red')]:m.cyl('indicator_'+mat,(x,1.35,-.373),.035,.016,mat,'z',16)

# Wide stepped megablocks, with three genuinely different masses and proportions.
def megablock(name,variant):
    m=model(name)
    specs=[[(0,9,0,26,18,21),(0,27,0,22,18,19),(1,40,0,29,8,23),(-2,52,1,18,16,17),(-2,66,1,23,10,20),(-4,78,2,14,14,13)],
           [(0,10,0,28,20,24),(0,26,0,26,12,22),(-6,45,1,14,26,18),(7,48,1,14,32,18),(0,66,1,30,8,20),(4,75,2,16,10,13)],
           [(0,10,0,29,20,24),(0,23,0,34,6,27),(0,38,0,24,24,20),(0,53,0,32,6,25),(2,66,1,21,20,18),(2,80,2,26,8,20)]]
    for level,(cx,cy,cz,w,h,d) in enumerate(specs[variant]):
        m.box('structural_mass',(cx,cy,cz),(w,h,d),'city_concrete',.25)
        m.block('tier_lip',(cx,cy-h/2+.25,cz),(w+.5,.50,d+.5),'city_dark')
        for y in [cy-h/2+2.1+i*2.8 for i in range(max(1,int((h-2)/2.8)))]:
            m.block('recessed_front',(cx,y,cz-d/2-.018),(w-1,.82,.065),'city_glass')
            m.block('recessed_front',(cx,y,cz+d/2+.018),(w-1,.82,.065),'city_glass')
            for side in [-1,1]:
                m.block('recessed_side',(cx+side*(w/2+.018),y,cz),(.065,.82,d-1),'city_glass')
                for row in range(max(1,int((d-1)/2.8))):
                    zz=cz-d/2+1.4+row*2.8
                    m.block('window_separators',(cx+side*(w/2+.065),y,zz),(.10,.85,.12),'city_light')
            for col in range(max(1,int((w-1)/2.8))):
                xx=cx-w/2+1.4+col*2.8
                m.block('window_separators',(xx,y,cz-d/2-.065),(.12,.85,.10),'city_light')
                if (col+level)%7==2:m.panel('pale_window',(xx+.8,y,cz-d/2-.062),1.08,.57,'city_paleglass',.002)
        for xx in [cx-w*.38,cx+w*.38]:m.block('vertical_service_spines',(xx,cy,cz-d/2-.22),(.55,h,.55),'city_dark')
    facade_z=min(cz-d/2 for cx,cy,cz,w,h,d in specs[variant] if cy-h/2<=33<=cy+h/2)
    m.box('facade_billboard',(0,33,facade_z-.18),(8.2,4.2,.40),'city_dark',.15)
    m.panel('runtime_billboard',(0,33,facade_z-.40),7.5,3.55,'city_teal',.04)
    for x in [-8,8]:
        m.box('podium_buttress',(x,8,-11.5),(3.2,16,3.5),'city_light',.3)
        m.block('service_band',(x,8,-13.27),(2.2,.65,.12),'rust')
    cx,cy,cz,w,h,d=specs[variant][-1]
    for x in [cx-w*.25,cx,cx+w*.25]:
        y=cy+h/2+1.25
        m.box('rooftop_equipment',(x,y,cz),(2.5,2.5,3),'city_dark',.15)
        m.cyl('exhaust_stacks',(x,y+2.3,cz),.34,2.3,'city_dark',n=12)
    for x,y,w,h in [(-7,14,1.5,3),(6,30,.8,2.6),(2,45,1.8,1.4)]:
        eligible=[cz-dd/2 for cx,cy,cz,ww,hh,dd in specs[variant] if cy-hh/2<=y and y+h<=cy+hh/2 and cx-ww/2<=x and x+w<=cx+ww/2]
        if eligible:
            zz=min(eligible)-.027
            m.face('broad_weathering','city_dark',[(x,y,zz),(x+.18,y+h,zz),(x+w,y+h*.7,zz),(x+w*.6,y+.4,zz)])
    return m
for name,v in [('megablock_civic',0),('megablock_twin',1),('megablock_terraced',2)]:megablock(name,v)

# Museum-scale Gutenberg-style reconstructed screw press, timber with iron fittings.
m=model('exhibit_printing_press')
for x in [-.61,.61]:
    m.box('uprights',(x,1.22,.12),(.22,2.44,.26),'timber',.025)
    m.box('feet',(x,.11,0),(.29,.22,1.23),'timber',.025)
    for y in [.32,2.02]:m.box('iron_straps',(x,y,-.019),(.25,.13,.023),'metal',.006)
m.box('top_crossbeam',(0,2.13,.12),(1.59,.28,.32),'timber',.035)
m.box('bottom_tie',(0,.29,.25),(1.31,.16,.17),'timber',.016)
m.cyl('screw_core',(0,1.91,.04),.071,1.20,'timber',n=16)
thread=[(.076*math.cos(i*2*math.pi/12),1.36+i*.010,.04+.076*math.sin(i*2*math.pi/12)) for i in range(115)]
m.tube('screw_thread',thread,.021,'timber_wear',6)
m.box('platen',(0,1.23,.025),(.98,.16,.71),'timber',.022)
m.box('platen_face',(0,1.132,.025),(.89,.025,.64),'metal',.009,axis='y')
m.box('carriage_bed',(0,.84,-.37),(1.05,.15,1.22),'timber',.02,axis='y')
m.box('type_bed',(0,.927,-.37),(.88,.025,1.01),'metal',.009,axis='y')
m.horizontal('unprinted_sheet','paper',0,-.47,.943,.72,.77)
for x in [-.48,.48]:m.box('carriage_rails',(x,.915,-.4),(.055,.065,1.32),'metal',.008)
m.box('screw_collar',(0,1.63,.04),(.22,.17,.22),'metal',.018)
m.beam('press_lever',(.05,1.65,-.015),(.97,1.64,-.20),.035,'timber_wear',10)
for x in [-.61,.61]:
    for y in [.34,2.04]:m.cyl('beam_bolts',(x,y,-.04),.025,.018,'ivory_dark','z',8)
m.face('wood_chips','timber_wear',[(-.716,.36,-.013),(-.69,.8,-.013),(-.60,.69,-.013),(-.63,.51,-.013)])

# Approximately 1:3 display model. No pilot, wheels or front-mounted propeller.
m=model('exhibit_wright_flyer')
for y in [.22,.86]:m.wing('main_wings',4.2,.61,y,0)
for x in [-1.8,-1.2,-.6,.6,1.2,1.8]:
    for z in [-.24,.24]:m.beam('wing_struts',(x,.24,z),(x,.86,z),.011,'timber_wear')
for x0,x1 in [(-1.8,-1.2),(-1.2,-.6),(.6,1.2),(1.2,1.8)]:
    for z in [-.24,.24]:
        m.beam('wing_rigging',(x0,.25,z),(x1,.84,z),.0035,'metal',5)
        m.beam('wing_rigging',(x1,.25,z),(x0,.84,z),.0035,'metal',5)
for y in [.25,.47]:m.wing('front_canard',1.1,.26,y,-1.08)
for x in [-.24,.24]:
    m.beam('canard_booms',(x,.23,-.16),(x,.25,-1.18),.010,'timber')
    m.beam('canard_struts',(x,.25,-1.1),(x,.47,-1.1),.008,'timber')
    m.tube('landing_skids',[(x,.16,-.91),(x,.07,-.74),(x,.05,-.48),(x,.05,.46),(x,.08,.58)],.015,'timber_wear')
    m.beam('tail_booms',(x,.25,.15),(x*.55,.6,.92),.009,'timber')
for x in [-.12,.12]:m.box('twin_rudders',(x,.60,.91),(.018,.43,.30),'canvas',.003)
m.box('engine_block',(.20,.32,.065),(.32,.14,.20),'metal',.01)
for i in range(4):m.cyl('four_cylinder_engine',(.095+i*.07,.405,.075),.032,.06,'ivory_dark',n=10)
for x in [-.71,.71]:
    m.beam('propeller_shafts',(x,.60,.26),(x,.60,.65),.014,'metal')
    m.box('two_blade_propellers',(x,.60,.65),(.055,.87,.021),'timber_wear',.010)
    m.cyl('propeller_hubs',(x,.60,.649),.033,.036,'metal','z',12)
    m.beam('drive_chains',(.20,.39,.17),(x,.60,.31),.007,'metal',5)
m.face('fabric_patch','canvas_patch',[(1.21,.895,-.08),(1.31,.895,.13),(1.56,.895,.12),(1.48,.895,-.1)])

# 1829-pattern Rocket display model, 0-2-2 wheel arrangement and inclined cylinders.
m=model('exhibit_rocket')
m.box('display_base',(0,.035,0),(1.50,.07,2.58),'stone',.04,axis='y')
for x in [-.49,.49]:m.box('short_exhibit_rails',(x,.09,0),(.05,.045,2.35),'metal',.008)
m.box('chassis',(0,.48,.08),(1.04,.15,1.89),'metal',.015)
m.cylinder_axis('yellow_boiler',(0,.97,-.025),.40,1.52,'ochre','z',32)
for z in [-.71,-.24,.25,.66]:m.cylinder_axis('boiler_bands',(0,.97,z),.412,.046,'brass','z',32)
m.cylinder_axis('smokebox',(0,.97,-.82),.405,.12,'metal','z',32)
m.cylinder_axis('smokebox_door',(0,.97,-.893),.32,.027,'rubber','z',24)
m.beam('door_latch',(-.11,.98,-.912),(.10,.98,-.912),.018,'brass')
m.cyl('chimney',(0,1.85,-.68),.095,1.57,'metal',n=16)
m.cyl('chimney_flare',(0,2.637,-.68),.128,.08,'metal',n=16)
m.cyl('safety_valve',(0,1.49,.31),.046,.23,'brass',n=16)
for x in [-.535,.535]:
    m.wheel((x,.59,-.46),.47)
    m.wheel((x,.40,.79),.28)
    m.beam('inclined_cylinders',(x,1.07,.59),(x,.72,.13),.089,'metal',12)
    xx=x*1.13
    m.beam('piston_rods',(xx,.75,.18),(xx,.54,-.47),.020,'brass')
m.box('firebox',(0,.83,.90),(.57,.66,.40),'ivory_dark',.025)
m.box('rear_step',(0,.37,1.12),(.86,.065,.24),'timber',.009)
m.face('paint_loss','rust',[(-.22,1.31,-.773),(-.15,1.34,-.773),(-.04,1.23,-.773),(-.21,1.20,-.773)])

# More legible 90s air-taxi silhouettes; the existing lane component is retained.
m=model('hover_vehicle')
m.profile('body','ochre',(0,.39,0),[(-1.95,1.32,.23,.11),(-1.66,1.74,.44,.16),(.90,1.78,.43,.15),(1.74,1.54,.35,.13),(1.9,1.22,.25,.1)])
m.profile('cabin','ochre',(0,.78,.1),[(-.80,1.26,.37,.10),(-.61,1.43,.48,.14),(.56,1.35,.43,.12),(.78,1.08,.30,.09)])
m.panel('front_glass',(0,.80,-.712),1.13,.25,'city_glass',.07)
for side in [-1,1]:
    for z0,z1 in [(-.38,.02),(.06,.53)]:
        def sx(z):return side*(.715-(z+.40)*.034)
        pts=[(sx(z0),.70,z0),(sx(z1),.70,z1),(sx(z1),.93,z1),(sx(z0),.93,z0)]
        m.face('side_glass','city_glass',list(reversed(pts)) if side>0 else pts)
m.face('rear_glass','city_glass',[(-.46,.67,.896),(.46,.67,.896),(.46,.88,.896),(-.46,.88,.896)])
m.box('front_bumper',(0,.28,-1.98),(1.35,.09,.12),'metal',.035)
for x in [-.53,.53]:
    m.panel('headlights',(x,.40,-1.965),.24,.09,'ivory_edge',.025)
    for z in [-1.23,1.27]:
        m.cyl('lift_pods',(x,.11,z),.25,.19,'metal',n=16)
        m.cyl('lift_core',(x,.017,z),.16,.025,'teal_edge',n=12)
m.box('roof_sign',(0,1.05,.15),(.55,.10,.25),'ivory',.04)

# Matching imported path; keep the fold as geometry and the printed face separate.
m=model('folded_newspaper')
for i in range(4):m.box('folded_pages',(i*.0015,.006+i*.004,0),(.25,.004,.34),'paper',.0005,axis='y')
m.horizontal('printed_face','paper',.0045,0,.021,.25,.34)

manifest=[m.write() for m in models]
# Each model already references refined_palette.mtl. Add the new palette entries.
(a.OUT/'refined_palette.mtl').write_text('\n'.join('newmtl %s\nKd %.4f %.4f %.4f\nKa 0 0 0\nKs 0 0 0\nNs 1\nd 1\nillum 2\n'%(k,*v) for k,v in COLORS.items()))
(HERE/'environment_mesh_manifest.json').write_text(json.dumps({'colors':COLORS,'models':manifest},indent=2))
print('Authored',len(models),'environment/exhibit models. All geometry remains independently editable.')
