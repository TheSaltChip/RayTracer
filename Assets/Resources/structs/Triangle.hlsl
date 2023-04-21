#ifndef TRIANGLE_HLSL
#define TRIANGLE_HLSL

#include "Ray.hlsl"
#include "HitRecord.hlsl"

struct Triangle
{
    float3 posA, posB, posC;
    float3 normalA, normalB, normalC;

    HitRecord Hit(const Ray ray, const float minDist, float maxDist)
    {
        const float3 edgeAB = posB - posA;
        const float3 edgeAC = posC - posA;
        const float3 normalVector = cross(edgeAB, edgeAC);
        const float3 ao = ray.origin - posA;
        const float3 dao = cross(ao, ray.dir);

        const float determinant = -dot(ray.dir, normalVector);
        const float invDeterminant = 1 / determinant;

        //Calculate dist to triangle & barycentric coordinates of intersection point
        const float dist = dot(ao, normalVector) * invDeterminant;

        const float u = dot(edgeAC, dao) * invDeterminant;
        const float v = -dot(edgeAB, dao) * invDeterminant;
        const float w = 1 - u - v;

        HitRecord hitRecord = (HitRecord)0;

        if (dist < minDist || dist > maxDist) return hitRecord;

        hitRecord.didHit = determinant >= 1E-6 && dist >= 0 && u >= 0 && v >= 0 && w >= 0;
        hitRecord.hitPoint = ray.origin + ray.dir * dist;
        hitRecord.SetFaceNormal(ray.dir, normalize(normalA * w + normalB * u + normalC * v));
        hitRecord.dist = dist;

        return hitRecord;
    }
};

#endif
