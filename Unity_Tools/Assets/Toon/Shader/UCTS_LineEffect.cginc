

struct appdata
{
    fixed4 vertex : POSITION;
    fixed4 normal : NORMAL;
    fixed3 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float2 screenuv : TEXCOORD1;
    float3 objectPos : TEXCOORD2;
    float4 vertex : SV_POSITION;
    float depth : DEPTH;
    float3 normal : NORMAL;
    float3 viewDir : TEXCOORD3;
};


sampler2D _NoiseTex; float4 _NoiseTex_ST;
sampler2D _MainTex; float4 _MainTex_ST;
float4 _ShieldColor;
sampler2D _CameraDepthNormalsTexture;
float _Edge;
float _AnimationSpeed;
float _OffsetY, _Fraction, _WaveAmount, _RimIntensity;

v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
    o.screenuv = ((o.vertex.xy / o.vertex.w) + 1) / 2;
    o.screenuv.y = 1 - o.screenuv.y;
    o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z * _ProjectionParams.w;
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex)));
    o.objectPos = v.vertex.xyz;
    return o;
}

float triWave(float t, float offset, float yOffset) 
{
    return saturate(abs(frac(offset + t) * 2 - 1) + yOffset);
}

fixed4 texColor(v2f i, float rim, float objectPosOffset) 
{
    fixed4 mainTex = tex2D(_NoiseTex, i.uv);
    //mainTex.r *= triWave(_Time.x * 5, abs(i.objectPos.y) * 2, -0.7) * 6;
    //mainTex.g *= saturate(rim) * (sin(_Time.z + mainTex.b * 5) + 1);
    mainTex.r *= triWave(_Time.x * _AnimationSpeed, objectPosOffset * _Fraction, _OffsetY) * _WaveAmount;
    mainTex.g *= saturate(rim) * (sin(_Time.z + mainTex.b * 5) + 1);
    return mainTex.r * _ShieldColor + mainTex.g * _ShieldColor;
}

fixed4 frag(v2f i) : SV_Target
{
    float3 normalValues;
    float screenDepth;
    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.screenuv), screenDepth, normalValues);
    float diff = screenDepth - i.depth;
    float intersect = 0;

    if (diff > 0) 
    {
        intersect = 1 - smoothstep(0, _ProjectionParams.w * 0.4, diff);
    }

    float rim = _RimIntensity - abs(dot(i.normal, normalize(i.viewDir))) * 2;
    
    float northPole = (i.objectPos.y - 0.5) * 20;
    float glow = max(max(intersect, rim), northPole);
    fixed4 sweetColor = fixed4(_ShieldColor.rgb * pow(glow, _Edge), _ShieldColor.a);
    fixed4 hexes = texColor(i, rim, northPole);
    fixed4 col = _ShieldColor * sweetColor * glow + hexes;
    return col;
}
