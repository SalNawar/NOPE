# Piece-6 spec helper: WCAG contrast of proposed palettes (neutral XP + 8 cultures).
# Rev 1 (review 2026-09-24): two-ring hover outline (rings >= outline^2 apart), VerdictStrip role,
# DocumentBacking is diegetic (neutral only), scene-wins neutral values (Accept #296B38, Deny #752929),
# book rows #FFFFFF0A over the culture's WindowBody.
import json, sys
def lin(c):
    return c/12.92 if c <= 0.04045 else ((c+0.055)/1.055)**2.4
def lum(rgb):
    r,g,b = rgb[:3]
    return 0.2126*lin(r)+0.7152*lin(g)+0.0722*lin(b)
def ratio(a,b):
    la,lb = lum(a),lum(b)
    hi,lo = max(la,lb),min(la,lb)
    return (hi+0.05)/(lo+0.05)
def hx(s):
    s=s.lstrip('#'); return (int(s[0:2],16)/255,int(s[2:4],16)/255,int(s[4:6],16)/255)
def over(fg, a, bg):
    return tuple(fg[i]*a+bg[i]*(1-a) for i in range(3))

# role map: role -> (fillSeed, alpha, inkSeed, cls, surface)
MAP = {
 'Desktop':('wallpaper',1,None,None,True),
 'Taskbar':('chrome',1,None,None,True),
 'StartButton':('accent',1,'accentInk','Large',False),
 'Tray':('chromeDeep',1,'chromeInk','Text',False),
 'WindowBody':('paper',1,'ink','Text',True),
 'TitleBar':('chrome',1,'chromeInk','Text',True),
 'Button':('face',1,'faceInk','Text',False),
 'CloseButton':('reject',1,'rejectInk','Glyph',False),
 'AcceptButton':('approve',1,'approveInk','Text',False),
 'DenyButton':('reject',1,'rejectInk','Text',False),
 'ActionButton':('accent',1,'accentInk','Text',False),
 'SearchButton':('accent',1,'accentInk','Text',False),
 'DesktopIcon':('accent',0.85,'accentInk','Text',False),
 'BackButton':('accent',0.95,'accentInk','Text',False),
 'Panel':('chromeDeep',0.92,'chromeInk','Text',True),
 'PanelTitle':('chromeDeep',0.92,'panelTitle','Large',False),
 'ClaimStrip':('chromeDeep',0.8,'chromeInk','Large',False),
 'Alert':('reject',0.96,'rejectInk','Large',True),
 'StickyNote':('note',0.97,'ink','Text',False),
 'CompareBar':('note',1,'ink','Text',False),
 'CompareMatch':('note',1,'match','Text',False),
 'CompareMismatch':('note',1,'mismatch','Text',False),
 'CompareNeutral':('note',1,'ink','Text',False),
 'StartMenu':('chromeDeep',0.97,None,None,True),
 'MenuEntry':('accent',1,'accentInk','Text',False),
 'PowerEntry':('reject',1,'rejectInk','Text',False),
 'NewsletterBorder':('dark',1,None,None,False),
 'Newsletter':('paper',1,'ink','Text',True),
 'NewsletterButton':('dark',1,'darkInk','Text',False),
 'VerdictStrip':('chromeDeep',0.8,'chromeInk','Large',False),
 'InputField':('white',1,'ink','Text',False),
}
THRESH={'Text':4.5,'Large':3.0,'Glyph':3.0,'Hint':3.0}
DIEGETIC_SURFACES={'DiegeticPaper':(0.97,0.96,0.92),'DiegeticRow':over((1,1,1),0.7,(0.925,0.913,0.847))}

NEUTRAL_SEEDS = {
 'wallpaper':(0.23,0.45,0.74),'chrome':(0.13,0.34,0.86),'chromeInk':(1,1,1),'chromeDeep':(0.1,0.32,0.78),
 'accent':(0.24,0.6,0.23),'accentInk':(1,1,1),'paper':(0.925,0.913,0.847),'ink':(0.1,0.09,0.08),
 'face':(0.925,0.913,0.847),'faceInk':(0,0,0),'note':(1,1,0.88),'approve':hx('#296B38'),'approveInk':(1,1,1),
 'reject':(0.86,0.25,0.18),'rejectInk':(1,1,1),'dark':(0.16,0.15,0.13),'darkInk':(1,1,1),
 'match':(0.05,0.45,0.12),'mismatch':(0.72,0.1,0.08),'highlight':(1,0.92,0.35),'panelTitle':(0.7,0.85,1),
 'muted':(0.45,0.45,0.45),'white':(1,1,1),
 'ringDark':hx('#092E70'),'ringLight':hx('#FFD76E'),
}
# exact XP overrides: role -> (fill rgba or None, ink or None)
NEUTRAL_OVERRIDES = {
 'Panel':((0.07,0.1,0.16,0.92),(1,1,1)),
 'PanelTitle':((0.07,0.1,0.16,0.92),(0.7,0.85,1)),
 'StartMenu':((0.1,0.12,0.18,0.97),None),
 'ClaimStrip':((0.06,0.18,0.42,0.8),(1,1,1)),
 'ActionButton':((0.16,0.28,0.42,1),(1,1,1)),
 'SearchButton':((0.15,0.3,0.5,1),(1,1,1)),
 'DesktopIcon':((0.2,0.3,0.45,0.85),(1,1,1)),
 'BackButton':((0.2,0.3,0.5,0.95),(1,1,1)),
 'MenuEntry':((0.2,0.25,0.35,1),(1,1,1)),
 'DenyButton':(hx('#752929')+(1,),(1,1,1)),
 'VerdictStrip':((0.06,0.18,0.42,0.8),(1,1,1)),
 'Alert':((0.85,0.2,0.15,0.96),(1,1,1)),
 'PowerEntry':((0.5,0.2,0.2,1),(1,1,1)),
 'NewsletterBorder':((0.1,0.09,0.08,1),None),
 'StickyNote':((1,0.96,0.6,0.97),(0.1,0.09,0.08)),
}
def resolve(seeds, overrides):
    s=dict(NEUTRAL_SEEDS); s.update(seeds)
    out={}
    for role,(fs,a,ink,cls,surf) in MAP.items():
        fill = s[fs] if fs else None
        ik = s[ink] if ink else None
        alpha=a
        if role in overrides:
            f,i = overrides[role]
            if f is not None: fill=f[:3]; alpha=f[3]
            if i is not None: ik=i[:3]
        out[role]=(fill,alpha,ik,cls,surf)
    return s,out
def check(name, seeds, overrides, verbose=False):
    s,pal=resolve(seeds,overrides)
    fails=[]; worst=[]
    for role,(fill,a,ink,cls,surf) in pal.items():
        if ink is not None and fill is not None and cls:
            r=ratio(ink,fill)
            worst.append((r,role))
            if r < THRESH[cls]: fails.append(f'{role} ink/fill {r:.2f} < {THRESH[cls]}')
    # selection highlight: ink over highlight over diegetic row
    hl=over(s['highlight'],0.7,DIEGETIC_SURFACES['DiegeticRow'])
    r=ratio(NEUTRAL_SEEDS['ink'],hl)
    if r<4.5: fails.append(f'highlight {r:.2f}')
    # hover rings: dark inner + light outer; >= outline^2 apart guarantees one ring >= outline on ANY surface
    rr=ratio(s['ringDark'],s['ringLight'])
    if rr < 9.0: fails.append(f'rings {rr:.2f} < 9.0')
    # book rows (#FFFFFF0A, diegetic) over this culture's WindowBody: diegetic ink must stay readable
    book=over((1,1,1),10/255,pal['WindowBody'][0])
    r=ratio(NEUTRAL_SEEDS['ink'],book)
    if r<4.5: fails.append(f'book row ink {r:.2f}')
    # record rows (#FFFFFFB3), record labels (#595240) and note (#594D33) on this culture's WindowBody
    paper=pal['WindowBody'][0]
    for nm,val in [('record row',ratio(NEUTRAL_SEEDS['ink'],over((1,1,1),179/255,paper))),('record label',ratio(hx('#595240'),paper)),('record note',ratio(hx('#594D33'),paper))]:
        if val<4.5: fails.append(f'{nm} {val:.2f}')
    worst.append((rr**0.5,'rings (worst surface)'))
    # accept vs deny distinguishable (hue diff) - informational
    print(f'== {name}: {"PASS" if not fails else "FAIL"}  min text ratio {min(worst)[0]:.2f} ({min(worst)[1]})')
    for f in fails: print('   ',f)
    return not fails

CULTURES = json.load(open(sys.argv[1], encoding='utf-8')) if len(sys.argv)>1 else {}
ok = check('neutral', {}, NEUTRAL_OVERRIDES)
# diegetic scanner backing (neutral only): #D9DBE0 on #21242B
print('   DiegeticBacking ink/fill', round(ratio(hx('#D9DBE0'),hx('#21242B')),2))
for cid, seeds in CULTURES.items():
    ok &= check(cid, {k:hx(v) for k,v in seeds.items()}, {})
print('ALL OK' if ok else 'SOME FAIL')
