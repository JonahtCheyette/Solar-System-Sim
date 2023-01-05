Shader "Custom/Moon2"{
	Properties{
		[Header(Colors)]
		_FlatColor("Flat Color", Color) = (0, 0, 0, 1)
		_FlatMetallic("Metallic", Range(0, 1)) = 0.5
		_FlatSmoothness("Smoothness", Range(0, 1)) = 0.5

		_SteepColor("Steep Color", Color) = (0.25, 0.5, 0.5, 1)
		_SteepMetallic("Metallic", Range(0, 1)) = 0.5
		_SteepSmoothness("Smoothness", Range(0, 1)) = 0.5

		_ColorThreshold("Color Threshold", Range(0, 90)) = 45
		_ColorBlendSmoothness("Color Blend Smoothness", Range(0.00001, 90.0)) = 5.0

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
		_EjectaStrength("Ejecta Strength", Range(0, 2)) = 1.0

		_EjectaScale("Ejecta Scale", Float) = 1
		_EjectaOffsetX("Ejecta Offset X", Range(-1, 1)) = 0
		_EjectaOffsetY("Ejecta Offset Y", Range(-1, 1)) = 0

		[Header(Normal Maps)]
		[Header(Normal Maps Color 1)]
		[NoScaleOffset] _NormalMapOneLow("Low", 2D) = "blue" {}
		_NormalOneLowScale("Low Scale", Float) = 1
		_NormalOneLowStrength("Low Strength", Range(0.0, 1.0)) = 0.5

		[NoScaleOffset] _NormalMapOneHigh("High", 2D) = "blue" {}
		_NormalOneHighScale("High Scale", Float) = 1
		_NormalOneHighStrength("High Strength", Range(0.0, 1.0)) = 0.5

		_NormalOneThreshold("Threshold", Range(0, 1)) = 0.5
		_NormalOneBlendSmoothness("Blend Smoothness", Range(0.00001, 1)) = 0.5

		[Header(Normal Maps Color 2)]
		[NoScaleOffset] _NormalMapTwoLow("Low", 2D) = "blue" {}
		_NormalTwoLowScale("Low Scale", Float) = 1
		_NormalTwoLowStrength("Low Strength", Range(0.0, 1.0)) = 0.5

		[NoScaleOffset] _NormalMapTwoHigh("High", 2D) = "blue" {}
		_NormalTwoHighScale("High Scale", Float) = 1
		_NormalTwoHighStrength("High Strength", Range(0.0, 1.0)) = 0.5

		_NormalTwoThreshold("Threshold", Range(0, 1)) = 0.5
		_NormalTwoBlendSmoothness("Blend Smoothness", Range(0.00001, 1)) = 0.5
	}
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		#pragma target 4.0
		#include "..\Includes\MoonBasics.cginc"
		#include "..\Includes\Triplanar.cginc"

		fixed4 _FlatColor;
		half _FlatMetallic;
		half _FlatSmoothness;

		fixed4 _SteepColor;
		half _SteepMetallic;
		half _SteepSmoothness;

		float _ColorThreshold;
		float _ColorBlendSmoothness;

		sampler2D _NormalMapOneLow;
		sampler2D _NormalMapOneHigh;

		float _NormalOneThreshold;
		float _NormalOneBlendSmoothness;
		float _NormalOneLowScale;
		float _NormalOneLowStrength;
		float _NormalOneHighScale;
		float _NormalOneHighStrength;

		sampler2D _NormalMapTwoLow;
		sampler2D _NormalMapTwoHigh;

		float _NormalTwoThreshold;
		float _NormalTwoBlendSmoothness;
		float _NormalTwoLowScale;
		float _NormalTwoLowStrength;
		float _NormalTwoHighScale;
		float _NormalTwoHighStrength;

		void surf(Input IN, inout SurfaceOutputStandard o) {
			float3 normalPos = normalize(IN.pos);
			float steepness = 1 - dot(normalPos, IN.objectNormal);
			float colorLerpParam = steepnessBlend(steepness, _ColorThreshold, _ColorBlendSmoothness);
			fixed4 baseColor = lerp(_FlatColor, _SteepColor, colorLerpParam);

			fixed4 ejectaMask = getEjectaMask(normalPos);
			float ejectaAlpha = ejectaMask.r * _UseEjecta;
			fixed4 ejectaColor = ejectaMask * lerp(_EjectaTintOne, _EjectaTintTwo, colorLerpParam);

			fixed4 output = lerp(baseColor, ejectaColor, ejectaAlpha);
			output = lerp(output, _FresnelCol, IN.fresnel);
			output.a = 1;
			o.Albedo = output;

			o.Metallic = lerp(_FlatMetallic, _SteepMetallic, colorLerpParam);
			o.Smoothness = lerp(_FlatSmoothness, _SteepSmoothness, colorLerpParam);

			float normalBlendFactor = regularBlend(IN.noiseVal, _NormalOneThreshold, _NormalOneBlendSmoothness);
			float3 normalOne = lerp(triplanarNormal(IN.pos, IN.objectNormal, _NormalOneLowScale, IN.tangent, _NormalMapOneLow), triplanarNormal(IN.pos, IN.objectNormal, _NormalOneHighScale, IN.tangent, _NormalMapOneHigh), normalBlendFactor);
			normalOne = lerp(float3(0, 0, 1), normalOne, lerp(_NormalOneLowStrength, _NormalOneHighStrength, normalBlendFactor));

			normalBlendFactor = regularBlend(IN.noiseVal, _NormalTwoThreshold, _NormalTwoBlendSmoothness);
			float3 normalTwo = lerp(triplanarNormal(IN.pos, IN.objectNormal, _NormalTwoLowScale, IN.tangent, _NormalMapTwoLow), triplanarNormal(IN.pos, IN.objectNormal, _NormalTwoHighScale, IN.tangent, _NormalMapTwoHigh), normalBlendFactor);
			normalTwo = lerp(float3(0, 0, 1), normalTwo, lerp(_NormalTwoLowStrength, _NormalTwoHighStrength, normalBlendFactor));

			o.Normal = lerp(normalOne, normalTwo, colorLerpParam);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
