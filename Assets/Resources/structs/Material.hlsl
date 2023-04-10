#ifndef MATERIAL_HLSL
#define MATERIAL_HLSL

struct MatTypes
{
    uint lambertian;
    uint metal;
    uint dielectric;
};

// "enum" material_types {}
static const MatTypes MATERIAL_TYPES = {0, 1, 2};

struct Material
{
    uint type;
    float4 color;
    float fuzz;
    float refIdx;
    float4 emissionColor;
    float emissionStrength;
};

Material MakeLambertianMaterial(const float4 color)
{
    Material m;
    m.type = MATERIAL_TYPES.lambertian;
    m.color = color;
    m.fuzz = 0;
    m.refIdx = 0;

    return m;
}

Material MakeMetalMaterial(const float4 color, const float fuzz)
{
    Material m;
    m.type = MATERIAL_TYPES.metal;
    m.color = color;
    m.fuzz = fuzz < 1 ? fuzz : 1;
    m.refIdx = 0;

    return m;
}

Material MakeDielectricMaterial(const float refIdx)
{
    Material m;
    m.type = MATERIAL_TYPES.dielectric;
    m.color = float4(1, 1, 1, 1);
    m.fuzz = 0;
    m.refIdx = refIdx;

    return m;
}

#endif
