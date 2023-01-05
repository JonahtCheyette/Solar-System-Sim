#ifndef TRIPLANAR
#define TRIPLANAR
float3 ObjectToTangentVector(float4 tangent, float3 normal, float3 objectSpaceVector) {
	float3 normalizedTangent = normalize(tangent.xyz);
	float3 binormal = cross(normal, normalizedTangent) * tangent.w;
	float3x3 rot = float3x3 (normalizedTangent, binormal, normal);
	return mul(rot, objectSpaceVector);
}

// Reoriented Normal Mapping
// http://blog.selfshadow.com/publications/blending-in-detail/
// Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
float3 blend_rnm(float3 baseNormal, float3 normalMap) {
	return normalize(baseNormal + normalMap);
	baseNormal.z += 1;
	normalMap.xy = -normalMap.xy;

	return baseNormal * dot(baseNormal, normalMap) / baseNormal.z - normalMap;
}

float3 triplanarNormal(float3 pos, float3 normal, float3 scale, sampler2D normalMap) {
	float3 absNormal = abs(normal);

	// Calculate triplanar blend
	float3 blendWeight = saturate(pow(normal, 4)); //make customizable? needs to be even to prevent negative number shenanigans
	// Divide blend weight by the sum of its components. This will make x + y + z = 1
	blendWeight /= dot(blendWeight, 1);

	// Calculate triplanar coordinates
	float2 uvX = pos.zy * scale;
	float2 uvY = pos.xz * scale;
	float2 uvZ = pos.xy * scale;

	// Sample tangent space normal maps
	// UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
	float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
	float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
	float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

	// Swizzle normals to match tangent space and apply reoriented normal mapping blend
	tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
	tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
	tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

	// Apply input normal sign to tangent space Z
	float3 axisSign = sign(normal);
	tangentNormalX.z *= axisSign.x;
	tangentNormalY.z *= axisSign.y;
	tangentNormalZ.z *= axisSign.z;

	// Swizzle tangent normals to match input normal and blend together
	float3 outputNormal = normalize(tangentNormalX.zyx * blendWeight.x + tangentNormalY.xzy * blendWeight.y + tangentNormalZ.xyz * blendWeight.z);
	return outputNormal;
}

float3 triplanarNormal(float3 pos, float3 normal, float3 scale, sampler2D normalMap, float2 offset) {
	float3 absNormal = abs(normal);

	// Calculate triplanar blend
	float3 blendWeight = saturate(pow(normal, 4)); //make customizable? needs to be even to prevent negative number shenanigans
	// Divide blend weight by the sum of its components. This will make x + y + z = 1
	blendWeight /= dot(blendWeight, 1);

	// Calculate triplanar coordinates
	float2 uvX = pos.zy * scale + offset;
	float2 uvY = pos.xz * scale + offset;
	float2 uvZ = pos.xy * scale + offset;

	// Sample tangent space normal maps
	// UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
	float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
	float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
	float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

	// Swizzle normals to match tangent space and apply reoriented normal mapping blend
	tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
	tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
	tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

	// Apply input normal sign to tangent space Z
	float3 axisSign = sign(normal);
	tangentNormalX.z *= axisSign.x;
	tangentNormalY.z *= axisSign.y;
	tangentNormalZ.z *= axisSign.z;

	// Swizzle tangent normals to match input normal and blend together
	float3 outputNormal = normalize(tangentNormalX.zyx * blendWeight.x + tangentNormalY.xzy * blendWeight.y + tangentNormalZ.xyz * blendWeight.z);
	return outputNormal;
}

float3 triplanarNormal(float3 pos, float3 normal, float3 scale, float4 tangent, sampler2D normalMap) {
	float3 textureNormal = triplanarNormal(pos, normal, scale, normalMap);
	return ObjectToTangentVector(tangent, normal, textureNormal);
}
#endif