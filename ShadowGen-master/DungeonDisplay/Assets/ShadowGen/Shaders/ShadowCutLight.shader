Shader "Unlit/ShadowCutLight"
{
    Properties
    {
         _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ColorMask RGB

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            float signS(float2 p1, float2 p2, float2 p3)
            {
                return ((p1.r - p3.r) * (p2.g - p3.g)) - ((p2.r - p3.r) * (p1.g - p3.g));
            }

            bool PointInTriangleSmooth(float2 pt, float2 v1, float2 v2, float2 v3)
            {
                float d1, d2, d3;
                bool has_neg, has_pos;

                d1 = signS(pt, v1, v2);
                d2 = signS(pt, v2, v3);
                d3 = signS(pt, v3, v1);

                has_neg = (d1 < -0.005) || (d2 < -0.005) || (d3 < -0.005);
                has_pos = (d1 > 0.005) || (d2 > 0.005) || (d3 > 0.005);

                return !(has_neg && has_pos);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 position : WORLDPOSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _ShadowArrayA[666];
            float4 _ShadowArrayB[666];

            float4 _blockingArrayA[666];
            float4 _blockingArrayB[666];
            float4 _blockingArrayC[666];

            v2f vert (appdata v)
            {
                v2f o;
                o.position = v.vertex.rgb;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                float visionValue = float(1);

                float2 loc = mul((float3x3)unity_ObjectToWorld, i.position).rg;
                int j = 0;
                for (j = 0; j < 664 && _ShadowArrayA[j].r < 10000000; j = j + 3)
                {
                    float inTriangle = PointInTriangleSmooth(loc, _ShadowArrayA[j].rg, _ShadowArrayA[j + 1].rg, _ShadowArrayA[j + 2].rg);
                    inTriangle = abs(1 - inTriangle);
                    visionValue = visionValue * inTriangle;
                }

                for (j = 0; j < 664 && _ShadowArrayB[j].r < 10000000; j = j + 3)
                {
                    float inTriangle = PointInTriangleSmooth(loc, _ShadowArrayB[j].rg, _ShadowArrayB[j + 1].rg, _ShadowArrayB[j + 2].rg);
                    inTriangle = abs(1 - inTriangle);
                    visionValue = visionValue * inTriangle;
                }

                for (j = 0; j < 664 && _blockingArrayA[j].r < 10000000; j = j + 3)
                {
                    float inTriangle = PointInTriangleSmooth(loc, _blockingArrayA[j].rg, _blockingArrayA[j + 1].rg, _blockingArrayA[j + 2].rg);
                    visionValue = step(0.5, visionValue + inTriangle);
                }

                for (j = 0; j < 664 && _blockingArrayB[j].r < 10000000; j = j + 3)
                {
                    float inTriangle = PointInTriangleSmooth(loc, _blockingArrayB[j].rg, _blockingArrayB[j + 1].rg, _blockingArrayB[j + 2].rg);
                    visionValue = step(0.5, visionValue + inTriangle);
                }

                for (j = 0; j < 664 && _blockingArrayC[j].r < 10000000; j = j + 3)
                {
                    float inTriangle = PointInTriangleSmooth(loc, _blockingArrayC[j].rg, _blockingArrayC[j + 1].rg, _blockingArrayC[j + 2].rg);
                    visionValue = step(0.5, visionValue + inTriangle);
                }

                col = float4(visionValue, visionValue, visionValue, visionValue);
                return col;
            }

            ENDCG
        }
    }
}
