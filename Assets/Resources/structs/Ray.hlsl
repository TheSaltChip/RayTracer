#ifndef RAY_HLSL
#define RAY_HLSL
#include "HitRecord.hlsl"
#include "Assets/Resources/common/Random.hlsl"
#include "Assets/Resources/common/HelperFunctions.hlsl"

struct Ray
{
    float3 origin;
    float3 dir;
    float3 invDir;

    void MakeRay(const float3 _origin, const float3 direction)
    {
        origin = _origin;
        dir = UnitVector(direction);
        invDir = 1 / dir;
    }

    float3 PointAt(const float t)
    {
        return origin + t * dir;
    }

    // Branchless Ray/Bounding box intersection
    // Rays which just touch a corner, edge, or face of the bounding box will be considered intersecting
    // credit: https://tavianator.com/2022/ray_box_boundary.html
    bool Intersection(const float3 boxMin, const float3 boxMax, const float t)
    {
        float tMin = 0.0, tMax = t;

        UNITY_UNROLL
        for (int d = 0; d < 3; ++d)
        {
            const float t1 = (boxMin[d] - origin[d]) * invDir[d];
            const float t2 = (boxMax[d] - origin[d]) * invDir[d];

            tMin = min(max(t1, tMin), max(t2, tMin));
            tMax = max(min(t1, tMax), min(t2, tMax));
        }

        return tMin <= tMax;
    }

    void Scatter(const HitRecord rec, inout float3 attenuation)
    {
        switch (rec.material.type)
        {
        case 0:
            LambertianScatter(rec, attenuation);
            break;
        case 1:
            MetalScatter(rec, attenuation);
            break;
        case 2:
            DielectricScatter(rec, attenuation);
            break;
        case 3:
            IsotropicScatter(rec, attenuation);
            break;
        default:
            break;
        }
    }

    void LambertianScatter(const HitRecord rec, inout float3 attenuation)
    {
        MakeRay(rec.hitPoint, rec.normal + RandomHemisphereDirection(rec.normal));

        attenuation *= rec.material.color;
    }

    void MetalScatter(const HitRecord rec, inout float3 attenuation)
    {
        MakeRay(rec.hitPoint, reflect(dir, rec.normal) + rec.material.fuzz * RandomDirection());

        attenuation *= rec.material.color;
    }

    void DielectricScatter(const HitRecord rec, inout float3 attenuation)
    {
        attenuation *= float3(1.0, 1.0, 1.0);

        float cosine;
        float niOverNt;
        float3 outwardNormal;
        const float refIdx = rec.material.refIdx;

        if (dot(dir, rec.normal) > 0)
        {
            outwardNormal = -rec.normal;
            niOverNt = refIdx;
            cosine = refIdx * dot(dir, rec.normal) / length(dir);
        }
        else
        {
            outwardNormal = rec.normal;
            niOverNt = 1 / refIdx;
            cosine = -refIdx * dot(dir, rec.normal) / length(dir);
        }

        const float3 reflected = reflect(dir, rec.normal);

        float3 refracted;
        float3 direction = reflected;

        if (Refract(dir, outwardNormal, niOverNt, refracted))
        {
            const bool shouldReflect = Random() < Schlick(cosine, refIdx);
            direction = reflected * shouldReflect + refracted * (1 - shouldReflect);
        }

        MakeRay(rec.hitPoint, direction);
    }

    void IsotropicScatter(const HitRecord rec, inout float3 attenuation)
    {
        MakeRay(rec.hitPoint, RandomDirection());
        attenuation += rec.material.color;
    }
};

#endif
