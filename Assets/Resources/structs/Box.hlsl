#ifndef BOX_HLSL
#define BOX_HLSL

#include "HitRecord.hlsl"
#include "Rect.hlsl"

#define ITERATION(hitRec,side,ray,minDist,closestSoFar) const HitRecord hitRec = side.Hit(ray, minDist, closestSoFar);if (hitRec.didHit){ closestSoFar = hitRec.dist;hitRecord = hitRec;}

struct Box
{
    float3 min;
    float3 max;

    Rect sideX1;
    Rect sideX2;
    Rect sideY1;
    Rect sideY2;
    Rect sideZ1;
    Rect sideZ2;

    HitRecord Hit(Ray ray, const float minDist, const float maxDist)
    {
        HitRecord hitRecord = (HitRecord)0;
        float closestSoFar = maxDist;

        const HitRecord recordX1 = sideX1.Hit(ray, minDist, closestSoFar);
        if (recordX1.didHit)
        {
            closestSoFar = recordX1.dist;
            hitRecord = recordX1;
        }

        const HitRecord recordX2 = sideX2.Hit(ray, minDist, closestSoFar);
        if (recordX2.didHit)
        {
            closestSoFar = recordX2.dist;
            hitRecord = recordX2;
        }

        const HitRecord recordY1 = sideY1.Hit(ray, minDist, closestSoFar);
        if (recordY1.didHit)
        {
            closestSoFar = recordY1.dist;
            hitRecord = recordY1;
        }

        const HitRecord recordY2 = sideY2.Hit(ray, minDist, closestSoFar);
        if (recordY2.didHit)
        {
            closestSoFar = recordY2.dist;
            hitRecord = recordY2;
        }

        const HitRecord recordZ1 = sideZ1.Hit(ray, minDist, closestSoFar);
        if (recordZ1.didHit)
        {
            closestSoFar = recordZ1.dist;
            hitRecord = recordZ1;
        }

        const HitRecord recordZ2 = sideZ2.Hit(ray, minDist, closestSoFar);
        if (recordZ2.didHit)
        {
            closestSoFar = recordZ2.dist;
            hitRecord = recordZ2;
        }

        return hitRecord;
    }
};

#endif
