#include "Spectrum.hlsli"

cbuffer Constants : register(b0)
{
    float4 field : packoffset(c0);
    float4 style : packoffset(c1);
    float4 options : packoffset(c2);
    float4 tint : packoffset(c3);
    float4 values[64] : packoffset(c4);
};

float4 main(
    float4 position : SV_POSITION,
    float4 scenePosition : SCENE_POSITION,
    float4 uv0 : TEXCOORD0
) : SV_TARGET
{
    int count = (int)field.z;
    if (count < 1)
        return float4(0.0, 0.0, 0.0, 0.0);

    float width = field.x;
    float height = field.y;
    float radius = field.w;
    float threshold = style.x;
    float rimThickness = style.y;
    float rimBrightness = style.z;
    int window = (int)style.w;
    float bipolar = options.x;

    float halfWidth = width * 0.5;
    float halfHeight = height * 0.5;
    float columnWidth = width / count;
    float baseline = bipolar > 0.5 ? 0.0 : halfHeight;
    float span = bipolar > 0.5 ? halfHeight : height;

    float2 local = scenePosition.xy;
    float antialias = max(max(fwidth(local.x), fwidth(local.y)), 0.5);

    int nearest = (int)floor((local.x + halfWidth) / columnWidth);
    float density = 0.0;
    float2 gradient = float2(0.0, 0.0);

    int first = max(nearest - window, 0);
    int last = min(nearest + window, count - 1);

    [loop]
    for (int index = first; index <= last; index++)
    {
        float value = SpectrumLane(values[index >> 2], index & 3);
        float reach = (bipolar > 0.5 ? value : abs(value)) * span;
        float centreX = -halfWidth + (index + 0.5) * columnWidth;

        float slide = abs(reach) > 0.0001 ? saturate((local.y - baseline) / -reach) : 0.0;
        float2 delta = local - float2(centreX, baseline - slide * reach);
        float ratio = length(delta) / radius;
        if (ratio >= 1.0)
            continue;

        float shell = 1.0 - ratio * ratio;
        density += shell * shell * shell;
        gradient += delta * (-6.0 * shell * shell / (radius * radius));
    }

    float slope = max(length(gradient), 0.00001);
    float signedDistance = (density - threshold) / slope;
    float body = saturate(signedDistance / antialias + 0.5);
    float rim = LineCoverage(abs(signedDistance), rimThickness * 0.5, antialias) * saturate(rimThickness / antialias);

    float coverage = max(body, rim) * tint.a;
    float3 colour = lerp(tint.rgb, float3(1.0, 1.0, 1.0), rim * rimBrightness);
    return float4(colour * coverage, coverage);
}
