#ifndef MESH_INFO_HLSL
#define MESH_INFO_HLSL

#include "Material.hlsl"

struct MeshInfo
{
    uint firstTriangleIndex;
    uint numTriangles;
    float3 boundsMin;
    float3 boundsMax;
    Material material;
};
#endif
