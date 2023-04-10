Shader "Unlit/CombineTextures"
{
    SubShader
    {

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            uint NumRenderedFrames;

            UNITY_DECLARE_TEX2D(MainTex);
            UNITY_DECLARE_TEX2D_NOSAMPLER(MainOldTex);

            SamplerState MyPointClampSampler;

            v2f Vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 Frag(v2f i) : SV_Target
            {
                // sample the texture
                const float4 oldRender = UNITY_SAMPLE_TEX2D_SAMPLER(MainOldTex, MainTex, i.uv);
                const float4 newRender = UNITY_SAMPLE_TEX2D(MainTex, i.uv);

                const float weight = 1.0 / (NumRenderedFrames + 1.0);

                return oldRender * (1 - weight) + newRender * weight;
            }
            ENDHLSL
        }
    }
}