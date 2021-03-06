﻿
Shader "UnityChanToonShader/Toon_DoubleShadeWithFeather_TransClipping_StencilMask" 
{
    Properties 
    {
        [HideInInspector] _simpleUI ("SimpleUI", Int ) = 0
        [HideInInspector] _utsVersion ("Version", Float ) = 2.07
        [HideInInspector] _utsTechnique ("Technique", int ) = 0
        _StencilNo ("Stencil No", int) =1
        [Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull Mode", int) = 2 
        _ClippingMask ("ClippingMask", 2D) = "white" {}
        [MaterialToggle] _IsBaseMapAlphaAsClippingMask ("IsBaseMapAlphaAsClippingMask", Float ) = 0
        [MaterialToggle] _Inverse_Clipping ("Inverse_Clipping", Float ) = 0
        _Clipping_Level ("Clipping_Level", Range(0, 1)) = 0
        _Tweak_transparency ("Tweak_transparency", Range(-1, 1)) = 0
        _MainTex ("BaseMap", 2D) = "white" {}
        _BaseColor ("BaseColor", Color) = (1,1,1,1)
        _BlendTex("BlendMap", 2D) = "white" {}
        _BlendWeight("BlendWeight", Range(0, 1)) = 0
        _TexScale("Scale of Tex", float) = 1.0
        _TexRatio("Ratio of Tex", float) = 1.0
        _PlaneScale("Scale of Plane Mesh", Vector) = (1, 1, 0, 0)

        [HideInInspector] _Color ("Color", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_Base ("Is_LightColor_Base", Float ) = 1
        _1st_ShadeMap ("1st_ShadeMap", 2D) = "white" {}
        [MaterialToggle] _Use_BaseAs1st ("Use BaseMap as 1st_ShadeMap", Float ) = 0
        _1st_ShadeColor ("1st_ShadeColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_1st_Shade ("Is_LightColor_1st_Shade", Float ) = 1
        _2nd_ShadeMap ("2nd_ShadeMap", 2D) = "white" {}
        [MaterialToggle] _Use_1stAs2nd ("Use 1st_ShadeMap as 2nd_ShadeMap", Float ) = 0
        _2nd_ShadeColor ("2nd_ShadeColor", Color) = (1,1,1,1)

        _BlurMap("BlurMap", 2D) = "white"{}
        _BlurMapColor ("BlurColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_2nd_Shade ("Is_LightColor_2nd_Shade", Float ) = 1
        _NormalMap ("NormalMap", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 1)) = 1
        [MaterialToggle] _Is_NormalMapToBase ("Is_NormalMapToBase", Float ) = 0
        [MaterialToggle] _Set_SystemShadowsToBase ("Set_SystemShadowsToBase", Float ) = 1
        _Tweak_SystemShadowsLevel ("Tweak_SystemShadowsLevel", Range(-0.5, 0.5)) = 0
        _BaseColor_Step ("BaseColor_Step", Range(0, 1)) = 0.5
        _BaseShade_Feather ("Base/Shade_Feather", Range(0.0001, 1)) = 0.0001
        _ShadeColor_Step ("ShadeColor_Step", Range(0, 1)) = 0
        _1st2nd_Shades_Feather ("1st/2nd_Shades_Feather", Range(0.0001, 1)) = 0.0001
        [HideInInspector] _1st_ShadeColor_Step ("1st_ShadeColor_Step", Range(0, 1)) = 0.5
        [HideInInspector] _1st_ShadeColor_Feather ("1st_ShadeColor_Feather", Range(0.0001, 1)) = 0.0001
        [HideInInspector] _2nd_ShadeColor_Step ("2nd_ShadeColor_Step", Range(0, 1)) = 0
        [HideInInspector] _2nd_ShadeColor_Feather ("2nd_ShadeColor_Feather", Range(0.0001, 1)) = 0.0001
        _StepOffset ("Step_Offset (ForwardAdd Only)", Range(-0.5, 0.5)) = 0
        [MaterialToggle] _Is_Filter_HiCutPointLightColor ("PointLights HiCut_Filter (ForwardAdd Only)", Float ) = 1
        _Set_1st_ShadePosition ("Set_1st_ShadePosition", 2D) = "white" {}
        _Set_2nd_ShadePosition ("Set_2nd_ShadePosition", 2D) = "white" {}

        _HighColor("HighColor", Color) = (0,0,0,1)
        _HighColor_Tex("HighColor_Tex", 2D) = "white" {}
        [MaterialToggle] _Is_LightColor_HighColor("Is_LightColor_HighColor", Float) = 1
        [MaterialToggle] _Is_NormalMapToHighColor("Is_NormalMapToHighColor", Float) = 0
        _HighColor_Power("HighColor_Power", Range(0, 1)) = 0
        [MaterialToggle] _Is_SpecularToHighColor("Is_SpecularToHighColor", Float) = 0
        [MaterialToggle] _Is_BlendAddToHiColor("Is_BlendAddToHiColor", Float) = 0
        [MaterialToggle] _Is_UseTweakHighColorOnShadow("Is_UseTweakHighColorOnShadow", Float) = 0
        _TweakHighColorOnShadow("TweakHighColorOnShadow", Range(0, 1)) = 0
        _Set_HighColorMask("Set_HighColorMask", 2D) = "white" {}
        _Tweak_HighColorMaskLevel("Tweak_HighColorMaskLevel", Range(-1, 1)) = 0

        [MaterialToggle] _RimLight ("RimLight", Float ) = 0
        _RimLightColor ("RimLightColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_RimLight ("Is_LightColor_RimLight", Float ) = 1
        [MaterialToggle] _Is_NormalMapToRimLight ("Is_NormalMapToRimLight", Float ) = 0
        _RimLight_Power ("RimLight_Power", Range(0, 1)) = 0.1
        _RimLight_InsideMask ("RimLight_InsideMask", Range(0.0001, 1)) = 0.0001
        [MaterialToggle] _RimLight_FeatherOff ("RimLight_FeatherOff", Float ) = 0
        [MaterialToggle] _LightDirection_MaskOn ("LightDirection_MaskOn", Float ) = 0
        _Tweak_LightDirection_MaskLevel ("Tweak_LightDirection_MaskLevel", Range(0, 0.5)) = 0
        [MaterialToggle] _Add_Antipodean_RimLight ("Add_Antipodean_RimLight", Float ) = 0
        _Ap_RimLightColor ("Ap_RimLightColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_Ap_RimLight ("Is_LightColor_Ap_RimLight", Float ) = 1
        _Ap_RimLight_Power ("Ap_RimLight_Power", Range(0, 1)) = 0.1
        [MaterialToggle] _Ap_RimLight_FeatherOff ("Ap_RimLight_FeatherOff", Float ) = 0
        _Set_RimLightMask ("Set_RimLightMask", 2D) = "white" {}
        _Tweak_RimLightMaskLevel ("Tweak_RimLightMaskLevel", Range(-1, 1)) = 0

        [MaterialToggle] _MatCap ("MatCap", Float ) = 0
        _MatCap_Sampler ("MatCap_Sampler", 2D) = "black" {}
        _BlurLevelMatcap ("Blur Level of MatCap_Sampler", Range(0, 10)) = 0
        _MatCapColor ("MatCapColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_MatCap ("Is_LightColor_MatCap", Float ) = 1
        [MaterialToggle] _Is_BlendAddToMatCap ("Is_BlendAddToMatCap", Float ) = 1
        _Tweak_MatCapUV ("Tweak_MatCapUV", Range(-0.5, 0.5)) = 0
        _Rotate_MatCapUV ("Rotate_MatCapUV", Range(-1, 1)) = 0
        [MaterialToggle] _CameraRolling_Stabilizer ("Activate CameraRolling_Stabilizer", Float ) = 0
        [MaterialToggle] _Is_NormalMapForMatCap ("Is_NormalMapForMatCap", Float ) = 0
        _NormalMapForMatCap ("NormalMapForMatCap", 2D) = "bump" {}
        _BumpScaleMatcap ("Scale for NormalMapforMatCap", Range(0, 1)) = 1
        _Rotate_NormalMapForMatCapUV ("Rotate_NormalMapForMatCapUV", Range(-1, 1)) = 0
        [MaterialToggle] _Is_UseTweakMatCapOnShadow ("Is_UseTweakMatCapOnShadow", Float ) = 0
        _TweakMatCapOnShadow ("TweakMatCapOnShadow", Range(0, 1)) = 0
        _Set_MatcapMask ("Set_MatcapMask", 2D) = "white" {}
        _Tweak_MatcapMaskLevel ("Tweak_MatcapMaskLevel", Range(-1, 1)) = 0
        [MaterialToggle] _Inverse_MatcapMask ("Inverse_MatcapMask", Float ) = 0
        [MaterialToggle] _Is_Ortho ("Orthographic Projection for MatCap", Float ) = 0

        [KeywordEnum(NML,POS)] _OUTLINE("OUTLINE MODE", Float) = 0
        _Outline_Width ("Outline_Width", Float ) = 0
        _Farthest_Distance ("Farthest_Distance", Float ) = 100
        _Nearest_Distance ("Nearest_Distance", Float ) = 0.5
        _Outline_Sampler ("Outline_Sampler", 2D) = "white" {}
        _Outline_Color ("Outline_Color", Color) = (0.5,0.5,0.5,1)
        [MaterialToggle] _Is_BlendBaseColor ("Is_BlendBaseColor", Float ) = 0
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        [MaterialToggle] _Is_OutlineTex ("Is_OutlineTex", Float ) = 0
        _OutlineTex ("OutlineTex", 2D) = "white" {}
        _Offset_Z ("Offset_Camera_Z", Float) = 0
        [MaterialToggle] _Is_BakedNormal ("Is_BakedNormal", Float ) = 0
        _BakedNormal ("Baked Normal for Outline", 2D) = "white" {}
        _GI_Intensity ("GI_Intensity", Range(0, 1)) = 0
        _Unlit_Intensity ("Unlit_Intensity", Range(0.001, 4)) = 1
        [MaterialToggle] _Is_Filter_LightColor ("VRChat : SceneLights HiCut_Filter", Float ) = 0
        [MaterialToggle] _Is_BLD ("Advanced : Activate Built-in Light Direction", Float ) = 0
        _Offset_X_Axis_BLD (" Offset X-Axis (Built-in Light Direction)", Range(-1, 1)) = -0.05
        _Offset_Y_Axis_BLD (" Offset Y-Axis (Built-in Light Direction)", Range(-1, 1)) = 0.09
        [MaterialToggle] _Inverse_Z_Axis_BLD (" Inverse Z-Axis (Built-in Light Direction)", Float ) = 1

        [Header(LineEffect)]
        _ShieldColor("ShieldColor", Color) = (1, 1, 1, 0)
        _NoiseTex("NoiseTexture", 2D) = "white" {}
        _Edge("Edge Intensity", Range(0.001, 10)) = 2
        _AnimationSpeed("AnimationSpeed", Range(-50.0, 50.0)) = 0.0
        _OffsetY("OffsetY", Range(-1.0, 0.5)) = -0.5
        _Fraction("Fraction", Range(0.0, 2.0)) = 0.04
        _WaveAmount("WaveAmount", Range(0.0, 100.0)) = 6
        _RimIntensity("RimIntensity", Range(0.0, 1.0)) = 1
    }

    SubShader {
        Tags {
            "Queue"="AlphaTest-1"    //StencilMask Opaque and _Clipping
            "RenderType"="TransparentCutout"
        }
        Pass {
            Name "Outline"
            Tags {
            }
            Cull Front
            //v.2.0.4
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil {
                Ref[_StencilNo]
                Comp Always
                Pass Replace
                Fail Replace
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal vulkan xboxone ps4 switch
            #pragma target 3.0
            #pragma multi_compile _IS_OUTLINE_CLIPPING_YES 
            #pragma multi_compile _OUTLINE_NML _OUTLINE_POS
            #include "UCTS_Outline.cginc"
            ENDCG
        }

        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull[_CullMode]
            
            Stencil {
                Ref[_StencilNo]
                Comp Always
                Pass Replace
                Fail Replace
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal vulkan xboxone ps4 switch
            #pragma target 3.0
            #pragma multi_compile _IS_CLIPPING_TRANSMODE
            #pragma multi_compile _IS_PASS_FWDBASE
            #include "UCTS_DoubleShadeWithFeather.cginc"
            ENDCG
        }

        Pass
        {
            Name "LINE_EFFECT"

            Tags {
                "RenderType" = "TransparentCutout"
                "Queue" = "AlphaTest"
            }

            Cull Off
            ZWrite Off
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UCTS_LineEffect.cginc"
            ENDCG
        }
    }
    FallBack "Legacy Shaders/VertexLit"
    CustomEditor "UnityChan.UTS2GUI"
}
