
Shader "Custom/Light With Shadow" 
{

    Properties
    {
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Back

        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata 
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f 
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            float4 _Tint;
            sampler2D _MainTex;
            sampler2D  SahdowMap_Light, SahdowMap_View;
            float4 _MainTex_ST;
            float4x4 WorldToCamera, WorldToLight;
            float3 _LightDir1;

            v2f vert(appdata v)
            {
                v2f i;
                i.position = UnityObjectToClipPos(v.position);
                i.worldPos = mul(unity_ObjectToWorld, v.position);
                i.normal = UnityObjectToWorldNormal(v.normal);
                i.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return i;
            }


            float4 frag(v2f i) : SV_TARGET
            {
                float4 projected = mul(WorldToLight, float4(i.worldPos, 1.0f));
                float2 uv_Light = (projected.xy / projected.w) * 0.5 + 0.5;
                float LightDepth = tex2D(SahdowMap_Light, uv_Light).r;
                if (uv_Light.y > 1.0)
                {
                    LightDepth = 1.0;
                }

                float4 projected2 = mul(WorldToCamera, float4(i.worldPos, 1.0f));
                float2 uv_View = (-projected2.xy / projected2.w) * 0.5 + 0.5;
                float ViewDepth = tex2D(SahdowMap_View, uv_View).r;

                float bias = max(0.05 * (1.0 - dot(i.normal, _LightDir1)), 0.005);
                float Shadow = LightDepth + bias < ViewDepth ? 0 : 1;

                float4 Texture = tex2D(_MainTex, i.uv);
                float3 lightpos = _LightDir1 - i.worldPos;
                return saturate(dot(lightpos, i.normal)) *_Tint * Texture * Shadow;
            }
            ENDCG
        }
    }
}