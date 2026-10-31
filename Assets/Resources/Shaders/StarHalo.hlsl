void StarHalo_float(
    float2 UV,
    float Radius,
    float Falloff,
    float Intensity,
    out float Glow
)
{
    float2 p = UV - 0.5;

    float distance = length(p);

    float gradient = saturate(1.0 - distance / Radius);

    Glow = pow(gradient, Falloff) * Intensity;
}