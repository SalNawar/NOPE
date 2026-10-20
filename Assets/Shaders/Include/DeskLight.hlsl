// The desk's light (P0-A seam; owner LIGHT): the global tint and level the hall's
// day cycle sets for every desk object (Shader.SetGlobalColor/SetGlobalFloat), and
// DeskLight(), the multiplier a desk shader applies to its colour. The P0 stub
// returns 1 (today's look); nothing includes this file yet.
#ifndef NOPE_DESK_LIGHT_INCLUDED
#define NOPE_DESK_LIGHT_INCLUDED

float4 _DeskLightTint;
float _DeskLightLevel;

// The desk light's multiplier on a desk object's colour. The P0 stub: 1.
half3 DeskLight()
{
    return half3(1, 1, 1);
}

#endif
