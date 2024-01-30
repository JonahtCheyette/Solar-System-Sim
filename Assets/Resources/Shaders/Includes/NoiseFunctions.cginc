#include "Noise.cginc"
//The settings variables: x component is scale, y component is persistance, z is lacunarity.

float layeredNoise(float3 p, StructuredBuffer<float3> offsets, float3 settings) {
    float value = 0;

    uint octaves, stride;
    offsets.GetDimensions(octaves, stride);

    float amplitude = 1.0f;
    float frequency = 1.0f;
    for (uint i = 0; i < octaves; i++) {
        float3 samplePos = (offsets[i] + p) / settings.x * frequency;
        value += snoise(samplePos) * amplitude;

        amplitude *= settings.y;
        frequency *= settings.z;
    }

    return value;
}

float3 layeredNoiseGrad(float3 p, StructuredBuffer<float3> offsets, float3 settings) {
    float3 value = 0;

    uint octaves, stride;
    offsets.GetDimensions(octaves, stride);

    float amplitude = 1.0f;
    float frequency = 1.0f;
    for (uint i = 0; i < octaves; i++) {
        float3 samplePos = (offsets[i] + p) / settings.x * frequency;
        value += snoise_grad(samplePos).xyz * amplitude;

        amplitude *= settings.y;
        frequency *= settings.z;
    }

    return value;
}

float ridgeNoise(float3 p, StructuredBuffer<float3> offsets, float gain, float sharpness, float3 settings) {
    float value = 0;

    uint octaves, stride;
    offsets.GetDimensions(octaves, stride);

    float amplitude = 1.0f;
    float frequency = 1.0f;
    float ridgeWeight = 1.0f;
    for (uint i = 0; i < octaves; i++) {
        float noiseVal = 1 - abs(snoise((offsets[i] + p) / settings.x * frequency));
        //pow(f, p) throws a warning if f isn't wrapped in an absolute value function, hence why it's here, despite the fact that noiseVal can never be negative
        noiseVal = pow(abs(noiseVal), sharpness);
        noiseVal *= ridgeWeight;
        ridgeWeight = saturate(noiseVal * gain);
        //the ridgeWeight decreases more if the noiseVal is low. 
        //So by scaling the noiseVal by ridgeWeight valleys between mountains aren't too bumpy
        //as bumps are caused by low noiseValues followed by larger ones

        value += noiseVal * amplitude;

        amplitude *= settings.y;
        frequency *= settings.z;
    }

    return value;
}

// Sample the noise several times at small offsets from the centre and average the result
// This reduces some of the harsh jaggedness that can occur
// pos should be normalized already
float smoothedRidgeNoise(float3 pos, StructuredBuffer<float3> offsets, float gain, float sharpness, float3 settings) {
    float3 axisA = pos.x == 0 && pos.z == 0 ? cross(pos, float3(1, 0, 0)): cross(pos, float3(0, 1, 0));
    axisA = normalize(axisA);
    float3 axisB = cross(pos, axisA);

    float offsetDst = 0.0025f;
    float sample0 = ridgeNoise(pos, offsets, gain, sharpness, settings);
    float sample1 = ridgeNoise(pos - axisA * offsetDst, offsets, gain, sharpness, settings);
    float sample2 = ridgeNoise(pos + axisA * offsetDst, offsets, gain, sharpness, settings);
    float sample3 = ridgeNoise(pos - axisB * offsetDst, offsets, gain, sharpness, settings);
    float sample4 = ridgeNoise(pos + axisB * offsetDst, offsets, gain, sharpness, settings);
    return (sample0 + sample1 + sample2 + sample3 + sample4) / 5;
}