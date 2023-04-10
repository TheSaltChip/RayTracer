#ifndef RAY_HLSL
#define RAY_HLSL

struct Ray
{
    float3 origin;
    float3 dir;
    float3 invDir;

    float3 PointAtParameter(const float t)
    {
        return origin + t * dir;
    }
};

Ray MakeRay(const float3 origin, const float3 direction)
{
    Ray ray;

    ray.origin = origin;
    ray.dir = direction;
    ray.invDir = 1 / direction;

    return ray;
}

static float Min(float x, float y)
{
    return x < y ? x : y;
}

static float Max(float x, float y)
{
    return x > y ? x : y;
}

// Branchless Ray/Bounding box intersection
// Rays which just touch a corner, edge, or face of the bounding box will be considered non-intersecting
// credit: https://tavianator.com/2022/ray_box_boundary.html
bool Intersection(float3 boxMin, float3 boxMax, Ray ray, float t)
{
    float tmin = 0.0, tmax = t;

    for (int d = 0; d < 3; ++d)
    {
        float t1 = (boxMin[d] - ray.origin[d]) * ray.invDir[d];
        float t2 = (boxMax[d] - ray.origin[d]) * ray.invDir[d];

        tmin = Min(Max(t1, tmin), Max(t2, tmin));
        tmax = Max(Min(t1, tmax), Min(t2, tmax));
    }

    return tmin <= tmax;
}

#endif
