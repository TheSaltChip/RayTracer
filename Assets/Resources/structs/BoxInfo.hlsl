#ifndef BOX_INFO_HLSL
#define BOX_INFO_HLSL

#include "HitRecord.hlsl"

struct BoxInfo
{
    float3 min;
    float3 max;
    int firstBoxIndex;
    
    Material material;
};

#endif
