#ifndef DOF_CAMERA_HLSL
#define CAMERA_HLSL

#include "Ray.hlsl"
#include "../common/HelperFunctions.hlsl"
#include "../common/Random.hlsl"

struct DoFCamera
{
    float3 lowerLeftCorner;
    float3 horizontal;
    float3 vertical;
    float3 origin;
    float lensRadius;
    float3 u, v, w;

    Ray GetRay(const float s, const float t)
    {
        const float2 rd = lensRadius;// * RandomUnitCircle();
        const float3 offset = u * rd.x + v * rd.y;
        return MakeRay(origin + offset, lowerLeftCorner + s * horizontal + t * vertical - origin - offset);
    }
};


DoFCamera MakeDofCamera(const float3 lookFrom, const float3 lookAt, const float3 vup, const float vFov,
                           const float aspect, const float aperture, const float focusDist)
{
    const float theta = vFov * (3.14159265359f / 180.f);
    const float halfHeight = tan(theta / 2.f);
    const float halfWidth = aspect * halfHeight;

    DoFCamera c;

    c.w = UnitVector(lookFrom - lookAt);
    c.u = UnitVector(cross(vup, c.w));
    c.v = cross(c.w, c.u);

    c.lowerLeftCorner = lookFrom - halfWidth * c.u * focusDist - halfHeight * c.v * focusDist - focusDist * c.w;
    c.vertical = 2.f * halfHeight * c.v * focusDist;
    c.horizontal = 2.f * halfWidth * c.u * focusDist;
    c.origin = lookFrom;

    c.lensRadius = aperture / 2.f;

    return c;
}

#endif
