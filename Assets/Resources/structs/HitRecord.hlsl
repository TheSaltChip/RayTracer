#ifndef HITTABLE_HLSL
#define HITTABLE_HLSL

#include "Material.hlsl"

struct HitRecord
{
    bool didHit;
    float dist;
    float3 hitPoint;
    float3 normal;
    bool frontFace;
    Material material;

    void SetFaceNormal(const Ray ray, const float3 outwardNormal)
    {
        frontFace = dot(ray.dir, outwardNormal) < 0;
        normal = frontFace ? outwardNormal : -outwardNormal;
    }
};

#endif
