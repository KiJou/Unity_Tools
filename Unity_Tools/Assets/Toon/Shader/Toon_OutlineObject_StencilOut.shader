﻿
Shader "UnityChanToonShader/Helper/Toon_OutlineObject_StencilOut" 
{
    Properties 
    {
        [HideInInspector] _simpleUI ("SimpleUI", Int ) = 0
        [HideInInspector] _utsVersion ("Version", Float ) = 2.07
        [HideInInspector] _utsTechnique ("Technique", int ) = 2
        _StencilNo ("Stencil No", int) =1
        _MainTex ("BaseMap", 2D) = "white" {}
        _BaseColor ("BaseColor", Color) = (1,1,1,1)
        [HideInInspector] _Color ("Color", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_Base ("Is_LightColor_Base", Float ) = 1
//Outline
        [KeywordEnum(NML,POS)] _OUTLINE("OUTLINE MODE", Float) = 0
        _Outline_Width ("Outline_Width", Float ) = 0
        _Farthest_Distance ("Farthest_Distance", Float ) = 100
        _Nearest_Distance ("Nearest_Distance", Float ) = 0.5
        _Outline_Sampler ("Outline_Sampler", 2D) = "white" {}
        _Outline_Color ("Outline_Color", Color) = (0.5,0.5,0.5,1)
        [MaterialToggle] _Is_BlendBaseColor ("Is_BlendBaseColor", Float ) = 0
        [MaterialToggle] _Is_OutlineTex ("Is_OutlineTex", Float ) = 0
        _OutlineTex ("OutlineTex", 2D) = "white" {}
        _Offset_Z ("Offset_Camera_Z", Float) = 0
        [MaterialToggle] _Is_BakedNormal ("Is_BakedNormal", Float ) = 0
        _BakedNormal ("Baked Normal for Outline", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"    //StencilOut Opaque and _Clipping
            "RenderType"="Opaque"
        }
        Pass {
            Name "Outline"
            Tags {
            }
            Cull Front
            
            Stencil {
                Ref[_StencilNo]
                Comp NotEqual
                Pass Keep
                Fail Keep
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal vulkan xboxone ps4 switch
            #pragma target 3.0
            #pragma multi_compile _IS_OUTLINE_CLIPPING_NO 
            #pragma multi_compile _OUTLINE_NML _OUTLINE_POS
            #include "UCTS_Outline.cginc"
            ENDCG
        }
    }
    FallBack "Legacy Shaders/VertexLit"
}
