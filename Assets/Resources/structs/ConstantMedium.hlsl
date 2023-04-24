#ifndef CONSTANT_MEDIUM_HLSL
#define CONSTANT_MEDIUM_HLSL

#include "Material.hlsl"
#include "Ray.hlsl"
#include "HitRecord.hlsl"

struct ConstantMedium
{
    float3 min;
    float3 max;
    float negInvDensity;
    
    Material material;

    bool Hit(const Ray r, const float minDist, const float maxDist, out HitRecord rec)
    {
        
    }
};

#endif
