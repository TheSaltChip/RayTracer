#ifndef FOG_SPHERE_HLSL
#define FOG_SPHERE_HLSL

#include "Ray.hlsl"
#include "HitRecord.hlsl"
#include "Sphere.hlsl"

struct FogSphere
{
    float3 center;
    float radius;

    float density;
    float negInvDensity;

    Material material;

    bool Hit(Ray r, const float minDist, const float maxDist, out HitRecord rec)
    {
        rec = (HitRecord)0;

        float rec1Dist = 0, rec2Dist = 0;

        if (!BoundaryHit(r, (-1.#INF),1.#INF, rec1Dist))
            return false;

        if (!BoundaryHit(r, (-1.#INF),1.#INF, rec2Dist))
            return false;

        if (rec1Dist < minDist) rec1Dist = minDist;
        if (rec2Dist < maxDist) rec2Dist = maxDist;

        if (rec1Dist >= rec2Dist)
            return false;

        if (rec1Dist < 0)
            rec1Dist = 0;

        const float rayLength = length(r.dir);
        const float distanceInsideBoundary = (rec2Dist - rec1Dist) * rayLength;
        const float hitDistance = negInvDensity * log(Random());

        if (hitDistance > distanceInsideBoundary)
            return false;

        rec.didHit = true;
        rec.dist = rec1Dist + hitDistance / rayLength;
        rec.hitPoint = r.PointAt(rec.dist);
        rec.normal = float3(1, 0, 0);
        rec.frontFace = true;
        rec.material = material;

        return true;
    }

    bool BoundaryHit(const Ray r, const float minDist, const float maxDist, out float dist)
    {
        const float3 offsetRayOrigin = r.origin - center;

        const float a = SquareLength(r.dir);
        const float halfB = dot(offsetRayOrigin, r.dir);
        const float c = SquareLength(offsetRayOrigin) - radius * radius;

        const float discriminant = halfB * halfB - a * c;

        if (discriminant < 0)
            return false;

        const float sqrtDisc = sqrt(discriminant);
        const float invA = 1 / a;

        float root = (-halfB - sqrtDisc) * invA;

        if (root < minDist || root > maxDist)
        {
            root = (-halfB + sqrtDisc) * invA;
            if (root < minDist || root > maxDist)
                return false;
        }

        dist = root;

        return true;
    }
};

#endif
