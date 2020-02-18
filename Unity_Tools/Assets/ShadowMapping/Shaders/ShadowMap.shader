﻿Shader "G2Studios/Shadow/ShadowMap" 
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _CameraDepthTexture;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 depth: TEXCOORD0;
                float4 projPos : TEXCOORD1;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth = o.vertex.zw;
                o.projPos = ComputeScreenPos(o.vertex);
                //o.depth = COMPUTE_DEPTH_01;
                return o;
            }

            fixed4 frag(v2f i) : COLOR
            {
                return (EncodeFloatRGBA(min(i.depth,0.9999991)));

                //return EncodeFloatRGBA(i.depth.x / i.depth.y);

                // old
                //float depth = 1 - i.vertex.z;
                //float4 finalColor = float4(depth, pow(depth, 2), 0, 0);
                //return finalColor;
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}