Shader "G2Studios/Shadow/PlanarShadow"
{

    Properties
    {
        _ShadowColor("Shadow Color", Color) = (0,0,0,1)
        _PlaneHeight("PlaneHeight", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}

        Pass
        {

            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 0
                Comp Equal
                Pass IncrWrap
                ZFail Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            uniform float4 _ShadowColor;
            uniform float _PlaneHeight = 0;

            struct vsOut
            {
                float4 pos	: SV_POSITION;
            };

            vsOut vert(appdata_base v)
            {
                vsOut o;

                float4 vPosWorld = mul(unity_ObjectToWorld, v.vertex);
                float4 lightDirection = -normalize(_WorldSpaceLightPos0);

                float opposite = vPosWorld.y - _PlaneHeight;
                float cosTheta = -lightDirection.y;	// = lightDirection dot (0,-1,0)
                float hypotenuse = opposite / cosTheta;
                float3 vPos = vPosWorld.xyz + (lightDirection * hypotenuse);
                o.pos = mul(UNITY_MATRIX_VP, float4(vPos.x, _PlaneHeight, vPos.z, 1));
                return o;
            }

            fixed4 frag(vsOut i) : COLOR
            {
                return _ShadowColor;
            }

            ENDCG

        }
    }
}
