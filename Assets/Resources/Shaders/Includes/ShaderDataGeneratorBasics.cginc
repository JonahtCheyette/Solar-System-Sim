StructuredBuffer<float3> positions;
RWStructuredBuffer<float3> normals;
RWStructuredBuffer<float> output;

float3 generateTangent(float3 n) {
	return normalize(cross(n, n == float3(0, -1, 0) || n == float3(0, 1, 0) ? float3(0, 0, -1) : float3(0, -1, 0)));
}