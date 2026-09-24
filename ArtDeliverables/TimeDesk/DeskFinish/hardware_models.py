"""Reference-led hardware meshes. All exported objects are in local asset space."""
import math
import artlib as A
from artlib import *
from mathutils import Matrix, Vector


def build_hardware(texture_dir):
    material('Hardware_CRTClay','B5BAC0',.64,texture=texture_dir/'ivory_plastic.png')
    material('Hardware_CRTTrim','888F87',.73)
    material('Hardware_Cream','D9DFE5',.65,texture=texture_dir/'ivory_plastic.png')
    material('Hardware_Steel','ADB7BA',.64,.10)
    material('Hardware_Modifier','72918A',.69)
    material('Hardware_Keys','CACDC9',.68)
    material('Hardware_Seam','363D3C',.90)
    material('Hardware_Glass','243F47',.34)
    material('Hardware_Rubber','303735',.94)
    material('Hardware_CashPaint','A7BCB4',.60,.15,texture=texture_dir/'petrol_enamel.png')
    material('Hardware_CashEdge','526C65',.73)
    material('Hardware_BareMetal','9A9B8D',.49,.38)
    material('Hardware_Paper','CCC5AA',.97)
    material('Hardware_Amber','BC774C',.64)

    def bolt(name,p,axis='z'):
        cylinder(name+' head',p,.0045,.003,'Hardware_BareMetal',axis,20)
        x,y,z=p
        box(name+' slot',(x,y,z-.002),(.005,.001,.001),'Hardware_Seam',.0002)

    group('Finish_PC')
    # Folded steel chassis and a separate moulded front, with substantial drives.
    box('Rubber isolated case bottom',(0,.031,.015),(.957,.038,.716),'Hardware_Rubber',.005)
    for x in [-.381,.381]:
        for z in [-.252,.266]:box('Desktop rubber foot',(x,.011,z),(.074,.022,.086),'Hardware_Rubber',.004)
    box('Folded desktop steel cover',(0,.145,.018),(.970,.234,.700),'Hardware_Steel',.007)
    box('Upper cover rolled front edge',(0,.254,-.335),(.97,.018,.025),'Hardware_Cream',.004)
    face=box('Moulded desktop fascia',(0,.142,-.35),(.981,.223,.057),'Hardware_Cream',.009)
    # Deliberately generous recess depth remains readable at the scene camera.
    for cy in [.103,.191]:
        cut(face,box('Drive bay cutter',(-.104,cy,-.374),(.485,.068,.065),None,.004))
        box('Drive dark cavity',(-.104,cy,-.342),(.479,.063,.009),'Hardware_Seam',.003)
        box('Inset drive bezel',(-.104,cy,-.361),(.454,.051,.019),'Hardware_Steel',.003)
    box('Optical drawer surround',(-.129,.193,-.373),(.38,.035,.010),'Hardware_Seam',.003)
    box('Optical drawer front',(-.129,.192,-.379),(.365,.024,.013),'Hardware_Cream',.002)
    box('Optical eject recess',(.086,.178,-.377),(.027,.012,.005),'Hardware_Seam',.001)
    box('Optical eject switch',(.085,.178,-.382),(.020,.007,.008),'Hardware_Cream',.001)
    box('Floppy disk mouth',(-.126,.111,-.374),(.302,.011,.006),'Hardware_Seam',.001)
    box('Floppy centre finger recess',(-.126,.089,-.374),(.055,.025,.006),'Hardware_Seam',.001)
    box('Floppy eject',(.073,.091,-.382),(.036,.019,.015),'Hardware_Cream',.002)
    box('Power switch surround',(.348,.186,-.381),(.077,.042,.009),'Hardware_Seam',.004)
    box('Power rocker',(.348,.186,-.39),(.058,.026,.017),'Hardware_Amber',.003)
    cylinder('Drive activity diode',(.257,.19,-.382),.005,.003,'Indicator_Green','z',16)
    box('Front grille dark insert',(.32,.079,-.381),(.199,.050,.007),'Hardware_Seam',.003)
    for j in range(4):box('Raised grille slat',(.32,.061+j*.012,-.386),(.186,.005,.007),'Hardware_Cream',.001)
    box('Old inventory sticker',(-.417,.100,-.381),(.071,.047,.001),'Hardware_Paper',.001)
    box('Blank manufacturer insert',(-.417,.202,-.382),(.071,.019,.003),'Hardware_Modifier',.001)
    for x in [-.462,.462]:bolt('Front captive screw',(x,.05,-.381))
    for side in [-1,1]:
        box('Recessed case side vent',(side*.486,.134,.065),(.002,.115,.249),'Hardware_Seam',.001)
        for j in range(11):box('Steel side vent rib',(side*.488,.134,-.05+j*.023),(.004,.12,.009),'Hardware_Steel',.001)
    # A visibly separate swivel disc and forked tilt support.
    box('Broad monitor swivel foot',(0,.280,.045),(.51,.037,.40),'Hardware_CRTTrim',.012)
    cylinder('Swivel bearing rubber',(0,.302,.060),.159,.017,'Hardware_Rubber','y',56)
    cylinder('Swivel upper disc',(0,.316,.060),.150,.014,'Hardware_CRTTrim','y',56)
    for x in [-.107,.107]:
        box('Tilt yoke upright',(x,.357,.059),(.043,.086,.110),'Hardware_CRTTrim',.010)
        cylinder('Tilt pivot',(x,.394,.012),.029,.057,'Hardware_Seam','x',32)
    # Build the tube level, then tilt its complete assembly around the yoke.
    prior=set(A.current.objects)
    shell=loft('Tapered CRT rear shell',[
        (-.266,.862,.670,.036,.749),(-.190,.860,.670,.035,.749),
        (.100,.794,.608,.034,.763),(.390,.575,.454,.038,.757),
        (.455,.512,.410,.034,.751)],'Hardware_CRTClay')
    # Dark recessed ventilation fields and molded ribs follow the taper.
    for side in [-1,1]:
        for j in range(12):
            z=-.115+j*.027
            xx=.430-(z+.190)*(.033/.290) if z<=.100 else .397-(z-.100)*(.1095/.290)
            cut(shell,box('CRT cooling opening cutter',(side*(xx-.003),.748,z),(.038,.138,.013),None,.002))
            box('CRT recessed ventilation interior',(side*(xx-.017),.748,z),(.002,.133,.011),'Hardware_Seam',.001)
        for j in range(8):
            z=.225+j*.024;xx=.33-(z-.225)*.30
            box('Lower tube exhaust slit',(side*xx,.59,z),(.004,.055,.010),'Hardware_CRTTrim',.001)
    for j in range(13):
        x=(j-6)*.023
        box('Rear top exhaust',(x,1.001,.22),(.009,.003,.079),'Hardware_CRTTrim',.002)
    loft('Bezel assembly seam',[(-.277,.87,.681,.037,.749),(-.285,.87,.681,.037,.749)],'Hardware_Seam')
    # Continuous moulded bezel includes an inward slope around the aperture.
    loft('CRT rounded bezel',[
        (-.284,.872,.682,.038,.749),(-.303,.886,.689,.039,.749),
        (-.339,.882,.681,.033,.749),(-.345,.852,.654,.030,.749),
        (-.345,.705,.535,.029,.785),(-.321,.663,.501,.026,.785),
        (-.304,.650,.490,.025,.785)],'Hardware_CRTClay',False)
    loft('CRT glass rubber gasket',[
        (-.305,.651,.491,.024,.785),(-.310,.642,.482,.023,.785),
        (-.301,.633,.475,.022,.785)],'Hardware_Rubber',False)
    verts=[];faces=[];nx=32;ny=24
    for j in range(ny+1):
        y=(j/ny-.5)*.480
        for i in range(nx+1):
            x=(i/nx-.5)*.640
            z=-.328+.026*((x/.320)**2+(y/.240)**2)/2
            verts.append((x,y+.785,z))
    for j in range(ny):
        for i in range(nx):
            a=j*(nx+1)+i;faces.append((a,a+1,a+nx+2,a+nx+1))
    mesh('Convex blank four by three tube',verts,faces,'Hardware_Glass',True)
    box('Lower chin inset nameplate',(-.22,.446,-.347),(.151,.018,.003),'Hardware_CRTTrim',.002)
    for x in [.027,.062,.097]:
        box('Picture adjustment recess',(x,.447,-.347),(.028,.009,.003),'Hardware_Seam',.002)
        box('Picture adjustment button',(x,.447,-.352),(.019,.005,.008),'Hardware_CRTTrim',.001)
    cylinder('CRT power button collar',(.339,.448,-.348),.028,.007,'Hardware_Seam','z',36)
    cylinder('CRT power button',(.339,.448,-.356),.021,.014,'Hardware_Amber','z',36)
    cylinder('CRT status lamp',(.285,.448,-.349),.0045,.003,'Indicator_Green','z',16)
    patch('Worn service sticker',(-.364,.504,-.346),.040,.030,'Hardware_Paper')
    # A few case-edge scuffs, rather than coating the monitor in noise.
    patch('Wear at power button',(.385,.436,-.344),.018,.004,'Hardware_CRTTrim')
    pivot=Vector(v((0,.394,.012)))
    tilt=Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(-8),4,'X') @ Matrix.Translation(-pivot)
    for ob in set(A.current.objects)-prior:ob.matrix_world=tilt @ ob.matrix_world
    tube('CRT power cable',[(.16,.59,.46),(.22,.39,.51),(.31,.265,.42)],.006,'Hardware_Rubber')
    tube('Case power cable',[(.29,.13,.37),(.48,.029,.42),(.59,.012,.31),(.57,.012,.06)],.006,'Hardware_Rubber')

    group('Finish_Keyboard')
    # Low wedge with a real upper-shell recess and sculpted, shallow key tops.
    verts=[(-.473,.009,-.166),(.473,.009,-.166),(.473,.009,.166),(-.473,.009,.166),
           (-.471,.039,-.165),(.471,.039,-.165),(.471,.070,.165),(-.471,.070,.165)]
    case=mesh('Low sloping keyboard shell',verts,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],'Hardware_Cream')
    bevel(case,.008,4)
    box('Keyboard underside seam',(0,.015,0),(.932,.019,.318),'Hardware_Rubber',.006)
    def deck_y(z):return .039+(z+.165)*(.031/.33)
    for x,w,z,d in [(-.133,.628,-.008,.235),(.242,.122,-.023,.205),(.387,.124,-.015,.229),(-.138,.630,.130,.033)]:
        plate=box('Sunken key field',(x,deck_y(z)+.001,z),(w,.006,d),'Hardware_Seam',.004)
        plate.rotation_euler.x=math.atan(.031/.33)
    unit=.041
    def key(name,x,z,width=1,depth=1,mat='Hardware_Keys'):
        w=unit*width-.004;d=unit*depth-.005;bottom=deck_y(z)+.001;top=bottom+.014
        vv=[];ff=[]
        for yy,ww,dd in [(bottom,w,d),(top-.003,w-.003,d-.003),(top,w-.008,d-.007)]:
            vv.extend([(x+xx,yy+(zz)*(.031/.33),z+zz) for xx,zz in [(-ww/2,-dd/2),(ww/2,-dd/2),(ww/2,dd/2),(-ww/2,dd/2)]])
        for j in range(2):
            for i in range(4):ff.append((j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i))
        # Broad top facets meet a slight concave depression, not a tall pyramid.
        vv.append((x,top-.0015,z));ff.extend([(8+i,8+(i+1)%4,12) for i in range(4)])
        ob=mesh(name,vv,ff,mat);bevel(ob,.0006,2)
    rows=[[1]*13+[2],[1.5]+[1]*12+[1.5],[1.75]+[1]*11+[2.25],[2.25]+[1]*10+[2.75],[1.25,1.25,1.25,6.25,1.25,1.25,1.25,1.25]]
    for row,widths in enumerate(rows):
        x=-.440
        for i,w in enumerate(widths):
            mod=w>1.3 or row==4
            mat='Hardware_Modifier' if mod and w<6 else 'Hardware_Keys'
            if row==2 and i==len(widths)-1:mat='Hardware_Amber'
            key('Sculpted key row %d cap %d'%(row,i),x+w*unit/2,.074-row*.040,w,mat=mat);x+=w*unit
    key('Escape',-.4195,.13,mat='Hardware_Amber')
    for i in range(12):key('Function %d'%i,-.36+i*.038+(i//4)*.012,.13,mat='Hardware_Modifier')
    for row in range(2):
        for i in range(3):key('Navigation %d %d'%(row,i),.201+i*.040,.074-row*.040,mat='Hardware_Modifier')
    for x,z in [(.241,-.046),(.201,-.086),(.241,-.086),(.281,-.086)]:key('Arrow cluster',x,z,mat='Hardware_Modifier')
    for row in range(4):
        for i in range(3):key('Number pad %d %d'%(row,i),.347+i*.040,.074-row*.04)
    key('Wide zero',.367,-.086,2);key('Decimal',.427,-.086)
    for i in range(3):
        box('Keyboard indicator recess',(.345+i*.035,.069,.137),(.012,.003,.007),'Hardware_Seam',.001)
        box('Keyboard indicator light',(.345+i*.035,.071,.137),(.007,.003,.004),'Indicator_Green',.001)
    for x in [-.377,.377]:box('Keyboard rubber foot',(x,.005,.08),(.05,.01,.049),'Hardware_Rubber',.003)
    tube('Keyboard flexible cable',[(.29,.044,.162),(.40,.015,.25),(.49,.009,.38),(.51,.018,.63)],.004,'Hardware_Rubber')

    group('Finish_Till')
    # Low steel cashbox; no oversized checkout keyboard.
    for x in [-.245,.245]:
        for z in [-.15,.15]:box('Cashbox rubber foot',(x,.009,z),(.057,.018,.056),'Hardware_Rubber',.004)
    box('Folded cash drawer cabinet',(0,.113,0),(.646,.198,.406),'Hardware_CashPaint',.009)
    box('Cashbox rolled top lip',(0,.214,-.004),(.654,.019,.412),'Hardware_CashEdge',.005)
    box('Cashbox recessed upper lid',(0,.225,-.006),(.620,.014,.376),'Hardware_CashPaint',.007)
    for x in [-.20,.20]:
        cylinder('Rolled cashbox lid hinge',(x,.226,.195),.012,.09,'Hardware_CashEdge','x',24)
        cylinder('Lid hinge pin',(x,.226,.195),.005,.102,'Hardware_BareMetal','x',20)
        box('Lid hinge leaf',(x,.238,.172),(.071,.003,.038),'Hardware_CashEdge',.002)
    box('Drawer dark reveal',(0,.098,-.206),(.609,.142,.007),'Hardware_Seam',.005)
    box('Pressed drawer front',(0,.099,-.218),(.591,.118,.018),'Hardware_CashPaint',.009)
    box('Inset pull opening',(-.090,.119,-.23),(.204,.033,.005),'Hardware_Seam',.005)
    box('Drawer handle upper lip',(-.090,.133,-.238),(.206,.011,.019),'Hardware_CashEdge',.003)
    cylinder('Cash drawer lock barrel',(.216,.103,-.234),.021,.015,'Hardware_BareMetal','z',36)
    cylinder('Lock inner disc',(.216,.103,-.244),.015,.004,'Hardware_Seam','z',28)
    box('Lock key slot',(.216,.103,-.248),(.003,.021,.003),'Hardware_BareMetal',.0005)
    # Functional key in the drawer lock, echoing the reference's working cashbox.
    box('Inserted drawer key',(.216,.103,-.262),(.004,.009,.038),'Hardware_BareMetal',.001)
    keyring=[]
    for i in range(33):
        a=math.tau*i/32;keyring.append((.216+math.cos(a)*.015,.083+math.sin(a)*.020,-.283))
    tube('Drawer key bow',keyring,.002,'Hardware_BareMetal')
    patch('Scuffed pull contact',(-.07,.131,-.25),.055,.004,'Hardware_BareMetal')
    # Display on an angled short stand, small enough to belong to the box.
    box('Credit display mounting foot',(.073,.239,.073),(.239,.017,.112),'Hardware_CashEdge',.005)
    box('Credit display stem',(.073,.271,.103),(.121,.071,.035),'Hardware_CashEdge',.006)
    box('Credit terminal shell',(.073,.345,.080),(.342,.162,.083),'Hardware_CashEdge',.010)
    box('Credit terminal front rim',(.073,.345,.034),(.325,.147,.016),'Hardware_CashPaint',.006)
    box('Credit display gasket',(.073,.348,.023),(.295,.113,.008),'Hardware_Rubber',.004)
    box('Blank live credit screen',(.073,.348,.017),(.274,.093,.006),'Hardware_Glass',.003)
    box('Blank cashbox inventory label',(-.24,.241,-.102),(.085,.001,.042),'Hardware_Paper',.002)
    for x in [-.289,.289]:bolt('Cash drawer fastener',(x,.064,-.229))
    # A discreet chip at the actual pull, rather than repeated corner damage.
    patch('Worn drawer pull edge',(-.14,.140,-.249),.037,.003,'Hardware_BareMetal')
    tube('Credit display cable',[(.165,.29,.122),(.249,.235,.158),(.311,.04,.194),(.341,.012,.251)],.004,'Hardware_Rubber')

    # Publish exact attachment points for Unity; no guessed screen placement.
    p=(0,.785,-.328);pivot=(0,.394,.012);t=math.radians(8)
    screen=[0,pivot[1]+(p[1]-pivot[1])*math.cos(t)-(p[2]-pivot[2])*math.sin(t),pivot[2]+(p[1]-pivot[1])*math.sin(t)+(p[2]-pivot[2])*math.cos(t)]
    return {'screenLocalCenter':screen,'screenTiltDegrees':8,'screenSize':[.640,.480],
            'creditsLocalPosition':[.073,.348,.011],'creditsTextScale':.80,
            'chassisTop':.262,'hardwareFeetHeight':0}
