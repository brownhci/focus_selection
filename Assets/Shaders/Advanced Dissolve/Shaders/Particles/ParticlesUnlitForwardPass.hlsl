#ifndef LIGHTWEIGHT_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED
#define LIGHTWEIGHT_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

struct AttributesParticle
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    half4 color : COLOR;
#if defined(_FLIPBOOKBLENDING_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    float4 texcoords : TEXCOORD0;
    float2 texcoordBlend : TEXCOORD1;
#else
    float2 texcoords : TEXCOORD0;
    float2 texcoordBlend : TEXCOORD1;
#endif
    float4 tangent : TANGENT;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsParticle
{
    half4 color                     : COLOR;
    float2 texcoord                 : TEXCOORD0;
    
    float4 positionWS               : TEXCOORD1;

#ifdef _NORMALMAP
    half4 normalWS                  : TEXCOORD2;    // xyz: normal, w: viewDir.x
    half4 tangentWS                 : TEXCOORD3;    // xyz: tangent, w: viewDir.y
    half4 bitangentWS               : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
#else
    half3 normalWS                  : TEXCOORD2;
    half3 viewDirWS                 : TEXCOORD3;
#endif

#if defined(_FLIPBOOKBLENDING_ON)
    float3 texcoord2AndBlend        : TEXCOORD5;
#endif
#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    float4 projectedPosition        : TEXCOORD6;
#endif

#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    float4 shadowCoord              : TEXCOORD7;
#endif

    float3 vertexSH                 : TEXCOORD8; // SH
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO


    ADVANCED_DISSOLVE_DATA(9)
};

void InitializeInputData(VaryingsParticle input, half3 normalTS, out InputData output)
{
    output = (InputData)0;

    output.positionWS = input.positionWS.xyz;

#ifdef _NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    output.normalWS = TransformTangentToWorld(normalTS,
        half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
#else
    half3 viewDirWS = input.viewDirWS;
    output.normalWS = input.normalWS;
#endif

    output.normalWS = NormalizeNormalPerPixel(output.normalWS);
    
#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    output.viewDirectionWS = viewDirWS;
    
#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    output.shadowCoord = input.shadowCoord;
#else
    output.shadowCoord = float4(0, 0, 0, 0);
#endif

    output.fogCoord = (half)input.positionWS.w;
    output.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    output.bakedGI = SampleSHPixel(input.vertexSH, output.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle vertParticleUnlit(AttributesParticle input)
{
    VaryingsParticle output = (VaryingsParticle)0;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangent);

    // position ws is used to compute eye depth in vertFading
    output.positionWS.xyz = vertexInput.positionWS;
    output.positionWS.w = ComputeFogFactor(vertexInput.positionCS.z);
    output.clipPos = vertexInput.positionCS;
    output.color = input.color;
    
    half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
#if !SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif
    
#ifdef _NORMALMAP
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#endif

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.texcoord = input.texcoords.xy;
#ifdef _FLIPBOOKBLENDING_ON
    output.texcoord2AndBlend.xy = input.texcoords.zw;
    output.texcoord2AndBlend.z = input.texcoordBlend.x;
#endif
    
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    output.projectedPosition = ComputeScreenPos(vertexInput.positionCS);
#endif


    ADVANCED_DISSOLVE_INIT_DATA(vertexInput.positionCS, input.texcoords.xy, input.texcoordBlend.xy, output)

    return output;
}

half4 fragParticleUnlit(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);


float4 dissolaveAlpha = AdvancedDissolveGetAlpha(input.texcoord.xy, input.positionWS.xyz, input.normalWS, input.dissolveUV);
DoDissolveClip(dissolaveAlpha); 

float3 dissolveAlbedo = 0;
float3 dissolveEmission = 0;
float dissolveBlend = DoDissolveAlbedoEmission(dissolaveAlpha, dissolveAlbedo, dissolveEmission, input.texcoord.xy, 1);



    float2 uv = input.texcoord;
    float3 blendUv = float3(0, 0, 0);
#if defined(_FLIPBOOKBLENDING_ON)
    blendUv = input.texcoord2AndBlend;
#endif

    float4 projectedPosition = float4(0,0,0,0);
#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    projectedPosition = input.projectedPosition;
#endif

    half4 albedo = SampleAlbedo(uv, blendUv, _BaseColor, input.color, projectedPosition, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));

    albedo.rgb = lerp(albedo.rgb, dissolveAlbedo, dissolveBlend);

    
    half3 normalTS = SampleNormalTS(uv, blendUv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    
#if defined (_DISTORTION_ON)   
    albedo.rgb = Distortion(albedo, normalTS, _DistortionStrengthScaled, _DistortionBlend, projectedPosition);
#endif
    
    half3 diffuse = AlphaModulate(albedo.rgb, albedo.a);
    half alpha = albedo.a;
    
#if defined(_EMISSION)
    half3 emission = BlendTexture(TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), uv, blendUv).rgb * _EmissionColor.rgb;
#else
    half3 emission = half3(0, 0, 0);
#endif


    emission = lerp(emission, dissolveEmission, dissolveBlend);

    half3 result = diffuse + emission;
    half fogFactor = input.positionWS.w;
    result = MixFogColor(result, half3(0, 0, 0), fogFactor);
    return half4(result, alpha);
}

#endif // LIGHTWEIGHT_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED
