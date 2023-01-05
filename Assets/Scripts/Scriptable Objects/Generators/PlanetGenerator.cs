using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Celestial Body Generators/Planet Generator")]
public class PlanetGenerator : CelestialBodyGenerator {
    public int seed;

    public Color oceanShallowColor;
    public Color oceanDeepColor;
    [Min(0)]
    public float oceanBlendMultiplier;
    [Min(0)]
    public float oceanAlphaMultiplier;

    [Min(0)]
    public float oceanFloorDepth;
    [Range(0,1)]
    public float oceanDamper;
    [Range(0,1)]
    public float oceanSmoothing;
    [Min(0)]
    public float oceanDepthMultiplier;

    public float genShapeMultiplier;
    [Range(1, 8)]
    public int genShapeOctaves = 8;
    [Min(float.Epsilon)]
    public float genShapeScale = 350f;
    [Range(0, 1)]
    public float genShapePersistance = 0.5f;
    [Min(1)]
    public float genShapeLacunarity = 2f;
    private ComputeBuffer genShapeOctaveOffsets;

    public float ridgeMultiplier;
    [Range(1, 8)]
    public int ridgeOctaves = 8;
    [Min(float.Epsilon)]
    public float ridgeScale = 2f;
    [Range(0, 1)]
    public float ridgePersistance = 0.5f;
    [Min(1)]
    public float ridgeLacunarity = 2f;
    [Min(float.Epsilon)]
    public float ridgeGain = 1f;
    public float ridgeSharpness = 1f;
    private ComputeBuffer ridgeOctaveOffsets;

    public Vector3 mountainMaskOffset;
    public float mountainMaskShift;
    [Min(float.Epsilon)]
    public float mountainMaskScale = 2f;

    private ComputeBuffer mountainMaskOffsetBuffer;

    public override bool HasOceanEffect() {
        return true;
    }

    public override OceanDetails OceanDetails() {
        return new OceanDetails(radius, oceanBlendMultiplier, oceanAlphaMultiplier, ColToVec(oceanShallowColor), ColToVec(oceanDeepColor));
    }

    protected override void OnValidate() {
        if (ridgeSharpness == 0) { ridgeSharpness = 0.001f; }
        base.OnValidate();
    }

    public override void Setup() {
        GetGenerator("PlanetRadiusGenerator");
        base.Setup();
        SetupNoiseBuffers();
        generator.SetBuffer(kernel, "genShapeOctaveOffsets", genShapeOctaveOffsets);
        generator.SetFloats("genShapeNoiseSettings", new float[3] { genShapeScale, genShapePersistance, genShapeLacunarity });
        generator.SetFloat("genShapeMultiplier", genShapeMultiplier);

        generator.SetBuffer(kernel, "ridgeOctaveOffsets", ridgeOctaveOffsets);
        generator.SetFloats("ridgeNoiseSettings", new float[3] { ridgeScale, ridgePersistance, ridgeLacunarity });
        generator.SetFloat("ridgeMultiplier", ridgeMultiplier);
        generator.SetFloat("ridgeGain", ridgeGain);
        generator.SetFloat("ridgeSharpness", ridgeSharpness);

        generator.SetFloat("oceanFloorDepth", oceanFloorDepth);
        generator.SetFloat("oceanDamper", oceanDamper);
        generator.SetFloat("oceanSmoothing", oceanSmoothing);
        generator.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);

        generator.SetBuffer(kernel, "mountainMaskOffset", mountainMaskOffsetBuffer);
        generator.SetFloats("mountainMaskSettings", new float[3] { mountainMaskScale, 1, 1 });
        generator.SetFloat("mountainMaskShift", mountainMaskShift);
    }

    private void SetupNoiseBuffers() {
        Random.InitState(seed);

        if (genShapeOctaveOffsets == null || !genShapeOctaveOffsets.IsValid() || genShapeOctaveOffsets.count != genShapeOctaves) {
            if (genShapeOctaveOffsets != null) {
                genShapeOctaveOffsets.Dispose();
            }
            genShapeOctaveOffsets = new ComputeBuffer(genShapeOctaves, sizeof(float) * 3);
        }
        Vector3[] offsets = new Vector3[genShapeOctaves];
        for (int i = 0; i < genShapeOctaves; i++) {
            offsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10000f;
        }
        genShapeOctaveOffsets.SetData(offsets);

        if (ridgeOctaveOffsets == null || !ridgeOctaveOffsets.IsValid() || ridgeOctaveOffsets.count != ridgeOctaves) {
            if (ridgeOctaveOffsets != null) {
                ridgeOctaveOffsets.Dispose();
            }
            ridgeOctaveOffsets = new ComputeBuffer(ridgeOctaves, sizeof(float) * 3);
        }
        offsets = new Vector3[ridgeOctaves];
        for (int i = 0; i < ridgeOctaves; i++) {
            offsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10000f;
        }
        ridgeOctaveOffsets.SetData(offsets);

        if (mountainMaskOffsetBuffer == null || !mountainMaskOffsetBuffer.IsValid() || mountainMaskOffsetBuffer.count != 1) {
            if (mountainMaskOffsetBuffer != null) {
                mountainMaskOffsetBuffer.Dispose();
            }
            mountainMaskOffsetBuffer = new ComputeBuffer(1, sizeof(float) * 3);
        }
        offsets = new Vector3[1];
        offsets[0] = mountainMaskOffset;
        mountainMaskOffsetBuffer.SetData(offsets);
    }

    public override void Finish() {
        base.Finish();
        DestroyNoiseBuffers();
    }

    private void DestroyNoiseBuffers() {
        if (genShapeOctaveOffsets != null) {
            genShapeOctaveOffsets.Dispose();
        }
        if (ridgeOctaveOffsets != null) {
            ridgeOctaveOffsets.Dispose();
        }
        if (mountainMaskOffsetBuffer != null) {
            mountainMaskOffsetBuffer.Dispose();
        }
    }

    public override float[,] ProvideDataToShader() {
        float[,] returnVal = new float[1, 1];
        returnVal[0, 0] = radius;
        return returnVal;
    }
}