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
        dir = direction;
        invDir = 1 / direction;
    }

    float3 PointAt(const float t)
    {
        return origin + t * dir;
    }

    // Branchless Ray/Bounding box intersection
    // Rays which just touch a corner, edge, or face of the bounding box will be considered intersecting
    // credit: https://tavianator.com/2022/ray_box_boundary.html
    bool Intersection(float3 boxMin, float3 boxMax, float t)
    {
        float tmin = 0.0, tmax = t;

        for (int d = 0; d < 3; ++d)
        {
            float t1 = (boxMin[d] - origin[d]) * invDir[d];
            float t2 = (boxMax[d] - origin[d]) * invDir[d];

            tmin = min(max(t1, tmin), max(t2, tmin));
            tmax = max(min(t1, tmax), min(t2, tmax));
        }

        return tmin <= tmax;
    }

    void LambertianScatter(const HitRecord rec, inout float3 attenuation)
    {
        MakeRay(rec.hitPoint, rec.normal + RandomHemisphereDirection(rec.normal));

        attenuation *= rec.material.color;
    }

    void MetalScatter(const HitRecord rec, inout float3 attenuation)
    {
        const float3 reflected = reflect(UnitVector(dir), rec.normal);
        MakeRay(rec.hitPoint, reflected + rec.material.fuzz * RandomDirection());
        attenuation *= rec.material.color;
    }

    void DielectricScatter(const HitRecord rec, inout float3 attenuation)
    {
        float3 outwardNormal;
        const float3 reflected = reflect(dir, rec.normal);
        float niOverNt;

        attenuation *= float3(1.0, 1.0, 1.0);
        float3 refracted;
        float reflectProb = 1.0;
        float cosine;

        if (dot(dir, rec.normal) > 0)
        {
            outwardNormal = -rec.normal;
            niOverNt = rec.material.refIdx;
            cosine = rec.material.refIdx * dot(dir, rec.normal) / length(dir);
        }
        else
        {
            outwardNormal = rec.normal;
            niOverNt = 1 / rec.material.refIdx;
            cosine = -rec.material.refIdx * dot(dir, rec.normal) / length(dir);
        }

        if (Refract(dir, outwardNormal, niOverNt, refracted))
        {
            reflectProb = Schlick(cosine, rec.material.refIdx);
        }

        if (Random() < reflectProb)
        {
            MakeRay(rec.hitPoint, reflected);
        }
        else
        {
            MakeRay(rec.hitPoint, refracted);
        }
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
        default:
            break;
        }
    }
};

#endif
