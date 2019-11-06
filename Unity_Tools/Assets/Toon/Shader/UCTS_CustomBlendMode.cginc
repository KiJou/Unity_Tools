  #include "UCTS_Util.cginc"


uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
uniform float4 _BaseColor;
uniform float4 _Color;
uniform fixed _Use_BaseAs1st;
uniform fixed _Use_1stAs2nd;
uniform fixed _Is_LightColor_Base;
uniform sampler2D _1st_ShadeMap;
uniform float4 _1st_ShadeMap_ST;
uniform float4 _1st_ShadeColor;
uniform fixed _Is_LightColor_1st_Shade;
uniform sampler2D _2nd_ShadeMap;
uniform float4 _2nd_ShadeMap_ST;
uniform float4 _2nd_ShadeColor;

uniform fixed _Is_LightColor_2nd_Shade;
uniform sampler2D _NormalMap;
uniform float4 _NormalMap_ST;
uniform fixed _Is_NormalMapToBase;
uniform fixed _Set_SystemShadowsToBase;
uniform float _Tweak_SystemShadowsLevel;
uniform float _BaseColor_Step;
uniform float _BaseShade_Feather;
uniform sampler2D _Set_1st_ShadePosition;
uniform float4 _Set_1st_ShadePosition_ST;
uniform float _ShadeColor_Step;
uniform float _1st2nd_Shades_Feather;
uniform sampler2D _Set_2nd_ShadePosition;
uniform float4 _Set_2nd_ShadePosition_ST;

uniform fixed _Is_Ortho;
uniform float _CameraRolling_Stabilizer;
uniform fixed _BlurLevelMatcap;
uniform fixed _Inverse_MatcapMask;
uniform float _BumpScale;
uniform float _BumpScaleMatcap;

uniform float4 finalCustomColor;
uniform sampler2D _Emissive_Tex;
uniform float4 _Emissive_Tex_ST;
uniform float4 _Emissive_Color;
uniform fixed _Is_ViewCoord_Scroll;
uniform float _Rotate_EmissiveUV;
uniform float _Base_Speed;
uniform float _Scroll_EmissiveU;
uniform float _Scroll_EmissiveV;
uniform fixed _Is_PingPong_Base;
uniform float4 _ColorShift;
uniform float4 _ViewShift;
uniform float _ColorShift_Speed;
uniform fixed _Is_ColorShift;
uniform fixed _Is_ViewShift;
uniform float _Emissive_Power;
uniform float _Emissive_Intensity;

uniform float _Unlit_Intensity;
uniform fixed _Is_Filter_HiCutPointLightColor;
uniform fixed _Is_Filter_LightColor;
uniform float _StepOffset;
uniform fixed _Is_BLD;
uniform float _Offset_X_Axis_BLD;
uniform float _Offset_Y_Axis_BLD;
uniform fixed _Inverse_Z_Axis_BLD;
float3 finalColor;

sampler2D _GrabTexture;
float4 _GrabTexture_ST;

// Hatch
#ifdef _IS_PASS_HATCH
    uniform sampler2D _HatchSheet; uniform float4 _HatchSheet_ST;
    uniform half _HatchBlend;
    uniform float _AnimationSpeed;
    uniform uint _ColX;
    uniform uint _RowY;
#endif


//v.2.0.4
#ifdef _IS_TRANSCLIPPING_OFF
//
#elif _IS_TRANSCLIPPING_ON
    uniform sampler2D _ClippingMask; uniform float4 _ClippingMask_ST;
    uniform fixed _IsBaseMapAlphaAsClippingMask;
    uniform float _Clipping_Level;
    uniform fixed _Inverse_Clipping;
    uniform float _Tweak_transparency;
#endif


uniform float _GI_Intensity;

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
};
            
struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float3 normal : NORMAL;
    float3 normalDir : TEXCOORD2;
    float3 tangentDir : TEXCOORD3;
    float3 bitangentDir : TEXCOORD4;
    float mirrorFlag : TEXCOORD5;
    LIGHTING_COORDS(6,7)
};

v2f vert(appdata v)
{
    v2f o;
    o.normalDir    = UnityObjectToWorldNormal(v.normal);
    o.tangentDir   = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
    o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
    o.uv0 = TRANSFORM_TEX(v.uv0, _GrabTexture);
    o.uv1 = TRANSFORM_TEX(v.uv1, _Emissive_Tex);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.normal = mul(unity_ObjectToWorld, v.vertex);

#ifdef _IS_PASS_HATCH
    float2 size = float2(1.0f / _ColX, 1.0f / _RowY);
    uint frame = _ColX * _RowY;
    uint index = _Time.y * _AnimationSpeed;
    uint x = index % _ColX;
    uint y = floor((index % frame) / _ColX);
    float2 offsetHatch = float2(size.x * x , -size.y * y);
    float2 newUV = v.uv0 * size;
    newUV.y = newUV.y + size.y*(_RowY - 1);
    o.uv0 = newUV + offsetHatch;
#endif

    float3 crossFwd = cross(UNITY_MATRIX_V[0], UNITY_MATRIX_V[1]);
    o.mirrorFlag = dot(crossFwd, UNITY_MATRIX_V[2]) < 0 ? 1 : -1;
    TRANSFER_VERTEX_TO_FRAGMENT(o)
    return o;
}

float4 frag(v2f i) : SV_TARGET
{
    i.normalDir = normalize(i.normalDir);
    float3x3 tangentTransform = float3x3(i.tangentDir, i.bitangentDir, i.normalDir);
    float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.normal.xyz);
    float2 Set_UV0 = i.uv0;
    float2 Set_UV1 = i.uv1;

    float3 _NormalMap_var = UnpackScaleNormal(tex2D(_NormalMap, TRANSFORM_TEX(i.uv0, _NormalMap)), _BumpScale);
    float3 normalLocal = _NormalMap_var.rgb;
    float3 normalDirection = normalize(mul(normalLocal, tangentTransform)); // Perturbed normals
    float4 _MainTex_var = tex2D(_GrabTexture, TRANSFORM_TEX(Set_UV0, _GrabTexture));

#ifdef _IS_TRANSCLIPPING_OFF
    //
#elif _IS_TRANSCLIPPING_ON
    float4 _ClippingMask_var = tex2D(_ClippingMask,TRANSFORM_TEX(Set_UV1, _ClippingMask));
    float Set_MainTexAlpha = _MainTex_var.a;
    float _IsBaseMapAlphaAsClippingMask_var = lerp( _ClippingMask_var.r, Set_MainTexAlpha, _IsBaseMapAlphaAsClippingMask );
    float _Inverse_Clipping_var = lerp( _IsBaseMapAlphaAsClippingMask_var, (1.0 - _IsBaseMapAlphaAsClippingMask_var), _Inverse_Clipping );
    float Set_Clipping = saturate((_Inverse_Clipping_var+_Clipping_Level));
    clip(Set_Clipping - 0.5);
#endif

    UNITY_LIGHT_ATTENUATION(attenuation, i, i.normal.xyz);
    
    float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.normal.xyz, _WorldSpaceLightPos0.w));
    float3 addPassLightColor = (0.5 * dot(lerp(i.normalDir, normalDirection, _Is_NormalMapToBase), lightDirection) + 0.5) * _LightColor0.rgb * attenuation;
    float pureIntencity = max(0.001, (0.299 * _LightColor0.r + 0.587 * _LightColor0.g + 0.114 * _LightColor0.b));
    float3 lightColor = max(0, lerp(addPassLightColor, lerp(0, min(addPassLightColor, addPassLightColor / pureIntencity), _WorldSpaceLightPos0.w), _Is_Filter_LightColor));
    float3 halfDirection = normalize(viewDirection + lightDirection);

    fixed _sign_Mirror = i.mirrorFlag;
    float3 _Camera_Right = UNITY_MATRIX_V[0].xyz;
    float3 _Camera_Front = UNITY_MATRIX_V[2].xyz;
    float3 _Up_Unit = float3(0, 1, 0);
    float3 _Right_Axis = cross(_Camera_Front, _Up_Unit);
    if (_sign_Mirror < 0)
    {
        _Right_Axis = -1 * _Right_Axis;
    }
    else
    {
        _Right_Axis = _Right_Axis;
    }
    float _Camera_Right_Magnitude = sqrt(_Camera_Right.x * _Camera_Right.x + _Camera_Right.y * _Camera_Right.y + _Camera_Right.z * _Camera_Right.z);
    float _Right_Axis_Magnitude = sqrt(_Right_Axis.x * _Right_Axis.x + _Right_Axis.y * _Right_Axis.y + _Right_Axis.z * _Right_Axis.z);
    float _Camera_Roll_Cos = dot(_Right_Axis, _Camera_Right) / (_Right_Axis_Magnitude * _Camera_Right_Magnitude);
    float _Camera_Roll = acos(clamp(_Camera_Roll_Cos, -1, 1));
    fixed _Camera_Dir = _Camera_Right.y < 0 ? -1 : 1;

#ifdef _EMISSIVE_SIMPLE
    fixed4 _Emissive_Tex_var = tex2D(_Emissive_Tex, TRANSFORM_TEX(Set_UV1, _Emissive_Tex));
    float emissiveMask = _Emissive_Tex_var.a;
    clip(_Emissive_Tex_var.a - 0.5);
    finalCustomColor = (_Emissive_Tex_var * _Emissive_Color) * _Emissive_Intensity;
    finalCustomColor.w = mul(finalCustomColor.x, 1);
    finalColor = finalCustomColor.rgb;
    fixed4 finalRGBA = fixed4(finalColor, _Emissive_Power);

#elif _EMISSIVE_ANIMATION
    float3 viewNormal_Emissive = (mul(UNITY_MATRIX_V, _WorldSpaceCameraPos)).xyz;
    float3 NormalBlend_Emissive_Detail = viewNormal_Emissive * float3(-1,-1,1);
    float3 NormalBlend_Emissive_Base = (mul( UNITY_MATRIX_V, float4(viewDirection,0)).xyz*float3(-1,-1,1)) + float3(0,0,1);
    float3 noSknewViewNormal_Emissive = NormalBlend_Emissive_Base * dot(NormalBlend_Emissive_Base, NormalBlend_Emissive_Detail) / NormalBlend_Emissive_Base.z - NormalBlend_Emissive_Detail;
    float2 _ViewNormalAsEmissiveUV = noSknewViewNormal_Emissive.xy * (0.5+0.5);
    float2 _ViewCoord_UV = RotateUV(_ViewNormalAsEmissiveUV, -(_Camera_Dir*_Camera_Roll), float2(0.5,0.5), 1.0);

    if(_sign_Mirror < 0)
    {
        _ViewCoord_UV.x = 1 - _ViewCoord_UV.x;
    }
    else 
    {
        _ViewCoord_UV = _ViewCoord_UV;
    }
    float2 emissive_uv = lerp(i.uv1, _ViewCoord_UV, _Is_ViewCoord_Scroll);
    float4 _time_var = _Time;
    float _base_Speed_var = (_time_var.g * _Base_Speed);
    float _Is_PingPong_Base_var = lerp(_base_Speed_var, sin(_base_Speed_var), _Is_PingPong_Base );
    float2 scrolledUV = emissive_uv - float2(_Scroll_EmissiveU, _Scroll_EmissiveV) * _Is_PingPong_Base_var;
    float rotateVelocity = _Rotate_EmissiveUV * PI;
    float2 _rotate_EmissiveUV_var = RotateUV(scrolledUV, rotateVelocity, float2(0.5, 0.5), _Is_PingPong_Base_var);
    fixed4 _Emissive_Tex_var = tex2D(_Emissive_Tex, TRANSFORM_TEX(Set_UV1, _Emissive_Tex));
    float emissiveMask = _Emissive_Tex_var.a;
    _Emissive_Tex_var = tex2D(_Emissive_Tex, TRANSFORM_TEX(_rotate_EmissiveUV_var, _Emissive_Tex));
    clip(_Emissive_Tex_var.a - 0.5);
    float _colorShift_Speed_var = 1.0 - cos(_time_var.g * _ColorShift_Speed);
    float viewShift_var = smoothstep( 0.0, 1.0, max(0,dot(normalDirection, viewDirection)));
    float4 colorShift_Color = lerp(_Emissive_Color, lerp(_Emissive_Color, _ColorShift, _colorShift_Speed_var), _Is_ColorShift);
    float4 viewShift_Color  = lerp(_ViewShift, colorShift_Color, viewShift_var);
    float4 emissive_Color   = lerp(colorShift_Color, viewShift_Color, _Is_ViewShift);
    finalCustomColor = (emissive_Color * _Emissive_Tex_var) * _Emissive_Intensity;
    finalCustomColor.w = mul(finalCustomColor.x, 1);
    finalColor = finalCustomColor.rgb;
    fixed4 finalRGBA = fixed4(finalColor, _Emissive_Power);

#elif _EMISSIVE_PINGPONG
    float3 viewNormal_Emissive = (mul(UNITY_MATRIX_V, _WorldSpaceCameraPos)).xyz;
    float3 NormalBlend_Emissive_Detail = viewNormal_Emissive * float3(-1,-1,1);
    float3 NormalBlend_Emissive_Base = (mul( UNITY_MATRIX_V, float4(viewDirection,0)).xyz*float3(-1,-1,1)) + float3(0,0,1);
    float3 noSknewViewNormal_Emissive = NormalBlend_Emissive_Base * dot(NormalBlend_Emissive_Base, NormalBlend_Emissive_Detail) / NormalBlend_Emissive_Base.z - NormalBlend_Emissive_Detail;
    float2 _ViewNormalAsEmissiveUV = noSknewViewNormal_Emissive.xy * (0.5+0.5);
    float2 _ViewCoord_UV = RotateUV(_ViewNormalAsEmissiveUV, -(_Camera_Dir*_Camera_Roll), float2(0.5,0.5), 1.0);

    if(_sign_Mirror < 0)
    {
        _ViewCoord_UV.x = 1 - _ViewCoord_UV.x;
    }
    else 
    {
        _ViewCoord_UV = _ViewCoord_UV;
    }
    float2 emissive_uv = lerp(i.uv1, _ViewCoord_UV, _Is_ViewCoord_Scroll);
    float4 _time_var = _Time;
    float _base_Speed_var = (_time_var.g * _Base_Speed);
    float _Is_PingPong_Base_var = lerp(_base_Speed_var, sin(_base_Speed_var), _Is_PingPong_Base );
    float2 scrolledUV = emissive_uv - float2(_Scroll_EmissiveU, _Scroll_EmissiveV) * _Is_PingPong_Base_var;
    float rotateVelocity = _Rotate_EmissiveUV * PI;
    float2 _rotate_EmissiveUV_var = RotateUV(scrolledUV, rotateVelocity, float2(0.5, 0.5), _Is_PingPong_Base_var);
    fixed4 _Emissive_Tex_var = tex2D(_Emissive_Tex, TRANSFORM_TEX(Set_UV1, _Emissive_Tex));
    float emissiveMask = _Emissive_Tex_var.a;
    _Emissive_Tex_var = tex2D(_Emissive_Tex, TRANSFORM_TEX(_rotate_EmissiveUV_var, _Emissive_Tex));
    clip(_Emissive_Tex_var.a - 0.5);
    float _colorShift_Speed_var = 1.0 - cos(_time_var.g * _ColorShift_Speed);
    float viewShift_var = smoothstep( 0.0, 1.0, max(0,dot(normalDirection, viewDirection)));
    float4 colorShift_Color = lerp(_Emissive_Color, lerp(_Emissive_Color, _ColorShift, _colorShift_Speed_var), _Is_ColorShift);
    float4 viewShift_Color  = lerp(_ViewShift, colorShift_Color, viewShift_var);
    float4 emissive_Color   = lerp(colorShift_Color, viewShift_Color, _Is_ViewShift);
    float4 resultColor = emissive_Color * _Emissive_Tex_var;
    resultColor  *=  ( 0 + abs( sin( _time_var.w )));
    finalCustomColor = resultColor;
    finalCustomColor.w = mul(finalCustomColor.x, 1);
    finalColor = finalCustomColor.rgb;
    fixed4 finalRGBA = fixed4(finalColor, _Emissive_Power);

#elif _IS_PASS_HATCH
    fixed4 _HatchSheet_var = tex2D(_HatchSheet, TRANSFORM_TEX(Set_UV0, _HatchSheet));
    float hatchMask = _HatchSheet_var.a;
    clip(_HatchSheet_var.a - 0.5);
    finalCustomColor = (_HatchSheet_var);
    // OLD
    //fixed intensity = dot(finalRGBA.rgb, fixed3(0.2, 0.7152, 0.0722));
    //finalRGBA.rgb *= Hatching(Set_UV0 * _HatchOffset, intensity, _Hatch0, _Hatch1);
    finalCustomColor.w = mul(finalCustomColor.x, 1);
    finalColor = finalCustomColor.rgb;
    fixed4 finalRGBA = fixed4(finalColor, _HatchBlend);    
#endif

#ifdef _IS_TRANSCLIPPING_OFF

#elif _IS_TRANSCLIPPING_ON
    float Set_Opacity = saturate((_Inverse_Clipping_var+_Tweak_transparency));
    finalRGBA = fixed4(finalColor,Set_Opacity);
#endif


    return finalRGBA;
}
