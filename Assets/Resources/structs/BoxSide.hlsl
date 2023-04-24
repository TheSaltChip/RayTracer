#ifndef BOX_SIDE_HLSL
#define BOX_SIDE_HLSL

#include "Ray.hlsl"

struct BoxSide
{
    float3 pos0;
    float3 pos1;
    float4x4 rotation;
    float3 offset;

    bool Hit(const Ray r, const float minDist, const float maxDist, out HitRecord rec)
    {
        const float4 translatedOrigin = float4(r.origin - offset, 1);

        float4 origin = mul(rotation, translatedOrigin);
        float4 direction = mul(rotation, float4(r.dir, 1));

        Ray rotated = (Ray)0;
        rotated.MakeRay(origin.xyz, direction.xyz);

        const float t = (pos0.z - rotated.origin.z) * rotated.invDir.z;
        const float a = rotated.origin.x + t * rotated.dir.x;
        const float b = rotated.origin.y + t * rotated.dir.y;

        float2 p0 = pos0.xy;
        float2 p1 = pos1.xy;
        const float4 normal = float4(0, 0, 1, 1);

        rec = (HitRecord)0;

        if (t < minDist || t > maxDist
            || a < p0.x || a > p1.x
            || b < p0.y || b > p1.y)
            return false;

        const float4x4 invRot = transpose(rotation);

        rec.hitPoint = mul(invRot, float4(rotated.PointAt(t), 1)).xyz + offset;

        rec.SetFaceNormal(r.dir, mul(invRot, normal).xyz);

        rec.didHit = true;
        rec.dist = t;
        return true;
    }
};

#endif
