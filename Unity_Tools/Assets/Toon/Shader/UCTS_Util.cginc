
#define PI 3.141592654

float hash(float n)
{
    return frac(sin(n) * 43758.5453);
}

// The noise function returns a value in the range -1.0f -> 1.0f    
float noise(float3 x)
{
    float3 p = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    float n = p.x + p.y * 57.0 + 113.0 * p.z;
    return lerp(
                    lerp(
                    lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                    lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                    lerp(
                        lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                        lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
}

// UV回転をする関数
float2 RotateUV(float2 _uv, float _radian, float2 _piv, float _time)
{
    float RotateUV_ang = _radian;
    float RotateUV_cos = cos(_time * RotateUV_ang);
    float RotateUV_sin = sin(_time * RotateUV_ang);
    return (mul(_uv - _piv, float2x2(RotateUV_cos, -RotateUV_sin, RotateUV_sin, RotateUV_cos)) + _piv);
}

fixed3 DecodeLightProbe(fixed3 N)
{
    return ShadeSH9(float4(N, 1));
}

float3x3 getXYTranslationMatrix (float2 translation) 
{
    return float3x3(1, 0, translation.x, 0, 1, translation.y, 0, 0, 1);
}
        
float3x3 getXYRotationMatrix (float theta) 
{
    float s = -sin(theta);
    float c = cos(theta);
    return float3x3(c, -s, 0, s, c, 0, 0, 0, 1);
}
        
float3x3 getXYScaleMatrix (float2 scale) 
{
    return float3x3(scale.x, 0, 0, 0, scale.y, 0, 0, 0, 1);
} 
        
float2 applyMatrix (float3x3 m, float2 uv) 
{
    return mul(m, float3(uv.x, uv.y, 1)).xy;
}

fixed3 Hatching(float2 _uv, half _intensity, sampler2D Tex1, sampler2D Tex2)
{
    half3 hatch0 = tex2D(Tex1, _uv).rgb;
    half3 hatch1 = tex2D(Tex2, _uv).rgb;

    half3 overbright = max(0, _intensity - 1.0);

    half3 weightsA = saturate((_intensity * 6.0) + half3(-0, -1, -2));
    half3 weightsB = saturate((_intensity * 6.0) + half3(-3, -4, -5));

    weightsA.xy -= weightsA.yz;
    weightsA.z -= weightsB.x;
    weightsB.xy -= weightsB.zy;

    hatch0 = hatch0 * weightsA;
    hatch1 = hatch1 * weightsB;

    half3 hatching = overbright + hatch0.r +
        hatch0.g + hatch0.b +
        hatch1.r + hatch1.g +
        hatch1.b;

    return hatching;
}


float rand(float3 co)
{
    return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 56.787))) * 43758.5453);
}

float perlinNoise(float3 pos)
{
    float3 ip = floor(pos);
    float3 fp = smoothstep(0, 1, frac(pos));
    float4 a = float4(
        rand(ip + float3(0, 0, 0)),
        rand(ip + float3(1, 0, 0)),
        rand(ip + float3(0, 1, 0)),
        rand(ip + float3(1, 1, 0)));
    float4 b = float4(
        rand(ip + float3(0, 0, 1)),
        rand(ip + float3(1, 0, 1)),
        rand(ip + float3(0, 1, 1)),
        rand(ip + float3(1, 1, 1)));
    a = lerp(a, b, fp.z);
    a.xy = lerp(a.xy, a.zw, fp.y);
    return lerp(a.x, a.y, fp.x);
}

float perlin(float3 pos) 
{
    return  (perlinNoise(pos) * 32 +
            perlinNoise(pos * 2 ) * 16 +
            perlinNoise(pos * 4) * 8 +
            perlinNoise(pos * 8) * 4 +
            perlinNoise(pos * 16) * 2 +
            perlinNoise(pos * 32) ) / 64;
}

