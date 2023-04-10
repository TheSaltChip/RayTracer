#ifndef SCATTER_FUNCTIONS_HLSL
#define SCATTER_FUNCTIONS_HLSL

#include "../structs/Ray.hlsl"
#include "../structs/HitRecord.hlsl"
#include "Random.hlsl"
#include "HelperFunctions.hlsl"

bool LambertianScatter(const Ray rIn, const HitRecord rec, inout float3 attenuation, out Ray scattered);
bool MetalScatter(const Ray rIn, const HitRecord rec, inout float3 attenuation, out Ray scattered);
bool DielectricScatter(const Ray rIn,const HitRecord rec, inout float3 attenuation, out Ray scattered);

bool Scatter(const Ray rIn, const HitRecord rec, inout float3 attenuation, out Ray scattered)
{
    switch (rec.material.type)
    {
    case 0:
        return LambertianScatter(rIn, rec, attenuation, scattered);
    case 1:
        return MetalScatter(rIn, rec, attenuation, scattered);
    case 2:
        return DielectricScatter(rIn, rec, attenuation, scattered);
    default:
        return false;
    }
}

bool LambertianScatter(const Ray rIn, const HitRecord rec, inout float3 attenuation, out Ray scattered)
{
    scattered = MakeRay(rec.hitPoint, rec.normal + RandomHemisphereDirection(rec.normal));
    attenuation *= rec.material.color;

    return true;
}

bool MetalScatter(const Ray rIn, const HitRecord rec, inout float3 attenuation, out Ray scattered)
{
    const float3 reflected = reflect(UnitVector(rIn.dir), rec.normal);
    scattered = MakeRay(rec.hitPoint, reflected + rec.material.fuzz * RandomDirection());
    attenuation *= rec.material.color;

    return true;
}

bool DielectricScatter(const Ray rIn, const HitRecord rec, inout float3 attenuation, out Ray scattered)
{
    float3 outwardNormal;
    const float3 reflected = reflect(rIn.dir, rec.normal);
    float niOverNt;

    attenuation *= float3(1.0, 1.0, 1.0);
    float3 refracted;
    float reflectProb = 1.0;
    float cosine;

    if (dot(rIn.dir, rec.normal) > 0)
    {
        outwardNormal = -rec.normal;
        niOverNt = rec.material.refIdx;
        cosine = rec.material.refIdx * dot(rIn.dir, rec.normal) / length(rIn.dir);
    }
    else
    {
        outwardNormal = rec.normal;
        niOverNt = 1 / rec.material.refIdx;
        cosine = -rec.material.refIdx * dot(rIn.dir, rec.normal) / length(rIn.dir);
    }

    if (Refract(rIn.dir, outwardNormal, niOverNt, refracted))
    {
        reflectProb = Schlick(cosine, rec.material.refIdx);
    }

    if (Random() < reflectProb)
    {
        scattered = MakeRay(rec.hitPoint, reflected);
    }
    else
    {
        scattered = MakeRay(rec.hitPoint, refracted);
    }

    return true;
}


#endif
