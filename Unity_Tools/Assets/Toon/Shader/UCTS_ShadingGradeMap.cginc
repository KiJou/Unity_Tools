#include "UCTS_Util.cginc"

uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
uniform float4 _BaseColor;
uniform sampler2D _BlendTex; uniform float4 _BlendTex_ST;
uniform float _BlendWeight;
uniform float _TexScale;
uniform float _TexRatio;
uniform float2 _PlaneScale;
float _Theta;

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
uniform sampler2D _ShadingGradeMap;
uniform float4 _ShadingGradeMap_ST;
uniform float _Tweak_ShadingGradeMapLevel;
uniform fixed _BlurLevelSGM;
uniform float _1st_ShadeColor_Step;
uniform float _1st_ShadeColor_Feather;
uniform float _2nd_ShadeColor_Step;
uniform float _2nd_ShadeColor_Feather;

// HighColor
uniform float4 _HighColor;
uniform sampler2D _HighColor_Tex; uniform float4 _HighColor_Tex_ST;
uniform fixed _Is_LightColor_HighColor;
uniform fixed _Is_NormalMapToHighColor;
uniform float _HighColor_Power;
uniform fixed _Is_SpecularToHighColor;
uniform fixed _Is_BlendAddToHiColor;
uniform fixed _Is_UseTweakHighColorOnShadow;
uniform float _TweakHighColorOnShadow;
uniform sampler2D _Set_HighColorMask; uniform float4 _Set_HighColorMask_ST;
uniform float _Tweak_HighColorMaskLevel;

// RimLight
uniform fixed _RimLight;
uniform float4 _RimLightColor;
uniform fixed _Is_LightColor_RimLight;
uniform fixed _Is_NormalMapToRimLight;
uniform float _RimLight_Power;
uniform float _RimLight_InsideMask;
uniform fixed _RimLight_FeatherOff;
uniform fixed _LightDirection_MaskOn;
uniform float _Tweak_LightDirection_MaskLevel;
uniform fixed _Add_Antipodean_RimLight;
uniform float4 _Ap_RimLightColor;
uniform fixed _Is_LightColor_Ap_RimLight;
uniform float _Ap_RimLight_Power;
uniform fixed _Ap_RimLight_FeatherOff;
uniform sampler2D _Set_RimLightMask;
uniform float4 _Set_RimLightMask_ST;
uniform float _Tweak_RimLightMaskLevel;

// MatCap
uniform fixed _MatCap;
uniform sampler2D _MatCap_Sampler;
uniform float4 _MatCap_Sampler_ST;
uniform float4 _MatCapColor;
uniform fixed _Is_LightColor_MatCap;
uniform fixed _Is_BlendAddToMatCap;
uniform float _Tweak_MatCapUV;
uniform float _Rotate_MatCapUV;
uniform fixed _Is_NormalMapForMatCap;
uniform sampler2D _NormalMapForMatCap;
uniform float4 _NormalMapForMatCap_ST;
uniform float _Rotate_NormalMapForMatCapUV;
uniform fixed _Is_UseTweakMatCapOnShadow;
uniform float _TweakMatCapOnShadow;
uniform sampler2D _Set_MatcapMask;
uniform float4 _Set_MatcapMask_ST;
uniform float _Tweak_MatcapMaskLevel;

uniform fixed _Is_Ortho;
uniform float _CameraRolling_Stabilizer;
uniform fixed _BlurLevelMatcap;
uniform fixed _Inverse_MatcapMask;
uniform float _BumpScale;
uniform float _BumpScaleMatcap;
uniform float _Unlit_Intensity;
uniform fixed _Is_Filter_HiCutPointLightColor;
uniform fixed _Is_Filter_LightColor;
uniform float _StepOffset;
uniform fixed _Is_BLD;
uniform float _Offset_X_Axis_BLD;
uniform float _Offset_Y_Axis_BLD;
uniform fixed _Inverse_Z_Axis_BLD;

// Smear
uniform fixed4 _PrevPosition;
uniform fixed4 _Position;
uniform half _NoiseScale;
uniform half _NoiseHeight;
uniform half _DegreesAngleX;
uniform half _DegreesAngleY;


#ifdef _IS_TRANSCLIPPING_OFF
//
#elif _IS_TRANSCLIPPING_ON
    uniform sampler2D _ClippingMask; uniform float4 _ClippingMask_ST;
    uniform fixed _IsBaseMapAlphaAsClippingMask;
    uniform float _Clipping_Level;
    uniform fixed _Inverse_Clipping;
    uniform float _Tweak_transparency;
#endif

uniform float4 _LightColor0;
uniform float _GI_Intensity;

struct VertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float4 texcoord0  : TEXCOORD0;
};

struct VertexOutput
{
    float4 pos : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float4 posWorld : TEXCOORD1;
    float3 normalDir : TEXCOORD2;
    float3 tangentDir : TEXCOORD3;
    float3 bitangentDir : TEXCOORD4;
    float mirrorFlag : TEXCOORD5;
    LIGHTING_COORDS(6,7)
    UNITY_FOG_COORDS(8)
};

VertexOutput vert(VertexInput v)
{
    VertexOutput o = (VertexOutput) 0;
    o.uv0 = v.texcoord0;
    o.normalDir = UnityObjectToWorldNormal(v.normal);
    o.tangentDir = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
    o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);

    // Smear
    fixed4 worldPos = mul(unity_ObjectToWorld, v.vertex);
    fixed3 worldOffset = _Position.xyz - _PrevPosition.xyz;
    fixed3 localOffset = worldPos.xyz - _Position.xyz;
    float dirDot = dot(normalize(worldOffset), normalize(localOffset));
    fixed3 unitVec = fixed3(1, 1, 1) * _NoiseHeight;
    worldOffset = clamp(worldOffset, unitVec * -1, unitVec);
    worldOffset *= -clamp(dirDot, -1, 0) * lerp(1, 0, step(length(worldOffset), 0));    
    fixed3 smearOffset = -worldOffset.xyz * lerp(1, noise(worldPos * _NoiseScale), step(0, _NoiseScale));
    worldPos.xyz += smearOffset;
    v.vertex = mul(unity_WorldToObject, worldPos);

    float3 lightColor = _LightColor0.rgb;
    o.pos = UnityObjectToClipPos(v.vertex);

    float2 offset = float2((1 - _PlaneScale.x) * 0.5, (1 - _PlaneScale.y) * 0.5);
    o.uv0 = applyMatrix(
        getXYTranslationMatrix(float2(0.5, 0.5)), 
        applyMatrix( // scale
        getXYScaleMatrix(float2(1 / _TexRatio, 1) * _TexScale),
        applyMatrix( // rotate
        getXYRotationMatrix(_Theta), 
        applyMatrix(
        getXYTranslationMatrix(float2(-0.5, -0.5) + offset), (v.texcoord0 * _PlaneScale)))));

    float3 crossFwd = cross(UNITY_MATRIX_V[0], UNITY_MATRIX_V[1]);
    o.mirrorFlag = dot(crossFwd, UNITY_MATRIX_V[2]) < 0 ? 1 : -1;
    UNITY_TRANSFER_FOG(o, o.pos);
    TRANSFER_VERTEX_TO_FRAGMENT(o)
    return o;
}

float4 frag(VertexOutput i, fixed facing : VFACE) : SV_TARGET
{
    i.normalDir = normalize(i.normalDir);
    float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
    float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
    float2 Set_UV0 = i.uv0;
    //float3 _NormalMap_var = UnpackScaleNormal(tex2D(_NormalMap, TRANSFORM_TEX(Set_UV0, _NormalMap)), _BumpScale);
    float3 _NormalMap_var = UnpackNormal(tex2D(_NormalMap, Set_UV0));
    _NormalMap_var.xy *= _BumpScale;
    float3 normalLocal = _NormalMap_var.rgb;
    float3 normalDirection = normalize(mul(normalLocal, tangentTransform));

    float4 finalRGBA = float4(1,1,1,1);
    float4 _MainTex_var = tex2D(_MainTex, TRANSFORM_TEX(Set_UV0, _MainTex));
    float4 _BlendTex_var = tex2D(_BlendTex, TRANSFORM_TEX(Set_UV0, _BlendTex));
    _MainTex_var = lerp(_MainTex_var, _BlendTex_var, _BlendWeight);

#ifdef _IS_TRANSCLIPPING_OFF
#elif _IS_TRANSCLIPPING_ON
    float4 _ClippingMask_var = tex2D(_ClippingMask,TRANSFORM_TEX(Set_UV0, _ClippingMask));
    float Set_MainTexAlpha = _MainTex_var.a;
    float _IsBaseMapAlphaAsClippingMask_var = lerp( _ClippingMask_var.r, Set_MainTexAlpha, _IsBaseMapAlphaAsClippingMask );
    float _Inverse_Clipping_var = lerp( _IsBaseMapAlphaAsClippingMask_var, (1.0 - _IsBaseMapAlphaAsClippingMask_var), _Inverse_Clipping );
    //float Set_Clipping = saturate((_Inverse_Clipping_var+_Clipping_Level));
    float Set_Clipping = clamp((_Inverse_Clipping_var+_Clipping_Level), 0, 1);
    clip(Set_Clipping - 0.5);
#endif

    UNITY_LIGHT_ATTENUATION(attenuation, i, i.posWorld.xyz);

#ifdef _IS_PASS_FWDBASE
    float3 defaultLightDirection = normalize(UNITY_MATRIX_V[2].xyz + UNITY_MATRIX_V[1].xyz);
    //float3 defaultLightColor = saturate(max(half3(0.05,0.05,0.05) * _Unlit_Intensity, max(ShadeSH9(half4(0.0, 0.0, 0.0, 1.0)), ShadeSH9(half4(0.0, -1.0, 0.0, 1.0)).rgb) * _Unlit_Intensity));
    float3 defaultLightColor = clamp((max(half3(0.05,0.05,0.05) * _Unlit_Intensity, max(ShadeSH9(half4(0.0, 0.0, 0.0, 1.0)), ShadeSH9(half4(0.0, -1.0, 0.0, 1.0)).rgb) * _Unlit_Intensity)), 0, 1);
    float3 customLightDirection = normalize(mul( unity_ObjectToWorld, float4(((float3(1.0,0.0,0.0)*_Offset_X_Axis_BLD*10)+(float3(0.0,1.0,0.0)*_Offset_Y_Axis_BLD*10)+(float3(0.0,0.0,-1.0)*lerp(-1.0,1.0,_Inverse_Z_Axis_BLD))),0)).xyz);
    float3 lightDirection = normalize(lerp(defaultLightDirection,_WorldSpaceLightPos0.xyz,any(_WorldSpaceLightPos0.xyz)));
    lightDirection = lerp(lightDirection, customLightDirection, _Is_BLD);
    //float3 lightColor = lerp(max(defaultLightColor,_LightColor0.rgb),max(defaultLightColor, saturate(_LightColor0.rgb)),_Is_Filter_LightColor);
    float3 lightColor = lerp(max(defaultLightColor,_LightColor0.rgb),max(defaultLightColor, clamp((_LightColor0.rgb), 0, 1)),_Is_Filter_LightColor);

#elif _IS_PASS_FWDDELTA
    float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
    float3 addPassLightColor = (0.5*dot(lerp( i.normalDir, normalDirection, _Is_NormalMapToBase ), lightDirection)+0.5) * _LightColor0.rgb * attenuation;
    float pureIntencity = max(0.001,(0.299*_LightColor0.r + 0.587*_LightColor0.g + 0.114*_LightColor0.b));
    float3 lightColor = max(0, lerp(addPassLightColor, lerp(0,min(addPassLightColor,addPassLightColor/pureIntencity),_WorldSpaceLightPos0.w),_Is_Filter_LightColor));
#endif

    float3 halfDirection = normalize(viewDirection + lightDirection);
    _Color = _BaseColor;

#ifdef _IS_PASS_FWDBASE
    float3 Set_LightColor = lightColor.rgb;
    float3 Set_BaseColor = lerp((_MainTex_var.rgb * _BaseColor.rgb), ((_MainTex_var.rgb * _BaseColor.rgb) * Set_LightColor), _Is_LightColor_Base);


    float4 _1st_ShadeMap_var = lerp(tex2D(_1st_ShadeMap, TRANSFORM_TEX(Set_UV0, _1st_ShadeMap)), _MainTex_var, _Use_BaseAs1st);
    float3 _Is_LightColor_1st_Shade_var = lerp((_1st_ShadeMap_var.rgb * _1st_ShadeColor.rgb), ((_1st_ShadeMap_var.rgb * _1st_ShadeColor.rgb) * Set_LightColor), _Is_LightColor_1st_Shade);
    float _HalfLambert_var = 0.5 * dot(lerp(i.normalDir, normalDirection, _Is_NormalMapToBase), lightDirection) + 0.5; // Half Lambert

    float4 _ShadingGradeMap_var = tex2Dlod(_ShadingGradeMap, float4(TRANSFORM_TEX(Set_UV0, _ShadingGradeMap), 0.0, _BlurLevelSGM));
    float _SystemShadowsLevel_var = (attenuation * 0.5) + 0.5 + _Tweak_SystemShadowsLevel > 0.001 ? (attenuation * 0.5) + 0.5 + _Tweak_SystemShadowsLevel : 0.0001;
    float _ShadingGradeMapLevel_var = _ShadingGradeMap_var.r < 0.95 ? _ShadingGradeMap_var.r + _Tweak_ShadingGradeMapLevel : 1;

    //float Set_ShadingGrade = saturate(_ShadingGradeMapLevel_var) * lerp(_HalfLambert_var, (_HalfLambert_var * saturate(_SystemShadowsLevel_var)), _Set_SystemShadowsToBase);
    float Set_ShadingGrade = clamp(_ShadingGradeMapLevel_var, 0, 1) * lerp(_HalfLambert_var, (_HalfLambert_var * saturate(_SystemShadowsLevel_var)), _Set_SystemShadowsToBase);
    // Base and 1st Shade Mask
    //float Set_FinalShadowMask = saturate((1.0 + ((Set_ShadingGrade - (_1st_ShadeColor_Step - _1st_ShadeColor_Feather)) * (0.0 - 1.0)) / (_1st_ShadeColor_Step - (_1st_ShadeColor_Step - _1st_ShadeColor_Feather))));
    float Set_FinalShadowMask = clamp((1.0 + ((Set_ShadingGrade - (_1st_ShadeColor_Step - _1st_ShadeColor_Feather)) * (0.0 - 1.0)) / (_1st_ShadeColor_Step - (_1st_ShadeColor_Step - _1st_ShadeColor_Feather))), 0,1);

    float3 _BaseColor_var = lerp(Set_BaseColor, _Is_LightColor_1st_Shade_var, Set_FinalShadowMask);
    float4 _2nd_ShadeMap_var = lerp(tex2D(_2nd_ShadeMap, TRANSFORM_TEX(Set_UV0, _2nd_ShadeMap)), _1st_ShadeMap_var, _Use_1stAs2nd);

    // 1st and 2nd Shades Mask
    //float Set_ShadeShadowMask = saturate((1.0 + ((Set_ShadingGrade - (_2nd_ShadeColor_Step - _2nd_ShadeColor_Feather)) * (0.0 - 1.0)) / (_2nd_ShadeColor_Step - (_2nd_ShadeColor_Step - _2nd_ShadeColor_Feather))));
    float Set_ShadeShadowMask = clamp((1.0 + ((Set_ShadingGrade - (_2nd_ShadeColor_Step - _2nd_ShadeColor_Feather)) * (0.0 - 1.0)) / (_2nd_ShadeColor_Step - (_2nd_ShadeColor_Step - _2nd_ShadeColor_Feather))), 0,1);
    float3 _2ndCol = (_2nd_ShadeMap_var.rgb * _2nd_ShadeColor.rgb);
    float3 _2ndColWithLight = _2ndCol * Set_LightColor;
    float3 Set_FinalBaseColor = lerp(_BaseColor_var, lerp(_Is_LightColor_1st_Shade_var, lerp(_2ndCol, _2ndColWithLight, _Is_LightColor_2nd_Shade), Set_ShadeShadowMask), Set_FinalShadowMask);

    // HighColor
    float4 _Set_HighColorMask_var = tex2D(_Set_HighColorMask,TRANSFORM_TEX(Set_UV0, _Set_HighColorMask));
    float4 _HighColor_Tex_var = tex2D(_HighColor_Tex,TRANSFORM_TEX(Set_UV0, _HighColor_Tex));
    float _Specular_var = 0.5*dot(halfDirection,lerp( i.normalDir, normalDirection, _Is_NormalMapToHighColor )) + 0.5;
    //float _TweakHighColorMask_var = (saturate((_Set_HighColorMask_var.g+_Tweak_HighColorMaskLevel))*lerp( (1.0 - step(_Specular_var,(1.0 - pow(_HighColor_Power,5)))), pow(_Specular_var,exp2(lerp(11,1,_HighColor_Power))), _Is_SpecularToHighColor ));
    float _TweakHighColorMask_var = (clamp((_Set_HighColorMask_var.g+_Tweak_HighColorMaskLevel), 0, 1) * lerp( (1.0 - step(_Specular_var,(1.0 - pow(_HighColor_Power,5)))), pow(_Specular_var,exp2(lerp(11,1,_HighColor_Power))), _Is_SpecularToHighColor ));
    float3 _HighColor_var = (lerp((_HighColor_Tex_var.rgb*_HighColor.rgb), ((_HighColor_Tex_var.rgb*_HighColor.rgb)*Set_LightColor), _Is_LightColor_HighColor )*_TweakHighColorMask_var);
    //float3 Set_HighColor = (lerp(saturate((Set_FinalBaseColor - _TweakHighColorMask_var)), Set_FinalBaseColor, lerp(_Is_BlendAddToHiColor,1.0,_Is_SpecularToHighColor) ) + lerp( _HighColor_var, (_HighColor_var*((1.0 - Set_FinalShadowMask) + (Set_FinalShadowMask*_TweakHighColorOnShadow))), _Is_UseTweakHighColorOnShadow ));
    float3 Set_HighColor = (lerp(clamp((Set_FinalBaseColor - _TweakHighColorMask_var), 0, 1), Set_FinalBaseColor, lerp(_Is_BlendAddToHiColor,1.0,_Is_SpecularToHighColor) ) + lerp( _HighColor_var, (_HighColor_var*((1.0 - Set_FinalShadowMask) + (Set_FinalShadowMask*_TweakHighColorOnShadow))), _Is_UseTweakHighColorOnShadow ));

    // RimLight
    float4 _Set_RimLightMask_var = tex2D(_Set_RimLightMask,TRANSFORM_TEX(Set_UV0, _Set_RimLightMask));
    float3 _Is_LightColor_RimLight_var = lerp( _RimLightColor.rgb, (_RimLightColor.rgb*Set_LightColor), _Is_LightColor_RimLight );
    float _RimArea_var = (1.0 - dot(lerp( i.normalDir, normalDirection, _Is_NormalMapToRimLight ),viewDirection));
    float _RimLightPower_var = pow(_RimArea_var,exp2(lerp(3,0,_RimLight_Power)));
    //float _Rimlight_InsideMask_var = saturate(lerp( (0.0 + ( (_RimLightPower_var - _RimLight_InsideMask) * (1.0 - 0.0) ) / (1.0 - _RimLight_InsideMask)), step(_RimLight_InsideMask,_RimLightPower_var), _RimLight_FeatherOff ));
    float _Rimlight_InsideMask_var = clamp((lerp( (0.0 + ( (_RimLightPower_var - _RimLight_InsideMask) * (1.0 - 0.0) ) / (1.0 - _RimLight_InsideMask)), step(_RimLight_InsideMask,_RimLightPower_var), _RimLight_FeatherOff )), 0, 1);
    float _VertHalfLambert_var = 0.5*dot(i.normalDir,lightDirection)+0.5;
    //float3 _LightDirection_MaskOn_var = lerp( (_Is_LightColor_RimLight_var*_Rimlight_InsideMask_var), (_Is_LightColor_RimLight_var*saturate((_Rimlight_InsideMask_var-((1.0 - _VertHalfLambert_var)+_Tweak_LightDirection_MaskLevel)))), _LightDirection_MaskOn );
    float3 _LightDirection_MaskOn_var = lerp( (_Is_LightColor_RimLight_var*_Rimlight_InsideMask_var), (_Is_LightColor_RimLight_var * clamp(((_Rimlight_InsideMask_var-((1.0 - _VertHalfLambert_var)+_Tweak_LightDirection_MaskLevel))), 0, 1)), _LightDirection_MaskOn );
    float _ApRimLightPower_var = pow(_RimArea_var,exp2(lerp(3,0,_Ap_RimLight_Power)));
    //float3 Set_RimLight = (saturate((_Set_RimLightMask_var.g+_Tweak_RimLightMaskLevel))*lerp( _LightDirection_MaskOn_var, (_LightDirection_MaskOn_var+(lerp( _Ap_RimLightColor.rgb, (_Ap_RimLightColor.rgb*Set_LightColor), _Is_LightColor_Ap_RimLight )*saturate((lerp( (0.0 + ( (_ApRimLightPower_var - _RimLight_InsideMask) * (1.0 - 0.0) ) / (1.0 - _RimLight_InsideMask)), step(_RimLight_InsideMask,_ApRimLightPower_var), _Ap_RimLight_FeatherOff )-(saturate(_VertHalfLambert_var)+_Tweak_LightDirection_MaskLevel))))), _Add_Antipodean_RimLight ));
    float3 Set_RimLight = (clamp((_Set_RimLightMask_var.g+_Tweak_RimLightMaskLevel), 0,1)
        * lerp( _LightDirection_MaskOn_var, (_LightDirection_MaskOn_var+(lerp( _Ap_RimLightColor.rgb, (_Ap_RimLightColor.rgb*Set_LightColor), _Is_LightColor_Ap_RimLight )
        * clamp(((lerp( (0.0 + ( (_ApRimLightPower_var - _RimLight_InsideMask) * (1.0 - 0.0) ) / (1.0 - _RimLight_InsideMask)), step(_RimLight_InsideMask,_ApRimLightPower_var), _Ap_RimLight_FeatherOff )
        - (clamp(_VertHalfLambert_var, 0, 1) + _Tweak_LightDirection_MaskLevel))), 0, 1))), _Add_Antipodean_RimLight ));
    float3 _RimLight_var = lerp( Set_HighColor, (Set_HighColor+Set_RimLight), _RimLight );

    fixed _sign_Mirror = i.mirrorFlag;
    float3 _Camera_Right = UNITY_MATRIX_V[0].xyz;
    float3 _Camera_Front = UNITY_MATRIX_V[2].xyz;
    float3 _Up_Unit = float3(0, 1, 0);
    float3 _Right_Axis = cross(_Camera_Front, _Up_Unit);
    if (_sign_Mirror < 0)
    {
        _Right_Axis = -1 * _Right_Axis;
        _Rotate_MatCapUV = -1 * _Rotate_MatCapUV;
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
    float _Rot_MatCapUV_var_ang = (_Rotate_MatCapUV * PI) - _Camera_Dir * _Camera_Roll * _CameraRolling_Stabilizer;
    float2 _Rot_MatCapNmUV_var = RotateUV(Set_UV0, (_Rotate_NormalMapForMatCapUV * PI), float2(0.5, 0.5), 1.0);

    //float3 _NormalMapForMatCap_var = UnpackScaleNormal(tex2D(_NormalMapForMatCap, TRANSFORM_TEX(_Rot_MatCapNmUV_var, _NormalMapForMatCap)), _BumpScaleMatcap);
    float3 _NormalMapForMatCap_var = UnpackNormal(tex2D(_NormalMapForMatCap, _Rot_MatCapNmUV_var));
    _NormalMapForMatCap_var.xy *= _BumpScaleMatcap;
    float3 viewNormal = (mul(UNITY_MATRIX_V, float4(lerp(i.normalDir, mul(_NormalMapForMatCap_var.rgb, tangentTransform).rgb, _Is_NormalMapForMatCap), 0))).rgb;
    float3 NormalBlend_MatcapUV_Detail = viewNormal.rgb * float3(-1, -1, 1);
    float3 NormalBlend_MatcapUV_Base = (mul(UNITY_MATRIX_V, float4(viewDirection, 0)).rgb * float3(-1, -1, 1)) + float3(0, 0, 1);
    float3 noSknewViewNormal = NormalBlend_MatcapUV_Base * dot(NormalBlend_MatcapUV_Base, NormalBlend_MatcapUV_Detail) / NormalBlend_MatcapUV_Base.b - NormalBlend_MatcapUV_Detail;
    float2 _ViewNormalAsMatCapUV = (lerp(noSknewViewNormal, viewNormal, _Is_Ortho).rg * 0.5) + 0.5;
    float2 _Rot_MatCapUV_var = RotateUV((0.0 + ((_ViewNormalAsMatCapUV - (0.0 + _Tweak_MatCapUV)) * (1.0 - 0.0)) / ((1.0 - _Tweak_MatCapUV) - (0.0 + _Tweak_MatCapUV))), _Rot_MatCapUV_var_ang, float2(0.5, 0.5), 1.0);
    if (_sign_Mirror < 0)
    {
        _Rot_MatCapUV_var.x = 1 - _Rot_MatCapUV_var.x;
    }
    else
    {
        _Rot_MatCapUV_var = _Rot_MatCapUV_var;
    }

    float4 _MatCap_Sampler_var = tex2Dlod(_MatCap_Sampler, float4(TRANSFORM_TEX(_Rot_MatCapUV_var, _MatCap_Sampler), 0.0, _BlurLevelMatcap));
    float4 _Set_MatcapMask_var = tex2D(_Set_MatcapMask, TRANSFORM_TEX(Set_UV0, _Set_MatcapMask));
    //float _Tweak_MatcapMaskLevel_var = saturate(lerp(_Set_MatcapMask_var.g, (1.0 - _Set_MatcapMask_var.g), _Inverse_MatcapMask) + _Tweak_MatcapMaskLevel);
    float l = lerp(_Set_MatcapMask_var.g, (1.0 - _Set_MatcapMask_var.g), _Inverse_MatcapMask);
    float _Tweak_MatcapMaskLevel_var = clamp(l, 0, 1) + _Tweak_MatcapMaskLevel;
    float3 _Is_LightColor_MatCap_var = lerp((_MatCap_Sampler_var.rgb * _MatCapColor.rgb), ((_MatCap_Sampler_var.rgb * _MatCapColor.rgb) * Set_LightColor), _Is_LightColor_MatCap);
    float3 Set_MatCap = lerp(_Is_LightColor_MatCap_var, (_Is_LightColor_MatCap_var * ((1.0 - Set_FinalShadowMask) + (Set_FinalShadowMask * _TweakMatCapOnShadow)) + lerp(Set_FinalBaseColor * Set_FinalShadowMask * (1.0 - _TweakMatCapOnShadow), float3(0.0, 0.0, 0.0), _Is_BlendAddToMatCap)), _Is_UseTweakMatCapOnShadow);
    float3 matCapColorOnAddMode = _RimLight_var + Set_MatCap * _Tweak_MatcapMaskLevel_var;
    float _Tweak_MatcapMaskLevel_var_MultiplyMode = _Tweak_MatcapMaskLevel_var * lerp(1, (1 - (Set_FinalShadowMask) * (1 - _TweakMatCapOnShadow)), _Is_UseTweakMatCapOnShadow);
    float3 matCapColorOnMultiplyMode = Set_FinalBaseColor * (1 - _Tweak_MatcapMaskLevel_var_MultiplyMode) + Set_FinalBaseColor * Set_MatCap * _Tweak_MatcapMaskLevel_var_MultiplyMode + lerp(float3(0, 0, 0), Set_RimLight, _RimLight);
    float3 matCapColorFinal = lerp(matCapColorOnMultiplyMode, matCapColorOnAddMode, _Is_BlendAddToMatCap);
    float3 finalColor = lerp(_RimLight_var, matCapColorFinal, _MatCap);
    float3 envLightColor = DecodeLightProbe(normalDirection) < float3(1, 1, 1) ? DecodeLightProbe(normalDirection) : float3(1, 1, 1);
    float envLightIntensity = 0.299 * envLightColor.r + 0.587 * envLightColor.g + 0.114 * envLightColor.b < 1 ? (0.299 * envLightColor.r + 0.587 * envLightColor.g + 0.114 * envLightColor.b) : 1;
    //finalColor = saturate(finalColor) + (envLightColor * envLightIntensity * _GI_Intensity * smoothstep(1, 0, envLightIntensity / 2));
    finalColor = clamp(finalColor, 0, 1) + (envLightColor * envLightIntensity * _GI_Intensity * smoothstep(1, 0, envLightIntensity / 2));

#elif _IS_PASS_FWDDELTA
    _1st_ShadeColor_Step = saturate(_1st_ShadeColor_Step + _StepOffset);
    _2nd_ShadeColor_Step = saturate(_2nd_ShadeColor_Step + _StepOffset);
    float _LightIntensity = lerp(0,(0.299*_LightColor0.r + 0.587*_LightColor0.g + 0.114*_LightColor0.b)*attenuation,_WorldSpaceLightPos0.w) ;
    float3 Set_LightColor = lerp(lightColor,lerp(lightColor,min(lightColor,_LightColor0.rgb*attenuation*_1st_ShadeColor_Step),_WorldSpaceLightPos0.w),_Is_Filter_HiCutPointLightColor);
    float3 Set_BaseColor = lerp( (_MainTex_var.rgb*_BaseColor.rgb*_LightIntensity), ((_MainTex_var.rgb*_BaseColor.rgb)*Set_LightColor), _Is_LightColor_Base );
    float4 _1st_ShadeMap_var = lerp(tex2D(_1st_ShadeMap,TRANSFORM_TEX(Set_UV0, _1st_ShadeMap)),_MainTex_var,_Use_BaseAs1st);
    float3 _Is_LightColor_1st_Shade_var = lerp( (_1st_ShadeMap_var.rgb*_1st_ShadeColor.rgb*_LightIntensity), ((_1st_ShadeMap_var.rgb*_1st_ShadeColor.rgb)*Set_LightColor), _Is_LightColor_1st_Shade );
    float _HalfLambert_var = 0.5*dot(lerp( i.normalDir, normalDirection, _Is_NormalMapToBase ),lightDirection)+0.5; // Half Lambert
    float4 _ShadingGradeMap_var = tex2Dlod(_ShadingGradeMap,float4(TRANSFORM_TEX(Set_UV0, _ShadingGradeMap),0.0,_BlurLevelSGM));
    float _ShadingGradeMapLevel_var = _ShadingGradeMap_var.r < 0.95 ? _ShadingGradeMap_var.r+_Tweak_ShadingGradeMapLevel : 1;
    float Set_ShadingGrade = saturate(_ShadingGradeMapLevel_var)*lerp( _HalfLambert_var, (_HalfLambert_var*saturate(1.0+_Tweak_SystemShadowsLevel)), _Set_SystemShadowsToBase );
    float Set_FinalShadowMask = saturate((1.0 + ( (Set_ShadingGrade - (_1st_ShadeColor_Step-_1st_ShadeColor_Feather)) * (0.0 - 1.0) ) / (_1st_ShadeColor_Step - (_1st_ShadeColor_Step-_1st_ShadeColor_Feather)))); // Base and 1st Shade Mask
    float3 _BaseColor_var = lerp(Set_BaseColor,_Is_LightColor_1st_Shade_var,Set_FinalShadowMask);
    float4 _2nd_ShadeMap_var = lerp(tex2D(_2nd_ShadeMap,TRANSFORM_TEX(Set_UV0, _2nd_ShadeMap)),_1st_ShadeMap_var,_Use_1stAs2nd);
    float Set_ShadeShadowMask = saturate((1.0 + ( (Set_ShadingGrade - (_2nd_ShadeColor_Step-_2nd_ShadeColor_Feather)) * (0.0 - 1.0) ) / (_2nd_ShadeColor_Step - (_2nd_ShadeColor_Step-_2nd_ShadeColor_Feather)))); // 1st and 2nd Shades Mask
    float3 finalColor = lerp(_BaseColor_var,lerp(_Is_LightColor_1st_Shade_var,lerp( (_2nd_ShadeMap_var.rgb*_2nd_ShadeColor.rgb*_LightIntensity), ((_2nd_ShadeMap_var.rgb*_2nd_ShadeColor.rgb)*Set_LightColor), _Is_LightColor_2nd_Shade ),Set_ShadeShadowMask),Set_FinalShadowMask);
    float4 _Set_HighColorMask_var = tex2D(_Set_HighColorMask,TRANSFORM_TEX(Set_UV0, _Set_HighColorMask));
    float _Specular_var = 0.5*dot(halfDirection,lerp( i.normalDir, normalDirection, _Is_NormalMapToHighColor ))+0.5; // Specular
    float _TweakHighColorMask_var = (saturate((_Set_HighColorMask_var.g+_Tweak_HighColorMaskLevel))*lerp( (1.0 - step(_Specular_var,(1.0 - pow(_HighColor_Power,5)))), pow(_Specular_var,exp2(lerp(11,1,_HighColor_Power))), _Is_SpecularToHighColor ));
    float4 _HighColor_Tex_var = tex2D(_HighColor_Tex,TRANSFORM_TEX(Set_UV0, _HighColor_Tex));
    float3 _HighColor_var = (lerp( (_HighColor_Tex_var.rgb*_HighColor.rgb), ((_HighColor_Tex_var.rgb*_HighColor.rgb)*Set_LightColor), _Is_LightColor_HighColor )*_TweakHighColorMask_var);
    finalColor = finalColor + lerp(lerp( _HighColor_var, (_HighColor_var*((1.0 - Set_FinalShadowMask)+(Set_FinalShadowMask*_TweakHighColorOnShadow))), _Is_UseTweakHighColorOnShadow ),float3(0,0,0),_Is_Filter_HiCutPointLightColor);
    finalColor = saturate(finalColor);
#endif

#ifdef _IS_TRANSCLIPPING_OFF
    #ifdef _IS_PASS_FWDBASE
	    finalRGBA.rgb = finalColor;
    #elif _IS_PASS_FWDDELTA
	    finalRGBA.rgb = finalColor;
    #endif
#elif _IS_TRANSCLIPPING_ON
	//float Set_Opacity = saturate((_Inverse_Clipping_var+_Tweak_transparency));
	float Set_Opacity = clamp((_Inverse_Clipping_var+_Tweak_transparency), 0, 1);
    #ifdef _IS_PASS_FWDBASE
	    finalRGBA.rgb = finalColor;
        finalRGBA.a = Set_Opacity;
    #elif _IS_PASS_FWDDELTA
	    finalRGBA.rgb = finalColor * Set_Opacity;
        finalRGBA.a = 0;
    #endif
#endif

    UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
    return finalRGBA;
}
