#ifndef CAMERA_HLSL
#define CAMERA_HLSL

#include "Ray.hlsl"
#include "../common/HelperFunctions.hlsl"

struct Camera
{
    float3 lowerLeftCorner;
    float3 horizontal;
    float3 vertical;
    float3 origin;

    Ray GetRay(const float u, const float v) 
    {
        return MakeRay(origin, lowerLeftCorner + u * horizontal + v * vertical - origin);
    }
};

Camera MakeStaticCamera(const float3 lowerLeftCorner, const float3 horizontal, const float3 vertical,
                          const float3 origin)
{
    Camera c;

    c.lowerLeftCorner = lowerLeftCorner;
    c.horizontal = horizontal;
    c.vertical = vertical;
    c.origin = origin;

    return c;
}

Camera MakeAdjustableFovCamera(const float vFov, const float aspect)
{
    const float theta = vFov * 3.14159265359f / 180;
    const float halfHeight = tan(theta / 2);
    const float halfWidth = aspect * halfHeight;

    return MakeStaticCamera(float3(-halfWidth, -halfHeight, -1.0),
                              float3(2 * halfWidth, 0, 0),
                              float3(0, 2 * halfHeight, 0),
                              float3(0, 0, 0));
}


Camera MakeDynamicCamera(const float3 lookFrom, const float3 lookAt, const float3 vup, const float vFov,
                           const float aspect)
{
    const float theta = vFov * 3.14159265359f / 180;
    const float halfHeight = tan(theta / 2);
    const float halfWidth = aspect * halfHeight;

    const float3 w = UnitVector(lookFrom - lookAt);
    const float3 u = UnitVector(cross(vup, w));
    const float3 v = cross(w, u);

    return MakeStaticCamera(lookFrom - halfWidth * u - halfHeight * v - w,
                              2 * halfWidth * u,
                              2 * halfHeight * v,
                              lookFrom);
}

#endif
