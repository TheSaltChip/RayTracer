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

    HitRecord Hit(Ray r, const float tMin, const float tMax)
    {
        const float3 offsetRayOrigin = r.origin - center;

        const float a = SquareLength(r.dir);
        const float halfB = dot(offsetRayOrigin, r.dir);
        const float c = SquareLength(offsetRayOrigin) - radius * radius;

        const float discriminant = halfB * halfB - a * c;

        HitRecord rec = (HitRecord)0;

        if (discriminant < 0)
        {
            return rec;
        }

        const float sqrtDisc = sqrt(discriminant);
        const float invA = 1 / a;

        float root = (-halfB - sqrtDisc) * invA;

        if (root < tMin || root > tMax)
        {
            root = (-halfB + sqrtDisc) * invA;
            if (root < tMin || root > tMax)
            {
                return rec;
            }
        }

        rec.didHit = true;
        rec.dist = root;
        rec.hitPoint = r.PointAtParameter(rec.dist);
        rec.SetFaceNormal(r, (rec.hitPoint - center) / radius);
        rec.material = mat;
        return rec;
    }
};

Sphere MakeSphere(const float3 center, const float radius)
{
    Sphere s;

    s.center = center;
    s.radius = radius;
    s.mat = MakeLambertianMaterial(float4(0, 0, 0, 1));;

    return s;
}

Sphere MakeSphere(const float3 center, const float radius, const Material mat)
{
    Sphere s;

    s.center = center;
    s.radius = radius;
    s.mat = mat;

    return s;
}


#endif
