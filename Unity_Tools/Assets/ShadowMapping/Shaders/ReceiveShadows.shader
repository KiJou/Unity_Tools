Shader "G2Studios/Shadow/ReceiveShadow" 
{
    Properties
    {
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1.0
        _Color("Main Color", Color) = (1,1,1,1)
        _ShadowColor("Receive Shadow Color", Color) = (0.75,0.75,0.75,1)
        _MainTex("Texture", 2D) = "white" {}
        _Cutoff("Cutoff", Range(0, 1)) = 0.83
	}

    SubShader 
	{
		Tags { "RenderType" = "Opaque" }

        Pass 
		{        
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite[_ZWrite]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ HARD_SHADOWS VARIANCE_SHADOWS
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            float4 _Color;
            sampler2D _MainTex; float4 _MainTex_ST;   

            float4x4 _LightMatrix;
            float4x4 _LightVP;
            float4 _ShadowTexScale;
            float _Cutoff, _VarianceShadowExpansion;
            sampler2D _ShadowTex;
            float4 _ShadowColor;
            //UNITY_DECLARE_SHADOWMAP(_ShadowTex);

            float3 CTIllum(float4 wVertex, float3 normal)
            {
                float fresnel_val = 0.2;
                float roughness_val = .06;
                float k = 0.01;

                wVertex = mul(unity_WorldToObject, wVertex);
                float3 viewpos = -mul(UNITY_MATRIX_MV, wVertex).xyz;

                float3 col = float3(0, 0, 0);
                for (int i = 0; i < 2; i++)
                {
                    // View vector, light direction, and Normal in model-view space
                    float3 toLight = unity_LightPosition[i].xyz;
                    float3 L = normalize(toLight);
                    float3 V = normalize(viewpos);//float3(0, 0, 1);
                    float3 N = mul(UNITY_MATRIX_MV, float4(normal, 0));
                    N = normalize(N);

                    // Half vector from view to light vector
                    float3 H = normalize(V + L);

                    // Dot products
                    float NdotL = max(dot(N, L), 0);
                    float NdotV = max(dot(N, V), 0);
                    float NdotH = max(dot(N, H), 1.0e-7);
                    float VdotH = max(dot(V, H), 0);

                    // model the geometric attenuation of the surface
                    float geo_numerator = 2 * NdotH;
                    float geo_b = (geo_numerator * NdotV) / VdotH;
                    float geo_c = (geo_numerator * NdotL) / VdotH;
                    float geometric = 2 * NdotH / VdotH;
                    geometric = min(1, max(0, min(geo_b, geo_c)));

                    // calculate the roughness of the model
                    float r2 = roughness_val * roughness_val;
                    float NdotH2 = NdotH * NdotH;
                    float NdotH2_r = 1 / (NdotH2 * r2);
                    float roughness_exp = (NdotH2 - 1) * NdotH2_r;
                    float roughness = exp(roughness_exp) * NdotH2_r / (4 * NdotH2);

                    // Calculate the fresnel value
                    float fresnel = pow(1.0 - VdotH, 5.0);
                    fresnel *= 1 - fresnel_val;
                    fresnel += fresnel_val;

                    // Calculate the final specular value
                    float s = (1 - k)*(fresnel * geometric * roughness) / (NdotV * NdotL * 3.14 + 1.0e-7) + k;
                    float3 spec = float3(1, 1, 1)*s;

                    // apply to the model
                    float lengthSq = dot(toLight, toLight);
                    float atten = 1.0 / (1.0 + lengthSq * unity_LightAtten[i].z);
                    col += NdotL * (unity_LightColor[i].xyz * spec + unity_LightColor[i].xyz) * atten;
                }
                return col;
            }

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 worldPos : TEXCOORD1;
                float depth : TEXCOORD2;
                float4 shadowVertex : TEXCOORD3;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = v.normal;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.shadowVertex = mul(_LightVP, v.vertex);
                COMPUTE_EYEDEPTH(o.depth);
                return o; 
            }

            fixed4 frag (v2f i) : SV_Target
			{
                float4 lightSpacePos = mul(_LightMatrix, i.worldPos);
                float3 lightSpaceNorm = normalize(mul(_LightMatrix, mul(unity_ObjectToWorld, i.normal)));
                float depth = lightSpacePos.z / _ShadowTexScale.z;

                float2 uv = lightSpacePos.xy;
                uv += _ShadowTexScale.xy / 2;
                uv /= _ShadowTexScale.xy;

                float shadowIntensity = 1;
                float2 offset = lightSpaceNorm * _ShadowTexScale.w;
                float4 lightDepth = tex2D(_ShadowTex, uv + offset);

#ifdef HARD_SHADOWS
                float sDepth = lightDepth.r;
                shadowIntensity = step(sDepth, depth - _ShadowTexScale.w);
#endif

#ifdef VARIANCE_SHADOWS
                float2 s = lightDepth.rg;
                float x = s.r; 
                float x2 = s.g;               
                float var = x2 - x*x; 
                float p = depth <= x;                
                float delta = depth - x;
                float p_max = var / (var + delta*delta);
                float amount = _VarianceShadowExpansion;
                p_max = clamp( (p_max - amount) / (1 - amount), 0, 1);
                shadowIntensity = 1 - max(p, p_max);
#endif
                float value = shadowIntensity;
                float4 color = tex2D(_MainTex, i.uv) * _Color;
                color.xyz *= value;
                if (value <= _Cutoff)
                {
                    color += _ShadowColor;
                }
                color.xyz += UNITY_LIGHTMODEL_AMBIENT.xyz;
                return color;
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}