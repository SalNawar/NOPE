"""Offline authoring of the booth props; no Unity installer or runtime code."""
from mesh_authoring import Mesh, COLORS, OUT, HERE
import math, json

models=[]
def model(name):m=Mesh(name);models.append(m);return m

m=model('crt_monitor')
m.box('swivel_base',(0,.045,.015),(.73,.09,.52),'ivory',.035,axis='y')
m.cyl('swivel',(0,.13,.065),.16,.10,'ivory_dark')
m.profile('tube_casing','ivory',(0,.60,0),[(-.32,.86,.71,.05),(-.29,.92,.76,.07),(.10,.84,.69,.075),(.34,.60,.51,.07),(.38,.52,.44,.07)])
m.ring('front_bezel',(0,.62,0),(.92,.75,.075),(.73,.56,.045),-.345,-.365,'ivory_edge')
m.ring('screen_recess',(0,.62,0),(.73,.56,.045),(.68,.51,.045),-.365,-.390,'ivory_dark')
m.panel('crt_screen_4_3',(0,.62,-.392),.68,.51,'screen',.044)
m.box('control_strip',(0,.277,-.357),(.69,.065,.025),'ivory',.009)
m.cyl('power_button',(.29,.277,-.383),.025,.018,'ivory_dark','z')
m.cyl('power_indicator',(.235,.276,-.377),.005,.006,'green','z',12)
for i in range(3):m.box('adjust_buttons',(-.18+i*.045,.276,-.377),(.025,.010,.012),'ivory_dark',.003)
for i in range(9):m.box('tube_vent',(.433,.52+i*.025,-.08),(.009,.009,.15),'rubber',.002)
for x in [-.385,.385]:m.cyl('case_fasteners',(x,.306,-.364),.006,.003,'ivory_dark','z',8)
m.tube('power_cable',[(.10,.08,.3),(.25,.018,.5),(.52,.015,.54),(.67,.012,.48)],.012,'rubber')

m=model('keyboard')
m.box('keyboard_body',(0,.032,0),(.82,.064,.29),'ivory',.018,axis='y')
m.box('key_bed',(0,.064,.01),(.75,.006,.235),'ivory_dark',.006,axis='y')
for row in range(5):
    for col in range(16):
        if row==0 and 4<=col<=10:continue
        y=.078+row*.004
        mat='key_dark' if col in [0,15] or row==4 else 'key'
        m.box('keys_'+mat,((col-7.5)*.045,y,(row-2)*.043),(.038,.021,.034),mat,.006,axis='y')
m.box('spacebar',(0,.078,-.086),(.30,.021,.034),'key',.006,axis='y')
for i in range(3):m.cyl('status_indicators',(.31+i*.022,.073,.127),.003,.003,'green',n=8)
m.tube('keyboard_cable',[(.30,.02,.13),(.33,.015,.26),(.28,.012,.4),(.42,.012,.48)],.007,'rubber')

m=model('desk')
m.box('top',(0,1,0),(5.8,.12,1.8),'wood',.045,axis='y')
m.box('front_edge',(0,.905,-.84),(5.74,.13,.09),'wood_edge',.025)
m.box('lower_apron',(0,.76,-.75),(5.5,.18,.09),'wood',.015)
for x in [-2.65,2.65]:m.box('leg',(x,.45,.15),(.16,.9,1.22),'metal',.025)
for x in [-2.65,2.65]:m.box('edge_repair',(x,1.005,-.88),(.12,.05,.035),'brass',.008)

m=model('credits_till')
m.box('drawer',(0,.055,0),(.60,.11,.48),'ivory',.02)
m.box('drawer_seam',(0,.075,-.241),(.52,.012,.005),'ivory_dark',.001)
m.box('drawer_pull',(0,.059,-.25),(.14,.025,.028),'metal',.006)
m.profile('till_body','ivory_edge',(0,.22,0),[(-.22,.54,.12,.025),(-.17,.59,.20,.035),(.18,.57,.30,.04),(.23,.49,.26,.035)])
m.box('key_bed',(0,.343,-.055),(.43,.018,.28),'ivory_dark',.012,axis='y')
for row in range(4):
    for col in range(5):m.box('till_keys_'+('action' if col==4 else 'number'),((col-2)*.071,.362,(row-1.5)*.060-.055),(.057,.025,.048),'teal' if col==4 else 'key',.008,axis='y')
m.box('readout_housing',(0,.46,.15),(.39,.19,.14),'ivory',.02)
m.ring('readout_recess',(0,.46,0),(.35,.145,.012),(.32,.12,.009),.073,.068,'ivory_dark')
m.panel('runtime_credits',(0,.46,.066),.32,.12,'screen',.008)

m=model('next_sign')
m.box('sign_base',(0,.024,0),(.65,.048,.22),'brass',.018,axis='y')
m.box('sign_casing',(0,.14,.015),(.60,.23,.09),'ivory',.025)
m.ring('sign_recess',(0,.14,0),(.55,.18,.017),(.52,.15,.015),-.034,-.038,'ivory_dark')
m.panel('runtime_next',(0,.14,-.04),.52,.15,'teal',.015)
for x in [-.275,.275]:m.cyl('sign_screws',(x,.14,-.033),.007,.006,'brass','z',8)

for modelname,part,w,h,cy in [('digital_clock','clock',.44,.24,.11),('stability_device','stability',.42,.39,.19)]:
    m=model(modelname)
    m.box('casing',(0,cy,.01),(w,h,.14),'ivory',.025)
    sh=.15 if part=='clock' else .20;sy=.11 if part=='clock' else .235
    m.ring('display_recess',(0,sy,0),(w-.055,sh+.03,.018),(w-.085,sh,.014),-.066,-.070,'ivory_dark')
    m.panel('runtime_'+part,(0,sy,-.072),w-.085,sh,'screen',.014)
    if part=='stability':
        for x,mat in [(-.08,'green'),(.08,'ivory_dark')]:m.cyl('lamp_'+mat,(x,.062,-.068),.013,.009,mat,'z',12)

m=model('calendar')
m.box('calendar_backing',(0,.20,.005),(.28,.42,.035),'ivory',.012)
m.panel('runtime_calendar',(0,.20,-.014),.23,.34,'paper',.003)
m.box('binding',(0,.385,-.025),(.265,.035,.023),'teal',.006)
for x in [-.085,.085]:m.cyl('calendar_pins',(x,.39,-.041),.009,.013,'brass','z',12)

m=model('partition')
m.box('solid_panel',(0,1.02,0),(.08,2.04,2.05),'teal',.03)
m.box('lower_inset',(0,.62,0),(.091,.64,1.8),'teal_edge',.015)
for z in [-1.04,1.04]:m.box('terminal_post',(0,1.045,z),(.12,2.09,.12),'wood',.02)
m.box('cap',(0,2.08,0),(.15,.11,2.19),'wood_edge',.03,axis='y')
m.box('skirting',(0,.10,0),(.12,.15,2.14),'metal',.012)

m=model('scanner_tray')
m.box('tray_base',(0,.025,0),(.82,.05,.45),'metal',.025,axis='y')
m.box('scanner_glass',(0,.056,0),(.73,.008,.34),'screen',.013,axis='y')
for x in [-.39,.39]:m.box('side_rail',(x,.065,0),(.04,.07,.43),'ivory',.01)
m.box('rear_feed',(0,.085,.19),(.75,.12,.06),'ivory',.015)
m.box('feed_slot',(0,.078,.155),(.62,.018,.008),'rubber',.002)
m.box('front_rail',(0,.052,-.205),(.75,.045,.045),'ivory',.012)

m=model('desk_stamp')
m.box('stamp_rubber',(0,.012,0),(.16,.024,.085),'rubber',.01,axis='y')
m.box('stamp_base',(0,.033,0),(.175,.026,.10),'wood',.009,axis='y')
m.cyl('stamp_neck',(0,.089,0),.021,.09,'wood')
for y,r,h in [(.13,.029,.025),(.15,.043,.028),(.17,.044,.022),(.186,.028,.016)]:m.cyl('stamp_handle',(0,y,0),r,h,'wood_edge')

m=model('intercom')
m.profile('body','ivory',(0,.095,0),[(-.14,.29,.08,.02),(-.1,.36,.16,.025),(.12,.35,.23,.025),(.16,.29,.18,.025)])
for i in range(7):m.box('speaker_slots',(-.055,.2-i*.014,-.13),(.15-abs(3-i)*.018,.006,.012),'rubber',.002)
m.cyl('talk_button',(.115,.098,-.149),.026,.025,'brass','z',20)
m.cyl('activity_lamp',(.115,.168,-.138),.006,.005,'green','z',12)

# A shaped, segmented gate keeps its independently controlled opening.
m=model('portal_ring')
for i in range(24):
    a0=(i+.05)*2*math.pi/24;a1=(i+.95)*2*math.pi/24
    ring=[]
    for r,z in [(2.9,.25),(2.94,-.18),(2.83,-.34),(2.42,-.34),(2.34,-.20),(2.34,.22)]:
        ring.append([(r*math.cos(a),3.05+r*math.sin(a),z) for a in [a0,a1]])
    for j in range(len(ring)):
        k=(j+1)%len(ring)
        mat='brass' if i%6==0 else ('ivory_dark' if j in [0,5] else 'ivory')
        m.face('segment_'+mat,mat,[ring[j][1],ring[j][0],ring[k][0],ring[k][1]])
    for side in [0,1]:m.face('joint','metal',[ring[j][side] for j in (range(len(ring)) if side else reversed(range(len(ring))))])
    a=(a0+a1)/2
    for r in [2.55,2.72]:m.cyl('gate_fasteners',(r*math.cos(a),3.05+r*math.sin(a),-.352),.022,.015,'metal','z',10)
m.box('platform',(0,.12,0),(6.15,.24,1.6),'metal',.07,axis='y')
for x in [-2.15,2.15]:m.box('support',(x,.35,.10),(.72,.7,1.15),'ivory_dark',.04)

(OUT/'refined_palette.mtl').write_text('\n'.join('newmtl %s\nKd %.4f %.4f %.4f\nKa 0 0 0\nKs 0 0 0\nNs 1\nd 1\nillum 2\n'%(k,*v) for k,v in COLORS.items()))
manifest=[m.write() for m in models]
(HERE/'refined_mesh_manifest.json').write_text(json.dumps({'colors':COLORS,'models':manifest},indent=2))
print('Reauthored',len(models),'prop meshes; preserved existing import paths and backed up prior meshes.')
