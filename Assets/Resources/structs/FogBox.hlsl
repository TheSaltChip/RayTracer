#ifndef FOG_BOX_HLSL
#define FOG_BOX_HLSL

#include "HitRecord.hlsl"
#include "BoxSide.hlsl"
#include "Ray.hlsl"

struct FogBox
{
    float3 min;
    float3 max;

    BoxSide sideX1;
    BoxSide sideX2;
    BoxSide sideY1;
    BoxSide sideY2;
    BoxSide sideZ1;
    BoxSide sideZ2;

    float density;
    float negInvDensity;

    Material material;

    bool Hit(Ray ray, const float minDist, const float maxDist, out HitRecord rec)
    {
        rec = (HitRecord)0;

        float rec1Dist = 0, rec2Dist = 0;

        if (!BoundaryHit(ray, (-1.#INF),1.#INF, rec1Dist))
            return false;

        if (!BoundaryHit(ray, (-1.#INF),1.#INF, rec2Dist))
            return false;

        if (rec1Dist < minDist) rec1Dist = minDist;
        if (rec2Dist < maxDist) rec2Dist = maxDist;

        if (rec1Dist >= rec2Dist)
            return false;

        if (rec1Dist < 0)
            rec1Dist = 0;

        const float rayLength = length(ray.dir);
        const float distanceInsideBoundary = (rec2Dist - rec1Dist) * rayLength;
        const float hitDistance = negInvDensity * log(Random());

        if (hitDistance > distanceInsideBoundary)
            return false;

        rec.didHit = true;
        rec.dist = rec1Dist + hitDistance / rayLength;
        rec.hitPoint = ray.PointAt(rec.dist);
        rec.normal = float3(1, 0, 0);
        rec.frontFace = true;
        rec.material = material;

        return true;
    }

    bool BoundaryHit(Ray ray, const float minDist, const float maxDist, out float dist)
    {
        bool hitAnything = false;
        dist = maxDist;

        HitRecord tempRecord = (HitRecord)0;

        if (sideX1.Hit(ray, minDist, dist, tempRecord))
        {
            dist = tempRecord.dist;
            hitAnything = true;
        }

        if (sideX2.Hit(ray, minDist, dist, tempRecord))
        {
            dist = tempRecord.dist;
            hitAnything = true;
        }

        if (sideY1.Hit(ray, minDist, dist, tempRecord))
        {
            dist = tempRecord.dist;
            hitAnything = true;
        }

        if (sideY2.Hit(ray, minDist, dist, tempRecord))
        {
            dist = tempRecord.dist;
            hitAnything = true;
        }

        if (sideZ1.Hit(ray, minDist, dist, tempRecord))
        {
            dist = tempRecord.dist;
            hitAnything = true;
        }

        if (sideZ2.Hit(ray, minDist, dist, tempRecord))
        {
            dist = tempRecord.dist;
            hitAnything = true;
        }

        return hitAnything;
    }
};

#endif
