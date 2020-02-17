Shader "G2Studios/Shadow/ReceiveShadow" 
{
    Properties
    {
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1.0
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
	}

    SubShader 
	{
		Tags { "RenderType" = "Opaque" }

        Pass 
		{        
            Lighting On
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite[_ZWrite]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ HARD_SHADOWS VARIANCE_SHADOWS
            #include "UnityCG.cginc"

            float4 _Color;
            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _ShadowTex;
            float4x4 _LightMatrix;
            float4 _ShadowTexScale;
            float _Cutoff;

            float _MaxShadowIntensity;
            float _VarianceShadowExpansion;

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
                float4 wPos : TEXCOORD1;
                float depth : TEXCOORD2;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = v.normal;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                COMPUTE_EYEDEPTH(o.depth);
                return o; 
            }

            fixed4 frag (v2f i) : SV_Target
			{
                float4 texColor = tex2D(_MainTex, i.uv);
                float4 color = _Color;
                color = float4(CTIllum(i.wPos, i.normal), color.a);
                color *= texColor;

                // SHADOWS
                // light座標までの距離
                float4 lightSpacePos = mul(_LightMatrix, i.wPos);
                float3 lightSpaceNorm = normalize(mul(_LightMatrix, mul(unity_ObjectToWorld, i.normal)));
                float depth = lightSpacePos.z / _ShadowTexScale.z;

                float2 uv = lightSpacePos.xy;
                uv += _ShadowTexScale.xy / 2;
                uv /= _ShadowTexScale.xy;

                float shadowIntensity = 0;
                float2 offset = lightSpaceNorm * _ShadowTexScale.w;
                float4 samp = tex2D(_ShadowTex, uv + offset);


#ifdef HARD_SHADOWS
                float sDepth = samp.r;
                shadowIntensity = step(sDepth, depth - _ShadowTexScale.w);
                //shadowIntensity = sDepth;
#endif

#ifdef VARIANCE_SHADOWS
                // The moments of the fragment live in "_shadowTex"
                float2 s = samp.rg;

                // テクセル全体の平均/予想深度および深度^ 2
                // E(x) and E(x^2)
                float x = s.r; 
                float x2 = s.g;
                
                // テクセルの分散をに基づいて計算
                // the formula var = E(x^2) - E(x)^2
                float var = x2 - x*x; 

                // 基本深度に基づいて初期確率を計算
                // 深度がxより近い場合、フラグメントは100％
                // 点灯する確率（p = 1）
                float p = depth <= x;
                
                // チェビシェフの不等式を使用して確率の上限を計算する
                float delta = depth - x;
                float p_max = var / (var + delta*delta);

                // 光のにじみを軽減
                float amount = _VarianceShadowExpansion;
                p_max = clamp( (p_max - amount) / (1 - amount), 0, 1);
                shadowIntensity = 1 - max(p, p_max);
#endif
                float value = 1- shadowIntensity * _MaxShadowIntensity;
                color.xyz *= value;
                color.xyz += UNITY_LIGHTMODEL_AMBIENT.xyz;
                return color;

            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}