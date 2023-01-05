#ifndef MOONBASICS
#define MOONBASICS
struct Input {
	float3 pos;
	float3 objectNormal;
	float4 tangent;
	float noiseVal;
	float fresnel;
};

static const float PI = 3.14159265f;

struct EjectaCrater {
	float radius;
	float3 center;
	float3 localUp;
	float3 localRight;
};

#ifdef SHADER_API_D3D11
StructuredBuffer<EjectaCrater> ejectaCraters;
#endif
uint numEjectaCraters;
float ejectaThreshold;

float _UseEjecta;

sampler2D _EjectaTexture;
fixed4 _EjectaTintOne;
fixed4 _EjectaTintTwo;
float _EjectaScale;
float _EjectaOffsetX;
float _EjectaOffsetY;
float _EjectaStrength;

float4 _FresnelCol;
float _FresnelStrengthNear;
float _FresnelStrengthFar;
float _FresnelPow;
float bodyRadius;

fixed4 getEjectaMask(float3 normalPos) {
	fixed4 ejectaMask = fixed4(0, 0, 0, 1);
	#ifdef SHADER_API_D3D11
	for (uint i = 0; i < numEjectaCraters; i++) {
		float3 offset = normalPos - ejectaCraters[i].center;
		float maxRadius = ejectaCraters[i].radius * ejectaThreshold;
		float arcDistance = asin(length(offset) / 2) * 2;
		offset -= dot(ejectaCraters[i].center, offset) * ejectaCraters[i].center;
		offset = normalize(offset) * arcDistance / maxRadius;
		float2 UV = float2(dot(ejectaCraters[i].localUp, offset), dot(ejectaCraters[i].localRight, offset));
		ejectaMask += tex2D(_EjectaTexture, UV * _EjectaScale + float2(0.5 + _EjectaOffsetX, 0.5 + _EjectaOffsetY)) * _EjectaStrength * (arcDistance < maxRadius);
	}
	ejectaMask.w = 1;
	#endif
	return ejectaMask;
}

float steepnessBlend(float steepness, float baseThreshold, float blendSmoothness) {
	//a bit of awful math to blend between steepnesses when all we have are cosines of angles as opposed to angles themselves
	//this is a roughly equivalent blend to just using the angles
	//and it doesn't use arccos, so it's very fast
	float steepnessThreshold = cos(baseThreshold * PI / 180.0);
	float steepnessDiff = steepness - steepnessThreshold;
	float testAngle = min(max(baseThreshold + sign(steepnessDiff) * blendSmoothness, 0), 90.0) * PI / 180.0;
	return saturate(steepnessDiff / abs(cos(testAngle) - steepnessThreshold) + 0.5);
}

float regularBlend(float val, float threshold, float blendSmoothness) {
	return saturate((val - threshold) / blendSmoothness + 0.5);
}

void vert(inout appdata_full v, out Input o) {
	o.pos = v.vertex;
	o.objectNormal = v.normal;
	o.tangent = v.tangent;
	o.noiseVal = v.texcoord.x;
	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	float3 bodyWorldCentre = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
	float camRadiiFromSurface = (length(bodyWorldCentre - _WorldSpaceCameraPos.xyz) - bodyRadius) / bodyRadius;
	float3 normWorld = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)));
	float viewAlignment = dot(normalize(worldPos - _WorldSpaceCameraPos.xyz), normWorld);
	float fresStrength = lerp(_FresnelStrengthNear, _FresnelStrengthFar, smoothstep(0, 1, camRadiiFromSurface));
	o.fresnel = saturate(fresStrength * pow(1 + viewAlignment, _FresnelPow));
}
#endif