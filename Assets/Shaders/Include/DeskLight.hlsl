// The desk's one light (BUG-14; owner LIGHT): the level and colour the hall's
// day cycle gives every desk object, published as globals by DeskLightLevel
// (DeskLighting's maths) and multiplied in by every desk shader, so papers,
// props, the rack, the traveller and the passport photo follow one rule.
//   _DeskLightTint   rgb: the light's colour (white by day); a: 1 while published
//   _DeskLightLevel  the level, 1 = the full day (the desk as drawn), the lamp's share included
//   _DeskLightKey    NOPE/Desk Anime's key energy at the full day (HallLightingSO.deskKeyAtDay)
//   _DeskLampPool    xyz: the lamp pool's centre on the mat (world); w: its radius (metres)
//   _DeskLampShare   the lamp's part of the level (0 while it is off)
// Nothing published (a scene without the office, edit mode): every function
// returns today's look (DeskLight() = 1), so no scene goes dark by default.
#ifndef NOPE_DESK_LIGHT_INCLUDED
#define NOPE_DESK_LIGHT_INCLUDED

float4 _DeskLightTint;
float _DeskLightLevel;
float _DeskLightKey;
float4 _DeskLampPool;
float _DeskLampShare;

// 1 while DeskLightLevel publishes the desk light, 0 otherwise.
half DeskLightPublished()
{
    return saturate(_DeskLightTint.a);
}

// The desk light's multiplier on a flat (unlit) desk colour: papers, the
// passport photo, the traveller. Uniform over the desk (the photo and the
// traveller read the same value, so the photo check is fair at every hour).
half3 DeskLight()
{
    return lerp(half3(1, 1, 1), _DeskLightTint.rgb * _DeskLightLevel, DeskLightPublished());
}

// The lamp pool's weight at a desk point: 1 at the pool's centre, falling to 0 at its radius.
half DeskLampPool(float3 positionWS)
{
    float2 d = positionWS.xz - _DeskLampPool.xz;
    float r = max(_DeskLampPool.w, 1e-3);
    return 1 - smoothstep(0.25 * r, r, length(d));
}

// The desk light at a desk point: DeskLight() at the pool's centre, the
// lamp's share falling off away from it (the warm pool on the mat).
half3 DeskLightAt(float3 positionWS)
{
    half level = _DeskLightLevel - _DeskLampShare * (1 - DeskLampPool(positionWS));
    return lerp(half3(1, 1, 1), _DeskLightTint.rgb * level, DeskLightPublished());
}

#endif
