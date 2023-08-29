Shader "Unlit/RayTracer"
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
            #include "Assets/Resources/structs/BoxInfo.hlsl"
            #include "Assets/Resources/structs/BoxSide.hlsl"
            #include "Assets/Resources/structs/BoundingBox.hlsl"
            #include "Assets/Resources/structs/FogBox.hlsl"
            #include "Assets/Resources/structs/FogSphere.hlsl"
            #include "Assets/Resources/structs/MeshInfo.hlsl"
            #include "Assets/Resources/structs/Ray.hlsl"
            #include "Assets/Resources/structs/Rect.hlsl"
            #include "Assets/Resources/structs/Sphere.hlsl"
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

            float IntersectNode(const Ray ray, const float t, const BVHNode node)
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
            }


            HitRecord CalculateRayCollision(Ray ray)
            {
                float closestSoFar = FLOAT_MAX;
                const float minDist = MIN_DIST;
                HitRecord closestHitRecord = (HitRecord)0;

                BVHNode node = (BVHNode)BvhNodes[0];
                BVHNode stack[32];

                uint stackPtr = 0;
                HitRecord tempRecord = (HitRecord)0;

                while (true)
                {
                    if (node.triCount > 0)
                    {
                        for (uint i = 0; i < node.triCount; i++)
                        {
                            uint instPrim = TriangleIndices[node.leftFirst + i];
                            Triangle tri = Triangles[instPrim];
                            if (tri.Hit(ray, minDist, closestSoFar, tempRecord))
                            {
                                closestSoFar = tempRecord.dist;
                                closestHitRecord = tempRecord;
                                closestHitRecord.material = node.mat;
                            }
                        }
                        if (stackPtr == 0) break;

                        node = (BVHNode)stack[--stackPtr];
                        continue;
                    }

                    BVHNode child1 = BvhNodes[node.leftFirst];
                    BVHNode child2 = BvhNodes[node.leftFirst + 1];

                    float dist1 = IntersectNode(ray, closestSoFar, child1);
                    float dist2 = IntersectNode(ray, closestSoFar, child2);

                    if (dist1 > dist2)
                    {
                        float d = dist1;
                        dist1 = dist2;
                        dist2 = d;

                        BVHNode c = child1;
                        child1 = child2;
                        child2 = c;
                    }

                    if (dist1 == 1e30f)
                    {
                        if (stackPtr == 0) break;

                        node = stack[--stackPtr];
                    }
                    else
                    {
                        node = child1;
                        if (dist2 != 1e30f)
                        {
                            stack[stackPtr++] = child2;
                        }
                    }
                }

                return closestHitRecord;
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

                for (int i = 0; i < MaxBounceCount + 1; ++i)
                {
                    const HitRecord hitRecord = CalculateRayCollision(ray);

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