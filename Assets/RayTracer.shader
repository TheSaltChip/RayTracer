Shader "Unlit/RayTracer"
{
    SubShader
    {

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"
            #include "Assets/Resources/structs/Ray.hlsl"
            #include "Assets/Resources/structs/Rect.hlsl"
            #include "Assets/Resources/structs/Sphere.hlsl"
            #include "Assets/Resources/structs/Triangle.hlsl"
            #include "Assets/Resources/common/Random.hlsl"
            #include "Assets/Resources/common/ScatterFunctions.hlsl"

            const static float FLOAT_MAX = 3.402823466e+38F;

            float3 ViewParams;
            float4x4 CamLocalToWorldMatrix;

            int MaxBounceCount;
            int NumRaysPerPixel;
            uint Frame;
            float DivergeStrength;

            bool EnvironmentEnabled;
            float3 SkyColorHorizon;
            float3 SkyColorZenith;
            float3 SunLightDirection;
            float SunFocus;
            float SunIntensity;
            float3 GroundColor;

            struct MeshInfo
            {
                uint firstTriangleIndex;
                uint numTriangles;
                float3 boundsMin;
                float3 boundsMax;
                Material material;
            };


            StructuredBuffer<Sphere> Spheres;
            int NumSpheres;

            StructuredBuffer<Rect> Rects;
            int NumRects;

            StructuredBuffer<Triangle> Triangles;
            StructuredBuffer<MeshInfo> AllMeshInfo;
            int NumMeshes;

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

            HitRecord CalculateRayCollision(const Ray ray)
            {
                float closestSoFar = FLOAT_MAX;
                HitRecord closestHitRecord = (HitRecord)0;

                for (int i = 0; i < NumSpheres; ++i)
                {
                    Sphere s = Spheres[i];
                    const HitRecord tempRecord = s.Hit(ray, 0.0001, closestSoFar);

                    if (tempRecord.didHit)
                    {
                        closestSoFar = tempRecord.dist;
                        closestHitRecord = tempRecord;
                    }
                }

                for (int i = 0; i < NumRects; ++i)
                {
                    Rect r = Rects[i];
                    const HitRecord tempRecord = r.Hit(ray, 0.0001, closestSoFar);

                    if (tempRecord.didHit)
                    {
                        closestSoFar = tempRecord.dist;
                        closestHitRecord = tempRecord;
                    }
                }

                for (int meshIndex = 0; meshIndex < NumMeshes; ++meshIndex)
                {
                    const MeshInfo meshInfo = AllMeshInfo[meshIndex];
                    if (!Intersection(meshInfo.boundsMin, meshInfo.boundsMax, ray, closestSoFar))
                    {
                        continue;
                    }

                    for (int i = 0; i < meshInfo.numTriangles; ++i)
                    {
                        const int triIndex = meshInfo.firstTriangleIndex + i;
                        Triangle tri = Triangles[triIndex];
                        const HitRecord tempRecord = tri.Hit(ray, 0.0001, closestSoFar);

                        if (tempRecord.didHit)
                        {
                            closestSoFar = tempRecord.dist;
                            closestHitRecord = tempRecord;
                            closestHitRecord.material = meshInfo.material;
                        }
                    }
                }

                return closestHitRecord;
            }

            float3 GetEnvironmentLight(Ray ray)
            {
                const float skyGradientT = pow(smoothstep(0, 0.4, ray.dir.y), 0.35);
                const float3 skyGradient = lerp(SkyColorHorizon, SkyColorZenith, skyGradientT);
                // Doesnt work yet
                //const float sun = pow(max(0, dot(ray.direction, -SunLightDirection)), SunFocus) * SunIntensity;

                const float groundToSkyT = smoothstep(-0.01, 0, ray.dir.y);
                //const float sunMask = groundToSkyT >= 1;
                return lerp(GroundColor, skyGradient, groundToSkyT); // + sun * sunMask;
            }

            float3 Trace(Ray ray)
            {
                float3 incomingLight = (0);
                float3 rayColor = (1);

                for (int i = 0; i < MaxBounceCount + 1; ++i)
                {
                    const HitRecord hitRecord = CalculateRayCollision(ray);

                    if (!hitRecord.didHit)
                    {
                        if (EnvironmentEnabled)
                            incomingLight += GetEnvironmentLight(ray) * rayColor;
                        break;
                    }

                    const Material material = hitRecord.material;

                    const float3 emittedLight = material.emissionColor.xyz * material.emissionStrength;

                    incomingLight += emittedLight * rayColor;

                    Scatter(ray, hitRecord, rayColor, ray);
                }

                return incomingLight;
            }

            float4 Frag(const V2F i) : SV_Target
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;
                const uint pixelIndex = pixelCoord.x + pixelCoord.y * numPixels.x;
                SetSeed(pixelIndex + Frame * 547103u);

                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                const float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));
                const float3 camRight = CamLocalToWorldMatrix._m00_m10_m20;
                const float3 camUp = CamLocalToWorldMatrix._m01_m11_m21;

                const float invNumPixelX = 1.0f / (numPixels.x * 1.f);

                float3 totalIncomingLight = 0;

                for (int j = 0; j < NumRaysPerPixel; ++j)
                {
                    float2 jitter = RandomPointInCircle() * DivergeStrength * invNumPixelX;
                    const float3 jitteredViewPoint = viewPoint + camRight * jitter.x + camUp * jitter.y;

                    const Ray ray = MakeRay(_WorldSpaceCameraPos, normalize(jitteredViewPoint - _WorldSpaceCameraPos));

                    totalIncomingLight += Trace(ray);
                }

                float3 pixelCol = totalIncomingLight / NumRaysPerPixel;
                return float4((pixelCol), 1);
                //return float4(RandomPointInCircle(), 0, 1);
            }
            ENDHLSL
        }
    }
}