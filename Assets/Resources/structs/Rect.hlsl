#ifndef RECT_HLSL
#define RECT_HLSL

#include "Material.hlsl"
#include "HitRecord.hlsl"
#include "Ray.hlsl"

struct Orientation
{
    uint xy;
    uint xz;
    uint yz;
};

static const Orientation ORIENTATION = {0, 1, 2,};

struct Rect
{
    uint orientation;
    float3 pos0;
    float3 pos1;
    float k;
    float3 sinRotation;
    float3 cosRotation;
    float3 offset;
    float scale;
    Material material;

    HitRecord Hit(const Ray r, const float tMin, const float tMax)
    {
        Ray translated;
        switch (orientation)
        {
        case ORIENTATION.xy:
            translated = MakeRay(r.origin - offset, r.dir);
            break;
        case ORIENTATION.xz:
            translated = MakeRay(r.origin - offset, r.dir);
            break;
        case ORIENTATION.yz:
            translated = MakeRay(r.origin - offset, r.dir);
            break;
        default:
            translated = r;
            break;
        }

        float3 origin = translated.origin;
        float3 direction = translated.dir;


        float sinTheta = sinRotation.y;
        float cosTheta = cosRotation.y;

        float3x3 rotY = float3x3(
            cosTheta, 0, -sinTheta,
            0, 1, 0,
            sinTheta, 0, cosTheta);

        origin = mul(rotY, origin);
        direction = mul(rotY, direction);
        Ray rotated = MakeRay(origin, direction);

        float a, b, t;
        float2 p0, p1;
        float3 normal;

        switch (orientation)
        {
        case ORIENTATION.xy:
            t = (k - rotated.origin.z) * rotated.invDir.z;
            a = rotated.origin.x + t * rotated.dir.x;
            b = rotated.origin.y + t * rotated.dir.y;
            p0 = pos0.xy;
            p1 = pos1.xy;
            normal = float3(0, 0, 1);
            break;
        case ORIENTATION.xz:
            t = (k - rotated.origin.y) * rotated.invDir.y;
            a = rotated.origin.x + t * rotated.dir.x;
            b = rotated.origin.z + t * rotated.dir.z;
            p0 = pos0.xz;
            p1 = pos1.xz;
            normal = float3(0, 1, 0);
            break;
        case ORIENTATION.yz:
            t = (k - rotated.origin.x) * rotated.invDir.x;
            a = rotated.origin.y + t * rotated.dir.y;
            b = rotated.origin.z + t * rotated.dir.z;
            p0 = pos0.yz;
            p1 = pos1.yz;
            normal = float3(1, 0, 0);
            break;
        default:
            p0 = 0;
            p1 = 0;
            t = -1;
            a = 0;
            b = 0;
            break;
        }

        HitRecord rec = (HitRecord)0;

        if (t < tMin || t > tMax
            || a < p0.x || a > p1.x
            || b < p0.y || b > p1.y)
            return rec;


        float3 p = rotated.PointAtParameter(t);


        float3x3 rotCounterY = float3x3(
            cosTheta, 0, sinTheta,
            0, 1, 0,
            -sinTheta, 0, cosTheta);

        rec.hitPoint = mul(rotCounterY,p);
        normal = mul(rotCounterY, normal);

        rec.hitPoint += offset;
        rec.SetFaceNormal(rotated, normal);

        /* rec.hitPoint[0] = cosTheta * p[0] + sinTheta * p[2];
         rec.hitPoint[2] = -sinTheta * p[0] + cosTheta * p[2];
        
         rec.normal[0] = cosTheta * normal[0] + sinTheta * normal[2];
         rec.normal[2] = -sinTheta * normal[0] + cosTheta * normal[2];
        */

        rec.didHit = true;
        rec.dist = t;
        rec.material = material;
        return rec;
    }
};

#endif
