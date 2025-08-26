Shader "Unlit/LagueRayTracer"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"
            #include "Assets/Resources/common/Random.hlsl"
            #include "Assets/Resources/structs/Ray.hlsl"
            #include "Assets/Resources/structs/Triangle.hlsl"

            const static float FLOAT_MAX = 1.#INF;
            const static float MIN_DIST = 1e-4F;

            float3 ViewParams;
            float4x4 CamLocalToWorldMatrix;

            int MaxBounceCount;
            int NumRaysPerPixel;
            uint Frame;
            float DivergeStrength;

            bool EnvironmentEnabled;
            float3 SkyColorHorizon;
            float3 SkyColorZenith;
            float SunFocus;
            float SunIntensity;
            float3 GroundColor;

            struct BVHNode
            {
                float3 aabbMin, aabbMax;
                int leftFirst, triCount;
                float4x4 invTransform;
                Material mat;
            };

            struct TLASNode
            {
                float3 min;
                float3 max;
                int leftRight;
                int BLAS;
            };

            StructuredBuffer<BVHNode> BvhNodes;
            StructuredBuffer<Triangle> Triangles;
            StructuredBuffer<int> TriangleIndices;

            struct Appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct V2F
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            V2F Vert(const Appdata v)
            {
                V2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            struct Model
            {
                int nodeOffset;
                int triOffset;
                float4x4 worldToLocalMatrix;
                float4x4 localToWorldMatrix;
                Material material;
            };

            float IntersectNode(const Ray ray, const float3 boxMin, float3 boxMax)
            {
                float3 tMin = (boxMin - ray.origin) * ray.invDir;
                float3 tMax = (boxMax - ray.origin) * ray.invDir;
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                float tNear = max(max(t1.x, t1.y), t1.z);
                float tFar = min(min(t2.x, t2.y), t2.z);

                bool hit = tFar >= tNear && tFar > 0;
                float dst = hit ? tNear > 0 ? tNear : 0 : 1.#INF;
                return dst;
            };

            /*float IntersectNode(const Ray ray, const float t, const BVHNode node)
            {
                float tMin = 0.0, tMax = t;

                UNITY_UNROLL
                for (int d = 0; d < 3; ++d)
                {
                    const float t1 = (node.aabbMin[d] - ray.origin[d]) * ray.invDir[d];
                    const float t2 = (node.aabbMax[d] - ray.origin[d]) * ray.invDir[d];

                    tMin = min(max(t1, tMin), max(t2, tMin));
                    tMax = max(min(t1, tMax), min(t2, tMax));
                }

                if (tMax >= tMin && tMin < t && tMax > 0)
                {
                    return tMin;
                }

                return 1e30f;
            }*/

            HitRecord RayTriangle(Ray ray, Triangle tri)
            {
                float3 edgeAB = tri.posB - tri.posA;
                float3 edgeAC = tri.posC - tri.posA;
                float3 normalVector = cross(edgeAB, edgeAC);
                float3 ao = ray.origin - tri.posA;
                float3 dao = cross(ao, ray.dir);

                float determinant = -dot(ray.dir, normalVector);
                float invDet = 1 / determinant;

                // Calculate dst to triangle & barycentric coordinates of intersection point
                float dst = dot(ao, normalVector) * invDet;
                float u = dot(edgeAC, dao) * invDet;
                float v = -dot(edgeAB, dao) * invDet;
                float w = 1 - u - v;

                // Initialize hit info
                HitRecord hitInfo = (HitRecord)0;
                hitInfo.didHit = determinant >= 1E-8 && dst >= 0 && u >= 0 && v >= 0 && w >= 0;
                hitInfo.hitPoint = ray.origin + ray.dir * dst;
                hitInfo.normal = normalize(tri.normalA * w + tri.normalB * u + tri.normalC * v);
                hitInfo.dist = dst;
                return hitInfo;
            }

            // Thanks to https://tavianator.com/2011/ray_box.html
            float RayBoundingBoxDst(Ray ray, float3 boxMin, float3 boxMax)
            {
                float3 tMin = (boxMin - ray.origin) * ray.invDir;
                float3 tMax = (boxMax - ray.origin) * ray.invDir;
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                float tNear = max(max(t1.x, t1.y), t1.z);
                float tFar = min(min(t2.x, t2.y), t2.z);

                bool hit = tFar >= tNear && tFar > 0;
                float dst = hit ? tNear > 0 ? tNear : 0 : 1.#INF;
                return dst;
            };


            HitRecord RayTriangleBVH(inout Ray ray, float rayLength, int nodeOffset, int triOffset, inout int2 stats)
            {
                HitRecord result;
                result.dist = rayLength;

                int stack[32];
                int stackIndex = 0;
                stack[stackIndex++] = nodeOffset + 0;

                while (stackIndex > 0)
                {
                    BVHNode node = BvhNodes[stack[--stackIndex]];
                    bool isLeaf = node.triCount > 0;

                    if (isLeaf)
                    {
                        for (int i = 0; i < node.triCount; i++)
                        {
                            Triangle tri = Triangles[triOffset + node.leftFirst + i];
                            HitRecord triHitInfo = RayTriangle(ray, tri);
                            stats[0]++; // count triangle intersection tests

                            if (triHitInfo.didHit && triHitInfo.dist < result.dist)
                            {
                                result = triHitInfo;
                                result.triIndex = node.leftFirst + i;
                            }
                        }
                    }
                    else
                    {
                        int childIndexA = nodeOffset + node.leftFirst + 0;
                        int childIndexB = nodeOffset + node.leftFirst + 1;
                        BVHNode childA = BvhNodes[childIndexA];
                        BVHNode childB = BvhNodes[childIndexB];

                        float dstA = RayBoundingBoxDst(ray, childA.aabbMin, childA.aabbMax);
                        float dstB = RayBoundingBoxDst(ray, childB.aabbMin, childB.aabbMax);
                        stats[1] += 2; // count bounding box intersection tests

                        // We want to look at closest child node first, so push it last
                        bool isNearestA = dstA <= dstB;
                        float dstNear = isNearestA ? dstA : dstB;
                        float dstFar = isNearestA ? dstB : dstA;
                        int childIndexNear = isNearestA ? childIndexA : childIndexB;
                        int childIndexFar = isNearestA ? childIndexB : childIndexA;

                        if (dstFar < result.dist) stack[stackIndex++] = childIndexFar;
                        if (dstNear < result.dist) stack[stackIndex++] = childIndexNear;
                    }
                }


                return result;
            }

            HitRecord CalculateRayCollision(Ray worldRay, out int2 stats)
            {
                HitRecord result = (HitRecord)0;
                result.dist = 1.#INF;
                Ray localRay;

                /*for (int i = 0; i < modelCount; i++)
                {
                    Model model = ModelInfo[i];*/
                // Transform ray into model's local coordinate space
                localRay.origin = mul(BvhNodes[0].invTransform, float4(worldRay.origin, 1));
                localRay.dir = mul(BvhNodes[0].invTransform, float4(worldRay.dir, 0));
                localRay.invDir = 1 / localRay.dir;

                // Traverse bvh to find closest triangle intersection with current model
                HitRecord hit = RayTriangleBVH(worldRay, result.dist, 0, 0, stats);

                // Record closest hit
                if (hit.dist < result.dist)
                {
                    result.didHit = true;
                    result.dist = hit.dist;
                    result.normal = normalize(mul(BvhNodes[0].invTransform, float4(hit.normal, 0)));
                    result.hitPoint = worldRay.origin + worldRay.dir * hit.dist;
                    result.material = BvhNodes[0].mat;
                }
                // }

                return result;
            }

            float3 GetEnvironmentLight(Ray ray)
            {
                if (!EnvironmentEnabled) return 0;

                const float skyGradientT = pow(smoothstep(0, 0.4, ray.dir.y), 0.35);
                const float3 skyGradient = lerp(SkyColorHorizon, SkyColorZenith, skyGradientT);

                const float sun = pow(max(0, dot(ray.dir, _WorldSpaceLightPos0.xyz)), SunFocus) * SunIntensity;

                const float groundToSkyT = smoothstep(-0.01, 0, ray.dir.y);

                return lerp(GroundColor, skyGradient, groundToSkyT) + sun * (groundToSkyT >= 1);
            }

            float3 Trace(Ray ray)
            {
                float3 incomingLight = 0;
                float3 rayColor = 1;
                int2 stats;

                for (int i = 0; i < MaxBounceCount + 1; ++i)
                {
                    const HitRecord hitRecord = CalculateRayCollision(ray, stats);

                    if (!hitRecord.didHit)
                    {
                        incomingLight += GetEnvironmentLight(ray) * rayColor;
                        break;
                    }

                    const Material material = hitRecord.material;

                    const float3 emittedLight = material.emissionColor.xyz * material.emissionStrength;

                    incomingLight += emittedLight * rayColor;

                    ray.Scatter(hitRecord, rayColor);

                    const float p = max(rayColor.r, max(rayColor.g, rayColor.b));

                    if (Random() >= p)
                        break;

                    rayColor *= 1.0f / p;
                }

                return incomingLight;
            }

            float4 Frag(const V2F i) : SV_Target
            {
                const uint2 numPixels = _ScreenParams.xy;
                const uint2 pixelCoord = i.uv * numPixels;

                const uint pixelIndex = pixelCoord.x + pixelCoord.y * numPixels.x;
                SetSeed(pixelIndex + Frame * 547103u);

                const float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                const float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1.0));
                const float3 camRight = CamLocalToWorldMatrix._m00_m10_m20;
                const float3 camUp = CamLocalToWorldMatrix._m01_m11_m21;

                const float invNumPixelX = 1.0 / (numPixels.x * 1.0);

                float3 totalIncomingLight = 0;
                Ray ray = (Ray)0;

                for (int j = 0; j < NumRaysPerPixel; ++j)
                {
                    const float2 jitter = RandomPointInCircle() * DivergeStrength * invNumPixelX;
                    const float3 jitteredViewPoint = viewPoint + camRight * jitter.x + camUp * jitter.y;

                    ray.MakeRay(_WorldSpaceCameraPos, normalize(jitteredViewPoint - _WorldSpaceCameraPos));

                    totalIncomingLight += Trace(ray);
                }

                const float3 pixelCol = totalIncomingLight / NumRaysPerPixel;

                return float4(pixelCol, 1);
            }
            ENDHLSL
        }
    }
}