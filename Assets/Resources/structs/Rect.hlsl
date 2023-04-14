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
        Ray translated = (Ray) 0;
        translated.MakeRay(r.origin - offset, r.dir);

        float3 origin = translated.origin;
        float3 direction = translated.dir;

        // @formatter:off
        float3x3 rotX = float3x3(
            1,       0,             0,
            0,  cosRotation.x, sinRotation.x,
            0, -sinRotation.x, cosRotation.x);

        float3x3 rotY = float3x3(
            cosRotation.y, 0, -sinRotation.y,
                  0,       1,       0,
            sinRotation.y, 0, cosRotation.y);

        float3x3 rotZ = float3x3(
             cosRotation.z, sinRotation.z, 0,
            -sinRotation.z, cosRotation.z, 0,
                   0,             0,       1);
        // @formatter:on

        //origin = mul(rotX, origin);
        origin = mul(rotY, origin);
        //origin = mul(rotZ, origin);

        //direction = mul(rotX, direction);
        direction = mul(rotY, direction);
        //direction = mul(rotZ, direction);

        Ray rotated = (Ray) 0;
        rotated.MakeRay(origin, direction);

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

        // @formatter:off
        float3x3 rotCounterX = float3x3(
            1,        0,            0,
            0,  cosRotation.x, -sinRotation.x,
            0, sinRotation.x, cosRotation.x);

        float3x3 rotCounterY = float3x3(
            cosRotation.y, 0, sinRotation.y,
                  0,       1,       0,
            -sinRotation.y, 0, cosRotation.y);

        float3x3 rotCounterZ = float3x3(
             cosRotation.z, -sinRotation.z, 0,
            sinRotation.z, cosRotation.z, 0,
                   0,             0,       1);
        // @formatter:on

        //rec.hitPoint = mul(rotCounterZ, );
        rec.hitPoint = mul(rotCounterY, p);
        //rec.hitPoint = mul(rotCounterX, rec.hitPoint);


        //normal = mul(rotCounterZ, normal);
        normal = mul(rotCounterY, normal);
        //normal = mul(rotCounterX, normal);


        rec.hitPoint += offset;
        rec.SetFaceNormal(translated.dir, normal);


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
