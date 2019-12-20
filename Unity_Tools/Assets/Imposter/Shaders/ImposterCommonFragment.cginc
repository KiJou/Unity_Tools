#ifndef IMPOSTERCOMMONFRAGMENT_CGINC
#define IMPOSTERCOMMONFRAGMENT_CGINC

sampler2D _ImposterBaseTex;
sampler2D _ImposterWorldNormalDepthTex;
float4 _ImposterBaseTex_TexelSize;

half _ImposterFrames;
half _ImposterSize;
half3 _ImposterOffset;
half _ImposterFullSphere;
half _ImposterBorderClamp;


struct v2f
{
    half2 uv : TEXCOORD0;
    half2 grid : TEXCOORD2;
    half4 frame0 : TEXCOORD3;
    half4 frame1 : TEXCOORD4;
    half4 frame2 : TEXCOORD5;
    half4 vertex : SV_POSITION;
};

struct Ray
{
    half3 Origin;
    half3 Direction;
};

half2 VirtualPlaneUV( half3 planeNormal, half3 planeX, half3 planeZ, half3 center, half2 uvScale, Ray rayLocal )
{
    half normalDotOrigin = dot(planeNormal,rayLocal.Origin);
    half normalDotCenter = dot(planeNormal,center);
    half normalDotRay = dot(planeNormal,rayLocal.Direction);
    
    half planeDistance = normalDotOrigin-normalDotCenter;
    planeDistance *= -1.0;
    
    half intersect = planeDistance / normalDotRay;
    
    half3 intersection = ((rayLocal.Direction * intersect) + rayLocal.Origin) - center;
    
    half dx = dot(planeX,intersection);
    half dz = dot(planeZ,intersection);
    
    half2 uv = half2(0,0);
    
    if ( intersect > 0 )
    {
        uv = half2(dx,dz);
    }
    else
    {
        uv = half2(0,0);
    }   
    uv /= uvScale;
    uv += half2(0.5,0.5);
    return uv;
}


void ImposterVertex(inout v2f imp )
{
    //incoming vertex, object space
    half4 vertex = imp.vertex;
    
    //camera in object space
    half3 objectSpaceCameraPos = mul( unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz,1) ).xyz;
    half2 texcoord = imp.uv;
    float4x4 objectToWorld = unity_ObjectToWorld;
    float4x4 worldToObject = unity_WorldToObject;

    half3 imposterPivotOffset = _ImposterOffset.xyz;
    half framesMinusOne = _ImposterFrames-1;
    
    float3 objectScale = float3(
        length(float3(objectToWorld[0].x, objectToWorld[1].x, objectToWorld[2].x)),      
        length(float3(objectToWorld[0].y, objectToWorld[1].y, objectToWorld[2].y)),
        length(float3(objectToWorld[0].z, objectToWorld[1].z, objectToWorld[2].z)));
             
    float3 pivotToCameraRay = normalize(objectSpaceCameraPos.xyz-imposterPivotOffset.xyz);
    texcoord = half2(texcoord.x,texcoord.y)*(1.0/_ImposterFrames.x); 
    half2 size = _ImposterSize.xx * 2.0;
    
    half3 projected = SpriteProjection( pivotToCameraRay, _ImposterFrames, size, texcoord.xy );
    half3 vertexOffset = projected + imposterPivotOffset;
    vertexOffset = normalize(objectSpaceCameraPos-vertexOffset);
    vertexOffset += projected;
    vertexOffset -= vertex.xyz;
    vertexOffset += imposterPivotOffset;

    half3 rayDirectionLocal = (imposterPivotOffset + projected) - objectSpaceCameraPos;                 
    half3 projInterpolated = normalize( objectSpaceCameraPos - (projected + imposterPivotOffset) ); 
    
    Ray rayLocal;
    rayLocal.Origin = objectSpaceCameraPos-imposterPivotOffset; 
    rayLocal.Direction = rayDirectionLocal; 
    
    half2 grid = VectorToGrid(pivotToCameraRay, _ImposterFullSphere);
    half2 gridRaw = grid;
    grid = saturate((grid+1.0)*0.5);
    grid *= framesMinusOne;    

    half2 gridFrac = frac(grid);    
    half2 gridFloor = floor(grid);   
    half4 weights = TriangleInterpolate( gridFrac ); 
    
    half2 frame0 = gridFloor;
    half2 frame1 = gridFloor + lerp(half2(0,1),half2(1,0),weights.w);
    half2 frame2 = gridFloor + half2(1,1);    
    half3 frame0ray = FrameXYToRay(frame0, framesMinusOne.xx, _ImposterFullSphere);
    half3 frame1ray = FrameXYToRay(frame1, framesMinusOne.xx, _ImposterFullSphere);
    half3 frame2ray = FrameXYToRay(frame2, framesMinusOne.xx, _ImposterFullSphere);
    
    half3 planeCenter = half3(0,0,0);    
    half3 plane0x;
    half3 plane0normal = frame0ray;
    half3 plane0z;
    half3 frame0local = FrameTransform( projInterpolated, frame0ray, plane0x, plane0z );
    frame0local.xz = frame0local.xz/_ImposterFrames.xx;
    
    half2 vUv0 = VirtualPlaneUV( plane0normal, plane0x, plane0z, planeCenter, size, rayLocal );
    vUv0 /= _ImposterFrames.xx;   
    
    half3 plane1x; 
    half3 plane1normal = frame1ray;
    half3 plane1z;
    half3 frame1local = FrameTransform( projInterpolated, frame1ray, plane1x, plane1z);
    frame1local.xz = frame1local.xz/_ImposterFrames.xx;
    
    half2 vUv1 = VirtualPlaneUV( plane1normal, plane1x, plane1z, planeCenter, size, rayLocal );
    vUv1 /= _ImposterFrames.xx;
    
    half3 plane2x;
    half3 plane2normal = frame2ray;
    half3 plane2z;
    half3 frame2local = FrameTransform( projInterpolated, frame2ray, plane2x, plane2z );
    frame2local.xz = frame2local.xz/_ImposterFrames.xx;
    
    half2 vUv2 = VirtualPlaneUV( plane2normal, plane2x, plane2z, planeCenter, size, rayLocal );
    vUv2 /= _ImposterFrames.xx;
    
    imp.vertex.xyz += vertexOffset;
    imp.uv = texcoord;
    imp.grid = grid;
    imp.frame0 = half4(vUv0.xy, frame0local.xz);
    imp.frame1 = half4(vUv1.xy, frame1local.xz);
    imp.frame2 = half4(vUv2.xy, frame2local.xz);
}

void ImposterSample( in v2f imp, out half4 baseTex, out half4 worldNormal )
{
    half2 fracGrid = frac(imp.grid);
    half4 weights = TriangleInterpolate( fracGrid );      
    half2 gridSnap = floor(imp.grid) / _ImposterFrames.xx;        
    half2 frame0 = gridSnap;
    half2 frame1 = gridSnap + (lerp(half2(0,1),half2(1,0),weights.w)/_ImposterFrames.xx);
    half2 frame2 = gridSnap + (half2(1,1)/_ImposterFrames.xx);    
    half2 vp0uv = frame0 + imp.frame0.xy;
    half2 vp1uv = frame1 + imp.frame1.xy; 
    half2 vp2uv = frame2 + imp.frame2.xy;
   
    float textureDims = _ImposterBaseTex_TexelSize.z;
    float frameSize = textureDims/_ImposterFrames; 
    float actualDims = floor(frameSize) * _ImposterFrames; 
    float scaleFactor = actualDims / textureDims;
   
    vp0uv *= scaleFactor;
    vp1uv *= scaleFactor;
    vp2uv *= scaleFactor;
   
    half2 gridSize = 1.0/_ImposterFrames.xx;
    gridSize *= _ImposterBaseTex_TexelSize.zw;
    gridSize *= _ImposterBaseTex_TexelSize.xy;
    float2 border = _ImposterBaseTex_TexelSize.xy*_ImposterBorderClamp;
    
    //for parallax modify
    half4 n0 = tex2Dlod( _ImposterWorldNormalDepthTex, half4(vp0uv, 0, 1 ) );
    half4 n1 = tex2Dlod( _ImposterWorldNormalDepthTex, half4(vp1uv, 0, 1 ) );
    half4 n2 = tex2Dlod( _ImposterWorldNormalDepthTex, half4(vp2uv, 0, 1 ) );
        
    half n0s = 0.5-n0.a;    
    half n1s = 0.5-n1.a;
    half n2s = 0.5-n2.a;
    
    half2 n0p = imp.frame0.zw * n0s;
    half2 n1p = imp.frame1.zw * n1s;
    half2 n2p = imp.frame2.zw * n2s;
    
    vp0uv += n0p;
    vp1uv += n1p;
    vp2uv += n2p;   
    vp0uv = clamp(vp0uv,frame0+border,frame0+gridSize-border);
    vp1uv = clamp(vp1uv,frame1+border,frame1+gridSize-border);
    vp2uv = clamp(vp2uv,frame2+border,frame2+gridSize-border);
    
    half2 ddxy = half2( ddx(imp.uv.x), ddy(imp.uv.y) );    
    worldNormal = ImposterBlendWeights( _ImposterWorldNormalDepthTex, imp.uv, vp0uv, vp1uv, vp2uv, weights, ddxy );
    baseTex = ImposterBlendWeights( _ImposterBaseTex, imp.uv, vp0uv, vp1uv, vp2uv, weights, ddxy );
}

#endif
