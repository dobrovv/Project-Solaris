
void DottedCircle_float(
    float2 UV,
    float Radius,
    float Thickness,
    float DotCount,
    float DotSize,
    out float Alpha
)
{
    float2 p = UV - float2(0.5, 0.5);

    // Distance from circle center
    float d = length(p);

    // Radial ring mask
    float inner = Radius - Thickness * 0.5;
    float outer = Radius + Thickness * 0.5;

    float radialAA = max(fwidth(d), 0.00001);

    float ring = smoothstep(
        inner - radialAA,
        inner + radialAA,
        d
    ) * (1.0 - smoothstep(
        outer - radialAA,
        outer + radialAA,
        d
    ));

    // Angular position
    float angle = atan2(p.y, p.x);
    float normalizedAngle = frac(angle / 6.28318530718);

    // Distance to center of each dot segment
    float segment = frac(normalizedAngle * DotCount);
    float angularDistance = min(segment, 1.0 - segment);

    // Convert angular distance to approximate UV distance
    float circumference = 6.28318530718 * Radius;
    float dotDistance = angularDistance * circumference / DotCount;

    // Dot radius
    float dotRadius = DotSize * 0.5;

    float dotAA = max(fwidth(dotDistance), 0.00001);

    float dots = 1.0 - smoothstep(
        dotRadius - dotAA,
        dotRadius + dotAA,
        dotDistance
    );

    Alpha = ring * dots;
}
