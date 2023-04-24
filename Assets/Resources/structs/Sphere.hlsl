#ifndef SPHERE_HLSL
#define SPHERE_HLSL

#include "Ray.hlsl"
#include "HitRecord.hlsl"
#include "Assets/Resources/common/HelperFunctions.hlsl"

struct Sphere
{
    float3 center;
    float radius;
    Material mat;

    bool Hit(Ray r, const float minDist, const float maxDist, out HitRecord rec)
    {
        const float3 offsetRayOrigin = r.origin - center;

        const float a = SquareLength(r.dir);
        const float halfB = dot(offsetRayOrigin, r.dir);
        const float c = SquareLength(offsetRayOrigin) - radius * radius;

        const float discriminant = halfB * halfB - a * c;

        rec = (HitRecord)0;

        if (discriminant < 0)
        {
            return false;
        }

        const float sqrtDisc = sqrt(discriminant);
        const float invA = 1 / a;

        float root = (-halfB - sqrtDisc) * invA;

        if (root < minDist || root > maxDist)
        {
            root = (-halfB + sqrtDisc) * invA;
            if (root < minDist || root > maxDist)
            {
                return false;
            }
        }

        rec.didHit = true;
        rec.dist = root;
        rec.hitPoint = r.PointAt(rec.dist);
        rec.SetFaceNormal(r.dir, (rec.hitPoint - center) / radius);
        rec.material = mat;
        
        return true;
    }
};


#endif
