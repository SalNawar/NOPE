# Reference model for the piece-6 ArabicShaper spec (presentation forms + simplified visual reorder).
# forms: letter -> (isolated, final, initial, medial); None where the form does not exist (right-joining).
F = {
 0x0621:(0xFE80,None,None,None),
 0x0622:(0xFE81,0xFE82,None,None),0x0623:(0xFE83,0xFE84,None,None),0x0624:(0xFE85,0xFE86,None,None),
 0x0625:(0xFE87,0xFE88,None,None),0x0626:(0xFE89,0xFE8A,0xFE8B,0xFE8C),0x0627:(0xFE8D,0xFE8E,None,None),
 0x0628:(0xFE8F,0xFE90,0xFE91,0xFE92),0x0629:(0xFE93,0xFE94,None,None),0x062A:(0xFE95,0xFE96,0xFE97,0xFE98),
 0x062B:(0xFE99,0xFE9A,0xFE9B,0xFE9C),0x062C:(0xFE9D,0xFE9E,0xFE9F,0xFEA0),0x062D:(0xFEA1,0xFEA2,0xFEA3,0xFEA4),
 0x062E:(0xFEA5,0xFEA6,0xFEA7,0xFEA8),0x062F:(0xFEA9,0xFEAA,None,None),0x0630:(0xFEAB,0xFEAC,None,None),
 0x0631:(0xFEAD,0xFEAE,None,None),0x0632:(0xFEAF,0xFEB0,None,None),0x0633:(0xFEB1,0xFEB2,0xFEB3,0xFEB4),
 0x0634:(0xFEB5,0xFEB6,0xFEB7,0xFEB8),0x0635:(0xFEB9,0xFEBA,0xFEBB,0xFEBC),0x0636:(0xFEBD,0xFEBE,0xFEBF,0xFEC0),
 0x0637:(0xFEC1,0xFEC2,0xFEC3,0xFEC4),0x0638:(0xFEC5,0xFEC6,0xFEC7,0xFEC8),0x0639:(0xFEC9,0xFECA,0xFECB,0xFECC),
 0x063A:(0xFECD,0xFECE,0xFECF,0xFED0),0x0641:(0xFED1,0xFED2,0xFED3,0xFED4),0x0642:(0xFED5,0xFED6,0xFED7,0xFED8),
 0x0643:(0xFED9,0xFEDA,0xFEDB,0xFEDC),0x0644:(0xFEDD,0xFEDE,0xFEDF,0xFEE0),0x0645:(0xFEE1,0xFEE2,0xFEE3,0xFEE4),
 0x0646:(0xFEE5,0xFEE6,0xFEE7,0xFEE8),0x0647:(0xFEE9,0xFEEA,0xFEEB,0xFEEC),0x0648:(0xFEED,0xFEEE,None,None),
 0x0649:(0xFEEF,0xFEF0,None,None),0x064A:(0xFEF1,0xFEF2,0xFEF3,0xFEF4),
}
LAMALEF = {0x0622:(0xFEF5,0xFEF6),0x0623:(0xFEF7,0xFEF8),0x0625:(0xFEF9,0xFEFA),0x0627:(0xFEFB,0xFEFC)}
def dual(c): return c in F and F[c][2] is not None
def joins(c): return c in F and c!=0x0621
def shape(s):
    cps=[ord(ch) for ch in s]; out=[]; i=0
    while i<len(cps):
        c=cps[i]
        if c not in F: out.append(c); i+=1; continue
        prev = cps[i-1] if i>0 else None
        prevJoins = prev is not None and dual(prev) and joins(c)  # previous letter can connect forward, and this one joins (never a hamza)
        # lam-alef
        if c==0x0644 and i+1<len(cps) and cps[i+1] in LAMALEF:
            iso,fin = LAMALEF[cps[i+1]]
            out.append(fin if prevJoins else iso); i+=2; continue
        nxt = cps[i+1] if i+1<len(cps) else None
        nextJoins = dual(c) and nxt is not None and joins(nxt)
        iso,fin,ini,med = F[c]
        if prevJoins and nextJoins: out.append(med)
        elif prevJoins: out.append(fin)
        elif nextJoins: out.append(ini)
        else: out.append(iso)
        i+=1
    return out
def cls(c):
    if 0x0600<=c<=0x06FF or 0xFB50<=c<=0xFDFF or 0xFE70<=c<=0xFEFF: return 'R'
    ch=chr(c)
    if ch.isdigit() or ('A'<=ch<='Z') or ('a'<=ch<='z'): return 'L'
    return 'N'
MIRROR={'(':')',')':'(','[':']',']':'[','<':'>','>':'<','{':'}','}':'{','«':'»','»':'«'}
def visual(s):
    cps=shape(s); k=[cls(c) for c in cps]
    # ET/ES rule: % + - . , : between/after digits join an L run when adjacent to a digit
    for i,c in enumerate(cps):
        if k[i]=='N' and chr(c) in '%+-.,:' :
            left = i>0 and k[i-1]=='L' and chr(cps[i-1]).isdigit()
            right = i+1<len(cps) and k[i+1]=='L' and chr(cps[i+1]).isdigit()
            if (chr(c) in '.,:' and left and right) or (chr(c) in '%' and left) or (chr(c) in '+-' and right):
                k[i]='L'
    # neutrals between two L become L, else R (RTL paragraph)
    res=list(k)
    for i in range(len(k)):
        if k[i]=='N':
            l=next((k[j] for j in range(i-1,-1,-1) if k[j]!='N'),None)
            r=next((k[j] for j in range(i+1,len(k)) if k[j]!='N'),None)
            res[i]='L' if (l=='L' and r=='L') else 'R'
    # build runs, reverse run order, reverse chars inside R runs (mirror brackets)
    runs=[]; 
    for i,c in enumerate(cps):
        if runs and runs[-1][0]==res[i]: runs[-1][1].append(c)
        else: runs.append([res[i],[c]])
    out=[]
    for d,chars in reversed(runs):
        # spaces inside a right-to-left run become no-break spaces (U+00A0), so TMP never
        # wraps inside an Arabic phrase (TMP_Text.cs:4741 skips 0xA0 as a break opportunity)
        if d=='R': out.extend(0xA0 if c==0x20 else ord(MIRROR.get(chr(c),chr(c))) for c in reversed(chars))
        else: out.extend(chars)
    return ''.join(chr(c) for c in out)
if __name__=='__main__':
    import sys
    for w in ['قبول','رفض','لا','الأدلة','إلى','الاستقرار: 87%','اليوم 3','سجل الأدلة','عدم تطابق','(اليوم)','اليوم 3 — الإحاطة الصباحية']:
        v=visual(w)
        print(w, '->', ' '.join(f'{ord(c):04X}' for c in v))
