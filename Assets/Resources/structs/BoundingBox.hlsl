#ifndef BOUNDING_BOX_HLSL
#define BOUNDING_BOX_HLSL

struct ElementTypes
{
    uint sphere;
    uint rect;
    uint box;
    uint fogSphere;
    uint fogBox;
    uint mesh;
    uint aabb;
};

static const ElementTypes ELEMENT_TYPES = {0, 1, 2, 3, 4, 5,6};

struct BoundingBox
{
    float3 minPos;
    float3 maxPos;
    int typeofElement;
    int index;
    
    float IntersectBox( const Ray ray, const float t)
    {
        float tMin = 0.0, tMax = t;

        UNITY_UNROLL
        for (int d = 0; d < 3; ++d)
        {
            const float t1 = (minPos[d] - ray.origin[d]) * ray.invDir[d];
            const float t2 = (maxPos[d] - ray.origin[d]) * ray.invDir[d];

            tMin = min(max(t1, tMin), max(t2, tMin));
            tMax = max(min(t1, tMax), min(t2, tMax));
        }

        if(tMax >= tMin && tMin < t && tMax >0)
        {
            return tMin;
        }

        return 1e30f;
    }
    
};




#endif
