#ifndef TRIANGLE_HLSL
#define TRIANGLE_HLSL

#include "Ray.hlsl"
#include "HitRecord.hlsl"

struct Triangle
{
    float3 posA, posB, posC;
    float3 normalA, normalB, normalC;

    bool Hit(const Ray ray, const float minDist, float maxDist, out HitRecord rec)
    {
        rec = (HitRecord)0;
        
        const float3 edgeAB = posB - posA;
        const float3 edgeAC = posC - posA;
        const float3 normalVector = cross(edgeAB, edgeAC);
        const float3 ao = ray.origin - posA;
        const float3 dao = cross(ao, ray.dir);

        const float determinant = -dot(ray.dir, normalVector);

        if (determinant < 1E-9)
        {
            return false;
        }

        const float invDeterminant = 1 / determinant;

        //Calculate dist to triangle & barycentric coordinates of intersection point
        const float dist = dot(ao, normalVector) * invDeterminant;

        if (dist < minDist
            || dist > maxDist
            || dist < 0 )
        {
            return false; 
        }

        const float baryEps = 1E-9;

        const float u = dot(edgeAC, dao) * invDeterminant;
        if (u < -baryEps || u > 1.0 + baryEps) return false;

        const float v = -dot(edgeAB, dao) * invDeterminant;
        if (v < -baryEps || (u + v) > 1.0 + baryEps) return false;

        const float w = 1 - u - v;


        if (w < 0)
            return false;

        rec.didHit = true;
        rec.hitPoint = ray.origin + ray.dir * dist;
        rec.SetFaceNormal(ray.dir, normalize(normalA * w + normalB * u + normalC * v));
        rec.dist = dist;

        return true;
    }

    float3 c;
};

#endif
