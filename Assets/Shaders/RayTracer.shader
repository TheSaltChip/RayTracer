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
            #pragma multi_compile __ USE_AI1_BVH_COLLISION_CALCULATION

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

            StructuredBuffer<Sphere> Spheres;
            int NumSpheres;

            StructuredBuffer<FogSphere> FogSpheres;
            int NumFogSpheres;

            StructuredBuffer<FogBox> FogBoxes;
            int NumFogBoxes;

            StructuredBuffer<Rect> Rects;
            int NumRects;

            StructuredBuffer<BoxInfo> BoxInfos;
            int NumBoxInfos;

            StructuredBuffer<BoxSide> BoxSides;
            int NumBoxSides;

            StructuredBuffer<Triangle> Triangles;
            StructuredBuffer<MeshInfo> AllMeshInfo;
            int NumMeshes;

            StructuredBuffer<BoundingBox> BoundingBoxes;
            int NumBoundingBoxes;

            StructuredBuffer<int> BoundingBoxIndices;

            #ifdef USE_1FE_BVH_COLLISION_CALCULATION


            #endif


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

            void CalculateSphereCollision(const Ray ray, const int index, inout HitRecord closestHitRecord,
                                          const float minDist,
                                          inout float closestSoFar)
            {
                Sphere s = Spheres[index];

                HitRecord tempRecord = (HitRecord)0;

                if (s.Hit(ray, minDist, closestSoFar, tempRecord))
                {
                    closestSoFar = tempRecord.dist;
                    closestHitRecord = tempRecord;
                }
            }

            void CalculateFogSphereCollision(const Ray ray, const int index, inout HitRecord closestHitRecord,
                                             const float minDist,
                                             inout float closestSoFar)
            {
                FogSphere fs = FogSpheres[index];

                HitRecord tempRecord = (HitRecord)0;

                if (fs.Hit(ray, minDist, closestSoFar, tempRecord))
                {
                    closestSoFar = tempRecord.dist;
                    closestHitRecord = tempRecord;
                }
            }

            void CalculateRectCollision(const Ray ray, const int index, inout HitRecord closestHitRecord,
                                        const float minDist,
                                        inout float closestSoFar)
            {
                Rect r = Rects[index];

                HitRecord tempRecord = (HitRecord)0;

                if (r.Hit(ray, minDist, closestSoFar, tempRecord))
                {
                    closestSoFar = tempRecord.dist;
                    closestHitRecord = tempRecord;
                }
            }

            void CalculateFogBoxCollision(Ray ray, const int index, inout HitRecord closestHitRecord,
                                          const float minDist,
                                          inout float closestSoFar)
            {
                FogBox fg = FogBoxes[index];

                if (!ray.Intersection(fg.min, fg.max, closestSoFar))
                    return;

                HitRecord tempRecord = (HitRecord)0;

                if (fg.Hit(ray, minDist, closestSoFar, tempRecord))
                {
                    closestSoFar = tempRecord.dist;
                    closestHitRecord = tempRecord;
                }
            }

            void CalculateBoxCollision(Ray ray, const int index, inout HitRecord closestHitRecord,
                                       const float minDist,
                                       inout float closestSoFar)
            {
                const BoxInfo boxInfo = BoxInfos[index];

                if (!ray.Intersection(boxInfo.min, boxInfo.max, closestSoFar))
                    return;

                HitRecord tempRecord = (HitRecord)0;

                for (int j = 0; j < 6; ++j)
                {
                    BoxSide side = BoxSides[boxInfo.firstBoxIndex + j];

                    if (side.Hit(ray, minDist, closestSoFar, tempRecord))
                    {
                        closestSoFar = tempRecord.dist;
                        closestHitRecord = tempRecord;
                        closestHitRecord.material = boxInfo.material;
                    }
                }
            }

            void CalculateMeshCollision(Ray ray, const int index, inout HitRecord closestHitRecord,
                                        const float minDist,
                                        inout float closestSoFar)
            {
                const MeshInfo meshInfo = AllMeshInfo[index];

                if (!ray.Intersection(meshInfo.boundsMin, meshInfo.boundsMax, closestSoFar))
                    return;

                HitRecord tempRecord = (HitRecord)0;

                for (uint j = 0; j < meshInfo.numTriangles; ++j)
                {
                    Triangle tri = Triangles[meshInfo.firstTriangleIndex + j];

                    if (tri.Hit(ray, minDist, closestSoFar, tempRecord))
                    {
                        closestSoFar = tempRecord.dist;
                        closestHitRecord = tempRecord;
                        closestHitRecord.material = meshInfo.material;
                    }
                }
            }

            #ifdef USE_AI1_BVH_COLLISION_CALCULATION
            HitRecord CalculateRayCollision(Ray ray)
            {
                float closestSoFar = FLOAT_MAX;
                const float minDist = MIN_DIST;
                HitRecord closestHitRecord = (HitRecord)0;

                BoundingBox box = BoundingBoxes[0];

                BoundingBox stack[32];

                int pointer = 0;

                while (true)
                {
                    if (box.typeofElement != ELEMENT_TYPES.aabb)
                    {
                        switch (box.typeofElement)
                        {
                        case ELEMENT_TYPES.sphere:
                            CalculateSphereCollision(ray, box.index, closestHitRecord, minDist, closestSoFar);
                            break;
                        case ELEMENT_TYPES.rect:
                            CalculateRectCollision(ray, box.index, closestHitRecord, minDist, closestSoFar);
                            break;
                        case ELEMENT_TYPES.box:
                            CalculateBoxCollision(ray, box.index, closestHitRecord, minDist, closestSoFar);
                            break;
                        case ELEMENT_TYPES.fogSphere:
                            CalculateFogSphereCollision(ray, box.index, closestHitRecord, minDist,
                                                        closestSoFar);
                            break;
                        case ELEMENT_TYPES.fogBox:
                            CalculateFogBoxCollision(ray, box.index, closestHitRecord, minDist, closestSoFar);
                            break;
                        case ELEMENT_TYPES.mesh:
                            CalculateMeshCollision(ray, box.index, closestHitRecord, minDist, closestSoFar);
                            break;
                        default:
                            break;
                        }

                        if (pointer == 0)
                            break;

                        box = (BoundingBox)stack[--pointer];

                        continue;
                    }

                    BoundingBox box1 = BoundingBoxes[box.index];
                    BoundingBox box2 = BoundingBoxes[box.index + 1];

                    float dist1 = box1.IntersectBox(ray, closestSoFar);
                    float dist2 = box2.IntersectBox(ray, closestSoFar);

                    if (dist1 > dist2)
                    {
                        float d = dist1;
                        dist1 = dist2;
                        dist2 = d;

                        BoundingBox b = box1;
                        box1 = box2;
                        box2 = b;
                    }

                    if (dist1 == 1e30f)
                    {
                        if (pointer == 0)
                            break;

                        box = stack[--pointer];
                    }
                    else
                    {
                        box = box1;
                        stack[pointer++] = box2;
                    }
                }

                return closestHitRecord;
            }
            #else
            HitRecord CalculateRayCollision(Ray ray)
            {
                float closestSoFar = FLOAT_MAX;
                const float minDist = MIN_DIST;
                HitRecord closestHitRecord = (HitRecord)0;

                const int maxNum =
                    max(NumSpheres,
                        max(NumFogSpheres,
                            max(NumMeshes,
                                max(NumRects,
                                    max(NumFogBoxes, NumBoxInfos)))));

                for (int i = 0; i < maxNum; ++i)
                {
                    if (i < NumSpheres)
                        CalculateSphereCollision(ray, i, closestHitRecord, minDist, closestSoFar);

                    if (i < NumFogSpheres)
                        CalculateFogSphereCollision(ray, i, closestHitRecord, minDist, closestSoFar);

                    if (i < NumRects)
                        CalculateRectCollision(ray, i, closestHitRecord, minDist, closestSoFar);

                    if (i < NumBoxInfos)
                        CalculateBoxCollision(ray, i, closestHitRecord, minDist, closestSoFar);

                    if (i < NumFogBoxes)
                        CalculateFogBoxCollision(ray, i, closestHitRecord, minDist, closestSoFar);

                    if (i < NumMeshes)
                        CalculateMeshCollision(ray, i, closestHitRecord, minDist, closestSoFar);
                }

                return closestHitRecord;
            }
            #endif


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