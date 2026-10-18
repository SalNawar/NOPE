// A moving paper's bend (Saleh's 1018a playtest: "all documents are stiff"): the same height field
// as the engine-free PaperBend (Assets/Scripts/Visuals/PaperBend.cs, tested in PaperBendTests); keep
// the two in step. A bow along the travel (the trailing end drooping most), a twist of the corners and
// a ripple running back from the leading edge. In the face's unit space (x, y from -0.5 to 0.5; the
// face quad is scaled to the paper's metres), depths in metres away from the viewer (the face's +z).
//   _Flex     (travel x, travel y, bow, lag)
//   _Flex2    (twist, ripple height, ripple phase, ripple half waves)
//   _FlexSize (the paper's width and height in metres)
#ifndef TIMEDESK_PAPER_BEND
#define TIMEDESK_PAPER_BEND

// The depth at xy and its slopes per unit of xy.
float PaperBendDepth(float2 xy, float4 flex, float4 flex2, float2 size, out float2 slope)
{
    slope = float2(0, 0);
    if (flex.z == 0 && flex2.x == 0 && flex2.y == 0)
        return 0;
    float2 dir = flex.xy;
    float along = max(abs(dir.x) * size.x + abs(dir.y) * size.y, 1e-6);
    float across = max(abs(dir.y) * size.x + abs(dir.x) * size.y, 1e-6);
    float2 sxy = float2(dir.x * size.x, dir.y * size.y) / along;
    float2 txy = float2(-dir.y * size.x, dir.x * size.y) / across;
    float2 X = xy * 2.0;
    float s = dot(X, sxy);
    float t = dot(X, txy);
    float w = (1.0 - s) * 0.5;
    float angle = PI * flex2.w * s - flex2.z;
    float depth = flex.z * s * s * (1.0 - flex.w * s) + flex2.x * s * t + flex2.y * w * w * sin(angle);
    float ds = flex.z * (2.0 * s - 3.0 * flex.w * s * s) + flex2.x * t + flex2.y * (-w * sin(angle) + w * w * cos(angle) * PI * flex2.w);
    float dt = flex2.x * s;
    slope = (ds * sxy + dt * txy) * 2.0;
    return depth;
}

#endif
