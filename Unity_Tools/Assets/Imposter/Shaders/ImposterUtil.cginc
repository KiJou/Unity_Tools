
half3 NormalizePerPixelNormal (half3 n)
{
    #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        return n;
    #else
        return normalize(n);
    #endif
}

half3 PerPixelWorldNormal(float4 i_tex, half4 tangentToWorld[3])
{
#ifdef _NORMALMAP
    half3 tangent = tangentToWorld[0].xyz;
    half3 binormal = tangentToWorld[1].xyz;
    half3 normal = tangentToWorld[2].xyz;

    #if UNITY_TANGENT_ORTHONORMALIZE
        normal = NormalizePerPixelNormal(normal);
        tangent = normalize (tangent - normal * dot(tangent, normal));
        half3 newB = cross(normal, tangent);
        binormal = newB * sign (dot (newB, binormal));
    #endif

    half3 normalTangent = NormalInTangentSpace(i_tex);
    // @TODO: see if we can squeeze this normalize on SM2.0 as well
    half3 normalWorld = NormalizePerPixelNormal(tangent * normalTangent.x + binormal * normalTangent.y + normal * normalTangent.z);
#else
    half3 normalWorld = normalize(tangentToWorld[2].xyz);
#endif
    return normalWorld;
}

half4 BakeNormalsDepth( sampler2D bumpMap, half2 uv, half depth, half4 tangentToWorld[3] )
{
    half4 tex = tex2D( bumpMap, uv );   
    half3 worldNormal = PerPixelWorldNormal(tex, tangentToWorld);    
    return half4( worldNormal.xyz*0.5+0.5, 1-depth );
}

half4 ImposterBlendWeights( sampler2D tex, half2 uv, half2 frame0, half2 frame1, half2 frame2, half4 weights, half2 ddxy )
{    
    half4 samp0 = tex2Dgrad( tex, frame0, ddxy.x, ddxy.y );
    half4 samp1 = tex2Dgrad( tex, frame1, ddxy.x, ddxy.y );
    half4 samp2 = tex2Dgrad( tex, frame2, ddxy.x, ddxy.y );
    half4 result = samp0*weights.x + samp1*weights.y + samp2*weights.z;
    
    return result;
}

float Isolate( float c, float w, float x )
{
    return smoothstep(c-w,c,x)-smoothstep(c,c+w,x);
}

float SphereMask( float2 p1, float2 p2, float r, float h )
{
    float d = distance(p1,p2);
    return 1-smoothstep(d,r,h);
}

half3 OctaHemiEnc( half2 coord )
{
 	coord = half2( coord.x + coord.y, coord.x - coord.y ) * 0.5;
 	half3 vec = half3( coord.x, 1.0 - dot( half2(1.0,1.0), abs(coord.xy) ), coord.y  );
 	return vec;
}

half3 OctaSphereEnc( half2 coord )
{
    half3 vec = half3( coord.x, 1-dot(1,abs(coord)), coord.y );
    if ( vec.y < 0 )
    {
        half2 flip = vec.xz >= 0 ? half2(1,1) : half2(-1,-1);
        vec.xz = (1-abs(vec.zx)) * flip;
    }
    return vec;
}

half3 GridToVector( half2 coord, half fullSphere )
{
    half3 vec;
    if ( fullSphere )
    {
        vec = OctaSphereEnc(coord);
    }
    else
    {
        vec = OctaHemiEnc(coord);
    }
    return vec;
}

half2 VecToHemiOct( half3 vec )
{
	vec.xz /= dot( 1.0, abs(vec) );
	return half2( vec.x + vec.z, vec.x - vec.z );
}

half2 VecToSphereOct( half3 vec )
{
    vec.xz /= dot( 1,  abs(vec) );
    if ( vec.y <= 0 )
    {
        half2 flip = vec.xz >= 0 ? half2(1,1) : half2(-1,-1);
        vec.xz = (1-abs(vec.zx)) * flip;
    }
    return vec.xz;
}
	
half2 VectorToGrid( half3 vec, half fullSphere  )
{
    half2 coord;

    if (fullSphere)
    {
        coord = VecToSphereOct( vec );
    }
    else
    {
        vec.y = max(0.001,vec.y);
        vec = normalize(vec);
        coord = VecToHemiOct( vec );
    }
    return coord;
}

half4 TriangleInterpolate( half2 uv )
{
    uv = frac(uv);

    half2 omuv = half2(1.0,1.0) - uv.xy;
    
    half4 res = half4(0,0,0,0);
    //frame 0
    res.x = min(omuv.x,omuv.y); 
    //frame 1
    res.y = abs( dot( uv, half2(1.0,-1.0) ) );
    //frame 2
    res.z = min(uv.x,uv.y); 
    //mask
    res.w = saturate(ceil(uv.x-uv.y));    
    return res;
}

half3 FrameXYToRay( half2 frame, half2 frameCountMinusOne, half fullSphere)
{
    half2 f = frame.xy / frameCountMinusOne;
    f = (f-0.5)*2.0; 
    half3 vec = GridToVector(f, fullSphere);
    vec = normalize(vec);
    return vec;
}

half3 ITBasis( half3 vec, half3 basedX, half3 basedY, half3 basedZ )
{
    return half3( dot(basedX,vec), dot(basedY,vec), dot(basedZ,vec) );
}
 
half3 FrameTransform( half3 projRay, half3 frameRay, out half3 worldX, out half3 worldZ  )
{
    //TODO something might be wrong here
    worldX = normalize( half3(-frameRay.z, 0, frameRay.x) );
    worldZ = normalize( cross(worldX, frameRay ) );     
    projRay *= -1.0;     
    half3 local = normalize( ITBasis( projRay, worldX, frameRay, worldZ ) );
    return local;
}

half3 SpriteProjection( half3 pivotToCameraRayLocal, half frames, half2 size, half2 coord )
{
    half3 gridVec = pivotToCameraRayLocal;   
    half3 y = normalize(gridVec);    
    half3 x = normalize( cross( y, half3(0.0, 1.0, 0.0) ) );
    half3 z = normalize( cross( x, y ) );
    half2 uv = ((coord*frames)-0.5) * 2.0;

    half3 newX = x * uv.x;
    half3 newZ = z * uv.y;
    
    half2 halfSize = size*0.5;
    
    newX *= halfSize.x;
    newZ *= halfSize.y;
    
    half3 res = newX + newZ;  
     
    return res;
}

