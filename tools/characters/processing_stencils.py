"""Exact row-decomposed versions of the approved numpy disk stencils.

The disk is a union of horizontal runs. Reusing those runs reduces memory
traffic without changing its footprint, border values, or float precision.
Enabled only by the full-library pipeline; the pilot entry point is untouched.
"""
import math
import numpy as np
from charkit import imgops as io

_original_bleed=io.bleed


def _filter(values, radius, op, border):
    if radius<=0:
        return values.copy()
    runs={dy:min(radius,math.isqrt(radius*radius+radius-dy*dy)) for dy in range(-radius,radius+1)}
    needed=set(runs.values())
    horizontal={}
    grown=values.copy()
    if 0 in needed:
        horizontal[0]=grown.copy()
    for dx in range(1,radius+1):
        op(grown,io.shift(values,0,dx,border),out=grown)
        op(grown,io.shift(values,0,-dx,border),out=grown)
        if dx in needed:
            horizontal[dx]=grown.copy()
    out=np.full_like(values,border)
    for dy,half in runs.items():
        op(out,io.shift(horizontal[half],dy,0,border),out=out)
    return out


def dilate(mask,r):
    return _filter(mask,r,np.logical_or,False)


def erode(mask,r):
    return _filter(mask,r,np.logical_and,True)


def local_max(values,valid,r):
    src=np.where(valid,values,-np.inf).astype(np.float32)
    return _filter(src,r,np.maximum,-np.inf)


def bleed(rgb,valid,iterations):
    ys,xs=np.nonzero(valid)
    if not len(xs) or iterations<=0:
        return rgb.copy(),valid.copy()
    y0,y1=max(0,int(ys.min())-iterations),min(valid.shape[0],int(ys.max())+iterations+1)
    x0,x1=max(0,int(xs.min())-iterations),min(valid.shape[1],int(xs.max())+iterations+1)
    grown,reached=_original_bleed(rgb[y0:y1,x0:x1],valid[y0:y1,x0:x1],iterations)
    out,mask=rgb.copy(),valid.copy()
    out[y0:y1,x0:x1]=grown
    mask[y0:y1,x0:x1]=reached
    return out,mask


def install():
    io.dilate=dilate
    io.erode=erode
    io.local_max=local_max
    io.bleed=bleed
