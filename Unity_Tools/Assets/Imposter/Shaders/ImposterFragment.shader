Shader "G2Studios/Imposter/Fragment"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)

        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.3

        _ImposterBaseTex("Imposter Base", 2D) = "black" {}
        _ImposterWorldNormalDepthTex("WorldNormal+Depth", 2D) = "black" {}
        _ImposterFrames("Frames",  float) = 8
        _ImposterSize("Radius", float) = 1
        _ImposterOffset("Offset", Vector) = (0,0,0,0)
        _ImposterFullSphere("Full Sphere", float) = 0
        _ImposterBorderClamp("Border Clamp", float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        ZTest LEqual
        ZWrite on
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "ImposterUtil.cginc"
            #include "ImposterCommonFragment.cginc"

            struct appdata
            {
                float4 texCoord : TEXCOORD0;
                float4 plane0 : TEXCOORD1;
                float4 plane1 : TEXCOORD2;
                float4 plane2 : TEXCOORD3;
                float3 tangentWorld : TANGENT;
                float3 bitangentWorld : TEXCOORD4;
                float3 normalWorld : NORMAL;
                float4 screenPos : SV_POSITION;
            };

            half _Glossiness;
            half _Metallic;
            half _Cutoff;
            fixed4 _Color;

            v2f vert (appdata_full v)
            {
                v2f o = (v2f)0;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return 1;
            }
            ENDCG
        }
    }
}
