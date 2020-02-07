
Shader "Hidden/Show Depth"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4x4 _LV_Mat;
            half4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float depth : DEPTH;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                float3 WorldToLight = -mul(_LV_Mat, float4(o.worldPos,1.0));
                o.depth = WorldToLight.z * (1 / 8.3);  // 8.3 is far plane of the Light Camera
                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float Depth = 1 - (1 - i.depth);
                return fixed4(Depth, Depth, Depth, 1);
            }
            ENDCG
        }
    }

}
