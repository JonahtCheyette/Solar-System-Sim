Shader "Custom/Moon1" {
	Properties{
		[Header(Colors)]
		_LowColor("Color 1", Color) = (0.25, 0.5, 0.5, 1)
		_LowMetallic("Metallic 1", Range(0,1)) = 0.5
		_LowSmoothness("Smoothness 1", Range(0,1)) = 0.5

		_HighColor("Color 2", Color) = (0, 0, 0, 1)
		_HighMetallic("Metallic 2", Range(0,1)) = 0.5
		_HighSmoothness("Smoothness 2", Range(0,1)) = 0.5

		_ColorThreshold("Color Threshold", Range(0, 1)) = 0.5
		_ColorBlendSmoothness("Color Blend Smoothness", Range(0.00001, 1)) = 0.5

		[Header(Fresnel)]
		_FresnelCol("Fresnel Colour", Color) = (1,1,1,1)
		_FresnelStrengthNear("Fresnel Strength Min", Float) = 2
		_FresnelStrengthFar("Fresnel Strength Max", Float) = 5
		_FresnelPow("Fresnel Sharpness", Float) = 12

		[Header(Ejecta)]
		[Toggle()]
		_UseEjecta("Use Ejecta", Float) = 1
		[NoScaleOffset] _EjectaTexture("Ejecta Texture", 2D) = "black" {}

		_EjectaTintOne("Ejecta Tint 1", Color) = (0.25, 0.5, 0.5, 1)
		_EjectaTintTwo("Ejecta Tint 2", Color) = (0.5, 0.25, 0.5, 1)
		_EjectaStrength("Ejecta Strength", Range(0, 2)) = 1

		_EjectaScale("Ejecta Scale", Float) = 1
		_EjectaOffsetX("Ejecta Offset X", Range(-1, 1)) = 0
		_EjectaOffsetY("Ejecta Offset Y", Range(-1, 1)) = 0

		[Header(Normal Maps)]
		[Header(Normal Maps Color 1)]
		[NoScaleOffset] _NormalMapOneFlat("Flat", 2D) = "blue" {}
		_NormalOneFlatScale("Flat Scale", Float) = 1
		_NormalOneFlatStrength("Flat Strength", Range(0, 1)) = 0.5

		[NoScaleOffset] _NormalMapOneSteep("Steep", 2D) = "blue" {}
		_NormalOneSteepScale("Steep Scale", Float) = 1
		_NormalOneSteepStrength("Steep Strength", Range(0, 1)) = 0.5

		_NormalOneThreshold("Threshold", Range(0, 90)) = 45
		_NormalOneBlendSmoothness("Blend Smoothness", Range(0.00001, 90)) = 5

		[Header(Normal Maps Color 2)]
		[NoScaleOffset] _NormalMapTwoFlat("Flat", 2D) = "blue" {}
		_NormalTwoFlatScale("Flat Scale", Float) = 1
		_NormalTwoFlatStrength("Flat Strength", Range(0, 1)) = 0.5

		[NoScaleOffset] _NormalMapTwoSteep("Steep", 2D) = "blue" {}
		_NormalTwoSteepScale("Steep Scale", Float) = 1
		_NormalTwoSteepStrength("Steep Strength", Range(0, 1)) = 0.5

		_NormalTwoThreshold("Threshold", Range(0, 90)) = 45
		_NormalTwoBlendSmoothness("Blend Smoothness", Range(0.00001, 90)) = 5
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

        #pragma target 4.0
		#include "..\Includes\Triplanar.cginc"
		#include "..\Includes\MoonBasics.cginc"
		
		fixed4 _LowColor;
		half _LowMetallic;
		half _LowSmoothness;

		fixed4 _HighColor;
		half _HighMetallic;
		half _HighSmoothness;

		float _ColorThreshold;
		float _ColorBlendSmoothness;

        sampler2D _NormalMapOneFlat;
		sampler2D _NormalMapOneSteep;

		float _NormalOneThreshold;
		float _NormalOneBlendSmoothness;
		float _NormalOneFlatScale;
		float _NormalOneFlatStrength;
		float _NormalOneSteepScale;
		float _NormalOneSteepStrength;

		sampler2D _NormalMapTwoFlat;
		sampler2D _NormalMapTwoSteep;

		float _NormalTwoThreshold;
		float _NormalTwoBlendSmoothness;
		float _NormalTwoFlatScale;
		float _NormalTwoFlatStrength;
		float _NormalTwoSteepScale;
		float _NormalTwoSteepStrength;

        void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 normalPos = normalize(IN.pos);
			float colorLerpParam = regularBlend(IN.noiseVal, _ColorThreshold, _ColorBlendSmoothness);
			fixed4 baseColor = lerp(_LowColor, _HighColor, colorLerpParam);

			fixed4 ejectaMask = getEjectaMask(normalPos);
			float ejectaAlpha = ejectaMask.r * _UseEjecta;
			fixed4 ejectaColor = ejectaMask * lerp(_EjectaTintOne, _EjectaTintTwo, colorLerpParam);

			fixed4 output = lerp(baseColor, ejectaColor, ejectaAlpha);
			output = lerp(output, _FresnelCol, IN.fresnel);
			output.a = 1;
			o.Albedo = output;

			o.Metallic = lerp(_LowMetallic, _HighMetallic, colorLerpParam);
			o.Smoothness = lerp(_LowSmoothness, _HighSmoothness, colorLerpParam);

			float steepness = 1 - dot(normalPos, IN.objectNormal);

			float normalBlendFactor = steepnessBlend(steepness, _NormalOneThreshold, _NormalOneBlendSmoothness);
			float3 normalOne = lerp(triplanarNormal(IN.pos, IN.objectNormal, _NormalOneFlatScale, IN.tangent, _NormalMapOneFlat), triplanarNormal(IN.pos, IN.objectNormal, _NormalOneSteepScale, IN.tangent, _NormalMapOneSteep), normalBlendFactor);
			normalOne = lerp(float3(0, 0, 1), normalOne, lerp(_NormalOneFlatStrength, _NormalOneSteepStrength, normalBlendFactor));
			
			normalBlendFactor = steepnessBlend(steepness, _NormalTwoThreshold, _NormalTwoBlendSmoothness);
			float3 normalTwo = lerp(triplanarNormal(IN.pos, IN.objectNormal, _NormalTwoFlatScale, IN.tangent, _NormalMapTwoFlat), triplanarNormal(IN.pos, IN.objectNormal, _NormalTwoSteepScale, IN.tangent, _NormalMapTwoSteep), normalBlendFactor);
			normalTwo = lerp(float3(0, 0, 1), normalTwo, lerp(_NormalTwoFlatStrength, _NormalTwoSteepStrength, normalBlendFactor));
			
			o.Normal = lerp(normalOne, normalTwo, colorLerpParam);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
