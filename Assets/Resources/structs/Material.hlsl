#ifndef MATERIAL_HLSL
#define MATERIAL_HLSL

struct MatTypes
{
    uint lambertian;
    uint metal;
    uint dielectric;
    uint isotropic;
};

// "enum" material_types {}
static const MatTypes MATERIAL_TYPES = {0, 1, 2, 3};

struct Material
{
    uint type;
    float4 color;
    float fuzz;
    float refIdx;
    float4 emissionColor;
    float emissionStrength;
};
#endif
